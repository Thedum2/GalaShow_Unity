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
            var dummyPlugin = new DummyGamePlugin();
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

            // 더미 플레이어들의 선택 시뮬레이션
            // Player 1, 3 생존, Player 2 탈락
            var state = RGFManager.Instance.State;

            yield return new WaitForSeconds(0.5f);
            state.UpdatePlayerStatus("1", true);

            yield return new WaitForSeconds(0.5f);
            state.UpdatePlayerStatus("2", false);

            yield return new WaitForSeconds(0.5f);
            state.UpdatePlayerStatus("3", true);

            GLog.Info("[Test] Player inputs complete (Alive: 2, Eliminated: 1)");
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
                _statusText.text = "<size=28><b>RGF Flow Test</b></size>\n\n게임 대기 중\n\n<size=16>[Run Full Flow Test] 메뉴를 실행하세요</size>";
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

            text.AppendLine("<size=32><b>RGF Flow Test</b></size>");
            text.AppendLine();

            // 현재 단계 표시
            text.AppendLine($"<size=24>Step {_currentTestStep}: {GetCurrentStepName()}</size>");
            text.AppendLine();

            var state = RGFManager.Instance.State;
            var isRunning = RGFManager.Instance.IsRunning;

            if (isRunning)
            {
                // Round 정보
                text.AppendLine($"<size=28><color=yellow>ROUND {state.CurrentRound}</color></size>");
                text.AppendLine($"<size=36><color=cyan>{GetPhaseDisplayName(state.CurrentPhase)}</color></size>");
                text.AppendLine();

                // 남은 시간 계산
                float elapsedTime = Time.time - state.PhaseStartTime;
                float remainingTime = Mathf.Max(0, state.PhaseDuration - elapsedTime);

                if (state.PhaseDuration > 0)
                {
                    text.AppendLine($"<size=24>남은 시간: {remainingTime:F1}초</size>");
                }
            }
            else
            {
                text.AppendLine("<color=green>라운드 대기 중...</color>");
            }

            text.AppendLine();

            // 상세 정보
            if (enableDetailedInfo)
            {
                text.AppendLine($"플레이어 수: {state.Players.Count}명");

                var alivePlayers = state.GetAlivePlayers();
                var eliminatedPlayers = state.GetEliminatedPlayers();
                text.AppendLine($"생존: <color=green>{alivePlayers.Count}명</color> | 탈락: <color=red>{eliminatedPlayers.Count}명</color>");
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

            int barLength = 30;
            int filledLength = (int)(progress * barLength);
            string progressBar = new string('█', filledLength) + new string('░', barLength - filledLength);

            text.AppendLine($"<size=20>[{progressBar}] {progress * 100:F0}%</size>");

            return text.ToString();
        }

        private string GetCurrentStepName()
        {
            switch (_currentTestStep)
            {
                case 0: return "Initializing...";
                case 1: return "Initialize Request";
                case 2: return "RegisterPlugin Request";
                case 3: return "StartRound & Phase Flow";
                case 4: return "Completed";
                default: return "Unknown";
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
            GLog.Info($"[DummyGame] PRESENT Phase");
            await Task.Delay(100);
        }

        public async Task OnInputAsync(GameState state)
        {
            GLog.Info($"[DummyGame] INPUT Phase");

            // 더미 플레이어 입력 시뮬레이션
            await Task.Delay(500);

            // 플레이어 1은 생존, 플레이어 2는 탈락
            state.UpdatePlayerStatus("1", true);
            state.UpdatePlayerStatus("2", false);
            state.UpdatePlayerStatus("3", true);
        }

        public async Task OnWaitAsync(GameState state)
        {
            GLog.Info($"[DummyGame] WAIT Phase");
            await Task.Delay(100);
        }

        public async Task OnExecuteAsync(GameState state)
        {
            GLog.Info($"[DummyGame] EXECUTE Phase");

            // 결과 계산 (더미)
            var alivePlayers = state.GetAlivePlayers();
            var eliminatedPlayers = state.GetEliminatedPlayers();

            GLog.Info($"[DummyGame] Alive: {alivePlayers.Count}, Eliminated: {eliminatedPlayers.Count}");

            await Task.Delay(100);
        }

        public async Task OnRevealAsync(GameState state)
        {
            GLog.Info($"[DummyGame] REVEAL Phase");
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
