using Galashow.Core;
using UnityEngine;

namespace Galashow.Common
{
    public class UIService : MonoBehaviour
    {
        int _countdown;
        public void SetupForRound(object info)
        {
            GLog.Debug("[UI] Setup for round");
        }

        public void ShowHud(bool on)
        {
            GLog.Debug($"[UI] HUD {(on ? "ON" : "OFF")}");
        }

        public void SetCountdown(float seconds)
        {
            int s = Mathf.CeilToInt(seconds);
            if (s == _countdown) return;
            _countdown = s;
            GLog.Trace($"[UI] Countdown: {_countdown}");
        }

        public void ShowSubtitle(string text, float seconds)
        {
            GLog.Info($"[UI] Subtitle: {text} ({seconds}s)");
        }

        public void ShowSelectionPrompt(bool on)
        {
            GLog.Debug($"[UI] Selection prompt {(on ? "ON" : "OFF")}");
        }

        public void MarkUserChoice(int userIndex, int choiceId)
        {
            GLog.Trace($"[UI] User {userIndex} → choice {choiceId}");
        }
    }
}