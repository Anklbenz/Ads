using Cysharp.Threading.Tasks;
using Plugins.RZDAds.Core.ApiSystem;
using UnityEngine;

namespace Plugins.RZDAds.Core
{
    public class Authenticator
    {
        private readonly Api _api;
        private readonly ILogger _logger;
        private readonly IDeviceIdProvider _deviceIdProvider;

        public bool IsAuthorized { get; private set; }

        public Authenticator(Api api, IDeviceIdProvider deviceIdProvider, ILogger logger = null)
        {
            _api = api;
            _logger = logger;
            _deviceIdProvider = deviceIdProvider;
        }

        public async UniTask<bool> AuthorizeDevice()
        {
            var uniqueAppKey = _deviceIdProvider.GetDeviceId();
       
            var registerResponse = await _api.RegisterDevice(uniqueAppKey, Application.platform);
            _logger?.Log($"[Authenticator] Login isOk: {registerResponse.isDone}. Key: {uniqueAppKey}");
            if (!registerResponse.isDone || string.IsNullOrEmpty(registerResponse.data.token))
                return false;

            var token = registerResponse.data.token;

            _api.SetAuthorizeToken(token);

            IsAuthorized = true;
            return true;
        }
    }
}