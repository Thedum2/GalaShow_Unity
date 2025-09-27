using Galashow.Core;
using UnityEngine;

namespace Galashow.Common
{
    public class AudioService : MonoBehaviour
    {
        public void PrepareForRound(object round)
        {
            // 사운드 버스/볼륨 초기화 등
            GLog.Debug("Audio PrepareForRound", tag: nameof(AudioService));
        }

        public void PlayBgm(string key, float fadeIn = 0f)
        {
            GLog.Info($"PlayBGM {key} (fadeIn={fadeIn})", tag: nameof(AudioService));
            // TODO: AudioMixer/Addressables/Bus 구현
        }

        public void StopBgm(float fadeOut = 0f)
        {
            GLog.Info($"StopBGM (fadeOut={fadeOut})", tag: nameof(AudioService));
        }

        public void PlaySfx(string key)
        {
            // 짧은 효과음
            GLog.Debug($"SFX {key}", tag: nameof(AudioService));
        }

        public void Duck(bool on)
        {
            // BGM ducking (sidechain 느낌)
            GLog.Debug($"Duck={(on ? "on" : "off")}", tag: nameof(AudioService));
        }
    }
}