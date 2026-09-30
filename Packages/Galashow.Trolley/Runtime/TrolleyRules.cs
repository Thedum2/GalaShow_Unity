using System;
using System.Collections.Generic;
using System.Linq;

namespace Galashow.Trolley
{
    /// <summary>
    /// 트롤리 입력 해석과 판정 (Unity 의존 없음)
    /// </summary>
    public static class TrolleyRules
    {
        public const string HostSource = "host";
        public const string RandomSource = "random";
        public const string NoInputKey = "none";

        /// <summary>
        /// 시청자 채팅을 선택지 ID로 해석한다. 1부터 시작하는 번호만 받는다 (예: "1", " 2 ")
        /// </summary>
        /// <returns>선택지 ID. 해당 없으면 null</returns>
        public static string ParseChoice(string message, IReadOnlyList<TrolleyChoice> choices)
        {
            var key = message?.Trim();
            if (string.IsNullOrEmpty(key) || choices == null || key.Length > 3 || !key.All(char.IsDigit))
            {
                return null;
            }

            return int.TryParse(key, out var number) && number >= 1 && number <= choices.Count ? choices[number - 1].Id : null;
        }

        /// <summary>
        /// 호스트 입력(선택지 ID 또는 번호)을 선택지 ID로 바꾼다
        /// </summary>
        public static string ParseHostChoice(string value, IReadOnlyList<TrolleyChoice> choices)
        {
            var byNumber = ParseChoice(value, choices);
            if (byNumber != null || choices == null)
            {
                return byNumber;
            }

            var key = value?.Trim();
            return choices.FirstOrDefault(c => string.Equals(c.Id, key, StringComparison.OrdinalIgnoreCase))?.Id;
        }

        /// <summary>
        /// 선택지 ID → 화면에 보여 주는 번호 (1부터)
        /// </summary>
        public static int NumberOf(string choiceId, IReadOnlyList<TrolleyChoice> choices)
        {
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i].Id == choiceId) return i + 1;
            }
            return 0;
        }

        /// <summary>
        /// 호스트와 같은 선택을 한 참가자만 생존 (match_host)
        /// </summary>
        /// <param name="participants">라운드 시작 시 생존 참가자</param>
        /// <param name="votes">참가자 ID → 선택지 ID (마지막 입력)</param>
        /// <param name="hostChoice">확정된 호스트 선택</param>
        public static TrolleyGameResult Judge(
            TrolleyGameData data,
            IReadOnlyList<string> participants,
            IReadOnlyDictionary<string, string> votes,
            string hostChoice,
            string hostChoiceSource,
            ICollection<string> autoAssigned = null)
        {
            var rule = data.Rule ?? new TrolleyRule();
            var result = new TrolleyGameResult
            {
                DilemmaId = data.DilemmaId,
                HostChoice = hostChoice,
                HostChoiceNumber = NumberOf(hostChoice, data.Choices),
                HostChoiceSource = hostChoiceSource
            };

            foreach (var choice in data.Choices)
            {
                result.Distribution[choice.Id] = 0;
            }
            result.Distribution[NoInputKey] = 0;

            foreach (var playerId in participants)
            {
                votes.TryGetValue(playerId, out var vote);
                result.Distribution[vote ?? NoInputKey]++;

                bool auto = autoAssigned != null && autoAssigned.Contains(playerId);
                string prefix = auto ? "입력 없음 → 자동 선택, " : "";
                int number = NumberOf(vote, data.Choices);
                bool survived;
                string reason;
                if (vote == null)
                {
                    survived = rule.NoInput == TrolleyRule.Survive;
                    reason = "입력 없음";
                }
                else if (vote == hostChoice)
                {
                    survived = true;
                    reason = $"{prefix}호스트와 같은 선택({number}번)";
                }
                else
                {
                    survived = false;
                    reason = $"{prefix}호스트와 다른 선택({number}번)";
                }

                result.Results.Add(new TrolleyPlayerResult
                {
                    ParticipantId = playerId,
                    Choice = vote,
                    AutoAssigned = auto,
                    Survived = survived,
                    Reason = reason
                });
            }

            if (result.Results.Count > 0 && result.Results.All(r => !r.Survived) && rule.AllEliminatedPolicy == TrolleyRule.AllSurvive)
            {
                result.RescuedAllEliminated = true;
                foreach (var r in result.Results)
                {
                    r.Survived = true;
                    r.Reason += " · 전원 탈락으로 전원 생존";
                }
            }

            foreach (var r in result.Results)
            {
                (r.Survived ? result.Survivors : result.Eliminated).Add(r.ParticipantId);
            }
            result.CalculateSurvivalRate();
            return result;
        }
    }
}
