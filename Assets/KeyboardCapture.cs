using UnityCommunity.UnitySingleton;
using UnityEngine;

public class KeyboardCapture : PersistentMonoSingleton<KeyboardCapture>
{
    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
#endif
    }
    public void SetCapture(bool capture)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = capture;
#endif
    }
}
