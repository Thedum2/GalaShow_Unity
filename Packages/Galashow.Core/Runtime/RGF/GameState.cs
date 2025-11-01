using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Galashow.Core
{
    /// <summary>
    /// RGF 게임 상태 관리 클래스
    /// 플레이어, 라운드, Phase 등의 게임 상태를 중앙에서 관리
    /// PluginContext 역할도 통합하여 플러그인 실행 컨텍스트 제공
    /// </summary>
    public class GameState
    {
        #region Phase & Round State

        /// <summary>
        /// 현재 Phase
        /// </summary>
        public GamePhase CurrentPhase { get; private set; }

        /// <summary>
        /// 이전 Phase
        /// </summary>
        public GamePhase PreviousPhase { get; private set; }

        /// <summary>
        /// 현재 라운드 번호
        /// </summary>
        public int CurrentRound { get; set; }

        /// <summary>
        /// 게임 타입
        /// </summary>
        public string GameType { get; set; }

        /// <summary>
        /// 현재 게임 데이터 (라운드별 설정, 문제 등)
        /// </summary>
        public object GameData { get; set; }

        #endregion

        #region Phase Execution Context

        /// <summary>
        /// Phase 시작 시간
        /// </summary>
        public float PhaseStartTime { get; set; }

        /// <summary>
        /// Phase 지속 시간 (초)
        /// </summary>
        public float PhaseDuration { get; set; }

        /// <summary>
        /// 취소 토큰 (Phase 전환 시 작업 취소용)
        /// </summary>
        public object CancellationToken { get; set; }

        /// <summary>
        /// Unity GameObject (게임 씬 내 오브젝트 접근용)
        /// </summary>
        public GameObject GameRoot { get; set; }

        #endregion

        #region Player & Input Data

        /// <summary>
        /// 플레이어 목록
        /// Key: 플레이어 ID
        /// </summary>
        public Dictionary<string, PlayerInfo> Players { get; private set; } = new Dictionary<string, PlayerInfo>();

        /// <summary>
        /// 플레이어 입력 데이터
        /// Key: 플레이어 ID, Value: 입력 데이터
        /// </summary>
        public Dictionary<string, object> PlayerInputs { get; set; } = new Dictionary<string, object>();

        #endregion

        #region Game Result & Config

        /// <summary>
        /// 게임 결과 데이터
        /// </summary>
        public object ResultData { get; set; }

        /// <summary>
        /// 게임 설정 데이터
        /// </summary>
        public Dictionary<string, object> Config { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// 게임 통계 데이터
        /// </summary>
        public GameStatistics Statistics { get; private set; } = new GameStatistics();

        #endregion

        #region Services

        /// <summary>
        /// 서비스 접근용 딕셔너리
        /// Key: 서비스 타입명, Value: 서비스 인스턴스
        /// </summary>
        public Dictionary<string, object> Services { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// 특정 타입의 서비스 가져오기
        /// </summary>
        public T GetService<T>() where T : class
        {
            var typeName = typeof(T).Name;
            if (Services.TryGetValue(typeName, out var service))
            {
                return service as T;
            }
            return null;
        }

        /// <summary>
        /// 서비스 등록
        /// </summary>
        public void RegisterService<T>(T service) where T : class
        {
            var typeName = typeof(T).Name;
            Services[typeName] = service;
        }

        #endregion

        #region Events

        /// <summary>
        /// Phase 변경 이벤트
        /// </summary>
        public event Action<GamePhase, GamePhase> OnPhaseChanged;

        #endregion

        #region Phase Management

        /// <summary>
        /// Phase 전환
        /// </summary>
        public void TransitPhase(GamePhase newPhase)
        {
            var oldPhase = CurrentPhase;
            PreviousPhase = oldPhase;
            CurrentPhase = newPhase;

            GLog.Info($"[RGF] Phase Transit: {oldPhase} → {newPhase}");
            OnPhaseChanged?.Invoke(oldPhase, newPhase);
        }

        #endregion

        #region Player Management

        /// <summary>
        /// 플레이어 추가
        /// </summary>
        public void AddPlayer(string playerId, string playerName)
        {
            if (!Players.ContainsKey(playerId))
            {
                Players[playerId] = new PlayerInfo
                {
                    Id = playerId,
                    Name = playerName,
                    IsAlive = true,
                    Score = 0
                };
            }
        }

        /// <summary>
        /// 플레이어 제거
        /// </summary>
        public void RemovePlayer(string playerId)
        {
            Players.Remove(playerId);
        }

        /// <summary>
        /// 플레이어 상태 업데이트
        /// </summary>
        public void UpdatePlayerStatus(string playerId, bool isAlive)
        {
            if (Players.TryGetValue(playerId, out var player))
            {
                player.IsAlive = isAlive;
            }
        }

        /// <summary>
        /// 생존 플레이어 목록
        /// </summary>
        public List<PlayerInfo> GetAlivePlayers()
        {
            return Players.Values.Where(p => p.IsAlive).ToList();
        }

        /// <summary>
        /// 탈락 플레이어 목록
        /// </summary>
        public List<PlayerInfo> GetEliminatedPlayers()
        {
            return Players.Values.Where(p => !p.IsAlive).ToList();
        }

        #endregion

        #region State Management

        /// <summary>
        /// 상태 초기화
        /// </summary>
        public void Reset()
        {
            CurrentPhase = GamePhase.READY;
            PreviousPhase = GamePhase.READY;
            CurrentRound = 0;
            GameType = string.Empty;
            GameData = null;
            PhaseStartTime = 0;
            PhaseDuration = 0;
            CancellationToken = null;
            ResultData = null;
            Players.Clear();
            PlayerInputs.Clear();
            Config.Clear();
            Statistics.Reset();
            Services.Clear();
        }

        #endregion
    }

    /// <summary>
    /// 플레이어 정보
    /// </summary>
    public class PlayerInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsAlive { get; set; }
        public int Score { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 게임 통계
    /// </summary>
    public class GameStatistics
    {
        public int TotalRounds { get; set; }
        public float TotalPlayTime { get; set; }
        public float AverageRoundTime { get; set; }
        public float SurvivalRate { get; set; }
        public Dictionary<string, object> CustomStats { get; set; } = new Dictionary<string, object>();

        public void Reset()
        {
            TotalRounds = 0;
            TotalPlayTime = 0;
            AverageRoundTime = 0;
            SurvivalRate = 0;
            CustomStats.Clear();
        }
    }
}
