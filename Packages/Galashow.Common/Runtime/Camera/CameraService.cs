using Galashow.Core;
using UnityEngine;

namespace Galashow.Common
{
    public class CameraService : MonoBehaviour  
    {
        public void PrepareForRound(object round)
        {
            // 시네머신/프리셋 복구/초기화 등
            GLog.Debug("Camera PrepareForRound", tag: nameof(CameraService));
        }

        public void PlayShot(string preset)
        {
            GLog.Info($"Camera Shot: {preset}", tag: nameof(CameraService));
            // TODO: Cinemachine blending, Timeline 등
        }

        public void Fade(float seconds, bool fadeIn)
        {
            GLog.Debug($"Camera Fade {(fadeIn ? "In" : "Out")} {seconds}s", tag: nameof(CameraService));
            // TODO: Fullscreen overlay / post exposure
        }

        public void Shake(float amplitude, float duration)
        {
            GLog.Debug($"Camera Shake amp={amplitude} dur={duration}", tag: nameof(CameraService));
        }
    }
}