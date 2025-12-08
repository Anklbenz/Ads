using System;
using UnityEngine;

namespace Plugins.RZDAds.ApiSystem
{
    public enum HttpStatus : int
    {
        //	DatabaseError = -2,
        ReadyToReceive = -1,
        NotConnected = 0,
        IsOk = 200,
        IsOkCreated = 201,
        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        InternalServerError = 500,
        BadGateway = 502,
        ServiceUnavailable = 503,
    }

    public enum HttpMethod
    {
        Post = 0,
        Get = 1,
        Put = 2,
        Patch = 3,
        Delete = 4,
    }

    [Serializable]
    public class RequestArgs
    {
        public string url;
        public HttpMethod method;
        public string jsonBody;
        public WWWForm FormData;
        public IProgress<float> Progress;

        public RequestArgs(
            string url,
            HttpMethod method,
            string jsonBody = null,
            WWWForm formData = null,
            IProgress<float> progress = null)
        {
            this.url = url;
            this.method = method;
            this.jsonBody = jsonBody;
            this.FormData = formData;
            this.Progress = progress;
        }
    }
    
    

    [Serializable]
    public class ApiResponse<T> : ApiResult
    {
        public T data;
        public T[] dataArray;
    }


    [Serializable]
    public class ApiResult
    {
        public string errorMessage;
        public bool isDone;
        public bool hasError;
        public long statusCode;
    }
}