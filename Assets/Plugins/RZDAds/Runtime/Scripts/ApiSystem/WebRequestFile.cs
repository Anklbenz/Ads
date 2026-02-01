using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Plugins.RZDAds.ApiSystem
{
    public class WebRequestFile
    {
        private const string TEMP_EXTENSION = ".temp";
        public long responseCode { get; set; }
        public string authorKey { get; set; } = string.Empty;

        public async UniTask<bool> RequestFile(string url, string savePath, IProgress<float> progress = null)
        {
            responseCode = (int)HttpStatus.ReadyToReceive;



            using var webRequest = UnityWebRequest.Get(url);
            var downloadHandlerFile = new DownloadHandlerFile(savePath)
            {
                removeFileOnAbort = true
            };

            webRequest.downloadHandler = downloadHandlerFile;
            webRequest.certificateHandler = new ForceAcceptAllCertificates();
            webRequest.SetRequestHeader("Authorization", authorKey);

            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
            {
                progress?.Report(operation.progress);
                await UniTask.Yield();
            }

            progress?.Report(1f);

            responseCode = webRequest.responseCode;
            return webRequest.result == UnityWebRequest.Result.Success;
        }


        public async UniTask<ApiResponse<Texture2D>> RequestFileToMemory(string url, IProgress<float> progress = null)
        {
            responseCode = (int)HttpStatus.ReadyToReceive;

            using var webRequest = UnityWebRequest.Get(url);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.certificateHandler = new ForceAcceptAllCertificates();
            webRequest.SetRequestHeader("Authorization", authorKey);

            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                progress?.Report(operation.progress);
                await UniTask.Yield();
            }

            progress?.Report(1f);

            responseCode = webRequest.responseCode;

            if (webRequest.result != UnityWebRequest.Result.Success)
                return new ApiResponse<Texture2D>(){errorMessage = webRequest.error};

            // Всё содержимое файла в оперативке
            byte[] data = webRequest.downloadHandler.data;
        
            Texture2D texture = BytesToTexture(data);


            return new ApiResponse<Texture2D>()
            {
                data = texture,
                isDone = texture != null,
            };
        }

        private Texture2D BytesToTexture(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(data);
                return tex;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to decode image: {e}");
                return null;
            }
        }
    }
}