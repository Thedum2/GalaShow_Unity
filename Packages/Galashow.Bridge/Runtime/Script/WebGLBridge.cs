using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace Galashow.Bridge
{
    public static class WebGLBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        public static extern void SendMessageToReact(string jsonMessage);

        [DllImport("__Internal")]
        public static extern int IsReactBridgeReady();

        [DllImport("__Internal")]
        public static extern void InitializeReactBridge();

        [DllImport("__Internal")]
        public static extern void InitializeReactBridgeRuntime();
#else
        // Editor/Non-WebGL 스텁
        public static void SendMessageToReact(string jsonMessage)
        {
            Debug.Log($"[WebGLBridge:Editor] Send to React: {jsonMessage}");
        }

        public static int IsReactBridgeReady() => 1;

        public static void InitializeReactBridge()
        {
            Debug.Log("[WebGLBridge:Editor] InitializeReactBridge()");
        }

        public static void InitializeReactBridgeRuntime()
        {
            Debug.Log("[WebGLBridge:Editor] InitializeReactBridgeRuntime()");
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
                Util.Log("[WebGLBridge] Initialization invoked (Runtime -> Bridge).");
            }
            catch (Exception e)
            {
                Util.LogError($"[WebGLBridge] Init failed: {e}");
            }
        }

        /// <summary>
        /// 브릿지 준비 여부 (IsReactBridgeReady() != 0)
        /// </summary>
        public static bool IsReady()
        {
            try
            {
                return IsReactBridgeReady() != 0;
            }
            catch (Exception e)
            {
                Util.LogError($"[WebGLBridge] IsReady check failed: {e}");
                return false;
            }
        }

        public static void Send(Message message)
        {
            if (message == null)
            {
                Util.Log("[WebGLBridge] Cannot send null message to React");
                return;
            }

            try
            {
                string json = JsonConvert.SerializeObject(message);
                Send(json);
            }
            catch (Exception e)
            {
                Util.LogError($"[WebGLBridge] Serialization failed: {e}");
            }
        }

        public static void Send(string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage))
            {
                Util.Log("[WebGLBridge] Cannot send empty message to React");
                return;
            }

            try
            {
                SendMessageToReact(jsonMessage);
#if !UNITY_WEBGL || UNITY_EDITOR
                Debug.Log($"[WebGLBridge:Editor] {jsonMessage}");
#endif
            }
            catch (Exception e)
            {
                Util.LogError($"[WebGLBridge] Native send failed: {e}");
            }
        }
    }
}
