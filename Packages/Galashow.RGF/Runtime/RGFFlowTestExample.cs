using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Galashow.Core;
using Galashow.Bridge;

namespace Galashow.RGF
{
    /// <summary>
    /// RGF 플로우 테스트 예제
    /// 더미 플러그인으로 전체 메시지 시퀀스를 테스트
    ///
    /// 시퀀스:
    /// 1. Initialize (InitializeProgress 0% → 50% → 100% → Initialize ACK)
    /// 2. RegisterPlugin (RegisterPlugin ACK)
    /// 3. StartRound (StartRound ACK → RoundStarted NTY)
    /// 4. Phase 순회 (각 Phase마다 Started → Ended → Changed 알림)
    /// 5. RoundCompleted NTY
    /// </summary>
    public class RGFFlowTestExample : MonoBehaviour
    {
        [Header("테스트 설정")]
        [SerializeField] private bool autoStart = false;
        [SerializeField] private float delayBetweenSteps = 1f;

        private async void Start()
        {
            if (autoStart)
            {
                await RunFullFlowTest();
            }
        }

        /// <summary>
        /// 전체 플로우 테스트 실행
        /// </summary>
        [ContextMenu("Run Full Flow Test")]
        public async Task RunFullFlowTest()
        {
            GLog.Info("========== RGF Flow Test Start ==========");

            // 1단계: Initialize
            await TestInitialize();
            await Task.Delay((int)(delayBetweenSteps * 1000));

            // 2단계: RegisterPlugin
            await TestRegisterPlugin();
            await Task.Delay((int)(delayBetweenSteps * 1000));

            // 3단계: StartRound (Phase 순회 포함)
            await TestStartRound();

            GLog.Info("========== RGF Flow Test Complete ==========");
        }

        /// <summary>
        /// 1. Initialize 테스트
        /// </summary>
        private async Task TestInitialize()
        {
            GLog.Info("--- Step 1: Initialize ---");

            var adapter = RGFBridgeAdapter.Instance;

            var initRequest = new Bridge.Model.Request.R2U.RGFInitialize
            {
                SessionId = "test-session-001",
                PlayerInfo = new List<Bridge.Model.Request.R2U.PlayerInfo>
                {
                    new Bridge.Model.Request.R2U.PlayerInfo
                    {
                        PlayerIdx = 1,
                        PlayerType = "human",
                        PlayerName = "Player 1"
                    },
                    new Bridge.Model.Request.R2U.PlayerInfo
                    {
                        PlayerIdx = 2,
                        PlayerType = "human",
                        PlayerName = "Player 2"
                    },
                    new Bridge.Model.Request.R2U.PlayerInfo
                    {
                        PlayerIdx = 3,
                        PlayerType = "human",
                        PlayerName = "Player 3"
                    }
                },
                Config = new Bridge.Model.Request.R2U.Config
                {
                    EnableDebugLog = true
                }
            };

            bool success = false;
            adapter.R2U_RGFManager_Initialize_REQ(
                initRequest,
                (ack) =>
                {
                    GLog.Info($"[Test] Initialize ACK - Initialized: {ack.Initialized}, Version: {ack.Version}");
                    success = true;
                },
                (error) =>
                {
                    GLog.Error($"[Test] Initialize failed: {error}");
                }
            );

            // 응답 대기
            int timeout = 0;
            while (!success && timeout < 50)
            {
                await Task.Delay(100);
                timeout++;
            }

            if (!success)
            {
                GLog.Error("[Test] Initialize timeout");
            }
        }

        /// <summary>
        /// 2. RegisterPlugin 테스트
        /// </summary>
        private async Task TestRegisterPlugin()
        {
            GLog.Info("--- Step 2: RegisterPlugin ---");

            var adapter = RGFBridgeAdapter.Instance;

            // 더미 플러그인 생성 및 등록
            var dummyPlugin = new DummyGamePlugin();
            var pluginUuid = RGFManager.Instance.RegisterPlugin(dummyPlugin);

            GLog.Info($"[Test] Dummy plugin registered with UUID: {pluginUuid}");

            // Bridge를 통한 RegisterPlugin 시뮬레이션
            var registerRequest = new List<Bridge.Model.Request.R2U.RGFRegisterPlugin>
            {
                new Bridge.Model.Request.R2U.RGFRegisterPlugin
                {
                    MiniGameIdx = 1,
                    MiniGameName = "DummyGame"
                }
            };

            bool success = false;
            adapter.R2U_RGFManager_RegisterPlugin_REQ(
                registerRequest,
                (ackList) =>
                {
                    foreach (var ack in ackList)
                    {
                        GLog.Info($"[Test] RegisterPlugin ACK - Game: {ack.MiniGameName}, UUID: {ack.MiniGamePluginIdx}");
                    }
                    success = true;
                },
                (error) =>
                {
                    GLog.Error($"[Test] RegisterPlugin failed: {error}");
                }
            );

            // 응답 대기
            int timeout = 0;
            while (!success && timeout < 50)
            {
                await Task.Delay(100);
                timeout++;
            }
        }

        /// <summary>
        /// 3. StartRound 테스트 (Phase 순회 포함)
        /// </summary>
        private async Task TestStartRound()
        {
            GLog.Info("--- Step 3: StartRound ---");

            var adapter = RGFBridgeAdapter.Instance;

            // 등록된 플러그인 UUID 가져오기
            var dummyPlugin = new DummyGamePlugin();
            var pluginUuid = RGFManager.Instance.RegisterPlugin(dummyPlugin);

            var startRoundRequest = new Bridge.Model.Request.R2U.RGFStartRound
            {
                MiniGamePluginIdx = pluginUuid,
                RoundNumber = 1,
                GameData = null,
                PhaseDuration = new Bridge.Model.Request.R2U.PhaseDuration
                {
                    Ready = 2,
                    Setup = 1,
                    Present = 2,
                    Input = 3,
                    Wait = 1,
                    Execute = 2,
                    Reveal = 2,
                    Cleanup = 1
                }
            };

            adapter.R2U_RGFManager_StartRound_REQ(
                startRoundRequest,
                (ack) =>
                {
                    GLog.Info($"[Test] StartRound ACK - Started: {ack.Started}, Round: {ack.RoundNumber}");
                },
                (error) =>
                {
                    GLog.Error($"[Test] StartRound failed: {error}");
                }
            );

            // 라운드 완료 대기
            while (RGFManager.Instance.IsRunning)
            {
                await Task.Delay(500);
            }

            GLog.Info("[Test] Round completed");
        }
    }

    /// <summary>
    /// 더미 게임 플러그인 (테스트용)
    /// </summary>
    public class DummyGamePlugin : IGamePlugin
    {
        public string GameName => "Dummy Test Game";

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
