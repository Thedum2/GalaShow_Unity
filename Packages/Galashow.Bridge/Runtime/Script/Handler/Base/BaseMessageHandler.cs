using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Galashow.Bridge
{
    public abstract class BaseMessageHandler : IMessageHandler
    {
        private string route;

        protected BaseMessageHandler(string route)
        {
            this.route = route;
        }

        public virtual string GetRoute() => route;

        public abstract void HandleRequest(Message message, Action<object> onSuccess, Action<string> onError);

        public virtual void HandleNotify(Message message)
        {
            // Do nothing by default.
        }
    }
}