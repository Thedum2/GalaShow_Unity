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
    /// RGF 플로우 테스트 예제
    /// Native 역할을 시뮬레이션하여 전체 RGF 플로우를 테스트
    ///
    /// 실제 Native에서 데이터가 온다고 가정하고 동작:
    /// 1. Initialize (InitializeProgress 0% → 50% → 100% → Initialize ACK)
    /// 2. RegisterPlugin (RegisterPlugin ACK)
    /// 3. StartRound (StartRound ACK → RoundStarted NTY)
    /// 4. Phase 순회 (각 Phase마다 Started → Ended → Changed 알림)
    /// 5. INPUT Phase에서 플레이어 선택 시뮬레이션
    /// 6. RoundCompleted NTY
    /// </summary>
    public class RGFFlowTestExample : MonoBehaviour
    {
        [Header("테스트 설정")]
        [SerializeField] private bool autoStart = false;
        [SerializeField] private float delayBetweenSteps = 1f;

        [Header("JSON 메시지 파일")]
        [SerializeField] private TextAsset initializeJson;
        [SerializeField] private TextAsset registerPluginJson;
        [SerializeField] private TextAsset startRoundJson;

        [Header("상태 표시 UI")]
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private bool enableDetailedInfo = true;

        private string _currentPluginUuid;
        private int _currentTestStep = 0;
        private bool _isTestRunning = false;

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
                GLog.Error("[Test✗] RGFBridgeAdapter initialization failed");
                return;
            }

            // Phase 이벤트 구독
            RegisterPhaseEvents();

            if (autoStart)
            {
                RunExample();
            }
        }

        private void Update()
        {
            UpdateStatusDisplay();
        }

        /// <summary>
        /// 전체 플로우 테스트 실행 (코루틴 사용)
        /// </summary>
        [ContextMenu("Run Full Flow Test")]
        public void RunExample()
        {
            if (_isTestRunning)
            {
                GLog.Warn("[RGFFlowTest] Test is already running!");
                return;
            }

            StartCoroutine(RunFullFlowTest());
        }

        private IEnumerator RunFullFlowTest()
        {
            _isTestRunning = true;
            _currentTestStep = 0;

            // 데이터 초기화
            _playerChoices.Clear();
            _survivors.Clear();
            _eliminated.Clear();

            GLog.Info("[Test] ===== RGF Flow Test Start =====");

            // 1단계: Initialize
            _currentTestStep = 1;
            bool initComplete = false;
            StartCoroutine(TestInitialize(() => initComplete = true));
            yield return new WaitUntil(() => initComplete);
            yield return new WaitForSeconds(delayBetweenSteps);

            // 2단계: RegisterPlugin
            _currentTestStep = 2;
            bool registerComplete = false;
            StartCoroutine(TestRegisterPlugin(() => registerComplete = true));
            yield return new WaitUntil(() => registerComplete);
            yield return new WaitForSeconds(delayBetweenSteps);

            // 3단계: StartRound (Phase 순회 포함)
            _currentTestStep = 3;

            // INPUT Phase에서 플레이어 입력 시뮬레이션
            StartCoroutine(SimulatePlayerInputs());

            bool roundComplete = false;
            StartCoroutine(TestStartRound(() => roundComplete = true));
            yield return new WaitUntil(() => roundComplete);

            _currentTestStep = 4;
            GLog.Info("[Test] ===== RGF Flow Test Complete =====");

            LogTestResults();

            _isTestRunning = false;
        }

        /// <summary>
        /// 1. Initialize 테스트
        /// Native에서 BridgeManager.ReceiveMessage를 통해 JSON 메시지가 온다고 가정
        /// RGFBridgeAdapter의 R2U_RGFManager_Initialize_REQ가 호출되고 콜백이 실행됨
        /// </summary>
        private IEnumerator TestInitialize(System.Action onComplete)
        {
            if (initializeJson == null)
            {
                GLog.Error("[Test✗] Initialize JSON file not assigned");
                onComplete?.Invoke();
                yield break;
            }

            // Native에서 JSON 메시지 전송 시뮬레이션
            string jsonMessage = initializeJson.text;
            GLog.Info($"[Test] Step 1: Initialize");
            BridgeManager.Instance.ReceiveMessage(jsonMessage);

            yield return null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 2. RegisterPlugin 테스트
        /// Native에서 BridgeManager.ReceiveMessage를 통해 JSON 메시지가 온다고 가정
        /// RGFBridgeAdapter의 R2U_RGFManager_RegisterPlugin_REQ가 호출되고 콜백이 실행됨
        /// </summary>
        private IEnumerator TestRegisterPlugin(System.Action onComplete)
        {
            if (registerPluginJson == null)
            {
                GLog.Error("[Test✗] RegisterPlugin JSON file not assigned");
                onComplete?.Invoke();
                yield break;
            }

            // 먼저 더미 플러그인을 Unity 쪽에서 등록
            var dummyPlugin = new DummyGamePlugin(this);
            _currentPluginUuid = RGFManager.Instance.RegisterPlugin(dummyPlugin);

            // Native에서 JSON 메시지 전송 시뮬레이션
            string jsonMessage = registerPluginJson.text;
            GLog.Info($"[Test] Step 2: RegisterPlugin");
            BridgeManager.Instance.ReceiveMessage(jsonMessage);

            yield return null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 3. StartRound 테스트 (Phase 순회 포함)
        /// Native에서 BridgeManager.ReceiveMessage를 통해 JSON 메시지가 온다고 가정
        /// RGFBridgeAdapter의 R2U_RGFManager_StartRound_REQ가 호출되고 콜백이 실행됨
        /// </summary>
        private IEnumerator TestStartRound(System.Action onComplete)
        {
            if (startRoundJson == null)
            {
                GLog.Error("[Test✗] StartRound JSON file not assigned");
                onComplete?.Invoke();
                yield break;
            }

            // JSON 파일 로드 및 플러그인 UUID 치환
            string jsonMessage = startRoundJson.text;
            jsonMessage = jsonMessage.Replace("PLUGIN_UUID_PLACEHOLDER", _currentPluginUuid);

            // Native에서 JSON 메시지 전송 시뮬레이션
            GLog.Info($"[Test] Step 3: StartRound");
            BridgeManager.Instance.ReceiveMessage(jsonMessage);

            yield return null;

            // 라운드 완료 대기
            yield return new WaitUntil(() => !RGFManager.Instance.IsRunning);

            onComplete?.Invoke();
        }

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

        #region Player Input Simulation

        /// <summary>
        /// 플레이어 입력 시뮬레이션 (Native에서 오는 데이터라고 가정)
        /// INPUT Phase가 시작되면 자동으로 플레이어 선택 등록
        /// </summary>
        private IEnumerator SimulatePlayerInputs()
        {
            // INPUT Phase까지 대기
            yield return new WaitUntil(() =>
                RGFManager.Instance.State.CurrentPhase == GamePhase.INPUT
            );

            GLog.Info("[Test] Simulating player inputs...");

            // 플레이어들의 선택 시뮬레이션 (1 또는 2 선택)
            yield return new WaitForSeconds(0.5f);
            _playerChoices["Player 1"] = 1;
            GLog.Info("[Test] Player 1 선택: 1");

            yield return new WaitForSeconds(0.5f);
            _playerChoices["Player 2"] = 2;
            GLog.Info("[Test] Player 2 선택: 2");

            yield return new WaitForSeconds(0.5f);
            _playerChoices["Player 3"] = 1;
            GLog.Info("[Test] Player 3 선택: 1");

            GLog.Info("[Test] Player inputs complete");
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

            // 테스트가 실행 중이 아니면 대기 메시지
            if (!_isTestRunning)
            {
                _statusText.text = "<size=70><b>RGF TEST</b></size>\n<size=60><b>RGF 테스트</b></size>\n<size=50><color=yellow>WAITING</color></size>\n<size=30>[Run Full Flow Test] 메뉴 실행</size>";
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

            if (isRunning)
            {
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
            }
            else
            {
                // 테스트 단계 표시
                text.AppendLine($"<size=70><b>{GetCurrentStepName()}</b></size>");
                text.AppendLine($"<size=50>Step {_currentTestStep}/4</size>");
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

        private string GetCurrentStepName()
        {
            switch (_currentTestStep)
            {
                case 0: return "초기화 중...";
                case 1: return "초기화";
                case 2: return "플러그인 등록";
                case 3: return "라운드 진행";
                case 4: return "테스트 완료";
                default: return "알 수 없음";
            }
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

        private string GetPhaseDisplayName(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.READY:
                    return "READY 준비 단계";
                case GamePhase.SETUP:
                    return "SETUP 설정 단계";
                case GamePhase.PRESENT:
                    return "PRESENT 문제 제시";
                case GamePhase.INPUT:
                    return "INPUT 입력 대기";
                case GamePhase.WAIT:
                    return "WAIT 입력 마감";
                case GamePhase.EXECUTE:
                    return "EXECUTE 결과 계산";
                case GamePhase.REVEAL:
                    return "REVEAL 결과 발표";
                case GamePhase.CLEANUP:
                    return "CLEANUP 정리 단계";
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
            await Task.Delay(100);
        }

        public async Task OnSetupAsync(GameState state)
        {
            GLog.Info($"[DummyGame] SETUP Phase");
            await Task.Delay(100);
        }

        public async Task OnPresentAsync(GameState state)
        {
            GLog.Info($"[DummyGame] PRESENT Phase - 문제: 1 또는 2를 선택하세요!");
            await Task.Delay(100);
        }

        public async Task OnInputAsync(GameState state)
        {
            GLog.Info($"[DummyGame] INPUT Phase - 플레이어 선택 대기 중...");
            // INPUT에서는 선택만 받음 (결과 계산 없음)
            await Task.Delay(100);
        }

        public async Task OnWaitAsync(GameState state)
        {
            GLog.Info($"[DummyGame] WAIT Phase - 입력 마감");
            await Task.Delay(100);
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

            await Task.Delay(100);
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

            await Task.Delay(100);
        }

        public async Task OnCleanupAsync(GameState state)
        {
            GLog.Info($"[DummyGame] CLEANUP Phase");
            await Task.Delay(100);
        }

        public async Task OnPhaseTransitionAsync(GamePhase from, GamePhase to)
        {
            GLog.Debug($"[DummyGame] Phase Transition: {from} → {to}");
            await Task.CompletedTask;
        }
    }
}
