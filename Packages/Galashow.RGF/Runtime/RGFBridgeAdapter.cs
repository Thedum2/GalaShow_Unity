using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Galashow.Core;
using Galashow.Bridge;
using Galashow.Bridge.Model;

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
                    foreach (var player in data.PlayerInfo)
                    {
                        RGFManager.Instance.State.AddPlayer(
                            player.PlayerIdx.ToString(),
                            player.PlayerName
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

                // RGFFlowTestExample에 성공 알림
                var flowTestMonitor = UnityEngine.Object.FindObjectOfType<RGFFlowTestExample>();
                flowTestMonitor?.OnInitializeSuccess();

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

                        // DummyGamePlugin인 경우 RGFFlowTestExample에 UUID 전달
                        if (pluginRequest.MiniGameName == "DummyGame")
                        {
                            var flowTestMonitor = UnityEngine.Object.FindObjectOfType<RGFFlowTestExample>();
                            flowTestMonitor?.OnRegisterPluginSuccess();
                            flowTestMonitor?.SetPluginUuid(pluginUuid);
                        }

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

                // RGFFlowTestExample에 성공 알림
                var flowTestMonitor = UnityEngine.Object.FindObjectOfType<RGFFlowTestExample>();
                flowTestMonitor?.OnStartRoundSuccess();

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
                await rgfManager.StartRoundAsync(
                    data.MiniGamePluginIdx,
                    data.RoundNumber,
                    data.GameData
                );

                // 라운드 완료 알림
                SendRoundCompletedNotify(data.RoundNumber, data.MiniGamePluginIdx);
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
            // 채팅 입력 처리 (현재는 로그만)
            if (data.ChatInfo != null)
            {
                GLog.Debug($"[BRIDGE→] Chat - {data.ChatInfo.Count} messages");
            }
        }

        #endregion

        #region Helper Methods

        private IGamePlugin CreatePluginByName(string gameName)
        {
            // 플러그인 팩토리 패턴
            // 실제로는 리플렉션이나 플러그인 레지스트리를 통해 동적으로 생성해야 함

            // 여기서는 더미 데이터를 위한 간단한 구현
            switch (gameName)
            {
                case "DummyGame":
                    // RGFFlowTestExample을 찾아서 DummyGamePlugin 생성
                    var flowTestMonitor = UnityEngine.Object.FindObjectOfType<RGFFlowTestExample>();
                    if (flowTestMonitor != null)
                    {
                        var plugin = new DummyGamePlugin(flowTestMonitor);
                        GLog.Info($"[BRIDGE] DummyGamePlugin created for RGFFlowTestExample");
                        return plugin;
                    }
                    else
                    {
                        GLog.Warn($"[BRIDGE⚠] RGFFlowTestExample not found in scene");
                        return null;
                    }

                default:
                    GLog.Warn($"[BRIDGE⚠] Unknown game plugin: {gameName}");
                    return null;
            }
        }

        private void SendRoundCompletedNotify(int roundNumber, string pluginIdx)
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
                TotalPlayTime = UnityEngine.Time.time - state.PhaseStartTime
            };

            _handler.RoundCompleted(roundNumber, pluginIdx, plugin.GameName, result);

            GLog.Info($"[BRIDGE←] RoundCompleted NTY - Survivors: {alivePlayers.Count}/{state.Players.Count}");
        }

        #endregion
    }
}
