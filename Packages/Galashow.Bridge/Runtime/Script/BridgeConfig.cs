using System;

namespace Galashow.Bridge
{
    [Serializable]
    public enum MessageType
    {
        REQ,
        ACK,
        NTY
    }

    [Serializable]
    public enum MessageDirection
    {
        R2U,
        U2R
    }

    [Serializable]
    public enum MessageStatus
    {
        success,
        error,
        timeout,
        pending
    }

    [Serializable]
    public class Message
    {
        public bool ok;
        public string type;
        public string route;
        public string id;
        public object data;
        public long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public string direction;
        public string status;
        public string error;
    }

    [Serializable]
    public class PendingRequest
    {
        public string requestId;
        public DateTime sentTime;
        public float timeoutSeconds;
        public Action<Message> onSuccess;
        public Action<string> onError;
        public Action onTimeout;
    }
    
    [Serializable]
    public class BrowserInfo
    {
        public string userAgent = "";
        public string platform = "";
        public string language = "";
        public bool cookieEnabled = false;
        public bool onLine = false;
        public ScreenInfo screen = new ScreenInfo();
        public WindowInfo window = new WindowInfo();
        public DocumentInfo document = new DocumentInfo();
    }

    [Serializable]
    public class ScreenInfo
    {
        public int width = 0;
        public int height = 0;
        public int availWidth = 0;
        public int availHeight = 0;
    }

    [Serializable]
    public class WindowInfo
    {
        public int innerWidth = 0;
        public int innerHeight = 0;
        public int outerWidth = 0;
        public int outerHeight = 0;
    }

    [Serializable]
    public class DocumentInfo
    {
        public string title = "";
        public string url = "";
        public string domain = "";
    }

    [Serializable]
    public class DeviceInfo
    {
        public bool isMobile = false;
        public bool isTablet = false;
        public bool isDesktop = false;
        public bool hasTouch = false;
        public string battery = "not_supported";
        public string vibration = "not_supported";
        public string geolocation = "not_supported";
        public string camera = "not_supported";
    }
}