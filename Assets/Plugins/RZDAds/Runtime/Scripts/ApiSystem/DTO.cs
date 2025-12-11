using System;

namespace Plugins.RZDAds.ApiSystem
{
    [Serializable]
    public class Request
    {
        public string token;
        public string timestamp;
    }

    [Serializable]
    public class Response
    {
        public string status;
        public string message;
        public bool data;
    }


//--------------------Device-------------------------   
    [Serializable]
    public class DeviceRegisterRequest
    {
        public string udid;
        public string platform; // "ios" / "android"
        public string timestamp; // "YYYY-MM-DD HH:mm:ss"
    }

    [Serializable]
    public class DeviceRegisterResponse
    {
        public string status; // "success"
        public string token; // DEVICE_TOKEN
    }

//--------------------Settings-------------------------


    [Serializable]
    public class BannerResponse
    {
        public string status;
        public BannerData data;
    }

    [Serializable]
    public class BannerData
    {
        public Banner banner;
    }

    [Serializable]
    public class Banner
    {
        public uint id;
        public string title;
        public string description;
        public string link;
        public Settings[] settings;
        public Media[] media;
    }

    [Serializable]
    public class Settings
    {
        public int duration;
        public bool is_closable;
    }

    [Serializable]
    public class Media
    {
        public string url;
        public string type;
        public string mime_type;
    }

//--------------------Events-------------------------
    [Serializable]
    public class EventRequest
    {
        public string token;
        public string timestamp;
        public string action;
        public uint banner_id;
        public float duration;
    }

    [Serializable]
    public class EncryptedRequest
    {
        public string key; // RSA-OAEP(SHA-256) AES key (base64)
        public string iv; // AES-GCM IV 12 bytes (base64)
        public string data; // AES ciphertext + tag (base64)
    }
}