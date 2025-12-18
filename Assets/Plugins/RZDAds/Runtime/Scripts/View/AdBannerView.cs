using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Plugins.RZDAds.Runtime.Scripts.View
{
    public class AdBannerView : MonoBehaviour
    {
        [SerializeField] private Button openAdButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Timer timer;
        [SerializeField] private VideoDisplay videoDisplay;
        [SerializeField] private ImageDisplay imageDisplay;

        private UniTaskCompletionSource _tcs;
        private CancellationTokenSource _cts;
        private Dictionary<string, IDisplay> _displayMap;
        private IDisplay _currentDisplay;
        
        private bool _clicked;
        private string _bannerUrl;


        public async UniTask<bool> Show(BannerContent banner)
        {
            _tcs = new UniTaskCompletionSource();
            _cts = new CancellationTokenSource();
            _clicked = false;
            _bannerUrl = banner.Url;
           
            //Открываем
            gameObject.SetActive(true);

            //Настраиваем контент согласно пришедшим данным
             var contentOk = await SetupContent(banner);
             if (!contentOk) 
             {
	             Hide();
	             return false;
             }

             timer.Show();
            closeButton.gameObject.SetActive(false);

            //Таск таймера
            try
            {
                await UpdateProgress(banner.Duration, _cts.Token);
            }
            catch (OperationCanceledException) { }

            timer.Hide();
            closeButton.gameObject.SetActive(true);
            
            //Таск нажатия на крестик
            await _tcs.Task;

            Hide();
            return _clicked;
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
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async UniTask<bool> SetupContent(BannerContent banner)
        {
            if (!_displayMap.TryGetValue(banner.Type, out IDisplay display))
            {
                Debug.LogError($"[View] Cannot show banner: unknown type {banner.Type}");
                return false;
            }

            _currentDisplay = display;
            _currentDisplay.Open();
	        var setResult = await _currentDisplay.TrySet(banner);

	        if (!setResult)
		        return false;

            description.text = banner.Description;

            timer.SetProgress(0f);
            timer.SetSeconds(banner.Duration);
            return true;
        }

        private async UniTask UpdateProgress(float duration, CancellationToken token)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();
                
                elapsed += Time.unscaledDeltaTime;
                timer.SetProgress(Mathf.Clamp01(elapsed / duration));
                timer.SetSeconds(Mathf.CeilToInt(duration - elapsed));

                await UniTask.Yield();
            }

            timer.SetProgress(1f);
            timer.SetSeconds(0);
        }


        private void OnClose()
            => _tcs?.TrySetResult();

        private void OnAdClick()
        {
            _clicked = true;
            Application.OpenURL(_bannerUrl);
        }

        private void OnEnable()
        {
            //Придурки в играх бывают подписываться на все кнопки в FindObjectByType
            closeButton.onClick.RemoveAllListeners();
            openAdButton.onClick.RemoveAllListeners();
            //
            closeButton.onClick.AddListener(OnClose);
            openAdButton.onClick.AddListener(OnAdClick);
        }

        private void OnDisable()
        {
            closeButton.onClick.RemoveListener(OnClose);
            openAdButton.onClick.RemoveListener(OnAdClick);
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