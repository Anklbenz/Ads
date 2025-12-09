using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Plugins.RZDAds.Core.View
{
    public class AdBannerView : MonoBehaviour
    {
        [SerializeField] private Button openAdButton;
        [SerializeField] private Button closeButton;
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

            gameObject.SetActive(true);
            SetupContent(banner);

            timer.gameObject.SetActive(true);
            closeButton.gameObject.SetActive(false);

            var timerTask = UpdateProgress(banner.Duration);

            var firstCompletedTaskIndex = await UniTask.WhenAny(timerTask, _tcs.Task);

            bool clickOrClose;

            if (firstCompletedTaskIndex == 0)
            {
                timer.gameObject.SetActive(false);
                closeButton.gameObject.SetActive(true);

                clickOrClose = await _tcs.Task;
            }
            else
            {
                clickOrClose = true;
            }

            Hide();
            return clickOrClose;
        }

        private void Hide()
        {
            gameObject.SetActive(false);
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
                _tcs.TrySetResult(false);
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
                //Если кликнули, завершить таймер 
                if (_tcs.Task.Status == UniTaskStatus.Succeeded)
                    break;

                elapsed += Time.unscaledDeltaTime;
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