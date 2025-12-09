using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Plugins.RZDAds.ApiSystem;
using Plugins.RZDAds.Core.ApiSystem;
using Plugins.RZDAds.Core.View;
using UnityEngine;

namespace Plugins.RZDAds.Core
{
    public static class AdService
    {
        private const string API_SETTINGS = "ApiSettings";

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

        // Метод нужно вызвать при старте приложения 
        public static async UniTask Initialize()
        {
            if (_initialized || _isInitializing)
                return;
            _isInitializing = true;
            
            // Logger можно не создавать тогда логгирования не будет
            _logger = new UnityLogger();

            //  apiSettings все ручки host и проч для REST
            var apiSettings = Resources.Load<ApiSettings>("ApiSettings");
            //api - REST клиент выполняет все запросы
            _api = new Api(apiSettings, _logger);

            //authenticator генерит уникальный ключ устройства и авторизует приложение
            //Все ручки в Api должны быть подписаны ключом полученные при авторизации
            _authenticator = new Authenticator(_api, new DeviceIdService(), _logger);
            
            //Скачивает и хранит Json с баннером отдает по требованию, докачивает новые
            _contentProvider = new BannerContentProvider(_api, _logger);
            //Шлет события
            _reporter = new EventReporter(_api, _logger);
            //Создаем View
            _bannerFactory = new BannerFactory();
            _view = _bannerFactory.Get();
            Object.DontDestroyOnLoad(_view.gameObject);

            //Попытка авторизоваться
            var isOk = await _authenticator.AuthorizeDevice();
            //Качаем контент
            if (isOk)
                _contentProvider.Init();

            _isInitializing = false;
            _initialized = true;
        }


        public static async UniTask Show()
        {
            _logger?.Log("[Ads] Try show ad");
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

                stopWatch.Start();
                var isClick = await _view.Show(content);
                stopWatch.Stop();

                await Report(content.Id, isClick, (float)stopWatch.Elapsed.TotalSeconds);
                if (isClick)
                    OpenUrl(content.Url);
            }
            finally
            {
                _isShowing = false;
            }
        }
        // Открываем Url рекламы 
        private static void OpenUrl(string url)
        {
            _logger?.Log($"[Ads] Try open url {url}");
            Application.OpenURL(url);
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

            var ok = await _authenticator.AuthorizeDevice();
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


        /*public static async UniTask<bool> IsReady()
        {
            if (!_initialized || !_contentProvider.IsReady)
                return false;

            return await CheckCanShow();
        }*/
        // Если надо завершить работу сервиса
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