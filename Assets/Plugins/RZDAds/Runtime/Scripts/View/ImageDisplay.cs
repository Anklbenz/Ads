using Cysharp.Threading.Tasks;
using Plugins.RZDAds.Runtime.Scripts.Banner;
using UnityEngine;
using UnityEngine.Networking;
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
           var texture = await LoadTexture(banner.LocalPath);

            //texture.height > 0 деление на 0
            var isValid = texture != null && texture.height > 0; 
            if (isValid)
                aspectRationFitter.aspectRatio = (float)texture.width / texture.height;
            
            image.texture = texture;
            return await UniTask.FromResult(isValid);
        }

        public void Clear()
        {
            if (image.texture != null)
                Destroy(image.texture);
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
        //Можно вынести в отдельный Loader так как файл локальный это не бизнес логика, а подробности коретного отображения
        private async UniTask<Texture2D> LoadTexture(string localPath)
        {
            var url = $"file://{localPath}";
            using var req = UnityWebRequestTexture.GetTexture(url);
            await req.SendWebRequest().ToUniTask();

            if (req.result != UnityWebRequest.Result.Success)
                return null;

            return DownloadHandlerTexture.GetContent(req);
        }
    }
    
}