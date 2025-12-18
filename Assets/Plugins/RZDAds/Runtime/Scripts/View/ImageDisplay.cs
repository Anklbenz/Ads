using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Plugins.RZDAds.Runtime.Scripts.View
{
    public interface IDisplay
    {
        UniTask<bool> TrySet(BannerContent banner);
        void Open();
        void Close();
        void Clear();
    }

    public class ImageDisplay : MonoBehaviour, IDisplay
    {
        [SerializeField] private AspectRatioFitter aspectRationFitter;
        [SerializeField] private RawImage image;

        public async UniTask<bool> TrySet(BannerContent banner)
        {
            var texture = banner.Texture;
            image.texture = texture;

            //texture.height > 0 деление на 0
            var isValid = texture != null && texture.height > 0; 
            if (isValid)
                aspectRationFitter.aspectRatio = (float)texture.width / texture.height;
            return await UniTask.FromResult(isValid);
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