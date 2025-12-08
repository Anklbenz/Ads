//Обрезанная версия Http клиента без поддержки массивов и NewtonSoft сериализатора 
//Нужна полная версия пишите anklbenz85@gmail.com - пришлю

using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Plugins.RZDAds.ApiSystem
{
    public abstract class ApiClientBase
    {
        private const long MIN_SUCCESS_WEB_REQUEST_CODE = 200;
        private const long MAX_SUCCESS_WEB_REQUEST_CODE = 299;

        private readonly WebRequestJson _webRequest = new();
        private readonly WebRequestFile _webRequestFile = new();

        // Делегат обработки 401 Unauthorized.
        // Параметр: responseText сервера.
        // Возвращает true — если токен обновлён и запрос можно повторить.
        protected Func<string, UniTask<bool>> handleUnauthorizedResponse;

        //----
        protected string accessToken { get; set; } = string.Empty;
        protected int maxRetries { get; set; } = 2;

        protected bool isLogEnabled { get; set; }

//----------------------------------------------File----------------------------------------------------------------------
        public async UniTask<bool> GetFileAsync(string fromUrl, string toPath, IProgress<float> progress = null)
        {
            _webRequestFile.authorKey = accessToken;
            var requestResult = await _webRequestFile.RequestFile(fromUrl, toPath, progress);

            return requestResult;
        }

        public async UniTask<ApiResponse<Texture2D>> GetTextureAsync(string fromUrl, IProgress<float> progress = null)
        {
            _webRequestFile.authorKey = accessToken;
            var requestResult = await _webRequestFile.RequestFileToMemory(fromUrl, progress);

            return requestResult;
        }
//----------------------------------------------Get----------------------------------------------------------------------

        protected async UniTask<ApiResponse<T>> GetAsync<T>(string requestUrl, bool expectsArray = false,
            IProgress<float> progress = null)
        {
            var requestParam = new RequestArgs(requestUrl, HttpMethod.Get, progress: progress);

            var result = await DoRequestAsync<T>(requestParam);
            return result;
        }

        protected async UniTask<ApiResponse<T>> GetWithQueryAsync<T, TQuery>(string requestUrl, TQuery query,
            bool expectsArray = false, IProgress<float> progress = null)
        {
            var url = WebUtils.ToQueryUrl(requestUrl, query);
            var requestParam = new RequestArgs(url, HttpMethod.Get, progress: progress);

            var result = await DoRequestAsync<T>(requestParam);
            return result;
        }

        protected async UniTask<ApiResponse<T>> GetWithPathAsync<T, TPath>(string requestUrl, TPath pathParam,
            bool expectsArray = false, IProgress<float> progress = null) where T : new() where TPath : class
        {
            var path = WebUtils.ConvertToPathParams(pathParam);
            var url = WebUtils.CombineUrl(requestUrl, path);

            var requestParams = new RequestArgs(url, HttpMethod.Get, progress: progress);

            var result = await DoRequestAsync<T>(requestParams);
            return result;
        }

        protected async UniTask<ApiResponse<T>> GetWithArrayAsync<T, TArray>(string requestUrl, TArray arrayParam,
            bool expectsArray = false, IProgress<float> progress = null) where T : new() where TArray : class
        {
            var param = WebUtils.ConvertToArrayParams(arrayParam);
            var url = WebUtils.CombineUrl(requestUrl, param);

            var requestParams = new RequestArgs(url, HttpMethod.Get, progress: progress);

            var result = await DoRequestAsync<T>(requestParams);
            return result;
        }

//----------------------------------------------Post----------------------------------------------------------------------
        protected async UniTask<ApiResponse<T>> PostAsync<T>(string requestUrl, bool expectsArray = false,
            IProgress<float> progress = null) where T : new()
        {
            var requestParams = new RequestArgs(requestUrl, HttpMethod.Post, progress: progress);

            var result = await DoRequestAsync<T>(requestParams);
            return result;
        }

        protected async UniTask<ApiResponse<T>> PostJsonAsync<T, TBody>(string requestUrl, TBody body,
            IProgress<float> progress = null) where T : new() where TBody : class
        {
            var bodyString = JsonUtility.ToJson(body);
            var requestParams = new RequestArgs(requestUrl, HttpMethod.Post, bodyString, progress: progress);

            var result = await DoRequestAsync<T>(requestParams);
            return result;
        }

        protected async UniTask<ApiResponse<T>> PostMultipartFormAsync<T>(string requestUrl, WWWForm formData,
            IProgress<float> progress = null) where T : new()
        {
            var requestParams = new RequestArgs(requestUrl, HttpMethod.Post, formData: formData, progress: progress);

            var result = await DoRequestAsync<T>(requestParams);
            return result;
        }

//----------------------------------------------Patch----------------------------------------------------------------------
        protected async UniTask<ApiResponse<T>> PatchJsonAsync<T, TBody>(string requestUrl, TBody body,
            IProgress<float> progress = null) where T : new()
        {
            var bodyString = JsonUtility.ToJson(body);
            var requestParams = new RequestArgs(requestUrl, HttpMethod.Patch, bodyString, progress: progress);

            var result = await DoRequestAsync<T>(requestParams);
            return result;
        }

        protected async UniTask<ApiResponse<T>> PatchWithPathAsync<T, TPath>(string requestUrl, TPath path,
            IProgress<float> progress = null) where T : new()
        {
            var url = WebUtils.ConvertToPathParams(path, requestUrl);
            var argParams = new RequestArgs(url, HttpMethod.Patch, progress: progress);

            var result = await DoRequestAsync<T>(argParams);
            return result;
        }

// Handler когда указан и путь и BodyJson 
        protected async UniTask<ApiResponse<T>> PatchWithPathAndBodyAsync<T, TPath, TBody>(string requestUrl,
            TPath path, TBody body, IProgress<float> progress = null) where T : new()
        {
            var url = WebUtils.ConvertToPathParams(path, requestUrl);
            var bodyString = JsonUtility.ToJson(body);
            var argParams = new RequestArgs(url, HttpMethod.Patch, jsonBody: bodyString, progress: progress);

            var result = await DoRequestAsync<T>(argParams);
            return result;
        }

//----------------------------------------------Core----------------------------------------------------------------------
        //Отправка запроса 
        private async UniTask<ApiResponse<T>> DoRequestAsync<T>(RequestArgs requestArgs)
        {
            int attempt = 0;

            while (true)
            {
                _webRequest.authorKey = accessToken;

                var responseText = await SendRequestAsync(requestArgs);
                var result = HandleResult<T>(responseText, requestArgs);

                //если не 401 или нет метода для перелогина возвращаем ошибку
                if (!IsUnauthorized(_webRequest.responseCode) || handleUnauthorizedResponse == null)
                    return result;

                // запускаем делегат если нужно ли обработать на клиенте 401
                var shouldRetry = await handleUnauthorizedResponse(responseText);

                if (!shouldRetry)
                    return result;

                attempt++;
                if (attempt > maxRetries)
                    return result;
            }
        }

        private async UniTask<string> SendRequestAsync(RequestArgs requestArgs)
        {
            Stopwatch stopWatch = null;
            if (isLogEnabled)
                stopWatch = Stopwatch.StartNew();

            var resultString = await _webRequest.Request(requestArgs);

            stopWatch?.Stop();

            if (isLogEnabled)
                UnityEngine.Debug.Log(
                    $"[{_webRequest.responseCode}] {requestArgs.method.ToString().ToUpper()} {requestArgs.url} ->{stopWatch.ElapsedMilliseconds}ms");

            return resultString;
        }

        //Обработка результата HTTP запроса
        private ApiResponse<T> HandleResult<T>(string responseText, RequestArgs requestArgs)
        {
            var isSuccess = IsSuccess(_webRequest.responseCode);

            if (!isSuccess)
                return new ApiResponse<T>()
                {
                    data = default,
                    errorMessage = !string.IsNullOrEmpty(_webRequest.errorDescription)
                        ? _webRequest.errorDescription
                        : responseText,
                    statusCode = _webRequest.responseCode,
                    hasError = _webRequest.hasError
                };

            try
            {
                return new ApiResponse<T>()
                {
                    data = JsonUtility.FromJson<T>(responseText),
                    isDone = true,
                    statusCode = _webRequest.responseCode,
                };
            }
            catch (Exception e)
            {
                return new ApiResponse<T>
                {
                    errorMessage = $"JSON parse error: {e.Message}",
                    statusCode = _webRequest.responseCode,
                    hasError = true
                };
            }
        }

        private bool IsUnauthorized(long statusCode) =>
            statusCode == (int)HttpStatus.Unauthorized;

        private bool IsSuccess(long requestCode) =>
            requestCode is >= MIN_SUCCESS_WEB_REQUEST_CODE and <= MAX_SUCCESS_WEB_REQUEST_CODE;
    }
}