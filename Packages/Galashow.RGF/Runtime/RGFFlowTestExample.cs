using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Galashow.Core;
using Galashow.Bridge;
using TMPro;
using Newtonsoft.Json.Linq;

namespace Galashow.RGF
{
    /// <summary>
    /// RGF 플로우 모니터링
    /// 실제 Native에서 오는 RGF 메시지를 받아 UI에 표시
    ///
    /// 동작:
    /// 1. Phase 이벤트 구독하여 현재 상태 모니터링
    /// 2. UI를 통해 게임 진행 상황 표시
    /// 3. 플레이어 선택 및 결과를 화면에 표시
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

        private string _testPluginUuid;

        // 진행 상황 추적
        private bool _isInitialized = false;
        private bool _isPluginRegistered = false;
        private bool _isRoundStarted = false;

        // 플레이어 선택 정보 저장
        public System.Collections.Generic.Dictionary<string, int> _playerChoices = new System.Collections.Generic.Dictionary<string, int>();
        public System.Collections.Generic.List<string> _survivors = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> _eliminated = new System.Collections.Generic.List<string>();

        private void Start()
        {
            // RGFBridgeAdapter 초기화 강제 (Singleton 인스턴스 생성)
            var adapter = RGFBridgeAdapter.Instance;
            if (adapter == null)
            {
                GLog.Error("[RGFMonitor] RGFBridgeAdapter initialization failed");
                return;
            }

            // Phase 이벤트 구독
            RegisterPhaseEvents();

            GLog.Info("[RGFMonitor] RGF 플로우 모니터링 시작 - Native 메시지 대기 중");
        }

        private void Update()
        {
            UpdateStatusDisplay();
        }

        /// <summary>
        /// 플러그인 UUID 설정 (RGFBridgeAdapter에서 호출)
        /// </summary>
        public void SetPluginUuid(string uuid)
        {
            _testPluginUuid = uuid;
            GLog.Info($"[RGFMonitor] Plugin UUID set: {uuid}");
        }

        /// <summary>
        /// Initialize 성공 콜백 (RGFBridgeAdapter에서 호출)
        /// </summary>
        public void OnInitializeSuccess()
        {
            _isInitialized = true;
            GLog.Info($"[RGFMonitor] Initialize completed successfully");
        }

        /// <summary>
        /// RegisterPlugin 성공 콜백 (RGFBridgeAdapter에서 호출)
        /// </summary>
        public void OnRegisterPluginSuccess()
        {
            _isPluginRegistered = true;
            GLog.Info($"[RGFMonitor] RegisterPlugin completed successfully");
        }

        /// <summary>
        /// StartRound 성공 콜백 (RGFBridgeAdapter에서 호출)
        /// </summary>
        public void OnStartRoundSuccess()
        {
            _isRoundStarted = true;
            GLog.Info($"[RGFMonitor] StartRound completed successfully");
        }

        #region Native Sample Message Sender

        /// <summary>
        /// 네이티브 샘플: Initialize 메시지 전송
        /// </summary>
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

        /// <summary>
        /// 네이티브 샘플: RegisterPlugin 메시지 전송
        /// </summary>
        [ContextMenu("Send Native Sample: RegisterPlugin")]
        public void SendNativeSample_RegisterPlugin()
        {
            if (registerPluginJson == null)
            {
                GLog.Error("[NativeSample] RegisterPlugin JSON file not assigned");
                return;
            }

            // 네이티브에서 보내는 RegisterPlugin 메시지 시뮬레이션
            GLog.Info("[NativeSample] Sending RegisterPlugin message...");
            BridgeManager.Instance.ReceiveMessage(registerPluginJson.text);
        }

        /// <summary>
        /// 네이티브 샘플: StartRound 메시지 전송
        /// </summary>
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

            // JSON 파일 로드 및 플러그인 UUID 치환
            string jsonMessage = startRoundJson.text;
            jsonMessage = jsonMessage.Replace("PLUGIN_UUID_PLACEHOLDER", _testPluginUuid);

            // 플레이어 입력 시뮬레이션 시작 (INPUT Phase에서 자동으로 선택 등록)
            StartCoroutine(SimulatePlayerInputs());

            GLog.Info("[NativeSample] Sending StartRound message...");
            BridgeManager.Instance.ReceiveMessage(jsonMessage);
        }

        /// <summary>
        /// 플레이어 입력 시뮬레이션 (테스트용)
        /// INPUT Phase가 시작되면 자동으로 플레이어 선택 등록
        /// </summary>
        private IEnumerator SimulatePlayerInputs()
        {
            // 기존 데이터 초기화
            _playerChoices.Clear();
            _survivors.Clear();
            _eliminated.Clear();

            // INPUT Phase까지 대기
            yield return new WaitUntil(() =>
                RGFManager.Instance.State.CurrentPhase == GamePhase.INPUT
            );

            GLog.Info("[NativeSample] Simulating player inputs...");

            // 플레이어들의 선택 시뮬레이션 (1 또는 2 선택)
            yield return new WaitForSeconds(0.5f);
            _playerChoices["Player 1"] = 1;
            GLog.Info("[NativeSample] Player 1 선택: 1");

            yield return new WaitForSeconds(0.5f);
            _playerChoices["Player 2"] = 2;
            GLog.Info("[NativeSample] Player 2 선택: 2");

            yield return new WaitForSeconds(0.5f);
            _playerChoices["Player 3"] = 1;
            GLog.Info("[NativeSample] Player 3 선택: 1");

            GLog.Info("[NativeSample] Player inputs complete");
        }

        #endregion

        #region Phase Events

        /// <summary>
        /// Phase 이벤트 등록
        /// </summary>
        private void RegisterPhaseEvents()
        {
            if (RGFManager.Instance != null)
            {
                RGFManager.Instance.OnPhaseStarted += OnPhaseStarted;
                RGFManager.Instance.OnPhaseEnded += OnPhaseEnded;
                RGFManager.Instance.State.OnPhaseChanged += OnPhaseChanged;
            }
        }

        private void OnPhaseStarted(GamePhase phase)
        {
            var duration = RGFManager.Instance.GetPhaseDuration(phase);
            GLog.Info($"[Phase→] {phase} ({duration}s)");
        }

        private void OnPhaseEnded(GamePhase phase)
        {
            // 종료 로그는 생략 (시작 로그만으로 충분)
        }

        private void OnPhaseChanged(GamePhase from, GamePhase to)
        {
            // Phase 변경은 디버그 레벨에서만
            GLog.Debug($"[Phase↔] {from} → {to}");
        }

        #endregion

        #region UI Update

        /// <summary>
        /// 상태 표시 업데이트
        /// </summary>
        private void UpdateStatusDisplay()
        {
            if (_statusText == null)
            {
                return;
            }

            // RGFManager가 없으면 대기 메시지
            if (RGFManager.Instance == null)
            {
                _statusText.text = "RGFManager 초기화 대기 중...";
                return;
            }

            // RGF가 실행 중이 아니면 진행 상황 표시
            if (!RGFManager.Instance.IsRunning)
            {
                var text = new System.Text.StringBuilder();
                text.AppendLine("<size=70><b>RGF MONITOR</b></size>");
                text.AppendLine("<size=60><b>RGF 모니터</b></size>");
                text.AppendLine();

                // 진행 상황 체크리스트
                text.AppendLine("<size=50>=== 진행 상황 ===</size>");
                text.AppendLine($"<size=45>{(_isInitialized ? "<color=green>✓</color>" : "<color=red>✗</color>")} Initialize</size>");
                text.AppendLine($"<size=45>{(_isPluginRegistered ? "<color=green>✓</color>" : "<color=red>✗</color>")} Register Plugin</size>");
                text.AppendLine($"<size=45>{(_isRoundStarted ? "<color=green>✓</color>" : "<color=red>✗</color>")} Start Round</size>");

                text.AppendLine();
                if (!_isInitialized)
                {
                    text.AppendLine("<size=40><color=yellow>Initialize 버튼 클릭</color></size>");
                }
                else if (!_isPluginRegistered)
                {
                    text.AppendLine("<size=40><color=yellow>RegisterPlugin 버튼 클릭</color></size>");
                }
                else if (!_isRoundStarted)
                {
                    text.AppendLine("<size=40><color=yellow>StartRound 버튼 클릭</color></size>");
                }
                else
                {
                    text.AppendLine("<size=40><color=yellow>라운드 진행 대기 중...</color></size>");
                }

                _statusText.text = text.ToString();

                if (_progressText != null)
                {
                    _progressText.text = "";
                }
                return;
            }

            // 상태 정보 생성
            var displayText = BuildStatusDisplayText();
            _statusText.text = displayText;

            // 진행도 정보 생성
            if (_progressText != null)
            {
                var progressDisplay = BuildProgressDisplayText();
                _progressText.text = progressDisplay;
            }
        }

        /// <summary>
        /// 상태 표시 텍스트 생성
        /// </summary>
        private string BuildStatusDisplayText()
        {
            var text = new System.Text.StringBuilder();

            var state = RGFManager.Instance.State;
            var isRunning = RGFManager.Instance.IsRunning;

            // 현재 Phase를 크게 표시 (영어 + 한글)
            text.AppendLine($"<size=100><b>{state.CurrentPhase}</b></size>");
            text.AppendLine($"<size=80><b>{GetPhaseSimpleName(state.CurrentPhase)}</b></size>");

            // Round 번호
            text.AppendLine($"<size=60>ROUND {state.CurrentRound}</size>");

            // 남은 시간 계산
            float elapsedTime = Time.time - state.PhaseStartTime;
            float remainingTime = Mathf.Max(0, state.PhaseDuration - elapsedTime);

            if (state.PhaseDuration > 0)
            {
                text.AppendLine($"<size=90><b>{remainingTime:F1}초</b></size>");
            }

            // Phase별 상세 정보
            if (enableDetailedInfo)
            {
                text.AppendLine();
                switch (state.CurrentPhase)
                {
                    case GamePhase.INPUT:
                        // 입력 중인 정보 표시
                        text.AppendLine($"<size=50>=== 선택 현황 ===</size>");
                        foreach (var choice in _playerChoices)
                        {
                            text.AppendLine($"<size=45>{choice.Key}: <b>{choice.Value}</b></size>");
                        }
                        break;

                    case GamePhase.EXECUTE:
                        // 결과 계산 중 표시
                        text.AppendLine($"<size=50>=== 결과 계산 중 ===</size>");
                        if (_survivors.Count > 0 || _eliminated.Count > 0)
                        {
                            text.AppendLine($"<size=45>생존: {_survivors.Count}명 / 탈락: {_eliminated.Count}명</size>");
                        }
                        break;

                    case GamePhase.REVEAL:
                        // 결과 발표
                        text.AppendLine($"<size=50>=== 최종 결과 ===</size>");
                        if (_survivors.Count > 0)
                        {
                            text.AppendLine($"<size=45><color=green>생존자</color></size>");
                            foreach (var survivor in _survivors)
                            {
                                var choice = _playerChoices.ContainsKey(survivor) ? _playerChoices[survivor] : 0;
                                text.AppendLine($"<size=40>  {survivor} (선택: {choice})</size>");
                            }
                        }
                        if (_eliminated.Count > 0)
                        {
                            text.AppendLine($"<size=45><color=red>탈락자</color></size>");
                            foreach (var elim in _eliminated)
                            {
                                var choice = _playerChoices.ContainsKey(elim) ? _playerChoices[elim] : 0;
                                text.AppendLine($"<size=40>  {elim} (선택: {choice})</size>");
                            }
                        }
                        break;

                    default:
                        // 기본 플레이어 정보
                        var alivePlayers = state.GetAlivePlayers();
                        var eliminatedPlayers = state.GetEliminatedPlayers();
                        text.AppendLine($"<size=50>생존 <color=green>{alivePlayers.Count}</color> / 탈락 <color=red>{eliminatedPlayers.Count}</color></size>");
                        break;
                }
            }

            return text.ToString();
        }

        /// <summary>
        /// 진행도 표시 텍스트 생성
        /// </summary>
        private string BuildProgressDisplayText()
        {
            var state = RGFManager.Instance.State;

            if (!RGFManager.Instance.IsRunning)
            {
                return "";
            }

            var text = new System.Text.StringBuilder();

            // Phase 진행도 바
            float elapsedTime = Time.time - state.PhaseStartTime;
            float progress = state.PhaseDuration > 0 ? Mathf.Clamp01(elapsedTime / state.PhaseDuration) : 1f;

            int barLength = 20;
            int filledLength = (int)(progress * barLength);
            string progressBar = new string('█', filledLength) + new string('░', barLength - filledLength);

            text.Append($"<size=40>[{progressBar}] {progress * 100:F0}%</size>");

            return text.ToString();
        }

        private string GetPhaseSimpleName(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.READY:
                    return "준비";
                case GamePhase.SETUP:
                    return "설정";
                case GamePhase.PRESENT:
                    return "문제 제시";
                case GamePhase.INPUT:
                    return "입력 대기";
                case GamePhase.WAIT:
                    return "입력 마감";
                case GamePhase.EXECUTE:
                    return "결과 계산";
                case GamePhase.REVEAL:
                    return "결과 발표";
                case GamePhase.CLEANUP:
                    return "정리";
                default:
                    return phase.ToString();
            }
        }

        #endregion

        #region Results Logging

        /// <summary>
        /// 테스트 결과 로그
        /// </summary>
        private void LogTestResults()
        {
            var state = RGFManager.Instance.State;

            GLog.Info("--- Test Results ---");
            GLog.Info($"Total Players: {state.Players.Count}");

            var alivePlayers = state.GetAlivePlayers();
            GLog.Info($"Survivors ({alivePlayers.Count}):");
            foreach (var player in alivePlayers)
            {
                GLog.Info($"  ✓ {player.Id} ({player.Name})");
            }

            var eliminatedPlayers = state.GetEliminatedPlayers();
            GLog.Info($"Eliminated ({eliminatedPlayers.Count}):");
            foreach (var player in eliminatedPlayers)
            {
                GLog.Info($"  ✗ {player.Id} ({player.Name})");
            }
        }

        #endregion

        private void OnDestroy()
        {
            // 이벤트 리스너 해제
            if (RGFManager.Instance != null)
            {
                RGFManager.Instance.OnPhaseStarted -= OnPhaseStarted;
                RGFManager.Instance.OnPhaseEnded -= OnPhaseEnded;

                if (RGFManager.Instance.State != null)
                {
                    RGFManager.Instance.State.OnPhaseChanged -= OnPhaseChanged;
                }
            }
        }
    }

    /// <summary>
    /// 더미 게임 플러그인 (테스트용)
    /// </summary>
    public class DummyGamePlugin : IGamePlugin
    {
        private RGFFlowTestExample _testExample;

        public DummyGamePlugin(RGFFlowTestExample testExample)
        {
            _testExample = testExample;
        }

        public string GameName => "DummyGame";

        public async Task OnReadyAsync(GameState state)
        {
            GLog.Info($"[DummyGame] READY Phase - Round {state.CurrentRound}");
            await Task.CompletedTask;
        }

        public async Task OnSetupAsync(GameState state)
        {
            GLog.Info($"[DummyGame] SETUP Phase");
            await Task.CompletedTask;
        }

        public async Task OnPresentAsync(GameState state)
        {
            GLog.Info($"[DummyGame] PRESENT Phase - 문제: 1 또는 2를 선택하세요!");
            await Task.CompletedTask;
        }

        public async Task OnInputAsync(GameState state)
        {
            GLog.Info($"[DummyGame] INPUT Phase - 플레이어 선택 대기 중...");
            // INPUT에서는 선택만 받음 (결과 계산 없음)
            await Task.CompletedTask;
        }

        public async Task OnWaitAsync(GameState state)
        {
            GLog.Info($"[DummyGame] WAIT Phase - 입력 마감");
            await Task.CompletedTask;
        }

        public async Task OnExecuteAsync(GameState state)
        {
            GLog.Info($"[DummyGame] EXECUTE Phase - 결과 계산 중...");

            // 선택 집계
            int choice1Count = 0;
            int choice2Count = 0;

            foreach (var choice in _testExample._playerChoices)
            {
                if (choice.Value == 1) choice1Count++;
                else if (choice.Value == 2) choice2Count++;
            }

            GLog.Info($"[DummyGame] 선택 집계 - 1: {choice1Count}명, 2: {choice2Count}명");

            // 더 적게 선택한 쪽이 탈락
            _testExample._survivors.Clear();
            _testExample._eliminated.Clear();

            foreach (var choice in _testExample._playerChoices)
            {
                bool survived;
                if (choice1Count == choice2Count)
                {
                    // 동점이면 모두 생존
                    survived = true;
                }
                else if (choice1Count < choice2Count)
                {
                    // 1을 선택한 사람이 적음 -> 1 선택자 탈락
                    survived = choice.Value != 1;
                }
                else
                {
                    // 2를 선택한 사람이 적음 -> 2 선택자 탈락
                    survived = choice.Value != 2;
                }

                if (survived)
                {
                    _testExample._survivors.Add(choice.Key);
                    state.UpdatePlayerStatus(choice.Key, true);
                }
                else
                {
                    _testExample._eliminated.Add(choice.Key);
                    state.UpdatePlayerStatus(choice.Key, false);
                }
            }

            GLog.Info($"[DummyGame] 결과 - 생존: {_testExample._survivors.Count}명, 탈락: {_testExample._eliminated.Count}명");

            await Task.CompletedTask;
        }

        public async Task OnRevealAsync(GameState state)
        {
            GLog.Info($"[DummyGame] REVEAL Phase - 결과 발표!");

            GLog.Info($"[DummyGame] === 생존자 ===");
            foreach (var survivor in _testExample._survivors)
            {
                var choice = _testExample._playerChoices[survivor];
                GLog.Info($"[DummyGame]   {survivor} (선택: {choice})");
            }

            GLog.Info($"[DummyGame] === 탈락자 ===");
            foreach (var eliminated in _testExample._eliminated)
            {
                var choice = _testExample._playerChoices[eliminated];
                GLog.Info($"[DummyGame]   {eliminated} (선택: {choice})");
            }

            await Task.CompletedTask;
        }

        public async Task OnCleanupAsync(GameState state)
        {
            GLog.Info($"[DummyGame] CLEANUP Phase");
            await Task.CompletedTask;
        }

        public async Task OnPhaseTransitionAsync(GamePhase from, GamePhase to)
        {
            GLog.Debug($"[DummyGame] Phase Transition: {from} → {to}");
            await Task.CompletedTask;
        }
    }
}
