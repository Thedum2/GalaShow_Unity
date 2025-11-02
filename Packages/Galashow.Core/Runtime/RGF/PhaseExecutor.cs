using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Galashow.Core
{
    /// <summary>
    /// Phase 실행 관리 클래스
    /// Phase별 실행 로직, 타이밍, 전환 처리 담당
    /// </summary>
    public class PhaseExecutor
    {
        /// <summary>
        /// Phase 시작 이벤트
        /// </summary>
        public event Action<GamePhase> OnPhaseStarted;

        /// <summary>
        /// Phase 종료 이벤트
        /// </summary>
        public event Action<GamePhase> OnPhaseEnded;

        /// <summary>
        /// 특정 Phase 실행
        /// </summary>
        public async Task ExecuteAsync(GamePhase phase, IGamePlugin plugin, GameState state)
        {
            if (plugin == null)
            {
                GLog.Error("[PhaseExecutor] Cannot execute phase without plugin");
                return;
            }

            // Phase 전환
            var oldPhase = state.CurrentPhase;
            state.TransitPhase(phase);
            state.PhaseStartTime = Time.time;

            // ========== GameState에서 Duration 가져오기 ==========
            state.PhaseDuration = state.GetPhaseDuration(phase);
            // ==================================================

            OnPhaseStarted?.Invoke(phase);

            // 이전 Phase 토큰 취소
            if (state.CancellationToken != null)
            {
                TaskRunner.Instance.CancelAll(state.CancellationToken);
            }

            // 새 토큰 생성
            state.CancellationToken = new object();

            GLog.Debug($"[PhaseExecutor] Phase {phase} started (duration: {state.PhaseDuration}s)");

            try
            {
                // Phase 전환 콜백
                await plugin.OnPhaseTransitionAsync(oldPhase, phase);

                // Phase별 플러그인 메서드 호출
                await ExecutePhaseMethodAsync(phase, plugin, state);

                // Phase 지속 시간 대기
                if (state.PhaseDuration > 0)
                {
                    await Task.Delay((int)(state.PhaseDuration * 1000));
                }

                GLog.Debug($"[PhaseExecutor] Phase {phase} ended");
            }
            catch (Exception ex)
            {
                GLog.Error($"[PhaseExecutor] Phase {phase} error: {ex.Message}");
                throw;
            }
            finally
            {
                OnPhaseEnded?.Invoke(phase);
            }
        }

        /// <summary>
        /// Phase별 플러그인 메서드 실행
        /// </summary>
        private async Task ExecutePhaseMethodAsync(GamePhase phase, IGamePlugin plugin, GameState state)
        {
            switch (phase)
            {
                case GamePhase.READY:
                    await plugin.OnReadyAsync(state);
                    break;
                case GamePhase.SETUP:
                    await plugin.OnSetupAsync(state);
                    break;
                case GamePhase.PRESENT:
                    await plugin.OnPresentAsync(state);
                    break;
                case GamePhase.INPUT:
                    await plugin.OnInputAsync(state);
                    break;
                case GamePhase.WAIT:
                    await plugin.OnWaitAsync(state);
                    break;
                case GamePhase.EXECUTE:
                    await plugin.OnExecuteAsync(state);
                    break;
                case GamePhase.REVEAL:
                    await plugin.OnRevealAsync(state);
                    break;
                case GamePhase.CLEANUP:
                    await plugin.OnCleanupAsync(state);
                    break;
            }
        }

        /// <summary>
        /// 전체 라운드 8단계 실행
        /// </summary>
        public async Task ExecuteFullRoundAsync(IGamePlugin plugin, GameState state)
        {
            await ExecuteAsync(GamePhase.READY, plugin, state);
            await ExecuteAsync(GamePhase.SETUP, plugin, state);
            await ExecuteAsync(GamePhase.PRESENT, plugin, state);
            await ExecuteAsync(GamePhase.INPUT, plugin, state);
            await ExecuteAsync(GamePhase.WAIT, plugin, state);
            await ExecuteAsync(GamePhase.EXECUTE, plugin, state);
            await ExecuteAsync(GamePhase.REVEAL, plugin, state);
            await ExecuteAsync(GamePhase.CLEANUP, plugin, state);
        }

        /// <summary>
        /// Phase 실행 중단
        /// </summary>
        public void Abort(GameState state)
        {
            if (state.CancellationToken != null)
            {
                TaskRunner.Instance.CancelAll(state.CancellationToken);
                GLog.Warn("[PhaseExecutor] Phase execution aborted");
            }
        }
    }
}
