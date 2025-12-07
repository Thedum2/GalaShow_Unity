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
            // 게임 데이터 초기화
            _gameData = state.GameData as TrolleyGameData;
            if (_gameData == null)
            {
                GLog.Error("[Trolley✗] Invalid game data");
                return;
            }

            // Phase Duration 설정
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

            // 결과 객체 초기화
            var result = new TrolleyGameResult
            {
                RoundNumber = state.CurrentRound,
                StartTime = UnityEngine.Time.time
            };
            state.ResultData = result;

            _playerSelectionTimes.Clear();
            GLog.Info($"[Trolley] Ready - Round {state.CurrentRound}: {_gameData.Title}");

            await Task.CompletedTask;
        }

        /// <summary>
        /// SETUP Phase: 게임 데이터 구축
        /// </summary>
        public async Task OnSetupAsync(GameState state)
        {
            // 서비스 생성 및 등록
            var uiService = new TrolleyUIService();
            var timerService = new TrolleyTimerService();
            var scoreService = new TrolleyScoreService();

            state.RegisterService(uiService);
            state.RegisterService(timerService);
            state.RegisterService(scoreService);

            // 서비스 초기화
            uiService.Initialize(_gameData);
            scoreService.Initialize(state);

            GLog.Info("[Trolley] Services initialized");

            await Task.CompletedTask;
        }

        /// <summary>
        /// PRESENT Phase: 문제 제시
        /// </summary>
        public async Task OnPresentAsync(GameState state)
        {
            var uiService = state.GetService<TrolleyUIService>();
            uiService?.ShowProblem();
            uiService?.ShowChoices();

            await Task.CompletedTask;
        }

        /// <summary>
        /// INPUT Phase: 플레이어 입력 수집
        /// </summary>
        public async Task OnInputAsync(GameState state)
        {
            var timerService = state.GetService<TrolleyTimerService>();
            var uiService = state.GetService<TrolleyUIService>();

            timerService?.StartTimer(state.PhaseDuration);

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

            uiService?.ShowMessage("선택을 시작하세요!");

            await Task.CompletedTask;
        }

        /// <summary>
        /// WAIT Phase: 입력 마감
        /// </summary>
        public async Task OnWaitAsync(GameState state)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// EXECUTE Phase: 결과 계산
        /// </summary>
        public async Task OnExecuteAsync(GameState state)
        {
            var totalPlayers = _gameData.Choices.Sum(c => c.SelectedPlayers.Count);

            var gameResult = state.ResultData as TrolleyGameResult;
            if (gameResult == null)
            {
                GLog.Error("[Trolley✗] GameResult not found");
                return;
            }

            var scoreService = state.GetService<TrolleyScoreService>();

            // 각 선택지별 생존/탈락 결정
            foreach (var choice in _gameData.Choices)
            {
                var choiceStat = new ChoiceStatistics
                {
                    SelectionCount = choice.SelectedPlayers.Count,
                    SelectionRate = totalPlayers > 0 ? (float)choice.SelectedPlayers.Count / totalPlayers : 0
                };

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
            }

            gameResult.CalculateSurvivalRate();

            if (_playerSelectionTimes.Count > 0)
            {
                var avgTime = _playerSelectionTimes.Values.Average(t => (DateTime.Now - t).TotalSeconds);
                gameResult.AverageSelectionTime = (float)avgTime;
            }

            // 점수 계산
            if (scoreService != null)
            {
                scoreService.CalculateSurvivalScores(gameResult.Survivors);

                foreach (var kvp in _playerSelectionTimes)
                {
                    var playerId = kvp.Key;
                    var selectionTime = (float)(DateTime.Now - kvp.Value).TotalSeconds;
                    scoreService.CalculateSpeedBonus(playerId, selectionTime, _gameData.InputTimeLimit);
                }

                var majorityChoice = _gameData.Choices.OrderByDescending(c => c.SelectedPlayers.Count).FirstOrDefault();
                if (majorityChoice != null)
                {
                    scoreService.CalculateMajorityBonus(majorityChoice.SelectedPlayers);
                }
            }

            GLog.Info($"[Trolley] Results - Survivors: {gameResult.Survivors.Count}, Eliminated: {gameResult.Eliminated.Count}");

            await Task.CompletedTask;
        }

        /// <summary>
        /// REVEAL Phase: 결과 연출
        /// </summary>
        public async Task OnRevealAsync(GameState state)
        {
            var gameResult = state.ResultData as TrolleyGameResult;
            if (gameResult == null)
            {
                GLog.Error("[Trolley✗] GameResult not found");
                return;
            }

            var uiService = state.GetService<TrolleyUIService>();
            var scoreService = state.GetService<TrolleyScoreService>();

            uiService?.ShowResult(gameResult);
            scoreService?.PrintRanking(state);

            GLog.Info($"[Trolley] Survival rate: {gameResult.SurvivalRate:P1}");

            await Task.CompletedTask;
        }

        /// <summary>
        /// CLEANUP Phase: 정리
        /// </summary>
        public async Task OnCleanupAsync(GameState state)
        {
            var gameResult = state.ResultData as TrolleyGameResult;
            if (gameResult != null)
            {
                gameResult.EndTime = UnityEngine.Time.time;
                gameResult.CalculateTotalPlayTime();
            }

            var uiService = state.GetService<TrolleyUIService>();
            var timerService = state.GetService<TrolleyTimerService>();

            uiService?.Clear();
            timerService?.Reset();

            _gameData = null;
            _playerSelectionTimes.Clear();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Phase 전환 콜백
        /// </summary>
        public async Task OnPhaseTransitionAsync(GamePhase from, GamePhase to)
        {
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
                GLog.Warn($"[Trolley⚠] Invalid choice: {choiceId}");
                return;
            }

            foreach (var c in _gameData.Choices)
            {
                c.SelectedPlayers.Remove(playerId);
            }

            choice.SelectedPlayers.Add(playerId);
            _playerSelectionTimes[playerId] = DateTime.Now;

            GLog.Debug($"[Trolley] Player {playerId} → {choice.Text}");
        }

        /// <summary>
        /// 게임 결과 가져오기
        /// ⚠️ 이제 GameState.ResultData에서 관리됨
        /// </summary>
        public TrolleyGameResult GetResult()
        {
            if (RGFManager.Instance != null)
            {
                return RGFManager.Instance.State.ResultData as TrolleyGameResult;
            }

            GLog.Warn("[Trolley⚠] RGFManager not initialized");
            return null;
        }
    }
}
