mergeInto(LibraryManager.library, {
  // C#: [DllImport("__Internal")] extern void SendMessageToReact(string jsonMessage);
  SendMessageToReact: function (jsonPtr) {
    try {
      var json = UTF8ToString(jsonPtr); // C# string → JS string
      // "onUnityMessage"로 통일해서 React로 전달
      if (typeof window !== "undefined") {
        if (typeof window.dispatchReactUnityEvent === "function") {
          window.dispatchReactUnityEvent("onUnityMessage", json);
        } else {
          window.__reactBridgeQueue = window.__reactBridgeQueue || [];
          window.__reactBridgeQueue.push({ name: "onUnityMessage", payload: json });
        }
      }
    } catch (e) {
      console.warn("[ReactBridge] SendMessageToReact failed:", e);
    }
  },

  // C#: [DllImport("__Internal")] extern int IsReactBridgeReady();
  IsReactBridgeReady: function () {
    try {
      return (typeof window !== "undefined" &&
              typeof window.dispatchReactUnityEvent === "function") ? 1 : 0;
    } catch (e) {
      return 0;
    }
  },

  // C#: [DllImport("__Internal")] extern void InitializeReactBridge();
  InitializeReactBridge: function () {
    try {
      if (typeof window !== "undefined") {
        window.__reactBridgeQueue = window.__reactBridgeQueue || [];
      }
    } catch (e) {
      console.warn("[ReactBridge] InitializeReactBridge failed:", e);
    }
  },

  // C#: [DllImport("__Internal")] extern void InitializeReactBridgeRuntime();
  InitializeReactBridgeRuntime: function () {
    try {
      if (typeof window === "undefined") return;
      var q = window.__reactBridgeQueue;
      if (typeof window.dispatchReactUnityEvent === "function" &&
          Array.isArray(q) && q.length > 0) {
        var buf = q.slice();
        q.length = 0;
        for (var i = 0; i < buf.length; i++) {
          var evt = buf[i];
          try { window.dispatchReactUnityEvent(evt.name, evt.payload); }
          catch (e) { console.warn("[ReactBridge] flush failed:", e); }
        }
      }
    } catch (e) {
      console.warn("[ReactBridge] InitializeReactBridgeRuntime failed:", e);
    }
  },

  // 선택사항: 이름 지정 이벤트로 바로 쏘는 유틸
  DispatchReactEvent: function (namePtr, payloadPtr) {
    try {
      var name = UTF8ToString(namePtr);
      var payload = UTF8ToString(payloadPtr);
      if (typeof window !== "undefined") {
        if (typeof window.dispatchReactUnityEvent === "function") {
          window.dispatchReactUnityEvent(name, payload);
        } else {
          window.__reactBridgeQueue = window.__reactBridgeQueue || [];
          window.__reactBridgeQueue.push({ name: name, payload: payload });
        }
      }
    } catch (e) {
      console.warn("[ReactBridge] DispatchReactEvent failed:", e);
    }
  }
});
