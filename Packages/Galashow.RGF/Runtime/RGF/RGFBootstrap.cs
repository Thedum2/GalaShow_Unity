using Galashow.Bridge;
using Galashow.Core;
using UnityEngine;
using UnityEngine.Scripting;

namespace Galashow.RGF
{
    /// <summary>
    /// 씬 구성과 상관없이 React 연동에 필요한 객체를 만든다.
    /// BridgeManager(React SendMessage 대상), RGFManager, RGFBridgeAdapter(RGF 라우트 처리)
    /// </summary>
    [Preserve]
    public static class RGFBootstrap
    {
        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            _ = BridgeManager.Instance;
            _ = RGFManager.Instance;
            _ = RGFBridgeAdapter.Instance;

#if UNITY_WEBGL && !UNITY_EDITOR
            // 키보드를 Unity가 독점하지 않게 한다 (React 입력창·단축키 사용)
            WebGLInput.captureAllKeyboardInput = false;
#endif
            GLog.Info("[RGF] Bootstrap ready");
        }
    }
}
