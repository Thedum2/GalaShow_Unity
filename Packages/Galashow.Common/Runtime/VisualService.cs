using Galashow.Core;

namespace Galashow.Common
{
    public class VisualService : PersistentMonoSingleton<VisualService>
    {
        public void PrepareForRound(object round)
        {
            GLog.Debug("Visual PrepareForRound", tag: nameof(VisualService));
        }

        public void PulseChoice(int choiceId)
        {
            // 특정 선택지에 하이라이트/펄스 FX
            GLog.Trace($"Pulse choice {choiceId}", tag: nameof(VisualService));
        }

        public void RevealResult(int resultChoiceId)
        {
            GLog.Info($"Reveal Result: {resultChoiceId}", tag: nameof(VisualService));
            // 파티클/포스트프로세싱/텍스트 등 종합 연출 지점
        }
    }
}