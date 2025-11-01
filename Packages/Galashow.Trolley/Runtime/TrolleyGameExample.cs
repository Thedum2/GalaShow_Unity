using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Galashow.Core;

namespace Galashow.Trolley
{
    /// <summary>
    /// Trolley 미니게임 실행 예시
    /// RGFManager를 사용하여 트롤리 딜레마 게임을 실행하는 샘플 코드
    /// </summary>
    public class TrolleyGameExample : MonoBehaviour
    {
        [Header("게임 설정")]
        [SerializeField] private bool autoStart = false;
        [SerializeField] private float inputSimulationDelay = 5f; // 입력 시뮬레이션 지연 시간

        private TrolleyDilemmaPlugin _trolleyPlugin;

        private void Start()
        {
            // 자동 시작이 활성화되어 있으면 게임 시작
            if (autoStart)
            {
                StartCoroutine(RunGameExample());
            }
        }

        /// <summary>
        /// 게임 예시 실행 (Inspector 버튼이나 코드에서 호출 가능)
        /// </summary>
        [ContextMenu("Run Game Example")]
        public void RunExample()
        {
            StartCoroutine(RunGameExample());
        }

        private IEnumerator RunGameExample()
        {
            GLog.Info("========================================");
            GLog.Info("  Trolley 미니게임 예시 시작");
            GLog.Info("========================================");

            // Step 1: RGFManager 초기화 확인
            if (RGFManager.Instance == null)
            {
                GLog.Error("[Example] RGFManager가 초기화되지 않았습니다!");
                yield break;
            }

            GLog.Info("[Example] Step 1: RGFManager 초기화 완료");

            // Step 2: TrolleyDilemmaPlugin 생성 및 등록
            _trolleyPlugin = new TrolleyDilemmaPlugin();
            RGFManager.Instance.RegisterPlugin(_trolleyPlugin);
            GLog.Info($"[Example] Step 2: 플러그인 등록 완료 - {_trolleyPlugin.GameType}");

            // Step 3: Phase 이벤트 리스너 등록
            RegisterPhaseEvents();
            GLog.Info("[Example] Step 3: Phase 이벤트 리스너 등록 완료");

            // Step 4: 게임 데이터 생성
            var gameData = CreateSampleGameData();
            GLog.Info("[Example] Step 4: 게임 데이터 생성 완료");
            LogGameData(gameData);

            // Step 5: 플레이어 추가
            AddSamplePlayers();
            GLog.Info("[Example] Step 5: 플레이어 추가 완료");
            LogPlayers();

            // Step 6: 라운드 시작 (비동기 작업을 코루틴으로 처리)
            GLog.Info("[Example] Step 6: 라운드 시작...");
            GLog.Info("========================================\n");

            // INPUT Phase에서 플레이어 선택 시뮬레이션을 위한 코루틴 시작
            StartCoroutine(SimulatePlayerInputs());

            // 라운드 실행 (Task를 코루틴으로 변환)
            var task = RGFManager.Instance.StartRoundAsync("trolley_dilemma", 1, gameData);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Exception != null)
            {
                GLog.Error($"[Example] 라운드 실행 중 에러 발생: {task.Exception.Message}");
                yield break;
            }

            // Step 7: 결과 확인
            GLog.Info("\n========================================");
            GLog.Info("  게임 결과");
            GLog.Info("========================================");
            LogGameResult();

            GLog.Info("\n========================================");
            GLog.Info("  Trolley 미니게임 예시 종료");
            GLog.Info("========================================");
        }

        /// <summary>
        /// 샘플 게임 데이터 생성
        /// </summary>
        private TrolleyGameData CreateSampleGameData()
        {
            return new TrolleyGameData
            {
                RoundNumber = 1,
                Title = "폭주하는 트롤리",
                Description = "제어 불능 상태의 트롤리가 5명을 향해 돌진하고 있습니다. 당신은 선로 전환기를 당겨 트롤리를 다른 선로로 바꿀 수 있지만, 그곳에는 1명이 있습니다. 어떻게 하시겠습니까?",
                InputTimeLimit = 10f, // 예시에서는 10초로 단축
                Difficulty = "normal",
                Choices = new System.Collections.Generic.List<TrolleyChoice>
                {
                    new TrolleyChoice
                    {
                        Id = "choice_a",
                        Text = "선로를 전환한다",
                        Description = "1명을 희생하여 5명을 구한다",
                        SurvivalRate = 0.8f, // 80% 생존율
                        SelectedPlayers = new System.Collections.Generic.List<string>()
                    },
                    new TrolleyChoice
                    {
                        Id = "choice_b",
                        Text = "아무것도 하지 않는다",
                        Description = "5명이 희생되지만 직접적인 행동을 하지 않는다",
                        SurvivalRate = 0.4f, // 40% 생존율
                        SelectedPlayers = new System.Collections.Generic.List<string>()
                    }
                }
            };
        }

        /// <summary>
        /// 샘플 플레이어 추가
        /// </summary>
        private void AddSamplePlayers()
        {
            var state = RGFManager.Instance.State;

            state.AddPlayer("player_1", "철수");
            state.AddPlayer("player_2", "영희");
            state.AddPlayer("player_3", "민수");
            state.AddPlayer("player_4", "지훈");
            state.AddPlayer("player_5", "수지");
            state.AddPlayer("player_6", "현우");
            state.AddPlayer("player_7", "예진");
            state.AddPlayer("player_8", "동현");
        }

        /// <summary>
        /// Phase 이벤트 등록
        /// </summary>
        private void RegisterPhaseEvents()
        {
            RGFManager.Instance.OnPhaseStarted += OnPhaseStarted;
            RGFManager.Instance.OnPhaseEnded += OnPhaseEnded;
            RGFManager.Instance.State.OnPhaseChanged += OnPhaseChanged;
        }

        /// <summary>
        /// Phase 시작 이벤트
        /// </summary>
        private void OnPhaseStarted(GamePhase phase)
        {
            var duration = RGFManager.Instance.GetPhaseDuration(phase);
            GLog.Info($">>> Phase Started: {phase} (Duration: {duration}s)");
        }

        /// <summary>
        /// Phase 종료 이벤트
        /// </summary>
        private void OnPhaseEnded(GamePhase phase)
        {
            GLog.Info($"<<< Phase Ended: {phase}");
        }

        /// <summary>
        /// Phase 변경 이벤트
        /// </summary>
        private void OnPhaseChanged(GamePhase from, GamePhase to)
        {
            GLog.Debug($"[State] Phase Changed: {from} → {to}");
        }

        /// <summary>
        /// 플레이어 입력 시뮬레이션
        /// INPUT Phase가 시작되면 자동으로 플레이어 선택 등록
        /// </summary>
        private IEnumerator SimulatePlayerInputs()
        {
            // INPUT Phase까지 대기
            yield return new WaitUntil(() =>
                RGFManager.Instance.State.CurrentPhase == GamePhase.INPUT
            );

            GLog.Info($"[Example] {inputSimulationDelay}초 후 플레이어 입력 시뮬레이션 시작...");
            yield return new WaitForSeconds(inputSimulationDelay);

            // 플레이어들의 선택 시뮬레이션
            // 60%는 choice_a, 40%는 choice_b 선택
            var state = RGFManager.Instance.State;
            var playerIds = new[] { "player_1", "player_2", "player_3", "player_4", "player_5", "player_6", "player_7", "player_8" };

            for (int i = 0; i < playerIds.Length; i++)
            {
                var playerId = playerIds[i];
                var choiceId = i < 5 ? "choice_a" : "choice_b"; // 처음 5명은 A, 나머지는 B

                _trolleyPlugin.RegisterPlayerChoice(playerId, choiceId);

                GLog.Info($"[Example] {playerId} → {choiceId}");

                // 약간의 지연으로 순차적 선택 시뮬레이션
                yield return new WaitForSeconds(0.2f);
            }

            GLog.Info("[Example] 모든 플레이어 입력 시뮬레이션 완료");
        }

        /// <summary>
        /// 게임 데이터 로그
        /// </summary>
        private void LogGameData(TrolleyGameData data)
        {
            GLog.Info($"  - 제목: {data.Title}");
            GLog.Info($"  - 설명: {data.Description}");
            GLog.Info($"  - 입력 제한 시간: {data.InputTimeLimit}초");
            GLog.Info($"  - 선택지 개수: {data.Choices.Count}");
            foreach (var choice in data.Choices)
            {
                GLog.Info($"    * {choice.Id}: {choice.Text} (생존율: {choice.SurvivalRate:P0})");
            }
        }

        /// <summary>
        /// 플레이어 목록 로그
        /// </summary>
        private void LogPlayers()
        {
            var state = RGFManager.Instance.State;
            GLog.Info($"  - 전체 플레이어 수: {state.Players.Count}");
            foreach (var kvp in state.Players)
            {
                var player = kvp.Value;
                GLog.Info($"    * {player.Id} ({player.Name}) - Alive: {player.IsAlive}");
            }
        }

        /// <summary>
        /// 게임 결과 로그
        /// </summary>
        private void LogGameResult()
        {
            var result = _trolleyPlugin.GetResult();

            if (result == null)
            {
                GLog.Warn("[Example] 게임 결과가 없습니다.");
                return;
            }

            GLog.Info($"라운드: {result.RoundNumber}");
            GLog.Info($"전체 생존율: {result.SurvivalRate:P1}");
            GLog.Info($"평균 선택 시간: {result.AverageSelectionTime:F2}초");
            GLog.Info("");

            // 생존자
            GLog.Info($"생존자 ({result.Survivors.Count}명):");
            foreach (var survivor in result.Survivors)
            {
                var playerInfo = RGFManager.Instance.State.Players[survivor];
                GLog.Info($"  ✓ {survivor} ({playerInfo.Name})");
            }
            GLog.Info("");

            // 탈락자
            GLog.Info($"탈락자 ({result.Eliminated.Count}명):");
            foreach (var eliminated in result.Eliminated)
            {
                var playerInfo = RGFManager.Instance.State.Players[eliminated];
                GLog.Info($"  ✗ {eliminated} ({playerInfo.Name})");
            }
            GLog.Info("");

            // 선택지별 통계
            GLog.Info("선택지별 통계:");
            foreach (var kvp in result.ChoiceStats)
            {
                var choiceId = kvp.Key;
                var stat = kvp.Value;
                GLog.Info($"  [{choiceId}]");
                GLog.Info($"    - 선택 수: {stat.SelectionCount}명 ({stat.SelectionRate:P0})");
                GLog.Info($"    - 생존 수: {stat.SurvivorCount}명");
                GLog.Info($"    - 실제 생존율: {stat.ActualSurvivalRate:P0}");
            }
        }

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
}
