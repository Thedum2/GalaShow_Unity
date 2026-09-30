using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Galashow.Core;
using Object = UnityEngine.Object;

namespace Galashow.RGF
{
    /// <summary>
    /// 미니게임 플러그인 기본 클래스
    /// 게임별 코드는 필요한 단계와 입력·판정만 재정의한다. 공통 처리:
    /// - gameData 변환·검증 (객체 또는 객체 1개 배열 → TData)
    /// - 라운드 시작 시 참가자(생존자) 목록 고정
    /// - INPUT 단계, 생존 참가자의 입력만 OnPlayerInput으로 전달
    /// - 무대 프리팹(StagePrefabPath) 생성·제거
    /// - 판정 결과를 GameState 생존 상태에 반영 (SetSurvived)
    /// </summary>
    /// <typeparam name="TData">StartRound gameData 모델</typeparam>
    public abstract class GamePluginBase<TData> : IGamePlugin, IGameDataParser, IPlayerInputReceiver, IHostInputReceiver, IRoundAbortHandler
        where TData : class
    {
        private GameObject _stageObject;
        private string _promptId;
        private readonly List<string> _participants = new List<string>();

        public abstract string GameName { get; }

        /// <summary>
        /// 현재 라운드 상태 (READY~CLEANUP 동안 유효)
        /// </summary>
        protected GameState State { get; private set; }

        /// <summary>
        /// 현재 라운드 데이터 (gameData가 없으면 null)
        /// </summary>
        protected TData Data { get; private set; }

        /// <summary>
        /// 라운드 시작 시점의 생존 참가자 ID
        /// </summary>
        protected IReadOnlyList<string> Participants => _participants;

        /// <summary>
        /// 무대 프리팹의 Resources 경로 (예: "Trolley/TrolleyStage"). null이면 무대를 만들지 않는다.
        /// 프리팹은 패키지의 Runtime/Resources 아래에 둔다.
        /// </summary>
        protected virtual string StagePrefabPath => null;

        /// <summary>
        /// 생성된 무대 (없으면 null)
        /// </summary>
        protected GameStage Stage { get; private set; }

        protected TStage GetStage<TStage>() where TStage : GameStage => Stage as TStage;

        #region Game Data

        public virtual object ParseGameData(JToken data)
        {
            var token = data;
            if (data is JArray array)
            {
                if (array.Count != 1)
                {
                    throw new FormatException($"{GameName} gameData array must contain exactly 1 item (got {array.Count})");
                }
                token = array[0];
            }

            if (token.Type != JTokenType.Object)
            {
                throw new FormatException($"{GameName} gameData must be an object (got {token.Type})");
            }

            var parsed = token.ToObject<TData>();
            ValidateGameData(parsed);
            return parsed;
        }

        /// <summary>
        /// 변환된 gameData 검증. 잘못되면 예외를 던져 라운드 시작을 거부한다.
        /// </summary>
        protected virtual void ValidateGameData(TData data)
        {
        }

        #endregion

        #region Phase Hooks (게임별 재정의)

        protected virtual Task OnReady() => Task.CompletedTask;
        protected virtual Task OnSetup() => Task.CompletedTask;
        protected virtual Task OnPresent() => Task.CompletedTask;
        protected virtual Task OnInput() => Task.CompletedTask;
        protected virtual Task OnWait() => Task.CompletedTask;
        protected virtual Task OnExecute() => Task.CompletedTask;
        protected virtual Task OnReveal() => Task.CompletedTask;
        protected virtual Task OnCleanup() => Task.CompletedTask;
        protected virtual Task OnPhaseTransition(GamePhase from, GamePhase to) => Task.CompletedTask;

        /// <summary>
        /// INPUT 단계에 생존 참가자가 보낸 입력
        /// </summary>
        protected virtual void OnPlayerInput(PlayerInput input)
        {
        }

        /// <summary>
        /// 호스트 입력 (단계 제한은 게임이 판단한다)
        /// </summary>
        /// <returns>받아들였으면 true</returns>
        protected virtual bool OnHostInput(HostInput input) => false;

        /// <summary>
        /// 라운드 강제 중단. 무대는 이후 자동 제거된다.
        /// </summary>
        protected virtual void OnAborted()
        {
        }

        #endregion

        #region Phase Control

        /// <summary>
        /// 현재 단계의 완료 조건 등록. 조건이 참이 되면 남은 시간과 상관없이 다음 단계로 넘어간다.
        /// 무한 대기 단계(phase_data -1)는 이 조건이 채워질 때까지 기다린다. 단계가 바뀌면 조건은 사라진다.
        /// </summary>
        protected void CompletePhaseWhen(Func<bool> condition)
        {
            if (State != null)
            {
                State.PhaseCompleteCondition = condition;
            }
        }

        /// <summary>
        /// 현재 단계를 바로 끝낸다
        /// </summary>
        protected void CompletePhase() => CompletePhaseWhen(() => true);

        #endregion

        #region Host Prompt

        /// <summary>
        /// React에 호스트 선택 팝업을 연다. 고른 값은 OnHostInput(command, 옵션 Id)으로 들어온다.
        /// 라운드가 끝나거나 중단되면 자동으로 닫힌다.
        /// </summary>
        protected void ShowHostPrompt(HostPrompt prompt)
        {
            CloseHostPrompt();
            prompt.PromptId ??= Guid.NewGuid().ToString("N");
            _promptId = prompt.PromptId;
            HostPromptBus.Open(prompt);
        }

        /// <summary>
        /// 열린 호스트 팝업을 닫는다 (선택 확정 시점 등)
        /// </summary>
        protected void CloseHostPrompt()
        {
            if (_promptId == null)
            {
                return;
            }
            HostPromptBus.Close(_promptId);
            _promptId = null;
        }

        #endregion

        #region Result Helpers

        /// <summary>
        /// 연습 라운드 여부 (Tutorial). 연습이면 SetSurvived가 생존 상태를 바꾸지 않는다.
        /// </summary>
        protected bool IsPractice => State != null && State.IsPractice;

        /// <summary>
        /// 참가자 생존·탈락 확정 (RoundCompleted 결과에 반영). 연습 라운드에서는 반영하지 않는다.
        /// </summary>
        protected void SetSurvived(string playerId, bool survived)
        {
            if (IsPractice)
            {
                return;
            }
            State.UpdatePlayerStatus(playerId, survived);
        }

        #endregion

        #region IGamePlugin

        Task IGamePlugin.OnReadyAsync(GameState state)
        {
            State = state;
            Data = state.GameData as TData;
            if (state.GameData != null && Data == null)
            {
                throw new InvalidOperationException($"{GameName} expects {typeof(TData).Name} but got {state.GameData.GetType().Name}");
            }

            _participants.Clear();
            _participants.AddRange(state.GetAlivePlayers().Select(p => p.Id));
            return OnReady();
        }

        Task IGamePlugin.OnSetupAsync(GameState state)
        {
            CreateStage(state);
            return OnSetup();
        }

        Task IGamePlugin.OnPresentAsync(GameState state) => OnPresent();
        Task IGamePlugin.OnInputAsync(GameState state) => OnInput();
        Task IGamePlugin.OnWaitAsync(GameState state) => OnWait();
        Task IGamePlugin.OnExecuteAsync(GameState state) => OnExecute();
        Task IGamePlugin.OnRevealAsync(GameState state) => OnReveal();

        async Task IGamePlugin.OnCleanupAsync(GameState state)
        {
            try
            {
                await OnCleanup();
            }
            finally
            {
                ReleaseRound();
            }
        }

        Task IGamePlugin.OnPhaseTransitionAsync(GamePhase from, GamePhase to) => OnPhaseTransition(from, to);

        void IPlayerInputReceiver.ReceiveInput(PlayerInput input)
        {
            if (State == null || State.CurrentPhase != GamePhase.INPUT)
            {
                return;
            }

            if (!State.Players.TryGetValue(input.PlayerId ?? "", out var player) || !player.IsAlive)
            {
                return;
            }

            OnPlayerInput(input);
        }

        bool IHostInputReceiver.ReceiveHostInput(HostInput input)
        {
            return State != null && OnHostInput(input);
        }

        void IRoundAbortHandler.OnRoundAborted(GameState state)
        {
            try
            {
                OnAborted();
            }
            finally
            {
                ReleaseRound();
            }
        }

        #endregion

        #region Stage

        /// <summary>
        /// 무대 오브젝트 생성. 기본은 StagePrefabPath 프리팹을 GameRoot 아래에 만든다.
        /// 프리팹 없이 코드로 무대를 만들 때 재정의한다. null이면 무대 없이 진행한다.
        /// </summary>
        protected virtual GameObject CreateStageObject(GameState state)
        {
            var path = StagePrefabPath;
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                GLog.Warn($"[{GameName}] Stage prefab not found: Resources/{path}");
                return null;
            }

            var parent = state.GameRoot != null ? state.GameRoot.transform : null;
            return Object.Instantiate(prefab, parent);
        }

        private void CreateStage(GameState state)
        {
            _stageObject = CreateStageObject(state);
            if (_stageObject == null)
            {
                return;
            }

            Stage = _stageObject.GetComponent<GameStage>();
            Stage?.Initialize(state);
        }

        private void ReleaseRound()
        {
            CloseHostPrompt();

            if (_stageObject != null)
            {
                Object.Destroy(_stageObject);
            }

            _stageObject = null;
            Stage = null;
            Data = null;
            State = null;
            _participants.Clear();
        }

        #endregion
    }
}
