# RGF (Round Game Framework)

**8단계 생명주기 기반의 Unity 미니게임 프레임워크**

## 📁 구조

```
Galashow.Core/Runtime/RGF/
├── GamePhase.cs          # 8단계 Phase enum
├── GameState.cs          # 게임 상태 + 실행 컨텍스트 통합
├── IGamePlugin.cs        # 플러그인 인터페이스
├── RGFManager.cs         # 핵심 게임 엔진
└── README.md             # 이 파일

Galashow.Trolley/Runtime/
├── TrolleyDilemmaPlugin.cs   # 트롤리 딜레마 플러그인 구현체
└── TrolleyGameData.cs         # 트롤리 게임 데이터 구조
```

## 🎮 8단계 생명주기

| Phase | 설명 | 권장 시간 |
|-------|------|----------|
| **READY** | 게임 준비 (플레이어 준비 확인) | 3초 |
| **SETUP** | 게임 데이터 구축 (리소스 로드) | 1초 |
| **PRESENT** | 문제/상황 제시 | 3초 |
| **INPUT** | 플레이어 입력 수집 | 30초 |
| **WAIT** | 입력 마감 대기 | 0초 |
| **EXECUTE** | 결과 계산 | 2초 |
| **REVEAL** | 결과 연출 | 8초 |
| **CLEANUP** | 정리 및 다음 준비 | 2초 |

## 🚀 사용법

### 1. 플러그인 작성

```csharp
using Galashow.Core;
using System.Threading.Tasks;

public class MyGamePlugin : IGamePlugin
{
    public string GameType => "my_game";
    public string GameName => "My Game";

    public async Task OnReadyAsync(GameState state)
    {
        // READY Phase 로직
        GLog.Info($"Ready for round {state.CurrentRound}");
    }

    public async Task OnSetupAsync(GameState state)
    {
        // 리소스 로드, 오브젝트 생성
    }

    public async Task OnPresentAsync(GameState state)
    {
        // 문제 화면 표시
    }

    public async Task OnInputAsync(GameState state)
    {
        // 플레이어 입력 대기
    }

    public async Task OnWaitAsync(GameState state)
    {
        // 입력 마감
    }

    public async Task OnExecuteAsync(GameState state)
    {
        // 결과 계산
    }

    public async Task OnRevealAsync(GameState state)
    {
        // 결과 연출
    }

    public async Task OnCleanupAsync(GameState state)
    {
        // 정리
    }

    public async Task OnPhaseTransitionAsync(GamePhase from, GamePhase to)
    {
        // Phase 전환 시 처리 (선택)
    }
}
```

### 2. 플러그인 등록

```csharp
void Start()
{
    var manager = RGFManager.Instance;

    // 플러그인 등록
    manager.RegisterPlugin(new MyGamePlugin());
    manager.RegisterPlugin(new TrolleyDilemmaPlugin());
}
```

### 3. 라운드 실행

```csharp
async void StartGame()
{
    var manager = RGFManager.Instance;

    // 게임 데이터 준비
    var gameData = new MyGameData
    {
        RoundNumber = 1,
        Title = "라운드 1",
        // ...
    };

    // 라운드 시작 (8단계 자동 실행)
    await manager.StartRoundAsync("my_game", 1, gameData);
}
```

### 4. 서비스 등록 (선택)

```csharp
void RegisterServices()
{
    var state = RGFManager.Instance.State;

    // 서비스 등록
    state.RegisterService(audioService);
    state.RegisterService(uiService);
    state.RegisterService(cameraService);
}

// 플러그인에서 사용
public async Task OnPresentAsync(GameState state)
{
    var uiService = state.GetService<UIService>();
    uiService?.ShowQuestion(gameData.Question);
}
```

## 📊 GameState

`GameState`는 게임의 모든 상태와 실행 컨텍스트를 포함합니다:

```csharp
public class GameState
{
    // Phase & Round
    public GamePhase CurrentPhase { get; }
    public int CurrentRound { get; set; }
    public string GameType { get; set; }
    public object GameData { get; set; }

    // Execution Context
    public float PhaseStartTime { get; set; }
    public float PhaseDuration { get; set; }
    public object CancellationToken { get; set; }
    public GameObject GameRoot { get; set; }

    // Players & Input
    public Dictionary<string, PlayerInfo> Players { get; }
    public Dictionary<string, object> PlayerInputs { get; set; }

    // Result & Config
    public object ResultData { get; set; }
    public Dictionary<string, object> Config { get; }
    public GameStatistics Statistics { get; }

    // Services
    public T GetService<T>() where T : class;
    public void RegisterService<T>(T service) where T : class;
}
```

## 🎯 특징

### 1. **플러그인 시스템**
- `IGamePlugin` 인터페이스 구현으로 새로운 게임 추가
- 동적 플러그인 등록/해제

### 2. **통합된 상태 관리**
- `GameState`가 게임 상태 + 실행 컨텍스트 통합
- 플레이어, 입력, 결과, 서비스 등 모든 정보 중앙 관리

### 3. **8단계 생명주기**
- 명확한 단계 구분으로 일관성 있는 게임 흐름
- Phase별 이벤트 및 취소 토큰 지원

### 4. **비동기 처리**
- 모든 Phase가 `async/await` 패턴 지원
- TaskRunner와 통합된 작업 취소

### 5. **의존성 분리**
- Core 패키지는 Bridge/Common 참조 없음
- 순수한 게임 프레임워크로 독립 동작

## 🔧 고급 기능

### Phase별 지속 시간 커스터마이징

```csharp
var manager = RGFManager.Instance;
manager.SetPhaseDuration(GamePhase.INPUT, 60f);  // 60초로 변경
```

### Phase 이벤트 구독

```csharp
manager.OnPhaseStarted += (phase) =>
{
    Debug.Log($"Phase started: {phase}");
};

manager.OnPhaseEnded += (phase) =>
{
    Debug.Log($"Phase ended: {phase}");
};
```

### 라운드 중단

```csharp
manager.AbortRound();
```

## 📦 의존성

- **Galashow.Core.TaskRunner**: 비동기 작업 관리
- **Unity Engine**: GameObject, Time 등

## 🔗 Bridge 통합 (선택)

Bridge 통신을 사용하려면 Common 패키지에 어댑터를 작성:

```csharp
public class GameBridgeAdapter : MonoBehaviour, IGamePort, ISimulationPort
{
    private RGFManager _rgfManager;

    void Start()
    {
        _rgfManager = RGFManager.Instance;

        // Bridge 핸들러 등록
        var handler = BridgeManager.Instance.GetHandler<GameHandler>("GameManager");
        handler?.AddPort(this);
    }

    public void R2U_SimulationManager_Selected_NTY(Notify.R2U.Selected data)
    {
        // 라운드 시작
        var gameData = ConvertToGameData(data);
        await _rgfManager.StartRoundAsync("game_type", data.Index, gameData);
    }

    // 기타 Bridge 통신 메서드들...
}
```

## 📝 예제: 트롤리 딜레마

`Packages/Galashow.Trolley/Runtime/TrolleyDilemmaPlugin.cs` 참고

---

**🎮 Created with RGF - Round Game Framework**
