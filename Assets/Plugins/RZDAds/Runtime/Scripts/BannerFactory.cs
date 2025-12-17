using Plugins.RZDAds.Runtime.Scripts.View;
using UnityEngine;

namespace Plugins.RZDAds.Runtime.Scripts
{
    public class BannerFactory
    {
        private const string VIEW_PATH = "AdBannerView";

        public AdBannerView Get()
        {
            var prefab = Resources.Load<AdBannerView>(VIEW_PATH);
            var obj = UnityEngine.Object.Instantiate(prefab);
            obj.gameObject.SetActive(false);
            return obj;
        }
    }
}