using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Plugins.RZDAds
{
    public class VideoDisplay : MonoBehaviour, IDisplay
    {
        [SerializeField] private AspectRatioFitter aspect;
        [SerializeField] private VideoPlayer player;
        [SerializeField] private RawImage rawImage;

        public void Set(BannerContent banner)
        {
            player.url = banner.VideoUrl;
            player.renderMode = VideoRenderMode.APIOnly;
            PrepareAndPlay().Forget();
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

        public void Clear()
        {
            player.Stop();
            rawImage.texture = null;
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}