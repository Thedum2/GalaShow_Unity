using System;
using System.Threading.Tasks;
using Galashow.Core;

namespace Galashow.RGF
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
            _phaseExecutor.SetCoroutineRunner(this);
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
        /// <returns>8단계를 정상 완료했으면 true. 중단·오류·시작 거부는 false</returns>
        public async Task<bool> StartRoundAsync(string pluginUuid, int roundNumber, object gameData = null, bool practice = false)
        {
            if (IsRunning)
            {
                GLog.Warn("[RGF] Round already running");
                return false;
            }

            var plugin = _pluginRegistry.Get(pluginUuid);
            if (plugin == null)
            {
                GLog.Error($"[RGF✗] Plugin not found: {pluginUuid}");
                return false;
            }

            IsRunning = true;
            _currentPlugin = plugin;
            CurrentPluginUuid = pluginUuid;
            State.CurrentRound = roundNumber;
            State.GameData = gameData;
            State.IsPractice = practice;

            GLog.Info($"[RGF] Round {roundNumber} Start - {plugin.GameName}");

            try
            {
                // 8단계 생명주기 실행
                bool completed = await _phaseExecutor.ExecuteFullRoundAsync(plugin, State);

                GLog.Info(completed ? $"[RGF] Round {roundNumber} Complete" : $"[RGF] Round {roundNumber} Aborted");
                return completed;
            }
            catch (Exception ex)
            {
                GLog.Error($"[RGF✗] Round error: {ex.Message}");
                NotifyAborted(plugin);
                return false;
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
        /// 참가자 입력 전달. 실행 중인 플러그인이 IPlayerInputReceiver일 때만 전달한다.
        /// </summary>
        public void SubmitInput(PlayerInput input)
        {
            if (!IsRunning || !(_currentPlugin is IPlayerInputReceiver receiver))
            {
                return;
            }

            receiver.ReceiveInput(input);
        }

        /// <summary>
        /// 호스트 입력 전달. 실행 중인 플러그인이 IHostInputReceiver일 때만 전달한다.
        /// </summary>
        public bool SubmitHostInput(HostInput input)
        {
            if (!IsRunning || !(_currentPlugin is IHostInputReceiver receiver))
            {
                return false;
            }

            return receiver.ReceiveHostInput(input);
        }

        /// <summary>
        /// 라운드 강제 중단
        /// 플러그인 정리(IRoundAbortHandler) 후 남은 Phase를 건너뛴다.
        /// 진행 중인 Phase 작업이 끝나면 StartRoundAsync가 false로 반환된다.
        /// </summary>
        public void AbortRound()
        {
            if (!IsRunning || _phaseExecutor.IsAborted)
            {
                return;
            }

            GLog.Warn("[RGF] Round aborted");

            _phaseExecutor.Abort(State);
            NotifyAborted(_currentPlugin);
        }

        private void NotifyAborted(IGamePlugin plugin)
        {
            if (!(plugin is IRoundAbortHandler handler))
            {
                return;
            }

            try
            {
                handler.OnRoundAborted(State);
            }
            catch (Exception ex)
            {
                GLog.Error($"[RGF✗] Abort handler error: {ex.Message}");
            }
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
