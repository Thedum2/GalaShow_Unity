using System;
using System.Collections.Generic;
using Galashow.Bridge;
using Galashow.Bridge.Model;
using Galashow.Core;
using UnityEngine;

namespace Galashow.Common
{
    public enum IssPhase { Idle, Init, Pre, Select, Post, End }
    
    [DefaultExecutionOrder(-5000)]
    public sealed class IssDirector : PersistentMonoSingleton<IssDirector>, IGamePort, ISimulationPort
    {
        private GameHandler _gameHandler;
        private SimulationHandler _simulationHandler;
        
        [Header("Durations (seconds)")]
        [Min(0)] public float preSeconds    = 25f;
        [Min(0)] public float postSeconds   = 15f;
        
        [Header("Services (auto-find if null)")]
        public AudioService  audioService;
        public CameraService cameraService;
        public SpaceService  spaceService;
        public UIService     uiService;
        public VisualService visualService;
        
        public IssPhase CurrentPhase { get; private set; } = IssPhase.Idle;
        public bool IsRunning => CurrentPhase != IssPhase.Idle;
        public Notify.R2U.Selected CurrentRound { get; private set; }

        object _roundToken;
        object _initToken;

        protected override void OnInitializing()
        {
            InitDirector();
        }
        public void InitDirector()
        {
            _gameHandler = BridgeManager.Instance.GetHandler<GameHandler>("GameManager");
            _gameHandler?.AddPort(this);
            
            _simulationHandler = BridgeManager.Instance.GetHandler<SimulationHandler>("SimulationManager");
            _simulationHandler?.AddPort(this);

            audioService ??= GetComponent<AudioService>();
            cameraService ??= GetComponent<CameraService>();
            spaceService  ??= GetComponent<SpaceService>();
            uiService     ??= GetComponent<UIService>();
            visualService ??= GetComponent<VisualService>();
        }

        void NewRoundToken() => _roundToken = new object();
        void CancelRoundTasks() { if (_roundToken != null) { TaskRunner.Instance.CancelAll(_roundToken); _roundToken = null; } }
        void NewInitToken()  => _initToken = new object();
        void CancelInitTasks(){ if (_initToken != null)  { TaskRunner.Instance.CancelAll(_initToken);  _initToken  = null; } }

        #region REACT Bridge
        
        public void R2U_GameManager_Initialize_REQ(Request.R2U.Initialize data, Action<Acknowledge.U2R.Initialize> onSuccess, Action<string> onError)
        {
            GLog.Info($"[R2U] Initialize REQ (player={data?.PlayerName}, session={data?.SessionId})", tag: nameof(IssDirector), ctx: this);
            HandleInitialization(data, onSuccess, onError);
        }

        public void R2U_GameManager_Ended_NTY()
        {
            GLog.Info("[R2U] Game Ended NTY", tag: nameof(IssDirector));
            CancelRoundTasks();
            CancelInitTasks();
            CurrentPhase = IssPhase.Idle;
            CurrentRound = null;
            GLog.Info("Game ended via NTY, returning to Idle state.", tag: nameof(IssDirector));
        }

        public void R2U_SimulationManager_Selected_NTY(Notify.R2U.Selected data)
        {
            GLog.Info($"[R2U] Selected Round index={data?.Index}", tag: nameof(IssDirector), ctx: this);
            HandleRoundSelected(data);
        }

        public void R2U_SimulationManager_PreStarted_REQ(Action onSuccess, Action<string> onError)
        {
            GLog.Debug("[R2U] Pre Start REQ", tag: nameof(IssDirector));
            HandlePrePhaseStart(onSuccess, onError);
        }

        public void R2U_SimulationManager_SelectStarted_NTY()
        {
            GLog.Debug("[R2U] Select Start NTY", tag: nameof(IssDirector));
            HandleSelectPhaseStart();
        }

        public void R2U_SimulationManager_SelectEvent_NTY(Notify.R2U.SelectEvent data)
        {
            if (CurrentPhase != IssPhase.Select) return;
            HandleSelectEvent(data);
        }

        public void R2U_SimulationManager_PostStarted_REQ(Request.R2U.PostStarted data, Action onSuccess, Action<string> onError)
        {
            GLog.Debug($"[R2U] Post Start REQ (result={data?.ResultChoiceId})", tag: nameof(IssDirector));
            HandlePostPhaseStart(data, onSuccess, onError);
        }

        public void R2U_SimulationManager_Ended_NTY()
        {
            GLog.Debug("[R2U] Round End NTY", tag: nameof(IssDirector));
            HandleRoundEnd();
        }
        #endregion

        private void HandleInitialization(Request.R2U.Initialize data, Action<Acknowledge.U2R.Initialize> onSuccess, Action<string> onError)
        {
            try
            {
                CurrentPhase = IssPhase.Init;
                CancelInitTasks();
                NewInitToken();

                float progress = 0f;
                float target = 0f;
                string currentTask = "Boot";

                TaskRunner.Instance.Every(0.2f, () =>
                {
                    progress = Mathf.MoveTowards(progress, target, 0.12f);
                    _gameHandler?.LoadingProgress(progress, currentTask);
                }, owner: _initToken, unscaled: true);

                var steps = new List<(string label, Action action)>
                {
                    ("Boot Core", () => {}),
                    ("Prepare Space", () => spaceService?.PrepareForRound(null)),
                    ("Prepare Audio", () => audioService?.PrepareForRound(null)),
                    ("Prepare Camera", () => cameraService?.PrepareForRound(null)),
                    ("Prepare UI", () => uiService?.SetupForRound(null)),
                    ("Prepare Visual", () => visualService?.PrepareForRound(null))
                };

                int idx = 0; int n = steps.Count;
                Action stepper = null;
                stepper = () =>
                {
                    if (_initToken == null) return; // 취소됨
                    if (idx >= n)
                    {
                        // 4) 마무리
                        currentTask = "Finalize";
                        target = 1.0f;

                        TaskRunner.Instance.Delay(0.3f, () =>
                        {
                            try
                            {
                                var ack = new Acknowledge.U2R.Initialize { GameVersion = Application.version, MaxPlayers  = 100 };
                                onSuccess?.Invoke(ack);
                                GLog.Info("Initialize ACK sent.", tag: nameof(IssDirector));
                            }
                            catch (Exception ex)
                            {
                                GLog.Error("Initialize ACK failed", tag: nameof(IssDirector), ex: ex);
                                onError?.Invoke(ex.Message);
                            }
                            finally { CancelInitTasks(); }
                        }, owner: _initToken, unscaled: true);
                        return;
                    }

                    var (label, action) = steps[idx];
                    currentTask = label;
                    target = Mathf.Clamp01((idx + 1) / (float)n * 0.97f);

                    try { action?.Invoke(); }
                    catch (Exception stepEx)
                    {
                        GLog.Error($"Initialize step failed: {label}", tag: nameof(IssDirector), ex: stepEx);
                        onError?.Invoke($"Init step error: {label} - {stepEx.Message}");
                        CancelInitTasks();
                        return;
                    }

                    idx++;
                    TaskRunner.Instance.DelayFrames(1, stepper, owner: _initToken);
                };
                stepper();
            }
            catch (Exception ex)
            {
                GLog.Error("Initialize REQ handler failed", tag: nameof(IssDirector), ex: ex);
                onError?.Invoke(ex.Message);
                CancelInitTasks();
            }
        }

        void HandleRoundSelected(Notify.R2U.Selected info)
        {
            CancelRoundTasks();
            NewRoundToken();
            CurrentRound = info;
            CurrentPhase = IssPhase.Init;

            spaceService?.PrepareForRound(info);
            cameraService?.PrepareForRound(info);
            audioService?.PrepareForRound(info);
            uiService?.SetupForRound(info);
            visualService?.PrepareForRound(info);

            uiService?.ShowSubtitle($"Round {info.Index}: {info.Title}", 2.0f);
            cameraService?.PlayShot("RoundIntro");

            GLog.Info($"Round {info.Index} initialized.", tag: nameof(IssDirector));
        }

        void HandlePrePhaseStart(Action onSuccess, Action<string> onError)
        {
            try
            {
                if (CurrentRound == null)
                {
                    GLog.Warn("PrePhase ignored: no round.", tag: nameof(IssDirector));
                    onError?.Invoke("PrePhase ignored: no round.");
                    return;
                }

                CancelRoundTasks();
                NewRoundToken();
                CurrentPhase = IssPhase.Pre;

                audioService?.PlayBgm("pre_bgm", fadeIn: 0.5f);
                uiService?.ShowHud(true);
                uiService?.SetChoices(CurrentRound.Choices);

                float seconds = preSeconds;
                TaskRunner.Instance.Every(1f, () => uiService?.SetCountdown(seconds = Mathf.Max(0, seconds - 1f)), owner: _roundToken, duration: preSeconds + 0.1f, unscaled: true);

                TaskRunner.Instance.Delay(preSeconds, () =>
                {
                    GLog.Info("Pre-phase ended. Sending ACK.", tag: nameof(IssDirector));
                    onSuccess?.Invoke();
                }, owner: _roundToken, unscaled: true);
            }
            catch (Exception ex)
            {
                GLog.Error("HandlePrePhaseStart failed", tag: nameof(IssDirector), ex: ex);
                onError?.Invoke(ex.Message);
            }
        }

        void HandleSelectPhaseStart()
        {
            if (CurrentRound == null) { GLog.Warn("SelectPhase ignored: no round.", tag: nameof(IssDirector)); return; }
            CancelRoundTasks();
            NewRoundToken();
            CurrentPhase = IssPhase.Select;

            audioService?.Duck(true);
            uiService?.ShowSelectionPrompt(true);

            // 타이머 로직 제거. React의 PostStarted_REQ를 무기한 대기합니다。
        }

        void HandleSelectEvent(Notify.R2U.SelectEvent ev)
        {
            visualService?.PulseChoice(ev.Select);
            uiService?.MarkUserChoice(ev.UserIndex, ev.Select);
            audioService?.PlaySfx("tick");
        }

        void HandlePostPhaseStart(Request.R2U.PostStarted result, Action onSuccess, Action<string> onError)
        {
            try
            {
                if (CurrentRound == null)
                {
                    GLog.Warn("PostPhase ignored: no round.", tag: nameof(IssDirector));
                    onError?.Invoke("PostPhase ignored: no round.");
                    return;
                }

                CancelRoundTasks();
                NewRoundToken();
                CurrentPhase = IssPhase.Post;

                audioService?.Duck(false);
                cameraService?.PlayShot("Result");
                visualService?.RevealResult(result.ResultChoiceId);

                float seconds = postSeconds;
                TaskRunner.Instance.Every(1f, () => uiService?.SetCountdown(seconds = Mathf.Max(0, seconds - 1f)), owner: _roundToken, duration: postSeconds + 0.1f, unscaled: true);

                TaskRunner.Instance.Delay(postSeconds, () =>
                {
                    GLog.Info("Post-phase ended. Sending ACK.", tag: nameof(IssDirector));
                    onSuccess?.Invoke();
                }, owner: _roundToken, unscaled: true);
            }
            catch (Exception ex)
            {
                GLog.Error("HandlePostPhaseStart failed", tag: nameof(IssDirector), ex: ex);
                onError?.Invoke(ex.Message);
            }
        }

        void HandleRoundEnd()
        {
            CancelRoundTasks();
            CurrentPhase = IssPhase.End;

            uiService?.SetCountdown(0);
            audioService?.StopBgm(fadeOut: 0.5f);
            cameraService?.PlayShot("RoundOutro");
            uiService?.ShowHud(false);

            TaskRunner.Instance.Delay(1.0f, () =>
            {
                CurrentRound = null;
                CurrentPhase = IssPhase.Idle;
                GLog.Info("Round ended → Idle", tag: nameof(IssDirector));
            }, owner: this, unscaled: true);
        }
    }
}