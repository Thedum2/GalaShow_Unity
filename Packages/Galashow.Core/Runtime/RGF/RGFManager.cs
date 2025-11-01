using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Galashow.Core
{
    /// <summary>
    /// RGF (Round Game Framework) 매니저
    /// 게임 플러그인을 로드하고 8단계 생명주기를 실행하는 핵심 엔진
    /// </summary>
    public class RGFManager : PersistentMonoSingleton<RGFManager>
    {
        /// <summary>
        /// 게임 상태
        /// </summary>
        public GameState State { get; private set; } = new GameState();

        /// <summary>
        /// 현재 실행 중인 플러그인
        /// </summary>
        private IGamePlugin _currentPlugin;

        /// <summary>
        /// 등록된 플러그인 딕셔너리
        /// Key: 게임 타입 (예: "trolley_dilemma")
        /// </summary>
        private Dictionary<string, IGamePlugin> _plugins = new Dictionary<string, IGamePlugin>();

        /// <summary>
        /// Phase별 기본 지속 시간 (초)
        /// </summary>
        private Dictionary<GamePhase, float> _phaseDurations = new Dictionary<GamePhase, float>
        {
            { GamePhase.READY, 3f },
            { GamePhase.SETUP, 1f },
            { GamePhase.PRESENT, 3f },
            { GamePhase.INPUT, 30f },
            { GamePhase.WAIT, 0f },
            { GamePhase.EXECUTE, 2f },
            { GamePhase.REVEAL, 8f },
            { GamePhase.CLEANUP, 2f }
        };

        /// <summary>
        /// 라운드 실행 중 여부
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Phase 시작 이벤트
        /// </summary>
        public event Action<GamePhase> OnPhaseStarted;

        /// <summary>
        /// Phase 종료 이벤트
        /// </summary>
        public event Action<GamePhase> OnPhaseEnded;

        protected override void Awake()
        {
            base.Awake();
            State.GameRoot = gameObject;
            GLog.Info("[RGF] RGFManager initialized");
        }

        #region Plugin Management

        /// <summary>
        /// 게임 플러그인 등록
        /// </summary>
        public void RegisterPlugin(IGamePlugin plugin)
        {
            if (plugin == null)
            {
                GLog.Error("[RGF] Cannot register null plugin");
                return;
            }

            _plugins[plugin.GameType] = plugin;
            GLog.Info($"[RGF] Plugin registered: {plugin.GameType} ({plugin.GameName})");
        }

        /// <summary>
        /// 게임 플러그인 등록 해제
        /// </summary>
        public void UnregisterPlugin(string gameType)
        {
            if (_plugins.Remove(gameType))
            {
                GLog.Info($"[RGF] Plugin unregistered: {gameType}");
            }
        }

        /// <summary>
        /// 플러그인 조회
        /// </summary>
        public IGamePlugin GetPlugin(string gameType)
        {
            _plugins.TryGetValue(gameType, out var plugin);
            return plugin;
        }

        /// <summary>
        /// 현재 실행 중인 플러그인
        /// </summary>
        public IGamePlugin CurrentPlugin => _currentPlugin;

        #endregion

        #region Phase Configuration

        /// <summary>
        /// Phase 지속 시간 설정
        /// </summary>
        public void SetPhaseDuration(GamePhase phase, float duration)
        {
            _phaseDurations[phase] = duration;
        }

        /// <summary>
        /// Phase 지속 시간 가져오기
        /// </summary>
        public float GetPhaseDuration(GamePhase phase)
        {
            return _phaseDurations.TryGetValue(phase, out var duration) ? duration : 0f;
        }

        #endregion

        #region Round Execution

        /// <summary>
        /// 라운드 시작
        /// </summary>
        /// <param name="gameType">게임 타입</param>
        /// <param name="roundNumber">라운드 번호</param>
        /// <param name="gameData">게임 데이터</param>
        public async Task StartRoundAsync(string gameType, int roundNumber, object gameData = null)
        {
            if (IsRunning)
            {
                GLog.Warn("[RGF] Round already running, ignoring start request");
                return;
            }

            if (!_plugins.TryGetValue(gameType, out var plugin))
            {
                GLog.Error($"[RGF] Plugin not found for game type: {gameType}");
                return;
            }

            IsRunning = true;
            _currentPlugin = plugin;
            State.GameType = gameType;
            State.CurrentRound = roundNumber;
            State.GameData = gameData;

            GLog.Info($"[RGF] Starting Round {roundNumber} - {gameType}");

            try
            {
                // 8단계 생명주기 실행
                await ExecutePhaseAsync(GamePhase.READY);
                await ExecutePhaseAsync(GamePhase.SETUP);
                await ExecutePhaseAsync(GamePhase.PRESENT);
                await ExecutePhaseAsync(GamePhase.INPUT);
                await ExecutePhaseAsync(GamePhase.WAIT);
                await ExecutePhaseAsync(GamePhase.EXECUTE);
                await ExecutePhaseAsync(GamePhase.REVEAL);
                await ExecutePhaseAsync(GamePhase.CLEANUP);

                GLog.Info($"[RGF] Round {roundNumber} completed");
            }
            catch (Exception ex)
            {
                GLog.Error($"[RGF] Round execution error: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
                _currentPlugin = null;
            }
        }

        /// <summary>
        /// 특정 Phase만 실행
        /// </summary>
        public async Task ExecutePhaseAsync(GamePhase phase)
        {
            // Phase 전환
            var oldPhase = State.CurrentPhase;
            State.TransitPhase(phase);
            State.PhaseStartTime = Time.time;
            State.PhaseDuration = _phaseDurations[phase];
            OnPhaseStarted?.Invoke(phase);

            // 이전 Phase 토큰 취소
            if (State.CancellationToken != null)
            {
                TaskRunner.Instance.CancelAll(State.CancellationToken);
            }

            // 새 토큰 생성
            State.CancellationToken = new object();

            GLog.Debug($"[RGF] Phase {phase} started (duration: {State.PhaseDuration}s)");

            try
            {
                // Phase 전환 콜백
                if (_currentPlugin != null)
                {
                    await _currentPlugin.OnPhaseTransitionAsync(oldPhase, phase);
                }

                // Phase별 플러그인 메서드 호출
                switch (phase)
                {
                    case GamePhase.READY:
                        await _currentPlugin.OnReadyAsync(State);
                        break;
                    case GamePhase.SETUP:
                        await _currentPlugin.OnSetupAsync(State);
                        break;
                    case GamePhase.PRESENT:
                        await _currentPlugin.OnPresentAsync(State);
                        break;
                    case GamePhase.INPUT:
                        await _currentPlugin.OnInputAsync(State);
                        break;
                    case GamePhase.WAIT:
                        await _currentPlugin.OnWaitAsync(State);
                        break;
                    case GamePhase.EXECUTE:
                        await _currentPlugin.OnExecuteAsync(State);
                        break;
                    case GamePhase.REVEAL:
                        await _currentPlugin.OnRevealAsync(State);
                        break;
                    case GamePhase.CLEANUP:
                        await _currentPlugin.OnCleanupAsync(State);
                        break;
                }

                // Phase 지속 시간 대기
                if (State.PhaseDuration > 0)
                {
                    await Task.Delay((int)(State.PhaseDuration * 1000));
                }

                GLog.Debug($"[RGF] Phase {phase} ended");
            }
            catch (Exception ex)
            {
                GLog.Error($"[RGF] Phase {phase} error: {ex.Message}");
                throw;
            }
            finally
            {
                OnPhaseEnded?.Invoke(phase);
            }
        }

        /// <summary>
        /// 라운드 강제 중단
        /// </summary>
        public void AbortRound()
        {
            if (!IsRunning)
            {
                return;
            }

            GLog.Warn("[RGF] Round aborted");

            if (State.CancellationToken != null)
            {
                TaskRunner.Instance.CancelAll(State.CancellationToken);
            }

            IsRunning = false;
            _currentPlugin = null;
        }

        #endregion

        #region State Management

        /// <summary>
        /// 상태 초기화
        /// </summary>
        public void ResetState()
        {
            State.Reset();
            GLog.Info("[RGF] State reset");
        }

        #endregion
    }
}
