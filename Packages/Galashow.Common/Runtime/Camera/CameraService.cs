using Galashow.Core;
using UnityEngine;

namespace Galashow.Common
{
    public class CameraService : MonoBehaviour  
    {
        public void PrepareForRound(object round)
        {
            GLog.Debug("[Camera] Preparing for round");
        }

        public void PlayShot(string preset)
        {
            GLog.Info($"[Camera] Shot: {preset}");
        }

        public void Fade(float seconds, bool fadeIn)
        {
            GLog.Debug($"[Camera] Fade {(fadeIn ? "in" : "out")} {seconds}s");
        }

        public void Shake(float amplitude, float duration)
        {
            GLog.Debug($"[Camera] Shake amp={amplitude} dur={duration}");
        }
    }
}