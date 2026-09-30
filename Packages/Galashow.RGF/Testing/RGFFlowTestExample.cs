using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Galashow.Core;
using Galashow.Bridge;
using Newtonsoft.Json;
using TMPro;

namespace Galashow.RGF.Testing
{
    /// <summary>
    /// RGF 플로우 테스트 모니터
    /// React 없이 샘플 REQ/NTY 메시지를 BridgeManager에 넣어 전체 흐름을 시험하고 상태를 화면에 표시한다.
    /// 샘플 입력도 실제 경로(RGFManager_ChatInput NTY → 어댑터 → 플러그인)로 보낸다.
    /// </summary>
    public class RGFFlowTestExample : MonoBehaviour
    {
        [Header("네이티브 샘플 테스트용 JSON")]
        [SerializeField] private TextAsset initializeJson;
        [SerializeField] private TextAsset registerPluginJson;
        [SerializeField] private TextAsset startRoundJson;

        [Header("상태 표시 UI")]
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private bool enableDetailedInfo = true;

        [Header("샘플 채팅 입력 (INPUT 단계에 순서대로 전송)")]
        [SerializeField] private int[] samplePlayerIdx = { 1, 2, 3 };
        [SerializeField] private string[] sampleMessages = { "1", "2", "1" };

        private string _testPluginUuid;
        private bool _isInitialized;
        private bool _isPluginRegistered;
        private bool _isRoundStarted;

        private static readonly IReadOnlyDictionary<string, string> NoChoices = new Dictionary<string, string>();

        private void Start()
        {
            var adapter = RGFBridgeAdapter.Instance;
            if (adapter == null)
            {
                GLog.Error("[RGFMonitor] RGFBridgeAdapter initialization failed");
                return;
            }

            adapter.Initialized += OnInitialized;
            adapter.PluginRegistered += OnPluginRegistered;
            adapter.RoundStartAccepted += OnRoundStartAccepted;
            RGFManager.Instance.OnPhaseStarted += OnPhaseStarted;

            GLog.Info("[RGFMonitor] RGF 플로우 모니터링 시작 - Native 메시지 대기 중");
        }

        private void OnDestroy()
        {
            if (RGFBridgeAdapter.Instance != null)
            {
                RGFBridgeAdapter.Instance.Initialized -= OnInitialized;
                RGFBridgeAdapter.Instance.PluginRegistered -= OnPluginRegistered;
                RGFBridgeAdapter.Instance.RoundStartAccepted -= OnRoundStartAccepted;
            }

            if (RGFManager.Instance != null)
            {
                RGFManager.Instance.OnPhaseStarted -= OnPhaseStarted;
            }
        }

        private void Update()
        {
            UpdateStatusDisplay();
        }

        #region Adapter Events

        private void OnInitialized()
        {
            _isInitialized = true;
            GLog.Info("[RGFMonitor] Initialize completed successfully");
        }

        private void OnPluginRegistered(string miniGameName, string uuid)
        {
            if (miniGameName != DummyGamePlugin.PluginId)
            {
                return;
            }

            _isPluginRegistered = true;
            _testPluginUuid = uuid;
            GLog.Info($"[RGFMonitor] Plugin UUID set: {uuid}");
        }

        private void OnRoundStartAccepted(int roundNumber)
        {
            _isRoundStarted = true;
            GLog.Info("[RGFMonitor] StartRound completed successfully");
        }

        private void OnPhaseStarted(GamePhase phase)
        {
            GLog.Info($"[Phase→] {phase} ({RGFManager.Instance.GetPhaseDuration(phase)}s)");
        }

        #endregion

        #region Native Sample Message Sender

        [ContextMenu("Send Native Sample: Initialize")]
        public void SendNativeSample_Initialize()
        {
            if (initializeJson == null)
            {
                GLog.Error("[NativeSample] Initialize JSON file not assigned");
                return;
            }

            GLog.Info("[NativeSample] Sending Initialize message...");
            BridgeManager.Instance.ReceiveMessage(initializeJson.text);
        }

        [ContextMenu("Send Native Sample: RegisterPlugin")]
        public void SendNativeSample_RegisterPlugin()
        {
            if (registerPluginJson == null)
            {
                GLog.Error("[NativeSample] RegisterPlugin JSON file not assigned");
                return;
            }

            GLog.Info("[NativeSample] Sending RegisterPlugin message...");
            BridgeManager.Instance.ReceiveMessage(registerPluginJson.text);
        }

        [ContextMenu("Send Native Sample: StartRound")]
        public void SendNativeSample_StartRound()
        {
            if (startRoundJson == null)
            {
                GLog.Error("[NativeSample] StartRound JSON file not assigned");
                return;
            }

            if (string.IsNullOrEmpty(_testPluginUuid))
            {
                GLog.Error("[NativeSample] Plugin not registered yet. Please send RegisterPlugin first.");
                return;
            }

            string jsonMessage = startRoundJson.text.Replace("PLUGIN_UUID_PLACEHOLDER", _testPluginUuid);

            StartCoroutine(SimulatePlayerInputs());

            GLog.Info("[NativeSample] Sending StartRound message...");
            BridgeManager.Instance.ReceiveMessage(jsonMessage);
        }

        /// <summary>
        /// INPUT 단계가 시작되면 샘플 채팅을 RGFManager_ChatInput NTY로 하나씩 보낸다
        /// </summary>
        private IEnumerator SimulatePlayerInputs()
        {
            yield return new WaitUntil(() => RGFManager.Instance.State.CurrentPhase == GamePhase.INPUT);

            GLog.Info("[NativeSample] Simulating player inputs...");
            int count = Mathf.Min(samplePlayerIdx.Length, sampleMessages.Length);
            for (int i = 0; i < count; i++)
            {
                yield return new WaitForSeconds(0.5f);
                SendChatInput(samplePlayerIdx[i], sampleMessages[i], i);
            }
            GLog.Info("[NativeSample] Player inputs complete");
        }

        private void SendChatInput(int playerIdx, string message, int inputIdx)
        {
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var nty = new
            {
                id = $"test_chat_{now}_{inputIdx}",
                type = "NTY",
                route = "RGFManager_ChatInput",
                ok = true,
                data = new
                {
                    roundNumber = RGFManager.Instance.State.CurrentRound,
                    inputEventTime = now,
                    inputIdx,
                    chatInfo = new[] { new { playerIdx, message } }
                },
                timestamp = now.ToString()
            };
            BridgeManager.Instance.ReceiveMessage(JsonConvert.SerializeObject(nty));
        }

        #endregion

        #region UI Update

        private IReadOnlyDictionary<string, string> CurrentChoices =>
            (RGFManager.Instance.CurrentPlugin as DummyGamePlugin)?.Choices ?? NoChoices;

        private void UpdateStatusDisplay()
        {
            if (_statusText == null)
            {
                return;
            }

            if (RGFManager.Instance == null)
            {
                _statusText.text = "RGFManager 초기화 대기 중...";
                return;
            }

            if (!RGFManager.Instance.IsRunning)
            {
                _statusText.text = BuildChecklistText();
                if (_progressText != null)
                {
                    _progressText.text = "";
                }
                return;
            }

            _statusText.text = BuildStatusDisplayText();
            if (_progressText != null)
            {
                _progressText.text = BuildProgressDisplayText();
            }
        }

        private string BuildChecklistText()
        {
            var text = new StringBuilder();
            text.AppendLine("<size=70><b>RGF MONITOR</b></size>");
            text.AppendLine("<size=60><b>RGF 모니터</b></size>");
            text.AppendLine();
            text.AppendLine("<size=50>=== 진행 상황 ===</size>");
            text.AppendLine($"<size=45>{Check(_isInitialized)} Initialize</size>");
            text.AppendLine($"<size=45>{Check(_isPluginRegistered)} Register Plugin</size>");
            text.AppendLine($"<size=45>{Check(_isRoundStarted)} Start Round</size>");
            text.AppendLine();

            string next = !_isInitialized ? "Initialize 버튼 클릭"
                : !_isPluginRegistered ? "RegisterPlugin 버튼 클릭"
                : !_isRoundStarted ? "StartRound 버튼 클릭"
                : "라운드 진행 대기 중...";
            text.AppendLine($"<size=40><color=yellow>{next}</color></size>");
            return text.ToString();
        }

        private static string Check(bool done) => done ? "<color=green>✓</color>" : "<color=red>✗</color>";

        private string BuildStatusDisplayText()
        {
            var text = new StringBuilder();
            var state = RGFManager.Instance.State;

            text.AppendLine($"<size=100><b>{state.CurrentPhase}</b></size>");
            text.AppendLine($"<size=80><b>{GetPhaseSimpleName(state.CurrentPhase)}</b></size>");
            text.AppendLine($"<size=60>ROUND {state.CurrentRound}</size>");

            float remainingTime = Mathf.Max(0, state.PhaseDuration - (Time.time - state.PhaseStartTime));
            if (state.PhaseDuration > 0)
            {
                text.AppendLine($"<size=90><b>{remainingTime:F1}초</b></size>");
            }

            if (!enableDetailedInfo)
            {
                return text.ToString();
            }

            text.AppendLine();
            var choices = CurrentChoices;
            switch (state.CurrentPhase)
            {
                case GamePhase.INPUT:
                    text.AppendLine("<size=50>=== 선택 현황 ===</size>");
                    foreach (var choice in choices)
                    {
                        text.AppendLine($"<size=45>{PlayerName(state, choice.Key)}: <b>{choice.Value}</b></size>");
                    }
                    break;

                case GamePhase.REVEAL:
                    text.AppendLine("<size=50>=== 최종 결과 ===</size>");
                    AppendPlayers(text, state, choices, state.GetAlivePlayers(), "<color=green>생존자</color>");
                    AppendPlayers(text, state, choices, state.GetEliminatedPlayers(), "<color=red>탈락자</color>");
                    break;

                default:
                    text.AppendLine($"<size=50>생존 <color=green>{state.GetAlivePlayers().Count}</color> / 탈락 <color=red>{state.GetEliminatedPlayers().Count}</color></size>");
                    break;
            }

            return text.ToString();
        }

        private static void AppendPlayers(StringBuilder text, GameState state, IReadOnlyDictionary<string, string> choices,
            List<PlayerInfo> players, string title)
        {
            if (players.Count == 0)
            {
                return;
            }

            text.AppendLine($"<size=45>{title}</size>");
            foreach (var player in players)
            {
                var choice = choices.TryGetValue(player.Id, out var c) ? c : "-";
                text.AppendLine($"<size=40>  {PlayerName(state, player.Id)} (선택: {choice})</size>");
            }
        }

        private static string PlayerName(GameState state, string playerId) =>
            state.Players.TryGetValue(playerId, out var player) && !string.IsNullOrEmpty(player.Name) ? player.Name : playerId;

        private string BuildProgressDisplayText()
        {
            var state = RGFManager.Instance.State;
            float progress = state.PhaseDuration > 0 ? Mathf.Clamp01((Time.time - state.PhaseStartTime) / state.PhaseDuration) : 1f;

            const int barLength = 20;
            int filledLength = (int)(progress * barLength);
            string progressBar = new string('█', filledLength) + new string('░', barLength - filledLength);
            return $"<size=40>[{progressBar}] {progress * 100:F0}%</size>";
        }

        private static string GetPhaseSimpleName(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.READY: return "준비";
                case GamePhase.SETUP: return "설정";
                case GamePhase.PRESENT: return "문제 제시";
                case GamePhase.INPUT: return "입력 대기";
                case GamePhase.WAIT: return "입력 마감";
                case GamePhase.EXECUTE: return "결과 계산";
                case GamePhase.REVEAL: return "결과 발표";
                case GamePhase.CLEANUP: return "정리";
                default: return phase.ToString();
            }
        }

        #endregion
    }
}
