using UnityEngine;
using Galashow.Core;
using Galashow.RGF;

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
        }

        /// <summary>
        /// 문제 표시
        /// </summary>
        public void ShowProblem()
        {
            if (_currentGameData == null)
            {
                GLog.Error("[Trolley✗] Game data not found");
                return;
            }

            CurrentMessage = $"[문제 제시]\n{_currentGameData.Title}\n{_currentGameData.Description}";
            GLog.Debug($"[Trolley] Problem: {_currentGameData.Title}");
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

            GLog.Debug($"[Trolley] Choices displayed: {_currentGameData.Choices.Count}");
        }

        /// <summary>
        /// 선택 진행률 표시
        /// </summary>
        public void ShowSelectionProgress(int selectedCount, int totalPlayers)
        {
            float progress = totalPlayers > 0 ? (float)selectedCount / totalPlayers : 0;
            GLog.Debug($"[Trolley] Selection: {selectedCount}/{totalPlayers} ({progress:P0})");
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
        }

        /// <summary>
        /// 메시지 표시
        /// </summary>
        public void ShowMessage(string message)
        {
            CurrentMessage = message;
            GLog.Debug($"[Trolley] UI: {message}");
        }

        /// <summary>
        /// UI 정리
        /// </summary>
        public void Clear()
        {
            CurrentMessage = string.Empty;
        }
    }
}
