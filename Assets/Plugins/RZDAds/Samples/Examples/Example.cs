
using Cysharp.Threading.Tasks;
using Plugins.RZDAds.Core;
using Plugins.RZDAds.Runtime.Scripts;
using UnityEngine;
using UnityEngine.UI;

public class Example : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private string appKey;

    private void Awake()
    {
        AdService.Initialize(appKey, true).Forget();
        button.onClick.AddListener(Show);
    }

    private void Show()
    {
        AdService.RequestShowAd().Forget();
    }

    public void OnDestroy()
    {
        button.onClick.RemoveListener(Show);
    }
}