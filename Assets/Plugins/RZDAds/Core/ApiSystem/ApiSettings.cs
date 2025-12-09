using UnityEngine;

namespace Plugins.RZDAds.ApiSystem {
	[CreateAssetMenu(fileName = "ApiSettings", menuName = "Configs/ApiSettings")]
	public class ApiSettings : ScriptableObject {
		[SerializeField] private bool isLogEnabled;
		
		[SerializeField] private string apiKey;
		[SerializeField] private string hostUrl;

		[Header("Handlers Urls")]
		[SerializeField] private string registerDevice;
		[SerializeField] private string checkReady;
		[SerializeField] private string getBanner;
		[SerializeField] private string sendEvent;
		public string AppKey => apiKey;

		public string RegisterDeviceUrl => GetUrl(registerDevice);
		public string CheckReadyUrl => GetUrl(checkReady);
		public string GetBannerUrl => GetUrl(getBanner);
		public string SendEventsUrl => GetUrl(sendEvent);
		private string GetUrl(string apiPath) {
			if (string.IsNullOrWhiteSpace(hostUrl))
				Debug.LogWarning("ApiSettings.hostUrl is empty!");

			return WebUtils.CombineUrl(hostUrl, apiPath);
		}
	}
}
