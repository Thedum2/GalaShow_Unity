﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using UnityCommunity.UnitySingleton;

namespace Galashow.Bridge
{
    public class BridgeManager : PersistentMonoSingleton<BridgeManager>
    {
        [Header("Settings")]
        [SerializeField] private float defaultTimeoutSeconds = 10f;
        [SerializeField] private string bridgeGameObjectName = "BridgeManager";

        private readonly Dictionary<string, IMessageHandler> _messageHandlers = new();
        private readonly Dictionary<string, PendingRequest> _pendingRequests = new();

        public static Action<Message> OnMessageReceived;
        public static Action<Message> OnMessageSent;

        private int _idIndex = 0;

        private IBridgeSender _sender;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            gameObject.name = bridgeGameObjectName;
            _sender = new BridgeSender(this);
            RegisterDefaultHandlers();
            StartCoroutine(CheckTimeouts());
            InitializeBridge();
        }

        private void InitializeBridge()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Util.Log("WebGL Bridge initialized");
#else
            Util.Log("Running in non-WebGL environment");
#endif
            MainHandler.Instance.Initialize();
        }

        #region Message Handler Registration

        public void RegisterHandler(IMessageHandler handler)
        {
            string route = handler.GetRoute();
            if (_messageHandlers.ContainsKey(route))
            {
                Util.Log($"Handler for route '{route}' already exists. Overwriting...");
            }

            _messageHandlers[route] = handler;

            if (handler is BaseMessageHandler baseHandler)
                baseHandler.__BindSender(_sender);

            Util.Log($"Registered handler for route: {route}");
        }

        public void UnregisterHandler(string route)
        {
            if (_messageHandlers.Remove(route))
            {
                Util.Log($"Unregistered handler for route: {route}");
            }
        }

        private void RegisterDefaultHandlers()
        {
            // 필요 시 기본 핸들러 등록
        }

        #endregion

        #region Message Receiving (React -> Unity)
        public void ReceiveMessage(string jsonMessage)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonMessage))
                {
                    Util.Log("Received empty message");
                    return;
                }

                var message = JsonConvert.DeserializeObject<Message>(jsonMessage);
                if (message == null)
                {
                    Util.Log("Failed to deserialize message");
                    return;
                }

                HandleIncomingMessage(message);
            }
            catch (Exception e)
            {
                Util.Log($"Failed to parse incoming message: {e.Message}\nRaw message: {jsonMessage}");
            }
        }
        private void HandleIncomingMessage(Message message)
        {
            string routeName = Util.ParseRoute(message.route).routeName;
            Util.Log($"Received {message.direction} message: {message.type} - {message.route}");
            OnMessageReceived?.Invoke(message);

            switch (message.type?.ToUpper())
            {
                case "REQ":
                    HandleRequest(routeName, message);
                    break;
                case "ACK":
                    HandleAcknowledge(routeName, message);
                    break;
                case "NTY":
                    HandleNotify(routeName, message);
                    break;
                default:
                    Util.Log($"[BridgeManager] Unknown message type: {message.type}");
                    break;
            }
        }
        private void HandleRequest(string route, Message message)
        {
            if (_messageHandlers.TryGetValue(route, out var handler))
            {
                handler.HandleRequest(
                    message,
                    onSuccess: (data) => SendAcknowledge(message.id, message.route, true, data),
                    onError: (error) => SendAcknowledge(message.id, message.route, false, null, error)
                );
            }
            else
            {
                string error = $"No handler found for route: {message.route}";
                Util.Log(error);
                SendAcknowledge(message.id, message.route, false, null, error);
            }
        }
        private void HandleAcknowledge(string route, Message message)
        {
            if (_pendingRequests.Remove(message.id, out var pending))
            {
                if (message.ok)
                {
                    pending.onSuccess?.Invoke(message);
                }
                else
                {
                    pending.onError?.Invoke(message.error ?? "Unknown error");
                }
            }
            else
            {
                Util.Log($"Received ACK for unknown request ID: {message.id}");
            }
        }
        private void HandleNotify(string route, Message message)
        {
            if (_messageHandlers.TryGetValue(route, out var handler))
            {
                handler.HandleNotify(message);
            }
            else
            {
                Util.Log($"No handler found for notification route: {message.route}");
            }
        }

        #endregion

        #region Message Sending (Unity -> React)
        
        private void SendRequestInternal(string route, object data = null,
            Action<Message> onSuccess = null,
            Action<string> onError = null,
            Action onTimeout = null,
            float timeoutSeconds = -1f)
        {
            if (timeoutSeconds < 0) timeoutSeconds = defaultTimeoutSeconds;

            var message = CreateMessage(MessageType.REQ, MessageDirection.U2R, route, data, true);

            _pendingRequests[message.id] = new PendingRequest
            {
                requestId = message.id,
                sentTime = DateTime.UtcNow,
                timeoutSeconds = timeoutSeconds,
                onSuccess = onSuccess,
                onError = onError,
                onTimeout = onTimeout
            };

            SendMessageToReactInternal(message);
        }
        private void SendNotifyInternal(string route, object data = null)
        {
            var message = CreateMessage(MessageType.NTY, MessageDirection.U2R, route, data, true);
            SendMessageToReactInternal(message);
        }
        private void SendAcknowledge(string requestId, string route, bool success, object data, string error = null)
        {
            var message = CreateMessage(MessageType.ACK, MessageDirection.U2R, route, data, success, requestId);
            if (!success) message.error = error;
            SendMessageToReactInternal(message);
        }
        private void SendMessageToReactInternal(Message message)
        {
            try
            {
                string json = JsonConvert.SerializeObject(message);
                WebGLBridge.Send(json);
                OnMessageSent?.Invoke(message);
            }
            catch (Exception e)
            {
                Util.Log($"Failed to send message to React: {e.Message}");
            }
        }

        #endregion

        #region Bridge Sender

        private sealed class BridgeSender : IBridgeSender
        {
            private readonly BridgeManager _mgr;
            public BridgeSender(BridgeManager mgr) => _mgr = mgr;

            public void Request(string route, object data = null,
                Action<Message> onSuccess = null,
                Action<string> onError = null,
                Action onTimeout = null,
                float timeoutSeconds = -1f)
            {
                _mgr.SendRequestInternal(route, data, onSuccess, onError, onTimeout, timeoutSeconds);
            }

            public void Notify(string route, object data = null)
            {
                _mgr.SendNotifyInternal(route, data);
            }
        }

        #endregion

        #region Utilities & State

        private Message CreateMessage(MessageType type, MessageDirection direction,
            string route, object data, bool ok, string customId = null)
        {
            string messageId = customId ?? GenerateMessageId(direction);

            return new Message
            {
                ok = ok,
                type = type.ToString(),
                route = route,
                id = messageId,
                data = data,
                direction = direction.ToString(),
                status = ok ? MessageStatus.success.ToString() : MessageStatus.error.ToString()
            };
        }

        private string GenerateMessageId(MessageDirection direction)
        {
            string prefix = direction == MessageDirection.U2R ? "u2r" : "r2u";
            string uuid = Guid.NewGuid().ToString();
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int currentId = _idIndex++;
            return $"{prefix}_{uuid}_{timestamp}_{currentId}";
        }

        private IEnumerator CheckTimeouts()
        {
            var waitForSeconds = new WaitForSeconds(1f);

            while (true)
            {
                yield return waitForSeconds;

                var currentTime = DateTime.UtcNow;
                var expiredRequests = _pendingRequests.Values
                    .Where(req => (currentTime - req.sentTime).TotalSeconds > req.timeoutSeconds)
                    .ToList();

                foreach (var expired in expiredRequests)
                {
                    if (_pendingRequests.Remove(expired.requestId, out _))
                    {
                        expired.onTimeout?.Invoke();
                        Util.Log($"Request timeout: {expired.requestId}");
                    }
                }
            }
        }

        #endregion

        #region Public API

        public T GetHandler<T>(string route) where T : class, IMessageHandler
        {
            return _messageHandlers.TryGetValue(route, out var h) ? h as T : null;
        }
        
        public bool IsConnected()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return WebGLBridge.IsReactBridgeReady();
#else
            return true;
#endif
        }

        public int GetPendingRequestCount() => _pendingRequests.Count;

        public void ClearAllPendingRequests()
        {
            var requests = _pendingRequests.Values.ToList();
            _pendingRequests.Clear();

            foreach (var req in requests)
            {
                try { req.onTimeout?.Invoke(); } catch { /* ignore */ }
            }
        }

        public bool HasHandler(string route) => _messageHandlers.ContainsKey(route);

        #endregion

        public override void ClearSingleton()
        {
            _messageHandlers.Clear();
            _pendingRequests.Clear();
            StopAllCoroutines();
        }
    }
}
