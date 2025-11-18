using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Galashow.Core;
using Galashow.RGF;

namespace Galashow.Trolley
{
    /// <summary>
    /// 트롤리 딜레마 게임 플러그인
    /// RGF의 8단계 생명주기를 구현하여 트롤리 딜레마 게임 로직을 처리
    /// </summary>
    public class TrolleyDilemmaPlugin : IGamePlugin
    {
        public string GameName => "트롤리 딜레마";

        private TrolleyGameData _gameData;
        private Dictionary<string, DateTime> _playerSelectionTimes = new Dictionary<string, DateTime>();

        /// <summary>
        /// READY Phase: 게임 준비
        /// </summary>
        public async Task OnReadyAsync(GameState state)
        {
            GLog.Info($"[Trolley] READY Phase - Round {state.CurrentRound}");

            // 게임 데이터 초기화
            _gameData = state.GameData as TrolleyGameData;
            if (_gameData == null)
            {
                GLog.Error("[Trolley] Invalid game data");
                return;
            }

            // ========== Phase Duration 설정 ==========
            // 각 게임에서 독립적으로 Phase 시간 설정
            state.SetPhaseDurations(new Dictionary<GamePhase, float>
            {
                { GamePhase.READY, 3f },
                { GamePhase.SETUP, 1f },
                { GamePhase.PRESENT, 5f },
                { GamePhase.INPUT, _gameData.InputTimeLimit },
                { GamePhase.WAIT, 2f },
                { GamePhase.EXECUTE, 3f },
                { GamePhase.REVEAL, 10f },
                { GamePhase.CLEANUP, 2f }
            });
            GLog.Info($"[Trolley] Phase durations configured (INPUT: {_gameData.InputTimeLimit}s)");
            // ========================================

            // ========== GameState에서 결과 관리 ==========
            var result = new TrolleyGameResult
            {
                RoundNumber = state.CurrentRound,
                StartTime = UnityEngine.Time.time
            };
            state.ResultData = result;
            // ==========================================

            _playerSelectionTimes.Clear();

            GLog.Debug($"[Trolley] Ready for round {state.CurrentRound}: {_gameData.Title}");

            await Task.CompletedTask;
        }

        /// <summary>
        /// SETUP Phase: 게임 데이터 구축
        /// </summary>
        public async Task OnSetupAsync(GameState state)
        {
            GLog.Info($"[Trolley] SETUP Phase - Loading resources");

            // ========== ServiceContainer 활용 예시 ==========
            // 서비스 생성 및 등록
            var uiService = new TrolleyUIService();
            var timerService = new TrolleyTimerService();
            var scoreService = new TrolleyScoreService();

            state.RegisterService(uiService);
            state.RegisterService(timerService);
            state.RegisterService(scoreService);

            GLog.Info("[Trolley] Services registered: UIService, TimerService, ScoreService");

            // UI 서비스 초기화
            uiService.Initialize(_gameData);

            // 점수 서비스 초기화
            scoreService.Initialize(state);
            // ==============================================

            // 선택지 UI 준비
            foreach (var choice in _gameData.Choices)
            {
                GLog.Debug($"[Trolley] Choice prepared: {choice.Id} - {choice.Text}");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// PRESENT Phase: 문제 제시
        /// </summary>
        public async Task OnPresentAsync(GameState state)
        {
            GLog.Info($"[Trolley] PRESENT Phase - Showing dilemma");

            // ========== ServiceContainer에서 서비스 가져오기 ==========
            var uiService = state.GetService<TrolleyUIService>();

            // UI 서비스를 통해 문제 표시
            uiService?.ShowProblem();
            uiService?.ShowChoices();
            // =======================================================

            await Task.CompletedTask;
        }

        /// <summary>
        /// INPUT Phase: 플레이어 입력 수집
        /// </summary>
        public async Task OnInputAsync(GameState state)
        {
            GLog.Info($"[Trolley] INPUT Phase - Collecting player choices (Time: {state.PhaseDuration}s)");

            // ========== TimerService 사용 ==========
            var timerService = state.GetService<TrolleyTimerService>();
            var uiService = state.GetService<TrolleyUIService>();

            // 타이머 시작 (GameState의 PhaseDuration 사용)
            timerService?.StartTimer(state.PhaseDuration);

            // 타이머 이벤트 등록
            if (timerService != null)
            {
                timerService.OnTimerWarning += (remaining) =>
                {
                    uiService?.ShowMessage($"⚠️ 남은 시간: {remaining:F0}초!");
                };

                timerService.OnTimerExpired += () =>
                {
                    uiService?.ShowMessage("⏰ 시간 종료!");
                };
            }

            // UI에서 선택 진행률 표시
            uiService?.ShowMessage("선택을 시작하세요!");
            // =======================================

            // ⚠️ 중복 대기 제거!
            // PhaseExecutor가 이미 PhaseDuration만큼 대기하므로
            // 여기서는 추가 대기 불필요

            await Task.CompletedTask;
        }

        /// <summary>
        /// WAIT Phase: 입력 마감
        /// </summary>
        public async Task OnWaitAsync(GameState state)
        {
            GLog.Info($"[Trolley] WAIT Phase - Input closed");

            // 선택지 비활성화
            // 예: uiService.DisableChoiceButtons();

            // 마감 효과
            // 예: audioService.PlayTimeUpSound();

            // 입력하지 않은 플레이어 처리 (랜덤 선택 또는 탈락)
            foreach (var choice in _gameData.Choices)
            {
                GLog.Debug($"[Trolley] Choice '{choice.Text}': {choice.SelectedPlayers.Count} players");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// EXECUTE Phase: 결과 계산
        /// </summary>
        public async Task OnExecuteAsync(GameState state)
        {
            GLog.Info($"[Trolley] EXECUTE Phase - Calculating results");

            var totalPlayers = _gameData.Choices.Sum(c => c.SelectedPlayers.Count);

            // ========== GameState에서 결과 가져오기 ==========
            var gameResult = state.ResultData as TrolleyGameResult;
            if (gameResult == null)
            {
                GLog.Error("[Trolley] GameResult not found in GameState");
                return;
            }

            var scoreService = state.GetService<TrolleyScoreService>();
            // ===============================================

            // 각 선택지별 생존/탈락 결정
            foreach (var choice in _gameData.Choices)
            {
                var choiceStat = new ChoiceStatistics
                {
                    SelectionCount = choice.SelectedPlayers.Count,
                    SelectionRate = totalPlayers > 0 ? (float)choice.SelectedPlayers.Count / totalPlayers : 0
                };

                // 생존율에 따라 생존자 결정
                var survivorCount = (int)(choice.SelectedPlayers.Count * choice.SurvivalRate);
                var survivors = choice.SelectedPlayers.Take(survivorCount).ToList();

                choiceStat.SurvivorCount = survivorCount;
                choiceStat.ActualSurvivalRate = choice.SelectedPlayers.Count > 0
                    ? (float)survivorCount / choice.SelectedPlayers.Count
                    : 0;

                gameResult.Survivors.AddRange(survivors);

                var eliminated = choice.SelectedPlayers.Skip(survivorCount).ToList();
                gameResult.Eliminated.AddRange(eliminated);

                gameResult.ChoiceStats[choice.Id] = choiceStat;

                GLog.Debug($"[Trolley] Choice '{choice.Text}': {survivorCount} survivors, {eliminated.Count} eliminated");
            }

            // 전체 생존율 계산 (GameResult 메서드 사용)
            gameResult.CalculateSurvivalRate();

            // 평균 선택 시간 계산
            if (_playerSelectionTimes.Count > 0)
            {
                var avgTime = _playerSelectionTimes.Values.Average(t => (DateTime.Now - t).TotalSeconds);
                gameResult.AverageSelectionTime = (float)avgTime;
            }

            // ========== ScoreService로 점수 계산 ==========
            if (scoreService != null)
            {
                // 생존자에게 점수 부여
                scoreService.CalculateSurvivalScores(gameResult.Survivors);

                // 빠른 선택 보너스 계산
                foreach (var kvp in _playerSelectionTimes)
                {
                    var playerId = kvp.Key;
                    var selectionTime = (float)(DateTime.Now - kvp.Value).TotalSeconds;
                    scoreService.CalculateSpeedBonus(playerId, selectionTime, _gameData.InputTimeLimit);
                }

                // 다수 선택 보너스
                var majorityChoice = _gameData.Choices.OrderByDescending(c => c.SelectedPlayers.Count).FirstOrDefault();
                if (majorityChoice != null)
                {
                    scoreService.CalculateMajorityBonus(majorityChoice.SelectedPlayers);
                }
            }
            // =============================================

            GLog.Info($"[Trolley] Results: {gameResult.Survivors.Count} survivors, {gameResult.Eliminated.Count} eliminated");

            await Task.CompletedTask;
        }

        /// <summary>
        /// REVEAL Phase: 결과 연출
        /// </summary>
        public async Task OnRevealAsync(GameState state)
        {
            GLog.Info($"[Trolley] REVEAL Phase - Showing results");

            // ========== GameState에서 결과 가져오기 ==========
            var gameResult = state.ResultData as TrolleyGameResult;
            if (gameResult == null)
            {
                GLog.Error("[Trolley] GameResult not found in GameState");
                return;
            }

            var uiService = state.GetService<TrolleyUIService>();
            var scoreService = state.GetService<TrolleyScoreService>();
            // ===============================================

            // 결과 화면 표시
            uiService?.ShowResult(gameResult);

            // 점수 랭킹 표시
            scoreService?.PrintRanking(state);

            // 통계 정보
            GLog.Info($"[Trolley] 최고 점수: {scoreService?.GetHighestScore() ?? 0}점");
            GLog.Info($"[Trolley] 평균 점수: {scoreService?.GetAverageScore() ?? 0:F1}점");

            // 통계 표시
            foreach (var kvp in gameResult.ChoiceStats)
            {
                var choiceId = kvp.Key;
                var stat = kvp.Value;
                GLog.Debug($"[Trolley] Choice {choiceId}: {stat.SelectionCount} selected ({stat.SelectionRate:P0}), {stat.SurvivorCount} survived");
            }

            // 생존율 표시
            GLog.Info($"[Trolley] Overall survival rate: {gameResult.SurvivalRate:P1}");

            await Task.CompletedTask;
        }

        /// <summary>
        /// CLEANUP Phase: 정리
        /// </summary>
        public async Task OnCleanupAsync(GameState state)
        {
            GLog.Info($"[Trolley] CLEANUP Phase - Cleaning up");

            // ========== 결과 최종 처리 ==========
            var gameResult = state.ResultData as TrolleyGameResult;
            if (gameResult != null)
            {
                gameResult.EndTime = UnityEngine.Time.time;
                gameResult.CalculateTotalPlayTime();
                GLog.Info($"[Trolley] Total play time: {gameResult.TotalPlayTime:F2}s");
            }
            // ==================================

            // ========== 서비스 정리 ==========
            var uiService = state.GetService<TrolleyUIService>();
            var timerService = state.GetService<TrolleyTimerService>();

            // UI 정리
            uiService?.Clear();

            // 타이머 리셋
            timerService?.Reset();

            GLog.Info("[Trolley] Services cleaned up");
            // ==============================

            // 상태 초기화
            _gameData = null;
            _playerSelectionTimes.Clear();

            GLog.Debug($"[Trolley] Cleanup complete");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Phase 전환 콜백
        /// </summary>
        public async Task OnPhaseTransitionAsync(GamePhase from, GamePhase to)
        {
            GLog.Debug($"[Trolley] Phase transition: {from} → {to}");

            // Phase 전환 시 공통 처리
            // 예: 페이드 효과, 전환 애니메이션 등

            await Task.CompletedTask;
        }

        /// <summary>
        /// 플레이어 선택 등록 (외부에서 호출)
        /// </summary>
        public void RegisterPlayerChoice(string playerId, string choiceId)
        {
            var choice = _gameData.Choices.FirstOrDefault(c => c.Id == choiceId);
            if (choice == null)
            {
                GLog.Warn($"[Trolley] Invalid choice: {choiceId}");
                return;
            }

            // 중복 선택 방지
            foreach (var c in _gameData.Choices)
            {
                c.SelectedPlayers.Remove(playerId);
            }

            choice.SelectedPlayers.Add(playerId);
            _playerSelectionTimes[playerId] = DateTime.Now;

            GLog.Info($"[Trolley] Player {playerId} selected: {choice.Text}");
        }

        /// <summary>
        /// 게임 결과 가져오기
        /// ⚠️ 이제 GameState.ResultData에서 관리됨
        /// </summary>
        public TrolleyGameResult GetResult()
        {
            // GameState에서 결과 가져오기
            if (RGFManager.Instance != null)
            {
                return RGFManager.Instance.State.ResultData as TrolleyGameResult;
            }

            GLog.Warn("[Trolley] RGFManager not initialized, cannot get result");
            return null;
        }
    }
}
