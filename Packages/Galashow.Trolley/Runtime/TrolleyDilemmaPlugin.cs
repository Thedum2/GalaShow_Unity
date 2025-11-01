using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Galashow.Core;

namespace Galashow.Trolley
{
    /// <summary>
    /// 트롤리 딜레마 게임 플러그인
    /// RGF의 8단계 생명주기를 구현하여 트롤리 딜레마 게임 로직을 처리
    /// </summary>
    public class TrolleyDilemmaPlugin : IGamePlugin
    {
        public string GameType => "trolley_dilemma";
        public string GameName => "트롤리 딜레마";

        private TrolleyGameData _gameData;
        private TrolleyGameResult _gameResult;
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

            _gameResult = new TrolleyGameResult
            {
                RoundNumber = state.CurrentRound
            };

            _playerSelectionTimes.Clear();

            // UI 서비스를 통해 준비 화면 표시
            // 예: uiService.ShowReadyScreen();

            GLog.Debug($"[Trolley] Ready for round {state.CurrentRound}: {_gameData.Title}");

            await Task.CompletedTask;
        }

        /// <summary>
        /// SETUP Phase: 게임 데이터 구축
        /// </summary>
        public async Task OnSetupAsync(GameState state)
        {
            GLog.Info($"[Trolley] SETUP Phase - Loading resources");

            // 씬 리소스 로드
            // 예: spaceService.LoadTrolleyScene();

            // 선택지 UI 준비
            foreach (var choice in _gameData.Choices)
            {
                GLog.Debug($"[Trolley] Choice prepared: {choice.Id} - {choice.Text}");
                // 예: CreateChoiceButton(choice);
            }

            // 오디오 준비
            // 예: audioService.LoadTrolleyBGM();

            await Task.CompletedTask;
        }

        /// <summary>
        /// PRESENT Phase: 문제 제시
        /// </summary>
        public async Task OnPresentAsync(GameState state)
        {
            GLog.Info($"[Trolley] PRESENT Phase - Showing dilemma");

            // 딜레마 제목 표시
            GLog.Info($"[Trolley] Title: {_gameData.Title}");

            // 딜레마 설명 표시
            if (!string.IsNullOrEmpty(_gameData.Description))
            {
                GLog.Info($"[Trolley] Description: {_gameData.Description}");
            }

            // UI에 문제 표시
            // 예: uiService.ShowDilemmaTitle(_gameData.Title, _gameData.Description);

            // 선택지 미리보기
            // 예: uiService.ShowChoicePreview(_gameData.Choices);

            // 애니메이션 재생
            // 예: visualService.PlayDilemmaAnimation();

            await Task.CompletedTask;
        }

        /// <summary>
        /// INPUT Phase: 플레이어 입력 수집
        /// </summary>
        public async Task OnInputAsync(GameState state)
        {
            GLog.Info($"[Trolley] INPUT Phase - Collecting player choices (Time: {_gameData.InputTimeLimit}s)");

            // 선택지 활성화
            // 예: uiService.EnableChoiceButtons(_gameData.Choices);

            // 카운트다운 표시
            // 예: uiService.ShowCountdown(_gameData.InputTimeLimit);

            // INPUT Phase에서는 플레이어 입력을 기다림
            // 실제 입력은 RegisterPlayerChoice() 메서드를 통해 외부에서 등록됨

            // 제한 시간 동안 대기
            var waitTime = _gameData.InputTimeLimit;
            await Task.Delay((int)(waitTime * 1000));

            GLog.Info($"[Trolley] Input time ended");
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

                _gameResult.Survivors.AddRange(survivors);

                var eliminated = choice.SelectedPlayers.Skip(survivorCount).ToList();
                _gameResult.Eliminated.AddRange(eliminated);

                _gameResult.ChoiceStats[choice.Id] = choiceStat;

                GLog.Debug($"[Trolley] Choice '{choice.Text}': {survivorCount} survivors, {eliminated.Count} eliminated");
            }

            // 전체 생존율 계산
            _gameResult.SurvivalRate = totalPlayers > 0
                ? (float)_gameResult.Survivors.Count / totalPlayers
                : 0;

            // 평균 선택 시간 계산
            if (_playerSelectionTimes.Count > 0)
            {
                var avgTime = _playerSelectionTimes.Values.Average(t => (DateTime.Now - t).TotalSeconds);
                _gameResult.AverageSelectionTime = (float)avgTime;
            }

            GLog.Info($"[Trolley] Results: {_gameResult.Survivors.Count} survivors, {_gameResult.Eliminated.Count} eliminated");

            // 결과를 context에 저장
            state.ResultData = _gameResult;

            await Task.CompletedTask;
        }

        /// <summary>
        /// REVEAL Phase: 결과 연출
        /// </summary>
        public async Task OnRevealAsync(GameState state)
        {
            GLog.Info($"[Trolley] REVEAL Phase - Showing results");

            // 결과 화면 표시
            // 예: uiService.ShowResults(_gameResult);

            // 생존자 강조
            // 예: visualService.HighlightSurvivors(_gameResult.Survivors);

            // 탈락자 연출
            // 예: visualService.PlayEliminationAnimation(_gameResult.Eliminated);

            // 통계 표시
            foreach (var kvp in _gameResult.ChoiceStats)
            {
                var choiceId = kvp.Key;
                var stat = kvp.Value;
                GLog.Debug($"[Trolley] Choice {choiceId}: {stat.SelectionCount} selected ({stat.SelectionRate:P0}), {stat.SurvivorCount} survived");
            }

            // 생존율 표시
            GLog.Info($"[Trolley] Overall survival rate: {_gameResult.SurvivalRate:P1}");

            // 결과 사운드
            // 예: audioService.PlayResultSound();

            await Task.CompletedTask;
        }

        /// <summary>
        /// CLEANUP Phase: 정리
        /// </summary>
        public async Task OnCleanupAsync(GameState state)
        {
            GLog.Info($"[Trolley] CLEANUP Phase - Cleaning up");

            // 게임 오브젝트 정리
            // 예: DestroyChoiceButtons();

            // 리소스 언로드
            // 예: spaceService.UnloadTrolleyScene();

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
        /// </summary>
        public TrolleyGameResult GetResult()
        {
            return _gameResult;
        }
    }
}
