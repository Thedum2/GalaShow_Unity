mergeInto(LibraryManager.library, {
    
    // React로 메시지 전송 (Unity -> React)
    SendMessageToReact: function(jsonMessagePtr) {
        var jsonMessage = UTF8ToString(jsonMessagePtr);
        
        try {
            console.log('[Unity-React] Sending message to React:', jsonMessage);
            
            if (typeof window !== 'undefined') {
                if (window.ReactNativeWebView) {
                    // React Native WebView 환경
                    window.ReactNativeWebView.postMessage(JSON.stringify({
                        type: 'unity-message',
                        data: jsonMessage
                    }));
                } else if (window.parent !== window) {
                    // iframe 환경
                    window.parent.postMessage({
                        type: 'unity-message',
                        data: jsonMessage
                    }, '*');
                } else {
                    // 일반 웹 환경
                    var event = new CustomEvent('unity-message', {
                        detail: { data: jsonMessage }
                    });
                    window.dispatchEvent(event);
                    
                    // react-unity-webgl 라이브러리 호환
                    if (window.unityMessageHandler) {
                        window.unityMessageHandler(jsonMessage);
                    }
                }
            }
            
        } catch (e) {
            console.error('[Unity-React] Failed to send message to React:', e);
        }
    },
    
    IsReactBridgeReady: function() {
        if (typeof window === 'undefined') return 0;
        
        return (
            window.ReactNativeWebView || 
            window.parent !== window || 
            window.unityMessageHandler ||
            document.querySelector('[data-react-app]') !== null
        ) ? 1 : 0;
    },
    
    // React와의 브리지 초기화
    InitializeReactBridge: function() {
        console.log("[Unity-React] Initializing React Bridge...");
        
        if (typeof window === 'undefined') {
            console.warn('[Unity-React] Window object not available');
            return;
        }
        
        // React에서 Unity로 메시지를 보내는 전역 함수
        window.sendMessageToUnity = function(jsonMessage) {
            try {
                console.log('[Unity-React] Received message from React:', jsonMessage);
                // Unity의 BridgeManager.ReceiveMessage 호출
                if (typeof SendMessage !== 'undefined') {
                    SendMessage('BridgeManager', 'ReceiveMessage', jsonMessage);
                }
            } catch (e) {
                console.error('[Unity-React] Failed to send message to Unity:', e);
            }
        };
        
        // React에서 사용할 수 있는 유틸리티 함수들
        window.unityBridge = {
            // React에서 Unity로 요청 전송
            sendRequest: function(route, data, callback) {
                var message = {
                    ok: true,
                    type: 'REQ',
                    route: route,
                    id: 'r2u_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9),
                    data: data,
                    timestamp: Date.now(),
                    direction: 'R2U',
                    status: 'pending'
                };
                
                if (callback) {
                    // 응답 대기 등록
                    window.unityResponseCallbacks = window.unityResponseCallbacks || {};
                    window.unityResponseCallbacks[message.id] = callback;
                }
                
                window.sendMessageToUnity(JSON.stringify(message));
                return message.id;
            },
            
            // React에서 Unity로 알림 전송
            sendNotification: function(route, data) {
                var message = {
                    ok: true,
                    type: 'NTY',
                    route: route,
                    id: 'r2u_nty_' + Date.now(),
                    data: data,
                    timestamp: Date.now(),
                    direction: 'R2U',
                    status: 'success'
                };
                
                window.sendMessageToUnity(JSON.stringify(message));
            },
            
            // Unity의 상태 확인
            isUnityReady: function() {
                return typeof SendMessage !== 'undefined' && window.unityInstance !== null;
            }
        };
        
        // Unity 메시지 수신 핸들러 설정
        window.addEventListener('unity-message', function(event) {
            try {
                var messageData = JSON.parse(event.detail.data);
                console.log('[Unity-React] Processing Unity message:', messageData);
                
                // ACK 메시지 처리 (요청에 대한 응답)
                if (messageData.type === 'ACK' && window.unityResponseCallbacks) {
                    var callback = window.unityResponseCallbacks[messageData.id];
                    if (callback) {
                        callback(messageData);
                        delete window.unityResponseCallbacks[messageData.id];
                    }
                }
                
                // 일반적인 메시지 브로드캐스트
                var broadcastEvent = new CustomEvent('unity-bridge-message', {
                    detail: messageData
                });
                window.dispatchEvent(broadcastEvent);
                
            } catch (e) {
                console.error('[Unity-React] Failed to process Unity message:', e);
            }
        });
        
        // PostMessage 수신 처리 (iframe 환경용)
        window.addEventListener('message', function(event) {
            if (event.data && event.data.type === 'react-to-unity') {
                window.sendMessageToUnity(event.data.message);
            }
        });
        
        console.log("[Unity-React] React Bridge initialized successfully");
        
        // 초기화 완료 이벤트 발송
        var initEvent = new CustomEvent('unity-bridge-ready');
        window.dispatchEvent(initEvent);
    },
    
    // 런타임 초기화 함수 (DOM 로드 후 호출)
    InitializeReactBridgeRuntime: function() {
        if (typeof window === 'undefined') return;
        
        console.log('[Unity-React] DOM Content Loaded - Runtime Init');
        
        // Unity 로드 완료 대기 후 브리지 초기화
        function checkUnityLoaded() {
            if (typeof SendMessage !== 'undefined') {
                console.log('[Unity-React] Unity loaded, initializing bridge...');
                
                // Unity에서 직접 초기화하도록 알림
                var initEvent = new CustomEvent('unity-loaded');
                window.dispatchEvent(initEvent);
                
            } else {
                setTimeout(checkUnityLoaded, 100);
            }
        }
        
        setTimeout(checkUnityLoaded, 500);
        
        // Unity Instance 설정 지원
        window.setUnityInstance = function(instance) {
            window.unityInstance = instance;
            console.log('[Unity-React] Unity instance set:', instance);
        };
    }
});