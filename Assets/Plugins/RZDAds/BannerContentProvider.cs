using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Plugins.RZDAds.ApiSystem;
using UnityEngine;

namespace Plugins.RZDAds
{
    
    public class BannerContent
    {
        public string Type;
        public uint Id;
        public string Title;
        public string Description;
        public string Url;
        public int Duration;
        public Texture2D Texture;
        public string VideoUrl;
    }

    public class BannerContentProvider
    {
        private readonly Api _api;
        private readonly Queue<BannerContent> _prepared = new();
        private bool _isLoading;

        public bool IsReady => _prepared.Count > 0;

        public BannerContentProvider(Api api)
        {
            _api = api;
           
        }
        
        public void Init()
        {
            _ = EnsureContentLoop();
        }
        
        private async UniTask EnsureContentLoop()
        {
            while (true)
            {
                if (!_isLoading && _prepared == null)
                {
                    await LoadNext();
                }

                // Немного подождать, не спамить
                await UniTask.Delay(250);
            }
        }
        public BannerContent Take()
        {
            if (!_prepared.TryDequeue(out var content))
            {
                LoadNext().Forget();
                return null;
            }

            LoadNext().Forget();
            return content;
        }

        private async UniTask LoadNext()
        {
            if (_isLoading)
                return;
            UnityEngine.Debug.Log("Start loading Content...");
            _isLoading = true;

            try
            {
                var response = await _api.GetBanner();
                UnityEngine.Debug.Log($"Banner data received: ({response.isDone} http:{response.statusCode})");
                var banner = response.data?.data;
                if (!response.isDone || banner == null)
                    return;
                UnityEngine.Debug.Log($"Banner id: ({banner.banner.id})");
                var content = await BuildContent(banner.banner);

                if (content != null)
                    _prepared.Enqueue(content);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async UniTask<BannerContent> BuildContent(Banner banner)
        {
            var media = banner.media?.FirstOrDefault();
            if (media == null)
                return null;

            var settings = banner.settings?.FirstOrDefault();

            Texture2D texture = null;
            if (media.type == "image")
            {
                texture = await GetTexture(media.url);
                UnityEngine.Debug.Log($"Banner id:({banner.id}) is image");
                UnityEngine.Debug.Log($"Banner id:({banner.id}) Texture received result: ({texture != null})");
                if (texture == null)
                {
                    UnityEngine.Debug.Log($"Banner id:({banner.id}) texture broken)");
                    return null;
                }
            }

            return new BannerContent
            {
                Type = media.type,
                Id = banner.id,
                Title = banner.title,
                Description = banner.description,
                Url = banner.link,
                Duration = /*settings?.duration ?? 10*/10,
                Texture = texture,
                VideoUrl = media.url
            };
        }

        private async UniTask<Texture2D> GetTexture(string url)
        {
            var textureResult = await _api.DownloadTexture(url);
            var texture = textureResult.data;

            if (!textureResult.isDone || texture == null)
                return null;

            return texture;
        }
    }
}