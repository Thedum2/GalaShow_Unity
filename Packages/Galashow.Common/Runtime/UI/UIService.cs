using Galashow.Core;
using UnityEngine;

namespace Galashow.Common
{
    public class UIService : MonoBehaviour
    {
        int _countdown;
        public void SetupForRound(object info)
        {
            GLog.Debug($"UI Setup: Round", tag: nameof(UIService));
        }

        public void ShowHud(bool on)
        {
            GLog.Debug($"HUD {(on ? "ON" : "OFF")}", tag: nameof(UIService));
        }
        
        public void SetCountdown(float seconds)
        {
            int s = Mathf.CeilToInt(seconds);
            if (s == _countdown) return;
            _countdown = s;
            GLog.Trace($"Countdown: {_countdown}", tag: nameof(UIService));
            // TODO: 텍스트/라딜/프로그레스 갱신
        }

        public void ShowSubtitle(string text, float seconds)
        {
            GLog.Info($"Subtitle: {text} ({seconds}s)", tag: nameof(UIService));
        }

        public void ShowSelectionPrompt(bool on)
        {
            GLog.Debug($"SelectPrompt {(on ? "ON" : "OFF")}", tag: nameof(UIService));
        }

        public void MarkUserChoice(int userIndex, int choiceId)
        {
            GLog.Trace($"User {userIndex} -> choice {choiceId}", tag: nameof(UIService));
            // TODO: 아이콘/라인/리스트 갱신
        }
    }
}