# Phase 1 실행 흐름과 코드 구조

> 최신 UI 구조/프리팹 적용 및 검증 상태: [UI_Foundation_Status.md](UI_Foundation_Status.md). 아래 이전 단계 기록보다 최신 내용을 우선한다.

## 최신 구조: 기능별 싱글톤 매니저

2026-09-18 사용자 결정에 따라 주요 전역 기능을 DataManager, SoundManager, SceneFlowManager, UIManager로 분리했다. 이전 GameBootstrap.Instance.Profile/Flow/Catalog 접근은 제거했다.

```text
BootScene / Game Services (DontDestroyOnLoad)
  GameBootstrap.Start
    1. DataManager.Initialize: 카탈로그 검사, JSON 로드, PlayerProfile 생성
    2. SoundManager.Initialize: DataManager의 설정 읽기, AudioSource 2개 구성
    3. SceneFlowManager.Initialize: 기존 SceneFlow 구성
    4. UIManager.Initialize: 화면 관리 준비
    5. SceneFlowManager.OpenHub: SelectScene 진입

SelectScene / HubView
  캐릭터·설정 변경 → DataManager.Instance → PlayerProfile → JSON 저장
  저장 성공 → Changed 이벤트 → UI 갱신, SoundManager 설정 반영
  게임 선택 → SceneFlowManager.Instance → GameScene
  패널·게임 이동 버튼 → SoundManager.Instance.PlayUiClick (클립 있을 때)

GameScene / GameEntryView
  DataManager.Instance에서 선택 캐릭터 조회
  SceneFlowManager.Instance에서 선택 게임 조회, 허브 복귀
```

## 책임과 파일 위치

모든 경로는 Client/Assets/_Project/ 기준이다.

| 파일 | 책임 |
| --- | --- |
| Scripts/Runtime/Managers/DataManager.cs | 데이터 싱글톤. Catalog, SelectedCharacterId, Coins, Settings, Snapshot 조회 및 SelectCharacter/UpdateSettings 제공 |
| Scripts/Runtime/Managers/SoundManager.cs | 사운드 싱글톤. PlayBgm/StopBgm/PlaySfx/PlayUiClick과 음소거 설정 반영 |
| Scripts/Runtime/Managers/SceneFlowManager.cs | 씬 이동 싱글톤. OpenHub/OpenGame, IsLoading/SelectedGame 제공 |
| Scripts/Runtime/Bootstrap/GameBootstrap.cs | 매니저 초기화 순서, 영속 루트 수명, 초기화 실패 정리, 최초 허브 진입 |
| Scripts/Runtime/Bootstrap/SceneFlow.cs | 카탈로그 이용 가능 여부, 빌드 씬 등록 확인, 비동기 로딩과 중복 요청 방지 |
| Scripts/Core/Save/PlayerProfile.cs | 복사 → 저장 → 현재 상태 교체 → Changed 이벤트. 실패 시 이전 데이터 유지 |
| Scripts/Core/Save/SaveData.cs | 저장 스키마·기본값·정규화·깊은 복사 |
| Scripts/Core/Save/ISaveRepository.cs | Load/Save 교체 경계 |
| Scripts/Runtime/Save/JsonSaveRepository.cs | JSON 파일 읽기·쓰기, 임시 파일 교체, 백업 복구 |
| Scripts/Runtime/Meta/GameCatalog.cs | 캐릭터·게임 정의 목록과 ID 검사 |
| Scripts/Runtime/Meta/Characters/CharacterDefinition.cs | 캐릭터 ID·이름·Portrait·대표 색·기본 해금·능력 ID |
| Scripts/Runtime/Meta/MiniGames/MiniGameDefinition.cs | 게임 ID·이름·썸네일·씬 이름·이용 가능 여부 |
| Scripts/Runtime/UI/HubView.cs | 허브 네 패널, 데이터 변경 이벤트, 저장 실패 안내, 매니저 호출 |
| Scripts/Runtime/UI/CharacterCardView.cs, MiniGameCardView.cs | 카드 표현과 클릭 전달 |
| Scripts/Runtime/UI/GameEntryView.cs | 선택 게임/캐릭터 안내 및 허브 복귀 |
| Scripts/Editor/Phase1Setup.cs | 데이터·씬 UI 생성 및 매니저 구성/참조 검사 |

## 호출 예시

아래 호출은 GameBootstrap 초기화 완료 후 사용한다.

```csharp
DataManager.Instance.SelectCharacter("char_bori");
var settings = DataManager.Instance.Settings;
settings.bgmEnabled = false;
DataManager.Instance.UpdateSettings(settings);
SoundManager.Instance.PlayBgm(bgmClip);
SoundManager.Instance.PlaySfx(effectClip);
SoundManager.Instance.StopBgm();
SceneFlowManager.Instance.OpenGame("block_gravity", out string error);
```

DataManager의 Settings/Snapshot은 복사본이다. 조회한 값만 수정하면 저장되지 않으며 UpdateSettings처럼 변경 메서드를 사용해야 한다. 저장 실패 예외는 기존 HubView에서 안내한다.

## 매니저 생명주기

- Instance는 Initialize가 성공한 후에만 게시한다. 접근 시 자동 생성하지 않는다.
- 같은 매니저를 같은 의존성으로 다시 초기화하면 기존 상태를 유지한다. 다른 인스턴스가 이미 초기화됐다면 예외로 거부한다.
- Bootstrap 루트 하나를 DontDestroyOnLoad로 유지한다. UI나 개별 캐릭터는 싱글톤이 아니다.
- Shutdown은 이벤트 구독·정적 참조·실행 상태를 정리한다. Bootstrap의 초기화 실패와 OnDestroy에서도 호출한다.
- RuntimeInitializeOnLoadMethod의 SubsystemRegistration에서 정적 참조를 초기화한다.
- SelectScene/GameScene을 매니저 없이 직접 Play하면 BootScene으로 이동한다.

## 사운드 범위

BGM용과 SFX용 AudioSource를 런타임에 각각 생성한다. 2D 사운드이며 playOnAwake는 false다. BGM은 반복 재생한다.

- 저장 성공 후 DataManager.Changed 이벤트로 SoundManager가 설정을 적용한다.
- BGM Off는 mute를 사용하여 재생 위치를 유지한다. 다시 켜면 이어서 들린다.
- SFX Off는 현재 효과음을 정지하고 새 PlaySfx 호출도 재생하지 않는다.
- Ui Click Clip은 선택 항목이다. 미지정 또는 PlayBgm/PlaySfx에 null 전달 시 재생하지 않는다.
- 실제 음원, 자동 시작할 BGM, 믹서 그룹, 볼륨 슬라이더, 진동 출력은 아직 없다.
- 오디오 API와 mute 상태 테스트는 실제 청취 검증을 대신하지 않는다.

## 어셈블리와 저장 형식

기존 Core/Runtime/Editor/Tests 경계를 유지한다. Core는 Unity 참조가 없으며 Runtime이 Core를 참조한다. 새 패키지는 추가하지 않았다.

저장 형식은 기존 schemaVersion 1과 player-save.json을 유지한다. JsonUtility에 맞춰 highScores는 gameId/score 객체 목록이며, 선택·코인·설정을 저장한다. 코인 지급·최고 기록 갱신은 추후 게임 결과 처리에서 추가한다.

## 적용과 검증

이미 생성한 BootScene도 Phase 1의 Create Hub And Data 메뉴를 다시 실행하면 기존 GameBootstrap 루트에 누락된 매니저만 추가한다. 화면·데이터 에셋을 덮어쓰지 않는다. 이후 Validate Hub를 실행한다.

실행한 테스트 결과와 남은 Play Mode·기기·청취 확인은 [작업 결과](Phase1_Status.md), 사람용 설명은 [HTML 가이드](../Human/Phase1_Guide.html)를 참조한다.
## UIManager 추가

- UIManager.Instance가 ShowPanel/Back/ShowHome/ShowNotice와 활성 씬 View 등록·해제를 담당한다.
- UISceneView를 HubView와 GameEntryView가 상속한다. 내부 패널 활성화와 텍스트 표시만 View에 남겼다.
- 초기화된 View가 RegisterUI로 등록하고 OnDisable에서 해제한다. 재활성화 때 재등록한다.
- 패널 이력은 씬 View에 한정하며 등록 해제 시 비운다. GameEntry에서 허브 패널을 여는 요청은 거부한다.
- UIManager는 실제 씬 이동이나 저장을 수행하지 않는다. 영속 매니저에는 씬 UI 참조를 직렬화하지 않는다.
- 공통 팝업 프리팹·UI 프리팹 추출·ResourceManager는 미구현이다. ShowNotice는 현재 안내 텍스트만 바꾼다.
- 새 파일: Scripts/Runtime/Managers/UIManager.cs, Scripts/Runtime/UI/UISceneView.cs, Tests/EditMode/UIManagerTests.cs.
- 사람용 최신 설명: [UI_Manager_Guide.html](../Human/UI_Manager_Guide.html).