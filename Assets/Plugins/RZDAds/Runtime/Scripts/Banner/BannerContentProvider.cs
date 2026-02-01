using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Plugins.RZDAds.Runtime.Scripts.ApiSystem;

namespace Plugins.RZDAds.Runtime.Scripts.Banner
{
    public class BannerContentProvider
    {
        private const int PREWARM_RETRIES = 2;
        private const int PREWARM_RETRY_DELAY_MILLISECONDS = 1000;
        private readonly Api _api;
        private readonly ILogger _logger;

        //очередь баннеров
        private readonly Queue<BannerContent> _prepared = new();
        private readonly int _bannerBufferSize;
        private bool _isLoading;
        private bool _isPrewarming;
        private bool _isInitialized;
        private readonly MediaCache _mediaCache;

        public bool HasBanner => _prepared.Count > 0;

        public BannerContentProvider(
            Api api,
            MediaCache mediaCache,
            int bannerBufferSize = 3,
            ILogger logger = null)
        {
            _api = api;
            _mediaCache = mediaCache;
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
            if (_isPrewarming)
                return;
            _isPrewarming = true;

            try
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
                        await UniTask.Delay(PREWARM_RETRY_DELAY_MILLISECONDS);
                    retries--;
                }
            }
            finally
            {
                _isPrewarming = false;
            }
        }

        private async UniTask LoadNext()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                //Получаю данные банера с сервера
                var response = await _api.GetBanner();

                var banner = response.data?.data?.banner;
                var mediaArray = banner?.media;
                if (!response.isDone || mediaArray == null || mediaArray.Length == 0)
                    return;

                var media = mediaArray.First();
                //Качаю если скачано ранее просто возвращаю путь
                var localPath = await _mediaCache.GetOrDownload(media);
                //localPath == null когда скачивание не удалось
                if (string.IsNullOrEmpty(localPath))
                    return;

                //Mapping json контента в контент для показа
                var content = ContentFactory.BuildContent(banner, localPath);

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
    }

    public static class ContentFactory
    {
        private const string VIDEO = "video";
        private const string IMAGE = "image";

        public static BannerContent BuildContent(RZDAds.ApiSystem.BannerDto bannerDto, string localPath)
        {
            var media = bannerDto.media?.FirstOrDefault();
            if (media == null)
                return null;

            var settings = bannerDto.settings?.FirstOrDefault();

            return media.type switch
            {
                IMAGE => new ImageBanner()
                {
                    LocalPath = localPath,
                    Id = bannerDto.id,
                    Title = bannerDto.title,
                    Description = bannerDto.description,
                    Url = bannerDto.link,
                    Duration = settings?.duration ?? 5,
                },
                VIDEO => new VideoBanner()
                {
                    LocalPath = localPath,
                    Id = bannerDto.id,
                    Title = bannerDto.title,
                    Description = bannerDto.description,
                    Url = bannerDto.link,
                    Duration = settings?.duration ?? 5,
                },
                _ => null
            };
        }
    }
}