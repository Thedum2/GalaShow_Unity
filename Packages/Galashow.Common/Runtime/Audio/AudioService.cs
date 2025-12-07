using Galashow.Core;
using UnityEngine;

namespace Galashow.Common
{
    public class AudioService : MonoBehaviour
    {
        public void PrepareForRound(object round)
        {
            GLog.Debug("[Audio] Preparing for round");
        }

        public void PlayBgm(string key, float fadeIn = 0f)
        {
            GLog.Info($"[Audio] BGM: {key} (fade={fadeIn}s)");
        }

        public void StopBgm(float fadeOut = 0f)
        {
            GLog.Info($"[Audio] Stop BGM (fade={fadeOut}s)");
        }

        public void PlaySfx(string key)
        {
            GLog.Debug($"[Audio] SFX: {key}");
        }

        public void Duck(bool on)
        {
            GLog.Debug($"[Audio] Duck {(on ? "on" : "off")}");
        }
    }
}