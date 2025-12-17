using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Plugins.RZDAds.Runtime.Scripts.View
{
    public class VideoDisplay : MonoBehaviour, IDisplay
    {
        [SerializeField] private AspectRatioFitter aspect;
        [SerializeField] private VideoPlayer player;
        [SerializeField] private RawImage rawImage;

        [SerializeField] private Button muteButton;
        [SerializeField] private Sprite muteSprite;
        [SerializeField] private Sprite unmuteSprite;
        [SerializeField] private Image muteImage;
        private bool _isMuted;

        public void Set(BannerContent banner)
        {
            player.url = banner.VideoUrl;
            player.renderMode = VideoRenderMode.APIOnly;
            PrepareAndPlay().Forget();
            Unmute();
        }

        private async UniTask PrepareAndPlay()
        {
            player.Prepare();
            while (!player.isPrepared)
                await UniTask.Yield();

            rawImage.texture = player.texture;

            if (player.width > 0 && player.height > 0)
                aspect.aspectRatio = player.width / (float)player.height;

            player.Play();
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
    }
}