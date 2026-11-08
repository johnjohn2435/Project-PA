# 부팅·씬·리소스·매니저 계약

> [색인](README.md) · [구조](../ARCHITECTURE.md)

## 소유와 순서

- `Client/Assets/_Project/Scripts/Runtime/Bootstrap/GameBootstrap.cs`가 BootScene의 `Bootstrap` 오브젝트에서 시작한다. 문자열 테이블을 먼저 준비한 뒤 ResourceManager 생성/사전 로딩 → DataManager → SoundManager → SceneFlowManager → UIManager 순으로 초기화한다.
- 매니저는 `Prefabs/Managers/`의 독립 루트 5개이며 각각 DDOL 수명이다. 통합 GameManagers 루트, Instance 접근 시 몰래 생성, 매니저 간 순환 초기화를 도입하지 않는다.
- 정상 부트 완료 시 Bootstrap만 씬과 함께 제거한다. 재진입 시 준비된 매니저·캐시를 재사용한다. 실패/중단은 부분 초기화까지 정리하고 재시도하며 로딩 중 중복 요청은 막는다.
- `ResourceManager`의 비동기 세대/취소와 `UIManager`의 씬 세션 취소를 보존한다. 이전 요청의 완료가 새 세션을 오염시키면 안 된다.

## 데이터 원본과 생성

- `Client/Assets/_Project/Resources/Data/StringTable.json`은 직접 관리하는 JSON 원본이다. 별도 문자열 컴파일 바이너리는 없다.
- `Client/Assets/_Project/Resources/Boot/BootResources.json`은 `BootResourceSetup.DiscoverResources`와 카탈로그를 반영한 등록 목록이다. Resources 경로는 확장자 없는 상대 경로다. `path/kind/required`와 중복·타입 검증을 유지한다.
- `Client/Assets/_Project/ScriptableObjects/GameCatalog.asset`이 캐릭터/게임 정의를 소유한다. `BootResourceManifest`가 캐릭터 정의·Portrait·Character 슬롯을 결합한다.
- 자산 추가 후 `Pocket Arcade/Boot/1. Configure Loading Screen And Resources`의 변경 범위를 먼저 확인하고 등록 목록을 갱신한다. `2. Validate Local Resource Inventory`로 누락을 검사한다. 구성은 검증만 하는 명령이 아니다.
- 런타임은 부트 캐시의 `GetLoaded`, `Spawn`, `Release`를 사용한다. 세션 고정 에셋과 생성 인스턴스의 수명을 구분한다. 테스트 주입 로더를 제품의 즉석 로딩 우회로로 쓰지 않는다.
- 필수 누락은 부트 실패, 선택 음원 누락은 무음 등 기존 명시된 선택 동작을 따른다. `CharacterDefinition.Portrait`의 기존 직접 Sprite 참조는 유지하지만 새 비동기 로딩 fallback을 추가하지 않는다.

## 씬과 부트 화면

- `Client/ProjectSettings/EditorBuildSettings.asset`의 정상 순서는 BootScene → SelectScene → GameScene이다. `SceneFlowManager`가 선택 게임과 이동을 소유하고 `UISceneHost.gameScreens`가 같은 GameScene에서 게임별 화면을 고른다.
- 매니저 없이 Select/Game을 직접 열면 `GameBootstrap.RequireManagers`의 Boot 복귀 경로가 적용된다. 직접 실행을 정상 부트 완료와 동일하게 보고하지 않는다.
- 부트 UI는 `Prefabs/UI/BootLoadingUI.prefab`, `BootDialog.prefab`과 `BootStatusView/BootDialogSystem`이 담당하며 UIManager 초기화 밖에 있다.
- 최신 부트 표현은 제목 + 민트색 단계 문구다. 진행 바/퍼센트는 숨긴 상태를 유지한다. 첫 `boot.prepare`는 문자열 테이블에서 편집 시 구운 문구이고 런타임 문장 하드코딩이 아니다.

## 근거와 검증

[부트 과거 기록](../Document/AI/Boot_Resources_Status.md), [독립 매니저 기록](../Document/AI/Independent_Managers_Status.md)의 초기 메뉴 미실행/진행 바 설명은 현재 자산과 구분한다.
`BootResourceTests`, `BootLoadingUITests`, `IndependentManagerTests`, `ManagerTests`, `CatalogAndSceneFlowTests`를 변경 범위에 맞춰 사용한다. 실행 방법과 한계는 [검증 안내](Verification.md).
