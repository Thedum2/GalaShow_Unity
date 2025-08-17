using System;

namespace Galashow.Bridge
{
    public interface IMessageHandler
    {
        string GetRoute();
        void HandleRequest(Message message, Action<object> onSuccess, Action<string> onError);
        void HandleNotify(Message message);
    }
}