using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;
using Galashow.Core;

namespace Galashow.RGF.Testing
{
    /// <summary>
    /// RGF 흐름 테스트용 더미 게임
    /// 채팅 "1" 또는 "2"를 받고, 더 적게 선택된 쪽을 탈락시킨다 (동점이면 전원 생존, 미입력은 생존).
    /// </summary>
    public class DummyGamePlugin : GamePluginBase<DummyGameData>
    {
        public const string PluginId = "DummyGame";

        private readonly Dictionary<string, string> _choices = new Dictionary<string, string>();

        public override string GameName => "DummyGame";

        /// <summary>
        /// 이번 라운드 입력 (참가자 ID → "1"/"2")
        /// </summary>
        public IReadOnlyDictionary<string, string> Choices => _choices;

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            GamePluginCatalog.Register(PluginId, "DummyGame", () => new DummyGamePlugin());
        }

        protected override Task OnReady()
        {
            _choices.Clear();
            GLog.Info($"[DummyGame] READY - Round {State.CurrentRound}, 참가 {Participants.Count}명");
            return Task.CompletedTask;
        }

        protected override Task OnPresent()
        {
            GLog.Info("[DummyGame] PRESENT - 문제: 1 또는 2를 선택하세요!");
            return Task.CompletedTask;
        }

        protected override void OnPlayerInput(PlayerInput input)
        {
            var message = input.Message?.Trim();
            if (message != "1" && message != "2")
            {
                return;
            }

            _choices[input.PlayerId] = message;
            GLog.Info($"[DummyGame] {input.PlayerId} 선택: {message}");
        }

        protected override Task OnExecute()
        {
            int count1 = _choices.Values.Count(c => c == "1");
            int count2 = _choices.Values.Count(c => c == "2");
            string loser = count1 == count2 ? null : (count1 < count2 ? "1" : "2");

            foreach (var playerId in Participants)
            {
                bool survived = loser == null || !_choices.TryGetValue(playerId, out var choice) || choice != loser;
                SetSurvived(playerId, survived);
            }

            GLog.Info($"[DummyGame] EXECUTE - 1: {count1}명, 2: {count2}명, 탈락 선택: {loser ?? "없음"}");
            return Task.CompletedTask;
        }

        protected override Task OnCleanup()
        {
            _choices.Clear();
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 더미 게임은 gameData를 쓰지 않는다
    /// </summary>
    public class DummyGameData
    {
    }
}
