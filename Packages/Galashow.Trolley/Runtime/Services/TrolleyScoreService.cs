using System.Collections.Generic;
using Galashow.Core;

namespace Galashow.Trolley
{
    /// <summary>
    /// Trolley 게임 점수 계산 서비스
    /// 플레이어 점수, 랭킹 등을 계산하고 관리
    /// </summary>
    public class TrolleyScoreService
    {
        /// <summary>
        /// 플레이어별 점수 (Key: PlayerId)
        /// </summary>
        private Dictionary<string, int> _scores = new Dictionary<string, int>();

        /// <summary>
        /// 생존 보너스 점수
        /// </summary>
        private const int SURVIVAL_BONUS = 100;

        /// <summary>
        /// 빠른 선택 보너스 (최대)
        /// </summary>
        private const int QUICK_CHOICE_BONUS = 50;

        /// <summary>
        /// 다수 선택 보너스
        /// </summary>
        private const int MAJORITY_BONUS = 30;

        /// <summary>
        /// 점수 초기화
        /// </summary>
        public void Initialize(GameState state)
        {
            _scores.Clear();
            foreach (var player in state.Players.Values)
            {
                _scores[player.Id] = 0;
            }
            GLog.Info($"[TrolleyScoreService] 점수 시스템 초기화: {_scores.Count}명");
        }

        /// <summary>
        /// 생존 점수 계산
        /// </summary>
        public void CalculateSurvivalScores(List<string> survivors)
        {
            foreach (var playerId in survivors)
            {
                AddScore(playerId, SURVIVAL_BONUS);
            }
            GLog.Info($"[TrolleyScoreService] 생존 점수 계산 완료: {survivors.Count}명 × {SURVIVAL_BONUS}점");
        }

        /// <summary>
        /// 선택 속도 보너스 계산
        /// </summary>
        public void CalculateSpeedBonus(string playerId, float selectionTime, float maxTime)
        {
            // 빠를수록 높은 점수 (최대 시간의 50% 이내면 최대 보너스)
            float speedRatio = 1f - (selectionTime / maxTime);
            int bonus = (int)(QUICK_CHOICE_BONUS * speedRatio);

            if (bonus > 0)
            {
                AddScore(playerId, bonus);
                GLog.Info($"[TrolleyScoreService] 빠른 선택 보너스: {playerId} +{bonus}점");
            }
        }

        /// <summary>
        /// 다수 선택 보너스
        /// </summary>
        public void CalculateMajorityBonus(List<string> majorityPlayers)
        {
            foreach (var playerId in majorityPlayers)
            {
                AddScore(playerId, MAJORITY_BONUS);
            }
            GLog.Info($"[TrolleyScoreService] 다수 선택 보너스: {majorityPlayers.Count}명 × {MAJORITY_BONUS}점");
        }

        /// <summary>
        /// 점수 추가
        /// </summary>
        public void AddScore(string playerId, int points)
        {
            if (!_scores.ContainsKey(playerId))
            {
                _scores[playerId] = 0;
            }

            _scores[playerId] += points;
        }

        /// <summary>
        /// 점수 가져오기
        /// </summary>
        public int GetScore(string playerId)
        {
            return _scores.TryGetValue(playerId, out var score) ? score : 0;
        }

        /// <summary>
        /// 전체 점수 가져오기
        /// </summary>
        public Dictionary<string, int> GetAllScores()
        {
            return new Dictionary<string, int>(_scores);
        }

        /// <summary>
        /// 랭킹 가져오기 (점수 높은 순)
        /// </summary>
        public List<(string PlayerId, int Score)> GetRanking()
        {
            var ranking = new List<(string, int)>();

            foreach (var kvp in _scores)
            {
                ranking.Add((kvp.Key, kvp.Value));
            }

            // 점수 내림차순 정렬
            ranking.Sort((a, b) => b.Item2.CompareTo(a.Item2));

            return ranking;
        }

        /// <summary>
        /// 랭킹 출력
        /// </summary>
        public void PrintRanking(GameState state)
        {
            GLog.Info("========== 점수 랭킹 ==========");

            var ranking = GetRanking();
            int rank = 1;

            foreach (var (playerId, score) in ranking)
            {
                var playerInfo = state.Players[playerId];
                string statusIcon = playerInfo.IsAlive ? "✓" : "✗";
                GLog.Info($"{rank}. {statusIcon} {playerInfo.Name} ({playerId}): {score}점");
                rank++;
            }

            GLog.Info("==============================");
        }

        /// <summary>
        /// 최고 점수 가져오기
        /// </summary>
        public int GetHighestScore()
        {
            int highest = 0;
            foreach (var score in _scores.Values)
            {
                if (score > highest)
                {
                    highest = score;
                }
            }
            return highest;
        }

        /// <summary>
        /// 평균 점수 가져오기
        /// </summary>
        public float GetAverageScore()
        {
            if (_scores.Count == 0) return 0;

            int total = 0;
            foreach (var score in _scores.Values)
            {
                total += score;
            }

            return (float)total / _scores.Count;
        }
    }
}
