
using Cysharp.Threading.Tasks;
using Plugins.RZDAds;
using UnityEngine;
using UnityEngine.UI;

public class Example : MonoBehaviour
{
    [SerializeField] private Button button;

    private void Awake()
    {
        AdService.Initialize().Forget();
        button.onClick.AddListener(Show);
    }

    private void Show()
    {
        AdService.Show().Forget();
    }

    public void OnDestroy()
    {
        button.onClick.RemoveListener(Show);
    }
}