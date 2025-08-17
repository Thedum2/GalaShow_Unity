using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Galashow.Bridge;

namespace Galashow.Bridge
{
    public class BridgeTestComponent : MonoBehaviour
    {
        [ContextMenu("Test - Hello World")]
        public void HelloWorld()
        {
            MainHandler.Instance.HelloWorld("GIENE YEONI");
        }
    }
}