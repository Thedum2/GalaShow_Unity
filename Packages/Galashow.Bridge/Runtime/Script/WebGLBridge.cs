using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using Galashow.Core;

namespace Galashow.Bridge
{
    public static class WebGLBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] public static extern void SendMessageToReact(string jsonMessage);
        [DllImport("__Internal")] public static extern int IsReactBridgeReady();
        [DllImport("__Internal")] public static extern void InitializeReactBridge();
        [DllImport("__Internal")] public static extern void InitializeReactBridgeRuntime();
#else
        
        public static void SendMessageToReact(string jsonMessage)
        {
            GLog.Debug($"[MESSAGE - R2U] {jsonMessage}");
        }

        public static int IsReactBridgeReady()
        {
            GLog.Debug($"[WebGLBridge] Send to React");
            return 1;
        }

        public static void InitializeReactBridge()
        {
            GLog.Debug("[WebGLBridge] InitializeReactBridge()");
        }

        public static void InitializeReactBridgeRuntime()
        {
            GLog.Debug("[WebGLBridge] InitializeReactBridgeRuntime()");
        }
#endif

        /// <summary>
        /// JSLib 초기화 시퀀스 호출(런타임 → 브릿지)
        /// </summary>
        public static void Init()
        {
            try
            {
                InitializeReactBridgeRuntime();
                InitializeReactBridge();
                GLog.Debug("[WebGLBridge] Initialization invoked (Runtime -> Bridge).");
            }
            catch (Exception e)
            {
                GLog.Debug($"[WebGLBridge] Init failed: {e}");
            }
        }

        public static void Send(Message message)
        {
            if (message == null)
            {
                GLog.Debug("[WebGLBridge] Cannot send null message to React");
                return;
            }

            try
            {
                string json = JsonConvert.SerializeObject(message);
                Send(json);
            }
            catch (Exception e)
            {
                GLog.Debug($"[WebGLBridge] Serialization failed: {e}");
            }
        }

        public static void Send(string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage))
            {
                GLog.Debug("[WebGLBridge] Cannot send empty message to React");
                return;
            }

            try
            {
                SendMessageToReact(jsonMessage);
#if !UNITY_WEBGL || UNITY_EDITOR
                GLog.Debug($"[MESSAGE - U2R] {jsonMessage}");
#endif
            }
            catch (Exception e)
            {
                GLog.Debug($"[WebGLBridge] Native send failed: {e}");
            }
        }
    }
}
