using UnityEngine;
using Galashow.Core;

namespace Galashow.Trolley
{
    /// <summary>
    /// Trolley 게임 UI 관리 서비스
    /// UI 업데이트, 애니메이션 등을 담당
    /// </summary>
    public class TrolleyUIService
    {
        private TrolleyGameData _currentGameData;

        /// <summary>
        /// 현재 표시 중인 메시지
        /// </summary>
        public string CurrentMessage { get; private set; }

        /// <summary>
        /// UI 초기화
        /// </summary>
        public void Initialize(TrolleyGameData gameData)
        {
            _currentGameData = gameData;
            GLog.Info("[TrolleyUIService] UI 초기화 완료");
        }

        /// <summary>
        /// 문제 표시
        /// </summary>
        public void ShowProblem()
        {
            if (_currentGameData == null)
            {
                GLog.Error("[TrolleyUIService] 게임 데이터가 없습니다");
                return;
            }

            CurrentMessage = $"[문제 제시]\n{_currentGameData.Title}\n{_currentGameData.Description}";
            GLog.Info($"[TrolleyUIService] 문제 표시: {_currentGameData.Title}");
        }

        /// <summary>
        /// 선택지 표시
        /// </summary>
        public void ShowChoices()
        {
            if (_currentGameData == null || _currentGameData.Choices == null)
            {
                return;
            }

            GLog.Info("[TrolleyUIService] 선택지 표시:");
            foreach (var choice in _currentGameData.Choices)
            {
                GLog.Info($"  - [{choice.Id}] {choice.Text}: {choice.Description}");
            }
        }

        /// <summary>
        /// 선택 진행률 표시
        /// </summary>
        public void ShowSelectionProgress(int selectedCount, int totalPlayers)
        {
            float progress = totalPlayers > 0 ? (float)selectedCount / totalPlayers : 0;
            GLog.Info($"[TrolleyUIService] 선택 진행률: {selectedCount}/{totalPlayers} ({progress:P0})");
        }

        /// <summary>
        /// 결과 표시
        /// </summary>
        public void ShowResult(TrolleyGameResult result)
        {
            if (result == null)
            {
                return;
            }

            CurrentMessage = $"[결과 발표]\n생존율: {result.SurvivalRate:P0}\n생존자: {result.Survivors.Count}명\n탈락자: {result.Eliminated.Count}명";
            GLog.Info($"[TrolleyUIService] 결과 표시: 생존율 {result.SurvivalRate:P0}");
        }

        /// <summary>
        /// 메시지 표시
        /// </summary>
        public void ShowMessage(string message)
        {
            CurrentMessage = message;
            GLog.Info($"[TrolleyUIService] 메시지: {message}");
        }

        /// <summary>
        /// UI 정리
        /// </summary>
        public void Clear()
        {
            CurrentMessage = string.Empty;
            GLog.Info("[TrolleyUIService] UI 정리 완료");
        }
    }
}
