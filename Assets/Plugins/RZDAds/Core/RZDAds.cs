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

        public static async UniTask Initialize()
        {
            if (_initialized || _isInitializing)
                return;
            _isInitializing = true;

            _logger = new UnityLogger();

            var apiSettings = Resources.Load<ApiSettings>("ApiSettings");
            _api = new Api(apiSettings, _logger);

            _authenticator = new Authenticator(_api, new DeviceIdService(), _logger);
            _contentProvider = new BannerContentProvider(_api, _logger);
            _reporter = new EventReporter(_api, _logger);

            _bannerFactory = new BannerFactory();
            _view = _bannerFactory.Get();
            Object.DontDestroyOnLoad(_view.gameObject);

            var isOk = await _authenticator.AuthorizeDevice();
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

                var content = _contentProvider.Take();

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

        private static void OpenUrl(string url)
        {
            _logger?.Log($"[Ads] Try open url {url}");
            Application.OpenURL(url);
        }

        private static async UniTask Report(uint id, bool isClick, float duration)
        {
            await _reporter.ReportShown(id, duration);

            if (isClick)
                await _reporter.ReportClicked(id);
        }

        private static async UniTask<bool> EnsureAuthorized()
        {
            if (_authenticator.IsAuthorized)
                return true;

            var ok = await _authenticator.AuthorizeDevice();
            return ok;
        }

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