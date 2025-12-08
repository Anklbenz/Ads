using UnityEngine;

namespace Plugins.RZDAds
{
    public interface IDeviceIdProvider
    {
        string GetDeviceId();
    }

    public class DeviceIdService : IDeviceIdProvider
    {
        private string _cachedId;
        private const string LOCAL_KEY = "local_device_uuid";

        public string GetDeviceId()
        {
            if (!string.IsNullOrEmpty(_cachedId))
                return _cachedId;

            // Платформенные ID
#if UNITY_IOS
            var idfv = GetIdfv();
            if (!string.IsNullOrEmpty(idfv))
                return _cachedId = idfv;
#endif

#if UNITY_ANDROID
            var gaid = GetGaid();
            if (!string.IsNullOrEmpty(gaid))
                return _cachedId = gaid;
#endif

            // Фоллбек: локальный UUID
            if (!PlayerPrefs.HasKey(LOCAL_KEY))
            {
                PlayerPrefs.SetString(LOCAL_KEY, System.Guid.NewGuid().ToString());
                PlayerPrefs.Save();
            }

            return _cachedId = PlayerPrefs.GetString(LOCAL_KEY);
        }

        // ----------- iOS --------------

        public string GetIdfv()
        {
#if UNITY_IOS
            return UnityEngine.iOS.Device.vendorIdentifier;
#else
            return null;
#endif
        }

        public string GetIdfa()
        {
#if UNITY_IOS
            return UnityEngine.iOS.Device.advertisingIdentifier;
#else
            return null;
#endif
        }

        // ----------- Android -----------

        public string GetGaid()
        {
#if UNITY_ANDROID
            string gaid = null;

            try
            {
                GooglePlayServices_AdvertisingId(out gaid);
            }
            catch
            {
                gaid = null;
            }

            return gaid;
#else
            return null;
#endif
        }

#if UNITY_ANDROID
        private void GooglePlayServices_AdvertisingId(out string adId)
        {
            adId = null;

            AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaClass advertisingIdClient =
                new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
            AndroidJavaObject adInfo =
                advertisingIdClient.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);

            adId = adInfo.Call<string>("getId");
        }
#endif
    }
}