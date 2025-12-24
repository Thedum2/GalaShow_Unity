using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Galashow.Core;

namespace Galashow.RGF
{
    /// <summary>
    /// Phase 실행 관리 클래스
    /// Phase별 실행 로직, 타이밍, 전환 처리 담당
    /// </summary>
    public class PhaseExecutor
    {
        private MonoBehaviour _coroutineRunner;

        /// <summary>
        /// Phase 시작 이벤트
        /// </summary>
        public event Action<GamePhase> OnPhaseStarted;

        /// <summary>
        /// Phase 종료 이벤트
        /// </summary>
        public event Action<GamePhase> OnPhaseEnded;

        /// <summary>
        /// 코루틴 실행을 위한 MonoBehaviour 설정
        /// </summary>
        public void SetCoroutineRunner(MonoBehaviour runner)
        {
            _coroutineRunner = runner;
        }

        /// <summary>
        /// 특정 Phase 실행
        /// </summary>
        public async Task ExecuteAsync(GamePhase phase, IGamePlugin plugin, GameState state)
        {
            if (_coroutineRunner == null)
            {
                GLog.Error("[RGF✗] PhaseExecutor: coroutine runner not set");
                return;
            }

            if (plugin == null)
            {
                GLog.Error("[RGF✗] PhaseExecutor: plugin is null");
                return;
            }

            var tcs = new TaskCompletionSource<bool>();
            _coroutineRunner.StartCoroutine(ExecuteCoroutine(phase, plugin, state, tcs));
            await tcs.Task;
        }

        /// <summary>
        /// Phase 실행 코루틴 (WebGL 호환)
        /// </summary>
        private IEnumerator ExecuteCoroutine(GamePhase phase, IGamePlugin plugin, GameState state, TaskCompletionSource<bool> tcs)
        {
            // Phase 전환
            var oldPhase = state.CurrentPhase;
            state.TransitPhase(phase);
            state.PhaseStartTime = Time.time;
            state.PhaseDuration = state.GetPhaseDuration(phase);

            OnPhaseStarted?.Invoke(phase);

            // 이전 Phase 토큰 취소
            if (state.CancellationToken != null)
            {
                TaskRunner.Instance.CancelAll(state.CancellationToken);
            }

            // 새 토큰 생성
            state.CancellationToken = new object();

            // Phase 전환 콜백
            var transitionTask = plugin.OnPhaseTransitionAsync(oldPhase, phase);
            yield return new WaitUntil(() => transitionTask.IsCompleted);

            if (transitionTask.Exception != null)
            {
                GLog.Error($"[RGF✗] Phase transition error: {transitionTask.Exception.Message}");
                tcs.TrySetException(transitionTask.Exception);
                yield break;
            }

            // Phase별 플러그인 메서드 호출
            var phaseTask = ExecutePhaseMethodAsync(phase, plugin, state);
            yield return new WaitUntil(() => phaseTask.IsCompleted);

            if (phaseTask.Exception != null)
            {
                GLog.Error($"[RGF✗] Phase {phase} error: {phaseTask.Exception.Message}");
                OnPhaseEnded?.Invoke(phase);
                tcs.TrySetException(phaseTask.Exception);
                yield break;
            }

            // Phase 지속 시간 대기
            if (state.PhaseDuration > 0)
            {
                float endTime = Time.time + state.PhaseDuration;
                while (Time.time < endTime)
                {
                    yield return null;
                }
            }

            OnPhaseEnded?.Invoke(phase);
            tcs.TrySetResult(true);
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
                GLog.Warn("[RGF] Phase aborted");
            }
        }
    }
}
