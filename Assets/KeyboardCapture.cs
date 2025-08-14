using UnityEngine;

public class KeyboardCapture : MonoBehaviour
{
    void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
#endif
    }

    // React에서 토글하고 싶을 때를 위해 공개 메서드도 추가
    public void SetCapture(bool capture)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = capture;
#endif
    }
}
