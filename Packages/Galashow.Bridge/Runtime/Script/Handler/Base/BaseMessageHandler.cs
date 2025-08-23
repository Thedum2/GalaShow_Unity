﻿using System;

namespace Galashow.Bridge
{
    public interface IMessageHandler
    {
        string GetRoute();
        void HandleRequest(Message message, Action<object> onSuccess, Action<string> onError);
        void HandleNotify(Message message);
        
    }
    public abstract class BaseMessageHandler : IMessageHandler
    {
        private readonly string _route;
        private IBridgeSender _bridge;
        
        protected BaseMessageHandler(string route)
        {
            _route = route;
        }

        public virtual string GetRoute() => _route;
        internal void __BindSender(IBridgeSender sender) => _bridge = sender;
        protected void REQ(string action, object data = null,
            Action<Message> onSuccess = null, Action<string> onError = null,
            Action onTimeout = null, float timeoutSeconds = -1f)
        {
            _bridge?.Request(BuildRoute(action), data, onSuccess, onError, onTimeout, timeoutSeconds);
        }
        protected void NTY(string action, object data = null)
        {
            _bridge?.Notify(BuildRoute(action), data);
        }
        protected virtual string BuildRoute(string action)
            => string.IsNullOrEmpty(action) ? GetRoute() : $"{GetRoute()}_{action}";

        public virtual void HandleRequest(Message message, Action<object> onSuccess, Action<string> onError)
        {
            
        }

        public virtual void HandleNotify(Message message)
        {
            
        }
    }
}