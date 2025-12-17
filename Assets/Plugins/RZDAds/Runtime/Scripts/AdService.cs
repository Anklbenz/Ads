//@Anklbenz8

using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Plugins.RZDAds.ApiSystem;
using Plugins.RZDAds.Runtime.Scripts.ApiSystem;
using Plugins.RZDAds.Runtime.Scripts.View;
using UnityEngine;

namespace Plugins.RZDAds.Runtime.Scripts
{
    public static class AdService
    {
        private const string API_SETTINGS_RESOURES_PATH = "ApiSettings";

        private static Api _api;
        private static BannerFactory _bannerFactory;
        private static Authenticator _authenticator;
        private static BannerContentProvider _contentProvider;
        private static EventReporter _reporter;

        private static AdBannerView _view;
        private static bool _initialized;
        private static bool _isInitializing;
        private static bool _isShowing;
        private static ILogger _logger;

        // Метод нужно вызвать при старте клиентского приложения 
        // appToken - уникальный ключ игры, logRequired нужен ли лог
        public static async UniTask Initialize(string appToken, bool logRequired = true)
        {
            if (_initialized || _isInitializing)
                return;
            _isInitializing = true;

            // Logger можно не создавать тогда логирования не будет
            _logger = logRequired
                ? new UnityLogger()
                : null;

            //apiSettings все ручки, host и проч конфиг для для REST
            var apiSettings = Resources.Load<ApiSettings>(API_SETTINGS_RESOURES_PATH);
            //api - REST клиент выполняет все запросы
            _api = new Api(apiSettings, _logger);
            //Ключ приложения нужен в Header каждого запроса 
            _api.ApplyAppToken(appToken);

            //authenticator генерит уникальный ключ устройства и авторизует приложение
            //Все запросы в Api должны содержать в Body ключ полученный при авторизации, иначе сервак не примет
            _authenticator = new Authenticator(_api, new DeviceIdService(), _logger);

            //Скачивает и хранит Json с баннером, докачивает Texture, отдает по требованию, докачивает новые (Buffer)
            _contentProvider = new BannerContentProvider(_api, 2, _logger);
            //Шлет на сервак результаты просмотра
            _reporter = new EventReporter(_api, _logger);
            //Создает View
            _bannerFactory = new BannerFactory();
            _view = _bannerFactory.Get();
            Object.DontDestroyOnLoad(_view.gameObject);

            //Попытка авторизоваться и подписать Api
            var isOk = await _authenticator.Authorize();
            //Качаем первую партию контента 
            if (isOk)
                _contentProvider.Init();

            _isInitializing = false;
            _initialized = true;
        }

        public static async UniTask Show()
        {
            _logger?.Log("[Ads] Attempt show ad");
            if (_isShowing)
                return;

            _isShowing = true;

            var stopWatch = new Stopwatch();
            try
            {
                var isAuthorized = await EnsureAuthorized();

                if (!isAuthorized)
                {
                    _logger?.Log($"[Ads] Authorized isOk: ({false})");
                    return;
                }

                var canShow = await CheckCanShow();
                if (!canShow)
                {
                    _logger?.Log($"[Ads] Server allow show: {false}");
                    return;
                }

                var content = _contentProvider.TakeBanner();

                if (content == null)
                    return;
                //Время нужно для сбора статистики
                stopWatch.Start();
                var isClick = await _view.Show(content);
                stopWatch.Stop();

                await Report(content.Id, isClick, (float)stopWatch.Elapsed.TotalSeconds);
            }
            finally
            {
                _isShowing = false;
            }
        }

        //Отправка отчета, класс Reporter (нажали или закрыли + длительность просмотра)
        private static async UniTask Report(uint id, bool isClick, float duration)
        {
            await _reporter.ReportShown(id, duration);

            if (isClick)
                await _reporter.ReportClicked(id);
        }

        // Если авторизован возвращает true, если нет пробует и возвращает результат
        private static async UniTask<bool> EnsureAuthorized()
        {
            if (_authenticator.IsAuthorized)
                return true;

            var ok = await _authenticator.Authorize();
            return ok;
        }

        // Сервер разрешает показ не чаще какого-то времени, сервер знает это время 
        // Проверка можно ли показать, сервер отвечает да/нет 
        private static async UniTask<bool> CheckCanShow()
        {
            var check = await _api.CheckCanShow();
            //check.data.data <- bool можно или нет 
            return check.isDone && check.data.data;
        }

        // Если надо завершить работу сервиса, очистить ресурсы
        public static void Dispose()
        {
            if (_view != null)
                UnityEngine.Object.Destroy(_view.gameObject);

            _view = null;
            _api = null;
            _bannerFactory = null;
            _authenticator = null;
            _contentProvider = null;
            _reporter = null;
            _isShowing = false;
            _initialized = false;
            _isInitializing = false;

            _logger?.Log("AdService disposed");
        }
    }
}