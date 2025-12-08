using System.Collections;
using UnityEngine;

	public class GUILog : MonoBehaviour {
		[SerializeField] private int fontSize = 17, messagesCount = 100;
		[SerializeField] private Color color = Color.yellow;

		private readonly Queue _myLogQueue = new Queue();
		private GUIStyle _guiStyle;

		private void Awake() {
			_guiStyle = CreateStyle();
		}

		private void HandleLog(string logString, string stackTrace, LogType type) {
			_myLogQueue.Enqueue("[" + global::System.DateTime.Now.ToString("HH:mm:ss") + "]" + "[" + type + "] : " + logString);

			if (type == LogType.Exception)
				_myLogQueue.Enqueue(stackTrace);

			while (_myLogQueue.Count > messagesCount)
				_myLogQueue.Dequeue();
		}

		private GUIStyle CreateStyle() {
			return new GUIStyle {
					fontSize = fontSize,
					normal = {textColor = color}
			};
		}

		private void OnEnable() {
			Application.logMessageReceived += HandleLog;
			Debug.Log("Started up logging.");
		}

		private void OnDisable() {
			Application.logMessageReceived -= HandleLog;
		}
		private void OnGUI() {
			GUILayout.BeginArea(new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height));
			GUILayout.Label("\n" + string.Join("\n", _myLogQueue.ToArray()), _guiStyle);
			GUILayout.EndArea();
		}
	}
