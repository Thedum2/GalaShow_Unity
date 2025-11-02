using System;
using System.Threading.Tasks;

namespace Galashow.Core
{
    /// <summary>
    /// RGF (Round Game Framework) 매니저
    /// 게임 플러그인을 로드하고 8단계 생명주기를 실행하는 핵심 엔진
    /// 세분화된 컴포넌트들을 조합하여 전체적인 게임 플로우 제어
    /// </summary>
    public class RGFManager : PersistentMonoSingleton<RGFManager>
    {
        #region Core Components

        /// <summary>
        /// 게임 상태
        /// </summary>
        public GameState State { get; private set; } = new GameState();

        /// <summary>
        /// 플러그인 레지스트리
        /// </summary>
        private PluginRegistry _pluginRegistry = new PluginRegistry();

        /// <summary>
        /// Phase 실행기
        /// </summary>
        private PhaseExecutor _phaseExecutor = new PhaseExecutor();

        /// <summary>
        /// 현재 실행 중인 플러그인
        /// </summary>
        private IGamePlugin _currentPlugin;

        /// <summary>
        /// 라운드 실행 중 여부
        /// </summary>
        public bool IsRunning { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Phase 시작 이벤트
        /// </summary>
        public event Action<GamePhase> OnPhaseStarted
        {
            add => _phaseExecutor.OnPhaseStarted += value;
            remove => _phaseExecutor.OnPhaseStarted -= value;
        }

        /// <summary>
        /// Phase 종료 이벤트
        /// </summary>
        public event Action<GamePhase> OnPhaseEnded
        {
            add => _phaseExecutor.OnPhaseEnded += value;
            remove => _phaseExecutor.OnPhaseEnded -= value;
        }

        #endregion

        protected override void Awake()
        {
            base.Awake();
            State.GameRoot = gameObject;
            GLog.Info("[RGF] RGFManager initialized");
        }

        #region Plugin Management

        /// <summary>
        /// 게임 플러그인 등록
        /// </summary>
        /// <returns>플러그인 UUID</returns>
        public string RegisterPlugin(IGamePlugin plugin)
        {
            return _pluginRegistry.Register(plugin);
        }

        /// <summary>
        /// 게임 플러그인 등록 해제
        /// </summary>
        public void UnregisterPlugin(string uuid)
        {
            _pluginRegistry.Unregister(uuid);
        }

        /// <summary>
        /// 플러그인 조회
        /// </summary>
        public IGamePlugin GetPlugin(string uuid)
        {
            return _pluginRegistry.Get(uuid);
        }

        /// <summary>
        /// 현재 실행 중인 플러그인
        /// </summary>
        public IGamePlugin CurrentPlugin => _currentPlugin;

        /// <summary>
        /// 현재 실행 중인 플러그인 UUID
        /// </summary>
        public string CurrentPluginUuid { get; private set; }

        #endregion

        #region Phase Configuration

        /// <summary>
        /// Phase 지속 시간 설정
        /// </summary>
        public void SetPhaseDuration(GamePhase phase, float duration)
        {
            State.SetPhaseDuration(phase, duration);
        }

        /// <summary>
        /// Phase 지속 시간 가져오기
        /// </summary>
        public float GetPhaseDuration(GamePhase phase)
        {
            return State.GetPhaseDuration(phase);
        }

        #endregion

        #region Round Execution

        /// <summary>
        /// 라운드 시작
        /// </summary>
        /// <param name="pluginUuid">플러그인 UUID</param>
        /// <param name="roundNumber">라운드 번호</param>
        /// <param name="gameData">게임 데이터</param>
        public async Task StartRoundAsync(string pluginUuid, int roundNumber, object gameData = null)
        {
            if (IsRunning)
            {
                GLog.Warn("[RGF] Round already running, ignoring start request");
                return;
            }

            var plugin = _pluginRegistry.Get(pluginUuid);
            if (plugin == null)
            {
                GLog.Error($"[RGF] Plugin not found for UUID: {pluginUuid}");
                return;
            }

            IsRunning = true;
            _currentPlugin = plugin;
            CurrentPluginUuid = pluginUuid;
            State.CurrentRound = roundNumber;
            State.GameData = gameData;

            GLog.Info($"[RGF] Starting Round {roundNumber} - {plugin.GameName} (UUID: {pluginUuid})");

            try
            {
                // 8단계 생명주기 실행
                await _phaseExecutor.ExecuteFullRoundAsync(plugin, State);

                GLog.Info($"[RGF] Round {roundNumber} completed");
            }
            catch (Exception ex)
            {
                GLog.Error($"[RGF] Round execution error: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
                _currentPlugin = null;
                CurrentPluginUuid = null;
            }
        }

        /// <summary>
        /// 특정 Phase만 실행
        /// </summary>
        public async Task ExecutePhaseAsync(GamePhase phase)
        {
            if (_currentPlugin == null)
            {
                GLog.Error("[RGF] No plugin loaded, cannot execute phase");
                return;
            }

            await _phaseExecutor.ExecuteAsync(phase, _currentPlugin, State);
        }

        /// <summary>
        /// 라운드 강제 중단
        /// </summary>
        public void AbortRound()
        {
            if (!IsRunning)
            {
                return;
            }

            GLog.Warn("[RGF] Round aborted");

            _phaseExecutor.Abort(State);

            IsRunning = false;
            _currentPlugin = null;
        }

        #endregion

        #region State Management

        /// <summary>
        /// 상태 초기화
        /// </summary>
        public void ResetState()
        {
            State.Reset();
            GLog.Info("[RGF] State reset");
        }

        #endregion
    }
}
