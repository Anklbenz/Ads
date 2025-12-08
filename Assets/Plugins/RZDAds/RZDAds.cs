using System.Diagnostics;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Plugins.RZDAds.ApiSystem;

namespace Plugins.RZDAds
{
    public class EventReporter
    {
        private readonly Api _api;

        public EventReporter(Api api)
        {
            _api = api;
        }

        public async UniTask ReportShown(uint id, float duration)
        {
            await _api.ReportEvent(id, "show", duration);
        }

        public async UniTask ReportClicked(uint id)
        {
            await _api.ReportEvent(id, "click", 0);
        }
    }


    public class Authenticator
    {
        private readonly Api _api;
        private static IDeviceIdProvider _deviceIdProvider;

        public bool IsAuthorized { get; private set; }

        public Authenticator(Api api)
        {
            _api = api;
            _deviceIdProvider = new DeviceIdService();
        }

        public async UniTask<bool> AuthorizeDevice()
        {
            var uniqueAppKey = _deviceIdProvider.GetDeviceId();
            var registerResponse = await _api.RegisterDevice(uniqueAppKey, Application.platform);

            if (!registerResponse.isDone || string.IsNullOrEmpty(registerResponse.data.token))
                return false;

            var token = registerResponse.data.token;

            _api.SetAuthorizeToken(token);

            IsAuthorized = true;
            return true;
        }
    }

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
        private static bool _isShowing;

        public static async UniTask Initialize()
        {
            if (_initialized)
                return;

            var apiSettings = Resources.Load<ApiSettings>("ApiSettings");
            _api = new Api(apiSettings);

            _authenticator = new Authenticator(_api);
            await _authenticator.AuthorizeDevice();

            _contentProvider = new BannerContentProvider(_api);
            _reporter = new EventReporter(_api);

            _bannerFactory = new BannerFactory();
            _view = _bannerFactory.Get();

            Object.DontDestroyOnLoad(_view.gameObject);

            _initialized = true;
        }


        public static async UniTask Show()
        {
            var stopWatch = new Stopwatch();
            UnityEngine.Debug.Log("Try Show");
            if (_isShowing)
                return;

            _isShowing = true;

            try
            {
                var isAuthorized = await CheckAuthorize();
                UnityEngine.Debug.Log($"Device authorized: ({isAuthorized})");
                if (!isAuthorized)
                    return;

                var canShow = await CheckCanShow();

                UnityEngine.Debug.Log($"Server allow show: {canShow}");
                if (!canShow)
                    return;

                var content = _contentProvider.Take();

                if (content == null)
                    return;

                stopWatch.Start();
                var isClick = await _view.Show(content);
                stopWatch.Stop();

                await Report(content.Id, isClick, (float)stopWatch.Elapsed.TotalSeconds);
                if (isClick)
                    Application.OpenURL(content.Url);
            }
            finally
            {
                _isShowing = false;
            }
        }

        private static async UniTask Report(uint id, bool isClick, float duration)
        {
            await _reporter.ReportShown(id, duration);
            UnityEngine.Debug.Log($"Is Clicked: ({isClick})");

            if (isClick)
                await _reporter.ReportClicked(id);
        }

        private static async UniTask<bool> CheckAuthorize()
        {
            if (_authenticator.IsAuthorized)
                return true;
            return await _authenticator.AuthorizeDevice();
        }

        private static async UniTask<bool> CheckCanShow()
        {
            var check = await _api.CheckCanShow();
            //check.data.data <- bool можно или нет 
            return check.isDone && check.data.data;
        }


        public static void Dispose()
        {
            Object.Destroy(_view.gameObject);
            _view = null;
            _api = null;
            _bannerFactory = null;
            _authenticator = null;
            _initialized = false;
        }
    }
}