using UnityEngine;
using UnityEngine.UI;

namespace Plugins.RZDAds
{
    public interface IDisplay
    {
        void Set(BannerContent banner);
        void Open();
        void Close();
        void Clear();
    }

    public class ImageDisplay : MonoBehaviour, IDisplay
    {
        [SerializeField] private AspectRatioFitter aspectRationFitter;
        [SerializeField] private RawImage image;

        public void Set(BannerContent banner)
        {
            var texture = banner.Texture;
            image.texture = texture;

            if (texture != null)
                aspectRationFitter.aspectRatio = (float)texture.width / texture.height;
        }

        public void Clear()
        {
            image.texture = null;
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