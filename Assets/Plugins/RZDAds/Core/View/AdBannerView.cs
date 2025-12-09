using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Plugins.RZDAds.Core.View
{
    public class AdBannerView : MonoBehaviour
    {
        [SerializeField] private GameObject content;

        [SerializeField] private Button closeButton;
        [SerializeField] private Button openAdButton;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Timer timer;
        [SerializeField] private VideoDisplay videoDisplay;
        [SerializeField] private ImageDisplay imageDisplay;

        private UniTaskCompletionSource<bool> _tcs;
        private Dictionary<string, IDisplay> _displayMap;
        private IDisplay _currentDisplay;

        public async UniTask<bool> Show(BannerContent banner)
        {
            _tcs = new UniTaskCompletionSource<bool>();

            SetupContent(banner);

            content.SetActive(true);

            timer.gameObject.SetActive(true);
            closeButton.gameObject.SetActive(false);

            await UpdateProgress(banner.Duration);

            timer.gameObject.SetActive(false);
            closeButton.gameObject.SetActive(true);

            var isClickOnAd = await _tcs.Task;

            Hide();

            return isClickOnAd;
        }

        private void Hide()
        {
            content.SetActive(false);
            foreach (var display in _displayMap.Values)
            {
                display.Clear();
                display.Close();
            }

            _currentDisplay = null;
        }

        private void SetupContent(BannerContent banner)
        {
            if (!_displayMap.TryGetValue(banner.Type, out IDisplay display))
            {
                Debug.LogError($"[View] Unknown banner type: {banner.Type}");
                return;
            }

            _currentDisplay = display;
            _currentDisplay.Open();
            _currentDisplay.Set(banner);

            description.text = banner.Description;

            timer.SetProgress(0f);
            timer.SetSeconds(banner.Duration);
        }

        private async UniTask UpdateProgress(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                timer.SetProgress(Mathf.Clamp01(elapsed / duration));
                timer.SetSeconds(Mathf.CeilToInt(duration - elapsed));

                await UniTask.Yield();
            }

            timer.SetProgress(1f);
            timer.SetSeconds(0);
        }


        private void OnClose()
            => _tcs.TrySetResult(false);

        private void OnAdOpen()
            => _tcs.TrySetResult(true);

        private void OnEnable()
        {
            closeButton.onClick.AddListener(OnClose);
            openAdButton.onClick.AddListener(OnAdOpen);
        }

        private void OnDisable()
        {
            closeButton.onClick.RemoveListener(OnClose);
            openAdButton.onClick.RemoveListener(OnAdOpen);
        }

        private void Awake()
        {
            _displayMap = new Dictionary<string, IDisplay>()
            {
                ["image"] = imageDisplay,
                ["video"] = videoDisplay,
            };
        }
    }
}