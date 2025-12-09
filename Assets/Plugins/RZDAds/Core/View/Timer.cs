using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Plugins.RZDAds
{
    public class Timer : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private TMP_Text text;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetProgress(float progress)
        {
            image.fillAmount = progress;
        }

        public void SetSeconds(int seconds)
        {
            text.text = seconds.ToString();
        }
    }
}