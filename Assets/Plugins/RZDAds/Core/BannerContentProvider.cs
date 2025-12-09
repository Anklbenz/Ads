using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Plugins.RZDAds.ApiSystem;
using Plugins.RZDAds.Core.ApiSystem;
using UnityEngine;

namespace Plugins.RZDAds.Core
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
        private const int BUFFER = 2;
        private readonly Api _api;
        private readonly ILogger _logger;
        private readonly Queue<BannerContent> _prepared = new();
        private bool _isLoading;

        public bool IsReady => _prepared.Count > 0;

        public BannerContentProvider(Api api, ILogger logger = null)
        {
            _api = api;
            _logger = logger;
        }

        public void Init()
        {
            Prewarm().Forget();
        }

        private async UniTask Prewarm()
        {
            //retires на случай если loadNext ничего не отдаст (напр ошибка сервера) цикл будет вечным
            //retries 5 раз попробует и все
            int retries = 5;
            while (_prepared.Count < BUFFER && retries > 0)
            {
                await LoadNext();
                retries--;
            }
        }

        public BannerContent TakeBanner()
        {
            if (!_prepared.TryDequeue(out var content))
            {
                LoadNext().Forget();
                return null;
            }

            if (_prepared.Count < BUFFER)
                LoadNext().Forget();
            return content;
        }

        private async UniTask LoadNext()
        {
            if (_isLoading || _prepared.Count >= BUFFER)
                return;
            _logger?.Log("[ContentProvider] Start loading");
            _isLoading = true;

            try
            {
                var response = await _api.GetBanner();
                _logger?.Log($"[ContentProvider] Banner received isOk: {response.isDone}");
                var banner = response.data?.data;
                if (!response.isDone || banner == null)
                    return;
             
                var content = await BuildContent(banner.banner);

                if (content != null)
                    _prepared.Enqueue(content);
            }
            catch (Exception e)
            {
                _logger?.Log($"[ContentProvider] LoadNext exception: {e}");
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
            _logger?.Log($"[ContentProvider] Banner id:{banner.id} is {media.type}.");
            if (media.type != "image")
                return new BannerContent
                {
                    Type = media.type,
                    Id = banner.id,
                    Title = banner.title,
                    Description = banner.description,
                    Url = banner.link,
                    Duration = settings?.duration ?? 5,
                    VideoUrl = media.url
                };

            var texture = await GetTexture(media.url);
            _logger?.Log($"[ContentProvider] Texture download isOk: {texture != null}");

            if (texture != null)
                return new BannerContent
                {
                    Type = media.type,
                    Id = banner.id,
                    Title = banner.title,
                    Description = banner.description,
                    Url = banner.link,
                    Duration = settings?.duration ?? 5,
                    Texture = texture,
                };

            return null;
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