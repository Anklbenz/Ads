using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Plugins.RZDAds.ApiSystem;
using Plugins.RZDAds.Runtime.Scripts.ApiSystem;
using UnityEngine;

namespace Plugins.RZDAds.Runtime.Scripts.Banner
{
    public class MediaCache
    {
        private readonly Api _api;
        private readonly ILogger _logger;
        private readonly string _root;

        public MediaCache(Api api, string cacheFolderName = "AdsCache", ILogger logger = null)
        {
            _api = api;
            _logger = logger;
            _root = Path.Combine(Application.persistentDataPath, cacheFolderName);
            Directory.CreateDirectory(_root);
        }

        public async UniTask<string> GetOrDownload(Media data)
        {
            var ext = data.ext.StartsWith(".")
                ? data.ext
                : "." + data.ext;

            var fileName = $"{data.hash}{ext}";
            var path = Path.Combine(_root, fileName);

            if (File.Exists(path))
            {
                Log($"fileName{fileName}, exists");
                return path;
            }
            
            var tempPath = $"{path}.tmp";
            Log($"Downloading {data.url} to {tempPath}");

            if (File.Exists(tempPath))
                File.Delete(tempPath);

            var isOk = await _api.GetFileAsync(data.url, tempPath);

            if (!isOk)
            {
                File.Delete(tempPath);
                return null;
            }

            File.Move(tempPath, path);
            return path;
        }
        private void Log(string msg) => 
            _logger?.Log(msg);
    }
}