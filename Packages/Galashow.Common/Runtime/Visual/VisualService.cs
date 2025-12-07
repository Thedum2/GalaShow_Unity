using Galashow.Core;
using UnityEngine;

namespace Galashow.Common
{
    public class VisualService : MonoBehaviour
    {
        public void PrepareForRound(object round)
        {
            GLog.Debug("[Visual] Preparing for round");
        }

        public void PulseChoice(int choiceId)
        {
            GLog.Trace($"[Visual] Pulse choice {choiceId}");
        }

        public void RevealResult(int resultChoiceId)
        {
            GLog.Info($"[Visual] Reveal result: {resultChoiceId}");
        }
    }
}