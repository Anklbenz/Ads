using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Plugins.RZDAds.ApiSystem {
	public class WebRequestJson {
		private const string REQUEST_CONTENT_TYPE = "application/json";
		private const string INSOMNIA = "insomnia/11.2.0";
		public bool hasError { get; private set; }
		public string errorDescription { get; private set; }
		public long responseCode { get; private set; }
		public string authorKey { get; set; } = string.Empty;

		public async UniTask<string> Request(RequestArgs requestArgs) {
			responseCode = (int)HttpStatus.ReadyToReceive;

			using var webRequest = CreateWebRequest(requestArgs);

			webRequest.SetRequestHeader("Content-Type", REQUEST_CONTENT_TYPE);
			webRequest.SetRequestHeader("User-Agent", INSOMNIA);
			webRequest.SetRequestHeader("X-Game-Token", authorKey);
			webRequest.certificateHandler = new ForceAcceptAllCertificates();

			var operation = webRequest.SendWebRequest();

			while (!operation.isDone) {
				requestArgs.Progress?.Report(webRequest.downloadProgress);
				await UniTask.Yield();
			}
			requestArgs.Progress?.Report(1f);

			responseCode = webRequest.responseCode;
			hasError = webRequest.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError;
			errorDescription = webRequest.error;

			return webRequest.downloadHandler.text;
		}

		private UnityWebRequest CreateWebRequest(RequestArgs argParams) {
			if (argParams.FormData != null)
				return UnityWebRequest.Post(argParams.url, argParams.FormData);


			var request = new UnityWebRequest(argParams.url, argParams.method.ToString()) {
					downloadHandler = new DownloadHandlerBuffer()
			};

			if (string.IsNullOrEmpty(argParams.jsonBody))
				return request;

			request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(argParams.jsonBody));
			request.SetRequestHeader("Content-Type", REQUEST_CONTENT_TYPE);

			return request;
		}
	}
}
