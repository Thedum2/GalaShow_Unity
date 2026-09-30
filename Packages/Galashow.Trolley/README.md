# 트롤리 딜레마

호스트가 고를 선택지를 시청자가 예상하고, 호스트와 같은 선택을 한 사람만 살아남는 미니게임. 규칙·데이터는 [docs/minigame-trolley.md](../../../docs/minigame-trolley.md), 패키지 규칙은 [docs/minigame-package.md](../../../docs/minigame-package.md)를 따른다.

| 항목 | 값 |
| --- | --- |
| 패키지 | `com.galashow.trolley` |
| 플러그인 ID | `galashow.trolley` |
| 어셈블리 | `Galashow.Trolley`, `Galashow.Trolley.Demo`, `Galashow.Trolley.Editor` |

## 구성

| 파일 | 역할 |
| --- | --- |
| `Runtime/TrolleyDilemmaPlugin.cs` | 단계 진행, 시청자 입력, 호스트 선택(`SubmitHostChoice`), 판정 반영 |
| `Runtime/TrolleyRules.cs` | 입력 해석(A/B·1/2·!A)과 `match_host` 판정 (Unity 의존 없음) |
| `Runtime/TrolleyGameData.cs` | gameData(6.1 형식)·rule·결과 모델 |
| `Runtime/Stage/TrolleyStage.cs` | 연출 흐름·카메라 샷 정의, 호스트 버튼 입력 |
| `Runtime/Stage/TrolleyCameraDirector.cs` | 샷 단위 카메라(블렌드·컷·더치 앵글·화각 킥) |
| `Runtime/Stage/TrolleyFx.cs` | Feel(MMF_Player) 피드백 묶음: 흔들림·FOV·플래시·정지 프레임·슬로모션·UI 펀치·카운트업 |
| `Runtime/Stage/TrolleyCast.cs` | 참가자 캐릭터 배치: 대기(출발점 옆) → 고른 선로에 눕기 → 치임/생존 |
| `Runtime/Stage/TrolleyWorld.cs` · `TrolleyHud.cs` · `TrolleyParticles.cs` · `TrolleyAudio.cs` | 샘플 3D 무대, 방송 UI, 파티클, 효과음(합성음 대체 포함) |
| `Runtime/Resources/Trolley/TrolleyFxProfile.asset` | 효과음·볼륨·흔들림 강도·슬로모션 설정 (인스펙터에서 조정) |
| `Demo/` | React 없이 가상 시청자로 반복 실행하는 데모 |
| `Editor/TrolleyDemoMenu.cs` | 메뉴 **Galashow → 트롤리 딜레마 → 데모 씬 열기** |

## 의존성

- **Feel 5.9.1** (`Assets/Feel`, 어셈블리 `MoreMountains.Tools`): `Galashow.Trolley.asmdef`가 참조한다. Feel을 지우면 이 패키지는 컴파일되지 않는다.
- 효과음은 Feel 데모 폴더(`Assets/Feel/NiceVibrations`, `MMFeedbacks/Demos`, `FeelDemos`)의 wav를 프로필에서 참조한다. 데모 폴더를 지우면 합성음으로 대체된다.
- 한글 표시: TMP 기본 폰트 Black Han Sans (`TMP Settings.asset`).
- **캐릭터**: `com.galashow.common`의 `CharacterCatalog`(`Packages/Galashow.Common/Runtime/Resources/Galashow/CharacterCatalog.asset`)가 Admin 아바타 이름 → `Assets/Quirky Series Ultimate/FREE/Prefabs`를 잇는다. 기본 이름은 Colobus·Gecko·Herring·Muskrat·Pudu·Sparrow·Squid·Taipan과 한글 별칭(콜로부스·게코·청어·사향쥐·푸두·참새·오징어·타이판). **Admin에 등록하는 아바타 이름을 이 이름과 맞추거나, 카탈로그 에셋의 별칭에 Admin 이름을 추가한다.** 이름이 없으면 참가자별로 고정된 대체 캐릭터가 나온다.

## 연출 흐름

| 단계 | 연출 |
| --- | --- |
| PRESENT | 분기점 위 항공샷 → 멀리(z -64)서 달려오는 트롤리를 쫓는 추적샷, 경적, "트롤리 딜레마" 문구, 딜레마 패널 팝인·카드 진입. 트롤리는 입력 마감 시점에 분기점 앞에 도착하도록 감속하며 달려온다(바퀴 회전·연기·엔진음이 속도에 연동) |
| INPUT | "예측 시작!", 입력한 캐릭터가 고른 선로에 누움. 카메라가 약 3.4초마다 샷을 바꿈: 전경 궤도 → 측면 저공 트래킹(컷) → 두 선로 사이 정면(컷) → 전경 → 근접 추적 → 누운 캐릭터 시점(컷). 매초 타이머 펀치·경고등. 마지막 5초는 트롤리 정면으로 밀고 들어가는 긴장 샷(화각 축소·더치 앵글)과 심장박동·붉은 비네트·3·2·1 |
| WAIT | "입력 마감!"·경적, 레버 주위를 도는 클로즈업, 가속하는 드럼롤·진동, "호스트의 선택은...?" |
| REVEAL | 레터박스, 레버와 함께 호스트가 지킨 쪽 공개(섬광·심벌), 트롤리가 **반대편 선로**로 질주하며 누운 캐릭터를 차례로 날림(첫 충돌은 정지 프레임→슬로모션·강한 흔들림·FOV·파편), 전체 구도에서 생존 캐릭터는 일어나 점프·탈락은 쓰러짐, 카운트업·분포 막대·색종이. 전원 구제면 급정거 |

슬로모션은 `Time.timeScale`을 바꾸므로 결과 단계가 실제 시간으로 약 0.5초 늘어난다.

## 실행해 보기

1. 메뉴 **Galashow → 트롤리 딜레마 → 데모 씬 열기** (`Assets/Scenes/TrolleyDemo.unity` 생성)
2. Play. 가상 시청자 16명(아바타 8종 순환 배정)이 INPUT 동안 채팅하고, 한 판이 끝나면 `R` 키로 다시 시작한다.
3. 호스트 선택: 문제 공개~입력 마감 동안 호스트 선택 팝업(Client는 React 팝업, 데모는 화면 위 데모 팝업)에서 **지킬 선로**를 고른다. 트롤리는 반대편으로 간다. 고르지 않으면 무작위.
   시청자는 채팅 `1` 또는 `2`만 입력한다.
4. `TrolleyDemo` 인스펙터에서 참가자 수, 아바타 목록, 입력률, A 선호도, 단계 시간을 바꾼다.

## 현재 한계

- 딜레마·선택지·호스트 선택은 React 팝업(`RGFManager_PromptOpened`)이 보여 준다. 팝업은 방송 화면에 보이므로 누르는 커서 위치가 드러날 수 있다(선택 후 버튼 모양은 바꾸지 않는다).
- `rule`의 기본값(미입력 탈락, 전원 탈락 시 전원 생존, 호스트 미선택 시 무작위)은 기획 문서의 예시 값이며 PRD에서 미정이다.
- 무대는 프리미티브 샘플이다. 선택지는 2개만 지원한다.
- 결과 상세(분포·사유)는 `GameState.ResultData`에만 있고 `RoundCompleted`로 보내지 않는다.
