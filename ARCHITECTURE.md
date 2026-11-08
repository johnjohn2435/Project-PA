# Pocket Arcade — 구조와 코드 진입점

세로 화면 모바일 미니게임 허브다. Unity Universal 2D/URP, MonoBehaviour 표현부와 순수 C# 규칙을 분리하며 게스트 플레이·저장은 로컬에서 완결된다. 서버·인증 SDK는 미구현/미정이다.
현재 설정은 Unity 6000.3.16f1, URP 17.3.0이다. 숫자보다 [ProjectVersion](Client/ProjectSettings/ProjectVersion.txt), [manifest](Client/Packages/manifest.json), [lock](Client/Packages/packages-lock.json)을 우선한다.

읽는 흐름: [AGENTS](AGENTS.md) → 이 문서 → [해당 기능 계약](Agent_Instruction/README.md) → 코드·데이터 → [검증](Agent_Instruction/Verification.md).
[사람용 안내](Document/Human/Agent_Instruction_Guide.html) · [문서 색인](Document/AI/README.md) · [남은 작업](Document/AI/TODO.md)
반복 변경은 [작업 절차](Agent_Instruction/Common-Changes.md)와 [함께 확인할 곳](Agent_Instruction/Change-Checklist.md), 중요한 구조 결정의 이유는 [ADR](Document/AI/adr/README.md)에서 찾는다.

## 핵심 구조

```text
Client/                       Unity 프로젝트
  Assets/_Project/
    Scripts/Core/             순수 C# 저장 모델·프로젝트 기본값
    Scripts/Runtime/          부트·매니저·UI·저장 어댑터·미니게임
    Scripts/Editor/           씬·프리팹·문자열·게임 구성/검증 도구
    Resources/                부트에서 읽는 로컬 UI·캐릭터·오디오·JSON
    Prefabs/Managers/         직접 참조하는 독립 매니저 5종
    Prefabs/UI/               부트 로딩/다이얼로그 UI
    ScriptableObjects/        카탈로그·캐릭터·게임·튜닝 원본
    Scenes/                   BootScene, SelectScene, GameScene
    Tests/EditMode/            규칙·저장·자산·UI 계약 검사
    Tests/PlayMode/            게임별 엔진 결합 검사
ArtPipeline/                  ComfyUI 원화·승인 자료·리깅 제작
Agent_Instruction/            현재 에이전트용 기능 계약
Document/AI/                  명세·결정·계획·과거 검증 자료
Document/Human/               HTML 안내와 관련 이미지
```

`StringTable.json`·ScriptableObject는 편집 원본이다. `BootResources.json`은 Resources 인벤토리와 카탈로그를 반영하는 생성/등록 목록이다. 리그·베이크는 ArtPipeline/Editor 도구와 승인 원본을 함께 확인한다. Unity 캐시·IDE 파일은 원본이 아니다.

## 작업 영역 → 우선 열 파일

아래 코드 경로는 `Client/Assets/_Project/` 기준이다.

| 작업 | 진입점 | 계약 |
|---|---|---|
| 부트·DDOL 수명 | `Scripts/Runtime/Bootstrap/GameBootstrap.cs`, `Scripts/Runtime/Managers/PersistentManager.cs` | [부트](Agent_Instruction/Boot-Resources.md) |
| 자산 사전 로딩 | `Scripts/Runtime/Managers/ResourceManager.cs`, `Scripts/Runtime/Bootstrap/BootResourceManifest.cs`, `Resources/Boot/BootResources.json` | [부트](Agent_Instruction/Boot-Resources.md) |
| 씬·게임 라우팅 | `Scripts/Runtime/Managers/SceneFlowManager.cs`, `Scripts/Runtime/Bootstrap/SceneFlow.cs`, `Scripts/Runtime/UI/UISceneHost.cs` | [부트](Agent_Instruction/Boot-Resources.md)·[게임](Agent_Instruction/MiniGames.md) |
| UI 수명·명령 큐 | `Scripts/Runtime/Managers/UIManager.cs`, `Scripts/Runtime/UI/UIBase.cs`, `PanelFrame.cs`, `UIFeedback.cs` | [UI](Agent_Instruction/UI.md) |
| 허브·친구 선택 | `Scripts/Runtime/UI/HubView.cs`, `HubController.cs`, `HubMedals.cs`, `CharacterRosterView.cs` | [UI](Agent_Instruction/UI.md) |
| 문자열 | `Resources/Data/StringTable.json`, `Scripts/Runtime/Localization/StringTable.cs`, `Scripts/Runtime/UI/LocalizedText.cs` | [UI](Agent_Instruction/UI.md) |
| 저장·보상·튜토리얼 | `Scripts/Core/Save/`, `Scripts/Runtime/Save/JsonSaveRepository.cs`, `Scripts/Runtime/Managers/DataManager.cs` | [저장](Agent_Instruction/Save-Data.md) |
| 소리·음소거 | `Scripts/Runtime/Managers/SoundManager.cs`, `Scripts/Runtime/UI/AudioSettingIcon.cs`, `Scripts/Runtime/Audio/AudioCue.cs` | [UI](Agent_Instruction/UI.md)·[저장](Agent_Instruction/Save-Data.md) |
| 블록 그래비티 | `Scripts/Runtime/MiniGames/BlockGravity/Application/BlockGravitySession.cs`, `Scripts/Runtime/MiniGames/BlockGravity/Domain/`, `Scripts/Runtime/MiniGames/BlockGravity/Presentation/Unity/` | [게임](Agent_Instruction/MiniGames.md) |
| 원판 생존전 | `Scripts/Runtime/MiniGames/TiltFloor/FloorSession.cs`, `Unity/FloorWorldView.cs`, `Unity/FloorTuning.cs` | [게임](Agent_Instruction/MiniGames.md) |
| 시간 분신 운반 | `Scripts/Runtime/MiniGames/EchoRoom/EchoSession.cs`, `Unity/EchoWorldView.cs`, `Unity/EchoTuning.cs` | [게임](Agent_Instruction/MiniGames.md) |
| 보관 중인 급정거 배달 | `Scripts/Runtime/MiniGames/UdadaDelivery/DeliverySession.cs`, `Unity/` | [게임](Agent_Instruction/MiniGames.md) |
| 캐릭터·리그 | `Scripts/Runtime/Characters/CharacterRigController.cs`, `Scripts/Runtime/UI/CharacterRigPreview.cs`, `Resources/Characters/` | [캐릭터](Agent_Instruction/Characters-Art.md) |

## 에디터 도구

메뉴는 `Pocket Arcade/` 아래다. 각 `Scripts/Editor/`의 실제 `MenuItem`을 확인한다.

- `BootResourceSetup`: 로컬 목록·부트 UI 구성/검증. `IndependentManagerSetup`: 독립 매니저 구성.
- `StringTableSetup`: 번역·폰트 적용/검증. `UIFrameSetup`: 프레임 마이그레이션/검증. `UIFoundationSetup`: 초기 UI 구성. 기존 화면에 무조건 재실행하지 않는다.
- `BlockGravity/BlockGravitySetup`, `TiltFloor/SurvivalSetup`, `EchoRoom/EchoSetup`, `UdadaDelivery/DeliveryBrakeSetup`: 해당 게임의 구성/검증.
- `HubParkSetup`: 현재 비활성 공원 실험을 다시 연결하는 구성 도구다. 일반 허브 검증용으로 실행하지 않는다.
- [클라이언트 보충 지침](Client/AGENTS.md)과 [도구 함정](Agent_Instruction/Pitfalls.md)을 함께 확인한다.
