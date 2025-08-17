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

#endif

        public static void SendToReact(string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage))
            {
                Util.Log("[WebGLBridge] Cannot send empty message to React");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                SendMessageToReact(jsonMessage);
                Util.Log($"[WebGLBridge] Message sent to React: {jsonMessage.Substring(0, Math.Min(100, jsonMessage.Length))}...");
            }
            catch (Exception e)
            {
                Util.LogError($"[WebGLBridge] Failed to send message to React: {e.Message}");
            }
#else
            Util.Log($"[WebGLBridge] Would send to React: {jsonMessage}");
#endif
        }

        public static void Initialize()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                InitializeReactBridge();
                InitializeReactBridgeRuntime();
                Util.Log("[WebGLBridge] React bridge initialized successfully");
            }
            catch (Exception e)
            {
                Util.LogError($"[WebGLBridge] Failed to initialize: {e.Message}");
            }
#else
            Util.Log("[WebGLBridge] Initialize called (not in WebGL build)");
#endif
        }

        public static bool IsBridgeReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                return IsReactBridgeReady() == 1;
            }
            catch (Exception e)
            {
                Util.LogError($"[WebGLBridge] Connection check failed: {e.Message}");
                return false;
            }
#else
            return false;
#endif
        }

        public class WebGLBridgeInitializer : MonoBehaviour
        {
            [Header("Auto Initialize")] [SerializeField]
            private bool autoInitialize = true;

            [SerializeField] private float initializeDelay = 1f;

            void Start()
            {
                if (autoInitialize)
                {
                    Invoke(nameof(InitializeBridge), initializeDelay);
                }
            }

            public void InitializeBridge()
            {
                WebGLBridge.Initialize();
            }
        }
    }
}
