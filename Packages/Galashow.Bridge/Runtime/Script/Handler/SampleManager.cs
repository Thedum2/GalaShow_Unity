using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Galashow.Bridge
{
    public class SampleManager : BaseMessageHandler
    {
        public SampleManager() : base("SampleManager") { }

        public override void HandleRequest(Message message, Action<object> onSuccess, Action<string> onError)
        {
            
        }

        public override void HandleNotify(Message message)
        {
            
        }
        
    }
}