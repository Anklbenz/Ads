using System;
using Cysharp.Threading.Tasks;
using Plugins.RZDAds.ApiSystem;
using UnityEngine;

namespace Plugins.RZDAds.Core.ApiSystem
{
    public class Api : ApiClientBase
    {
        private readonly ApiSettings _apiSettings;

        private string _token;

        public Api(ApiSettings apiSettings, ILogger logger = null) : base(logger)
        {
            _apiSettings = apiSettings;
            accessToken = apiSettings.AppKey;
        }

        public void SetAuthorizeToken(string token)
        {
            _token = token;
        }

        public async UniTask<ApiResponse<DeviceRegisterResponse>> RegisterDevice(string udid,
            RuntimePlatform deviceType)
        {
            var deviceResuest = new DeviceRegisterRequest()
            {
                udid = udid,
                platform = deviceType.ToString(),
                timestamp = Now()
            };
            return await PostJsonAsync<DeviceRegisterResponse, DeviceRegisterRequest>(_apiSettings.RegisterDeviceUrl,
                deviceResuest);
        }

        public async UniTask<ApiResponse<BannerResponse>> GetBanner()
        {
            var request = new Request()
            {
                token = _token,
                timestamp = Now()
            };
            return await PostJsonAsync<BannerResponse, Request>(_apiSettings.GetBannerUrl, request);
        }


        public async UniTask<ApiResponse<Response>> ReportEvent(uint id, string action, float duration)
        {
            var request = new EventRequest()
            {
                token = _token,
                action = action,
                banner_id = id,
                duration = duration,
                timestamp = Now()
            };
            return await PostJsonAsync<Response, EventRequest>(_apiSettings.SendEventsUrl, request);
        }

        public async UniTask<ApiResponse<Response>> CheckCanShow()
        {
            var request = new Request()
            {
                token = _token,
                timestamp = Now()
            };
            return await PostJsonAsync<Response, Request>(_apiSettings.CheckReadyUrl, request);
        }

        public async UniTask<ApiResponse<Texture2D>> DownloadTexture(string fromStaticUrl,
            IProgress<float> progress = null)
        {
            return await GetTextureAsync(fromStaticUrl, progress);
        }

        private string Now() =>
            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
}
/*public async UniTask<ApiResponse<DeviceRegisterResponse>> RegisterDevice(string udid, RuntimePlatform deviceType )
{
    var registerRequest = new DeviceRegisterRequest
    {
        udid = udid,
        platform = deviceType.ToString().ToLower(),
        timestamp =  DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
    };
    EncryptedRequest encryptedBody = Encryptor.Encrypt(registerRequest, _apiSettings.PublicKey);

    return await PostJsonAsync<DeviceRegisterResponse, EncryptedRequest>(_apiSettings.DeviceUrl, encryptedBody);
}*/