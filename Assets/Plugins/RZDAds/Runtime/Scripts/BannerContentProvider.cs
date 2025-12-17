using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Plugins.RZDAds.ApiSystem;
using Plugins.RZDAds.Runtime.Scripts.ApiSystem;
using UnityEngine;

namespace Plugins.RZDAds.Runtime.Scripts
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
        private const int PREWARM_RETRIES = 5;
        private const int PREWARM_DELAY_MILLISECONDS = 1000;
        private readonly Api _api;
        private readonly ILogger _logger;

        //очередь баннеров
        private readonly Queue<BannerContent> _prepared = new();
        private readonly int _bannerBufferSize;
        private bool _isLoading;
        private bool _isInitialized;

        public bool HasBanner => _prepared.Count > 0;

        public BannerContentProvider(
            Api api,
            int bannerBufferSize = 2,
            ILogger logger = null)
        {
            _api = api;
            _bannerBufferSize = bannerBufferSize;
            _logger = logger;
        }

        public void Init()
        {
            if (_isInitialized)
                return;
            _isInitialized = true;
            Prewarm().Forget();
        }

        public BannerContent TakeBanner()
        {
            // Пытаемся достать баннер и запускаем подогрев буфера
            // Если банера нет, то return null
            _prepared.TryDequeue(out var content);

            Prewarm().Forget();

            return content;
        }

        //Поддержание буфера с объявлений полным
        private async UniTask Prewarm()
        {
            //retires на случай если loadNext ничего не отдаст (напр ошибка сервера) цикл будет вечным
            //retries 5 раз попробует и все
            int retries = PREWARM_RETRIES;
            while (_prepared.Count < _bannerBufferSize && retries > 0)
            {
                int itemsBefore = _prepared.Count;
                await LoadNext();

                // Только Если загрузить не удалось, delay перед следующей попыткой
                if (itemsBefore == _prepared.Count)
                    await UniTask.Delay(PREWARM_DELAY_MILLISECONDS);
                retries--;
            }
        }

        private async UniTask LoadNext()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                var response = await _api.GetBanner();

                var banner = response.data?.data;
                if (!response.isDone || banner == null)
                    return;
                //Mapping json контента в контент для показа + загрузка Texture 
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