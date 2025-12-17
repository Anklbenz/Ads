namespace Plugins.RZDAds.Runtime.Scripts
{
    public interface ILogger
    {
        void Log(string message);
    }

    public class UnityLogger : ILogger
    {
        public void Log(string message)
            => UnityEngine.Debug.Log(message);
    }
}