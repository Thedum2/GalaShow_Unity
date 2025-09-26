namespace Galashow.Core
{
    public class KeyboardCapture : PersistentMonoSingleton<KeyboardCapture>
    {
        bool _capturing = true;

        void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = _capturing;
#endif
        }

        public void SetCapture(bool capture)
        {
            _capturing = capture;
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = _capturing;
#endif
        }
    }
}