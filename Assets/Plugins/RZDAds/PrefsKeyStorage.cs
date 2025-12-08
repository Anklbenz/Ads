using UnityEngine;

namespace Plugins.RZDAds
{
    public interface IKeyStorage
    {
        bool HasKey();
        string GetKey();
        void SetKey(string key);
    }
    public class PrefsKeyStorage : IKeyStorage
    {
        private const string LOCAL_KEY = "Ads_System_Key";

        public bool HasKey()
        {
            return PlayerPrefs.HasKey(LOCAL_KEY);
        }

        public string GetKey()
        {
            return HasKey()
                ? PlayerPrefs.GetString(LOCAL_KEY)
                : string.Empty;
        }

        public void SetKey(string key)
        {
            PlayerPrefs.SetString(LOCAL_KEY, key);
        }
    }
}