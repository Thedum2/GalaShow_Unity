﻿using System;

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
}
