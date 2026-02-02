//@Anklbenz8

using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Plugins.RZDAds.ApiSystem;
using Plugins.RZDAds.Runtime.Scripts.ApiSystem;
using Plugins.RZDAds.Runtime.Scripts.Banner;
using Plugins.RZDAds.Runtime.Scripts.View;
using UnityEngine;

namespace Plugins.RZDAds.Runtime.Scripts
{
    public enum AdServiceState
    {
        None, // никогда не инициализировались
        Initializing, // идет Initialize
        Initialized, // можно показывать рекламу
        Failed, // критическая ошибка
        Disposed
    }

    public static class AdService
    {
        private const string API_SETTINGS_RESOURCES_PATH = "ApiSettings";

        private static Api _api;
        private static ViewFactory _viewFactory;
        private static Authenticator _authenticator;
        private static BannerContentProvider _contentProvider;
        private static EventReporter _reporter;

        private static AdBannerView _view;
        private static bool _isShowing;
        private static ILogger _logger;

        private static AdServiceState _state = AdServiceState.None;
        public static AdServiceState State => _state;
        private static bool _canShowState;

        // Метод нужно вызвать при старте клиентского приложения 
        // appToken - уникальный ключ игры, logRequired нужен ли лог
        public static async UniTask Initialize(string appToken, bool logRequired = false)
        {
            if (_state == AdServiceState.Initialized ||
                _state == AdServiceState.Initializing)
                return;

            _state = AdServiceState.Initializing;

            try
            {
                // Logger можно не создавать тогда логирования не будет
                _logger = logRequired
                    ? new UnityLogger()
                    : null;

                if (string.IsNullOrWhiteSpace(appToken))
                    throw new ArgumentException("appToken is empty");

                //apiSettings все ручки, host и проч конфиг для для REST
                var apiSettings = Resources.Load<ApiSettings>(API_SETTINGS_RESOURCES_PATH);
                if (apiSettings == null)
                    throw new Exception("ApiSettings not found");

                //api - REST клиент выполняет все запросы
                _api = new Api(apiSettings, _logger);
                //Ключ приложения нужен в Header каждого запроса 
                _api.ApplyAppToken(appToken.Trim());

                //authenticator генерит уникальный ключ устройства и авторизует приложение
                //Все запросы в Api должны содержать в Body ключ полученный при авторизации, иначе сервак не примет
                _authenticator = new Authenticator(_api, new DeviceIdService(), _logger);

                //Скачивает и хранит Json с баннером, докачивает Texture, отдает по требованию, докачивает новые (Buffer)
                _contentProvider = new BannerContentProvider(
                    _api,
                    new MediaCache(_api, logger: _logger),
                    3,
                    _logger);

                //Шлет на сервак результаты просмотра
                _reporter = new EventReporter(_api, _logger);

                // View
                _viewFactory = new ViewFactory();

                //Попытка авторизоваться и подписать Api
                var authorized = await _authenticator.Authorize();
                if (authorized)
                {
                    //Качаем первую партию контента 
                    _contentProvider.Init();
                    //Запрашиваем право на предварительный показ
                    _ = RefreshCanShow();
                }

                if (_api == null || _contentProvider == null)
                    throw new Exception("AdService initialization incomplete");

                _state = AdServiceState.Initialized;
            }
            catch (Exception e)
            {
                _logger?.Log($"[Ads] Initialize failed: {e}");
                Cleanup();
                _state = AdServiceState.Failed;
            }
        }

        public static async UniTask RequestShowAd()
        {
            _logger?.Log($"[Ads] Show requested");
            if (_state != AdServiceState.Initialized)
            {
                _logger?.Log($"[Ads] Show rejected: \"State = {_state}\"");
                return;
            }

            if (_isShowing)
            {
                _logger?.Log($"[Ads] Show rejected: \"Previous show request not complete\"");
                return;
            }

            _isShowing = true;

            var stopWatch = new Stopwatch();
            try
            {
                var authorized = await EnsureAuthorized();

                if (!authorized)
                {
                    _logger?.Log($"[Ads] Show rejected: \"Not authorized\"");
                    return;
                }

                var canShow = CanShowSync();
                if (!canShow)
                {
                    _logger?.Log($"[Ads] Show rejected: \"Server didn't allow show\"");
                    return;
                }

                var content = _contentProvider.TakeBanner();

                if (content == null)
                {
                    _logger?.Log($"[Ads] Show rejected: \"Banner not ready yet\"");
                    return;
                }

                await RecreateView();
                
                //Время нужно для сбора статистики
                stopWatch.Start();
                var isClick = await _view.Show(content);
                stopWatch.Stop();

                await Report(content.Id, isClick, (float)stopWatch.Elapsed.TotalSeconds);
            }
            finally
            {
                _isShowing = false;
                _ = RefreshCanShow();
            }
        }

        //Отправка отчета, класс Reporter (нажали или закрыли + длительность просмотра)
        private static async UniTask Report(uint id, bool sendIsClick, float duration)
        {
            await _reporter.ReportShown(id, duration);

            if (sendIsClick)
                await _reporter.ReportClicked(id);
        }

        // Если авторизован возвращает true, если нет пробует и возвращает результат
        private static async UniTask<bool> EnsureAuthorized()
        {
            if (_authenticator.IsAuthorized)
                return true;

            var authorized = await _authenticator.Authorize();
            return authorized;
        }

        // Сервер разрешает показ не чаще какого-то времени, сервер знает это время 
        // Проверка можно ли показать, сервер отвечает да/нет 
        private static async UniTask RefreshCanShow()
        {
            try
            {
                var check = await _api.CheckCanShow();
                if (check.isDone)
                    _canShowState = check.data.data;
            }
            catch (Exception e)
            {
                _logger?.Log($"[Ads] RefreshCanShow failed: {e}");
            }
        }

        private static bool CanShowSync()
        {
            if (_canShowState)
            {
                _canShowState = false;
                return true;
            }

            return false;
        }

        private static async UniTask RecreateView()
        {
            if (_view != null)
            {
                UnityEngine.Object.Destroy(_view.gameObject);
                _view = null;
                await UniTask.DelayFrame(1);
            }

            _view = _viewFactory.Get();
            UnityEngine.Object.DontDestroyOnLoad(_view.gameObject);
        }

        // Если надо завершить работу сервиса, очистить ресурсы
        public static void Dispose()
        {
            if (_state == AdServiceState.Disposed)
                return;

            Cleanup();

            _state = AdServiceState.Disposed;
            _logger?.Log("AdService disposed");
        }

        private static void Cleanup()
        {
            if (_view != null)
                UnityEngine.Object.Destroy(_view.gameObject);

            _view = null;
            _api = null;
            _authenticator = null;
            _contentProvider = null;
            _reporter = null;
            _viewFactory = null;

            _isShowing = false;
        }
    }
}