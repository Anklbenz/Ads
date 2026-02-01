using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Plugins.RZDAds.Runtime.Scripts.Banner;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;

namespace Plugins.RZDAds.Runtime.Scripts.View
{
    public class VideoDisplay : MonoBehaviour, IDisplay
    {
        private const int ALLOWED_PREPARE_TIME = 20;
        [SerializeField] private AspectRatioFitter aspect;
        [SerializeField] private VideoPlayer player;
        [SerializeField] private RawImage rawImage;

        [SerializeField] private Button muteButton;
        [SerializeField] private Sprite muteSprite;
        [SerializeField] private Sprite unmuteSprite;
        [SerializeField] private Image muteImage;
        private bool _isMuted;
        private RenderTexture _rt;

        public async UniTask<bool> TrySet(BannerContent banner)
        {
            Reset();
            player.url = banner.LocalPath;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.waitForFirstFrame = true;
            
            _rt = new RenderTexture(16, 16, 0, RenderTextureFormat.ARGB32);
            _rt.Create();

            player.targetTexture = _rt;
            rawImage.texture = _rt;
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ALLOWED_PREPARE_TIME));

            void OnError(VideoPlayer _, string msg)
            {
                Debug.LogError($"[VideoDisplay] Video error: {msg}");
                cts.Cancel();
            }

            player.errorReceived += OnError;
            
            try
            {
                await PrepareAndPlay(cts.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
           
                Debug.LogError($"[VideoDisplay] Failed. Timeout: {ALLOWED_PREPARE_TIME} sec");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[VideoDisplay] Failed: {e}");
                return false;
            }
            finally
            {
                player.errorReceived -= OnError;
            }
        }



        private async UniTask PrepareAndPlay(CancellationToken ct)
        {
            //Некоторые видео долго грузятся и на старте виден белый квадрат RawImage 
            //Поэтому Disable rawImage до того как загрузится
            rawImage.enabled = false;

            player.Prepare();
            await UniTask.WaitUntil(
                () => player.isPrepared &&
                      player.texture != null &&
                      player.texture.width > 16,
                cancellationToken: ct);

            if (!player.isPrepared || player.texture == null || player.texture.width <= 16)
                throw new Exception("Video prepare failed");
           
            var videoW =(int) player.width;
            var videoH =(int) player.height;

            // правильный аспект
            aspect.aspectRatio = videoW / (float)videoH;

            // ПЕРЕСОЗДАЁМ RT под реальное разрешение
            player.targetTexture = null;

            _rt.Release();
            Destroy(_rt);

            _rt = new RenderTexture(videoW, videoH, 0, RenderTextureFormat.ARGB32);
            _rt.Create();

            player.targetTexture = _rt;
            rawImage.texture = _rt;

            player.Play();

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            rawImage.enabled = true;
        }

        private void ToggleMuted()
        {
            _isMuted = !_isMuted;
            if (!_isMuted)
                Unmute();
            else
                Mute();
        }

        private void Mute()
        {
            player.SetDirectAudioVolume(0, 0f);
            muteImage.sprite = muteSprite;
            _isMuted = true;
        }

        private void Unmute()
        {
            player.SetDirectAudioVolume(0, 1f);
            muteImage.sprite = unmuteSprite;
            _isMuted = false;
        }

        public void Clear()
        {
            player.Stop();
            rawImage.texture = null;
        }

        public void Open()
            => gameObject.SetActive(true);

        public void Close()
            => gameObject.SetActive(false);

        private void OnEnable()
        {
            muteButton.onClick.AddListener(ToggleMuted);
        }

        private void OnDisable()
        {
            muteButton.onClick.RemoveListener(ToggleMuted);
        }
        
        private void Reset()
        {
            player.Stop();
            player.clip = null;
            player.url = null;
            
            player.targetTexture = null;
            rawImage.texture = null;

            if (_rt == null) 
                return;
            _rt.Release();
            Destroy(_rt);
            _rt = null;
        }
    }
}