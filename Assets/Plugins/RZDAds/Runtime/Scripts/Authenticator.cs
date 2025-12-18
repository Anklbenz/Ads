using Cysharp.Threading.Tasks;
using Plugins.RZDAds.Runtime.Scripts.ApiSystem;
using UnityEngine;

namespace Plugins.RZDAds.Runtime.Scripts
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

        public async UniTask<bool> Authorize()
        {
            //генерация уникального ключа устройства
            var uniqueAppKey = _deviceIdProvider.GetDeviceId();
            
            //регистрация устройства
            var registerResponse = await _api.RegisterDevice(uniqueAppKey, Application.platform);
            _logger?.Log($"[Authenticator] authorization is: {registerResponse.isDone}. Key: {uniqueAppKey}");
            if (!registerResponse.isDone || string.IsNullOrEmpty(registerResponse.data.token))
                return false;

            var token = registerResponse.data.token;
            //Подписываем Api полученным токеном 
            _api.ApplyAuthorizationToken(token);

            IsAuthorized = true;
            return true;
        }
    }
}