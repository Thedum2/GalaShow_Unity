using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Galashow.Core;
using Galashow.Bridge;
using Galashow.Bridge.Model;
using Newtonsoft.Json.Linq;

namespace Galashow.RGF
{
    /// <summary>
    /// RGF와 Bridge를 연결하는 어댑터
    /// IRGFPort 인터페이스를 구현하여 React와의 통신을 중개
    /// </summary>
    public class RGFBridgeAdapter : MonoSingleton<RGFBridgeAdapter>, IRGFPort
    {
        private RGFHandler _handler;
        private bool _isInitialized = false;

        /// <summary>
        /// Initialize 처리 성공
        /// </summary>
        public event Action Initialized;

        /// <summary>
        /// 플러그인 등록 성공 (miniGameName, UUID)
        /// </summary>
        public event Action<string, string> PluginRegistered;

        /// <summary>
        /// StartRound 수락 (라운드 번호)
        /// </summary>
        public event Action<int> RoundStartAccepted;

        protected override void Awake()
        {
            base.Awake();

            // RGFHandler 생성 및 BridgeManager에 등록
            _handler = new RGFHandler();
            BridgeManager.Instance.RegisterHandler(_handler);

            // 이 어댑터를 Port로 등록
            _handler.AddPort(this);

            // RGFManager의 Phase 이벤트 구독
            SubscribeToRGFEvents();

            GLog.Info("[BRIDGE] RGFBridgeAdapter initialized");
        }

        private void OnDestroy()
        {
            HostPromptBus.Opened -= OnHostPromptOpened;
            HostPromptBus.Closed -= OnHostPromptClosed;
            if (_handler != null)
            {
                _handler.RemovePort(this);
                BridgeManager.Instance.UnregisterHandler(_handler.GetRoute());
            }
        }

        #region RGF Event Subscription

        private void SubscribeToRGFEvents()
        {
            var rgfManager = RGFManager.Instance;

            // Phase 시작 이벤트
            rgfManager.OnPhaseStarted += OnPhaseStarted;

            // Phase 종료 이벤트
            rgfManager.OnPhaseEnded += OnPhaseEnded;

            // GameState의 Phase 변경 이벤트
            rgfManager.State.OnPhaseChanged += OnPhaseChanged;

            // 호스트 선택 팝업 (게임 공통)
            HostPromptBus.Opened += OnHostPromptOpened;
            HostPromptBus.Closed += OnHostPromptClosed;
        }

        private void OnHostPromptOpened(HostPrompt prompt)
        {
            _handler.PromptOpened(new Notify.U2R.RGFPromptOpened
            {
                RoundNumber = RGFManager.Instance.State.CurrentRound,
                PromptId = prompt.PromptId,
                Command = prompt.Command,
                Title = prompt.Title,
                Description = prompt.Description,
                ActionLabel = prompt.ActionLabel,
                Hint = prompt.Hint,
                Options = prompt.Options.Select(o => new Notify.U2R.PromptOption
                {
                    Id = o.Id,
                    Number = o.Number,
                    Label = o.Label,
                    Description = o.Description
                }).ToList()
            });
        }

        private void OnHostPromptClosed(string promptId)
        {
            _handler.PromptClosed(RGFManager.Instance.State.CurrentRound, promptId);
        }

        private void OnPhaseStarted(GamePhase phase)
        {
            float duration = RGFManager.Instance.GetPhaseDuration(phase);
            _handler.PhaseStarted(phase.ToString(), duration);
        }

        private void OnPhaseEnded(GamePhase phase)
        {
            float duration = RGFManager.Instance.GetPhaseDuration(phase);
            _handler.PhaseEnded(phase.ToString(), duration);
        }

        private void OnPhaseChanged(GamePhase fromPhase, GamePhase toPhase)
        {
            var rgfManager = RGFManager.Instance;
            int roundNumber = rgfManager.State.CurrentRound;
            string pluginIdx = rgfManager.CurrentPluginUuid ?? "";

            _handler.PhaseChanged(roundNumber, pluginIdx, fromPhase.ToString(), toPhase.ToString());
        }

        #endregion

        #region IRGFPort Implementation

        public void R2U_RGFManager_Initialize_REQ(
            Request.R2U.RGFInitialize data,
                Action<Acknowledge.U2R.RGFInitialize> onSuccess,
                Action<string> onError)
        {
            GLog.Info($"[BRIDGE→] Initialize - Session: {data.SessionId}, Players: {data.PlayerInfo?.Count ?? 0}");

            try
            {
                // 진행도 알림
                _handler.InitializeProgress(0);

                // 플레이어 정보 등록
                if (data.PlayerInfo != null)
                {
                    // avatarName이 없는 참가자는 로비에서 고른 아바타(config.avatarNames)를 순서대로 받는다
                    var pool = data.Config?.AvatarNames?.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    int next = 0;
                    foreach (var player in data.PlayerInfo)
                    {
                        var avatar = player.AvatarName;
                        if (string.IsNullOrWhiteSpace(avatar) && pool != null && pool.Count > 0)
                        {
                            avatar = pool[next++ % pool.Count];
                        }

                        RGFManager.Instance.State.AddPlayer(
                            player.PlayerIdx.ToString(),
                            player.PlayerName,
                            avatar
                        );
                    }
                }

                _handler.InitializeProgress(50);

                // 설정 적용
                if (data.Config != null)
                {
                    // Config 처리
                }

                _handler.InitializeProgress(100);

                _isInitialized = true;
                onSuccess?.Invoke(new Acknowledge.U2R.RGFInitialize(true, "1.0.0"));

                Initialized?.Invoke();

                GLog.Info("[BRIDGE←] Initialize ACK");
            }
            catch (Exception ex)
            {
                GLog.Error($"[BRIDGE✗] Initialize failed: {ex.Message}");
                onError?.Invoke(ex.Message);
            }
        }

        public void R2U_RGFManager_RegisterPlugin_REQ(
            List<Request.R2U.RGFRegisterPlugin> data,
            Action<List<Acknowledge.U2R.RGFRegisterPlugin>> onSuccess,
            Action<string> onError)
        {
            GLog.Info($"[BRIDGE→] RegisterPlugin - Count: {data?.Count ?? 0}");

            try
            {
                var results = new List<Acknowledge.U2R.RGFRegisterPlugin>();

                foreach (var pluginRequest in data)
                {
                    IGamePlugin plugin = CreatePluginByName(pluginRequest.MiniGameName);

                    if (plugin != null)
                    {
                        string pluginUuid = RGFManager.Instance.RegisterPlugin(plugin);

                        PluginRegistered?.Invoke(pluginRequest.MiniGameName, pluginUuid);

                        results.Add(new Acknowledge.U2R.RGFRegisterPlugin(
                            pluginRequest.MiniGameIdx,
                            pluginRequest.MiniGameName,
                            pluginUuid
                        ));
                    }
                    else
                    {
                        GLog.Warn($"[BRIDGE⚠] Plugin not found: {pluginRequest.MiniGameName}");
                    }
                }

                onSuccess?.Invoke(results);

                GLog.Info("[BRIDGE←] RegisterPlugin ACK");
            }
            catch (Exception ex)
            {
                GLog.Error($"[BRIDGE✗] RegisterPlugin failed: {ex.Message}");
                onError?.Invoke(ex.Message);
            }
        }

        public async void R2U_RGFManager_StartRound_REQ(
            Request.R2U.RGFStartRound data,
            Action<Acknowledge.U2R.RGFStartRound> onSuccess,
            Action<string> onError)
        {
            GLog.Info($"[BRIDGE→] StartRound - Round: {data.RoundNumber}");

            try
            {
                var rgfManager = RGFManager.Instance;

                // 게임 데이터 변환·검증 (실패 시 ACK 전에 거부)
                bool hasGameData = data.GameData != null && data.GameData.Type != JTokenType.Null;
                object gameData = hasGameData ? data.GameData : null;
                if (rgfManager.GetPlugin(data.MiniGamePluginIdx) is IGameDataParser parser && hasGameData)
                {
                    gameData = parser.ParseGameData(data.GameData);
                }

                // Phase Duration 설정
                if (data.PhaseDuration != null)
                {
                    rgfManager.SetPhaseDuration(GamePhase.READY, data.PhaseDuration.Ready);
                    rgfManager.SetPhaseDuration(GamePhase.SETUP, data.PhaseDuration.Setup);
                    rgfManager.SetPhaseDuration(GamePhase.PRESENT, data.PhaseDuration.Present);
                    rgfManager.SetPhaseDuration(GamePhase.INPUT, data.PhaseDuration.Input);
                    rgfManager.SetPhaseDuration(GamePhase.WAIT, data.PhaseDuration.Wait);
                    rgfManager.SetPhaseDuration(GamePhase.EXECUTE, data.PhaseDuration.Execute);
                    rgfManager.SetPhaseDuration(GamePhase.REVEAL, data.PhaseDuration.Reveal);
                    rgfManager.SetPhaseDuration(GamePhase.CLEANUP, data.PhaseDuration.Cleanup);
                }

                // ACK 응답
                onSuccess?.Invoke(new Acknowledge.U2R.RGFStartRound(true, data.RoundNumber));

                RoundStartAccepted?.Invoke(data.RoundNumber);

                GLog.Info("[BRIDGE←] StartRound ACK");

                // 라운드 시작 알림
                var plugin = rgfManager.GetPlugin(data.MiniGamePluginIdx);
                if (plugin != null)
                {
                    _handler.RoundStarted(data.RoundNumber, data.MiniGamePluginIdx, plugin.GameName);
                    GLog.Info("[BRIDGE←] RoundStarted NTY");
                }
                else
                {
                    GLog.Error($"[BRIDGE✗] Plugin not found: {data.MiniGamePluginIdx}");
                    return;
                }

                // 라운드 실행 (비동기)
                bool completed = await rgfManager.StartRoundAsync(
                    data.MiniGamePluginIdx,
                    data.RoundNumber,
                    gameData,
                    data.Practice
                );

                // 라운드 완료 알림
                if (completed)
                {
                    SendRoundCompletedNotify(data.RoundNumber, data.MiniGamePluginIdx, data.Practice);
                }
            }
            catch (Exception ex)
            {
                GLog.Error($"[BRIDGE✗] StartRound failed: {ex.Message}");
                onError?.Invoke(ex.Message);
            }
        }

        public void R2U_RGFManager_AbortRound_REQ(
            Request.R2U.RGFAbortRound data,
            Action<Acknowledge.U2R.RGFAbortRound> onSuccess,
            Action<string> onError)
        {
            GLog.Info($"[BRIDGE→] AbortRound");

            try
            {
                RGFManager.Instance.AbortRound();
                onSuccess?.Invoke(new Acknowledge.U2R.RGFAbortRound(true, data.RoundNumber));
                GLog.Info("[BRIDGE←] AbortRound ACK");
            }
            catch (Exception ex)
            {
                GLog.Error($"[BRIDGE✗] AbortRound failed: {ex.Message}");
                onError?.Invoke(ex.Message);
            }
        }

        public void R2U_RGFManager_ChatInput_NTY(Notify.R2U.RGFChatInput data)
        {
            if (data?.ChatInfo == null)
            {
                return;
            }

            var rgfManager = RGFManager.Instance;
            if (!rgfManager.IsRunning || data.RoundNumber != rgfManager.State.CurrentRound)
            {
                GLog.Debug($"[BRIDGE→] Chat ignored - round {data.RoundNumber} not running");
                return;
            }

            GLog.Debug($"[BRIDGE→] Chat - {data.ChatInfo.Count} messages");

            // 입력 단계·생존 여부 확인은 플러그인(GamePluginBase)이 한다
            foreach (var chat in data.ChatInfo)
            {
                rgfManager.SubmitInput(new PlayerInput(chat.PlayerIdx.ToString(), chat.Message, data.InputEventTime));
            }
        }

        public void R2U_RGFManager_HostInput_NTY(Notify.R2U.RGFHostInput data)
        {
            if (data == null)
            {
                return;
            }

            var rgfManager = RGFManager.Instance;
            if (!rgfManager.IsRunning || data.RoundNumber != rgfManager.State.CurrentRound)
            {
                GLog.Debug($"[BRIDGE→] HostInput ignored - round {data.RoundNumber} not running");
                return;
            }

            bool accepted = rgfManager.SubmitHostInput(new HostInput(data.Command, data.Value));
            GLog.Info($"[BRIDGE→] HostInput {data.Command} {(accepted ? "accepted" : "rejected")}");
        }

        #endregion

        #region Helper Methods

        private IGamePlugin CreatePluginByName(string gameName)
        {
            // 미니게임 패키지는 GamePluginCatalog에 자신을 등록한다 (miniGameName = 플러그인 ID)
            if (GamePluginCatalog.TryCreate(gameName, out var plugin))
            {
                return plugin;
            }

            GLog.Warn($"[BRIDGE⚠] Unknown game plugin: {gameName}");
            return null;
        }

        private static readonly Newtonsoft.Json.JsonSerializer CamelCase = Newtonsoft.Json.JsonSerializer.Create(
            new Newtonsoft.Json.JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            });

        private static JToken ToCamelCaseJson(object value)
        {
            if (value == null) return null;
            try
            {
                return JToken.FromObject(value, CamelCase);
            }
            catch (Exception ex)
            {
                GLog.Warn($"[BRIDGE⚠] Result detail not serializable: {ex.Message}");
                return null;
            }
        }

        private void SendRoundCompletedNotify(int roundNumber, string pluginIdx, bool practice)
        {
            var rgfManager = RGFManager.Instance;
            var plugin = rgfManager.GetPlugin(pluginIdx);

            if (plugin == null)
            {
                GLog.Warn("[BRIDGE⚠] Cannot send RoundCompleted: plugin not found");
                return;
            }

            // GameState에서 결과 데이터 가져오기
            var state = rgfManager.State;
            var alivePlayers = state.GetAlivePlayers();
            var eliminatedPlayers = state.GetEliminatedPlayers();

            var result = new Notify.U2R.RoundResult
            {
                SurvivorsUserIdx = alivePlayers.Select(p => int.Parse(p.Id)).ToList(),
                EliminatedUserIdx = eliminatedPlayers.Select(p => int.Parse(p.Id)).ToList(),
                TotalParticipants = state.Players.Count,
                RemainingPlayers = alivePlayers.Count,
                TotalPlayTime = UnityEngine.Time.time - state.PhaseStartTime,
                Detail = ToCamelCaseJson(state.ResultData)
            };

            _handler.RoundCompleted(roundNumber, pluginIdx, plugin.GameName, result, practice);

            GLog.Info($"[BRIDGE←] RoundCompleted NTY - Survivors: {alivePlayers.Count}/{state.Players.Count}");
        }

        #endregion
    }
}
