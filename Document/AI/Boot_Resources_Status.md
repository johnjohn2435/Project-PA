# 부트 리소스 사전 로딩

> 최신 매니저 구조: [Independent_Managers_Status.md](Independent_Managers_Status.md). 이전 통합 루트 기록을 대체한다.


## 최신 상태 — 전체 로컬 로딩과 프로그레스바

- 원격 패치는 사용하지 않는다. 모든 사용 리소스는 로컬 포함 후 부트에서 준비한다. 이전 Addressables 패치 방향을 대체.
- BootStatusView에 Slider/percentage/retryButton 필드와 무통지 진행률 표시, 실패/재시도 상태 초기화를 추가했다.
- BootResourceSetup의 Configure 메뉴: 기존 BootScene·JSON 백업, Resources 파일 전체 탐색/등록, BootLoadingUI 프리팹 생성 및 기존 텍스트 UI를 교체. 기존 프리팹은 덮어쓰지 않는다.
- 메뉴: Pocket Arcade > Boot > 1. Configure Loading Screen And Resources → 2. Validate Local Resource Inventory.
- 생성 위치: Assets/_Project/Prefabs/UI/BootLoadingUI.prefab. 메뉴는 미실행이며 실제 프로그레스바 프리팹 생성·씬 연결·렌더링 검증은 아직 남아 있다. 프로젝트의 MCP 미연결 규칙에 따라 Editor 도구만 준비했다.
- ResourceManager의 기본 Spawn은 로드 완료 전/미등록 에셋 요청을 거부하며, 런타임 Resources.Load fallback을 제거. Initialize에 명시적으로 주입하는 기존 인메모리 테스트 로더만 테스트 격리용으로 유지. 실제 Bootstrap은 로더를 주입하지 않는다.
- 성공한 부트 Preload는 같은 세션에서 다시 실행하지 못한다. 재시도/재시작은 Shutdown 뒤 다시 초기화. 에셋은 세션 내내 고정 캐시.
- Play/Build guard로 Assets/_Project/Resources의 파일이 목록에서 빠진 경우 실행 전에 차단. 일반 Texture/Material/TextAsset 등의 Asset 종류도 지원. Sprite는 Single 임포트 사용. 부트 UI/카탈로그 직접 참조 의존성은 씬과 함께 로드된다.
- 최종 Unity 6000.3.16f1 Android Edit Mode **96/96 통과, skipped 0**. 기존 89 + 진행률/퍼센트/재시도/실제 인벤토리/부트 이후 추가 로딩 차단/일반 에셋 검사 7개. C# 컴파일 오류·경고 없음.
- 수정 소스/테스트 6개 파일이 검증 복사본과 해시 일치. 결과: Client/Logs/boot-loading-ui-editmode.log, Client/Logs/Tests/boot-loading-ui-editmode.xml.

아래는 이전 단계의 기록이며, 새 로딩바 적용에는 위 메뉴 실행이 필요하다. 이전의 씬 설정 불필요 안내를 대체한다.

## 구현

- 사용자가 사운드·UI·캐릭터를 부트에서 로드하고 미제공 에셋의 통로도 마련하도록 요청.
- Resources/Boot/BootResources.json: 버전1, path/kind/required 항목. UI 14개 필수, 오디오 5개 선택, 기존 캐릭터 3종의 Portrait/Character 각 선택 슬롯. 카탈로그의 신규 캐릭터도 기본 경로 자동 추가.
- BootResourceManifest: 경로/종류/중복 검증. GameCatalog의 CharacterDefinition을 직접 등록하고 Portrait의 기존 Sprite 참조를 fallback으로 사용. 직접 참조 에셋은 Unity가 부트씬과 함께 먼저 로드할 수 있음.
- ResourceManager: Object 타입 캐시, GetLoaded<T>, LoadAsync 기반 Preload, 진행률, 필수 누락 오류, 선택 누락 목록, 타입 검증, 중복 로딩 거부, generation으로 취소 후 늦은 결과 차단. 부트 리소스는 세션 고정, Evict/TrimUnused에서 제외. 인스턴스는 생성하지 않음.
- GameBootstrap: 등록목록 비동기 읽기 → 리소스 준비 → Data/Sound/SceneFlow/UI 초기화 → Ready 후 허브. 실패 시 Shutdown, Boot에 남아 Retry 허용. 등록목록부터 다시 로드.
- BootStatusView: 기존 label로 진행률·단계와 실패/재시도 표시. 실패 시 텍스트 클릭 또는 Enter로 재시도. 실패 상세 경로는 Console, 제품 화면에는 일반 안내 표시.
- SoundManager.UseBootResources: 클릭 클립 자동 연결, 허브 BGM 있으면 재생. 기존 직렬화 uiClickClip fallback 유지. Game BGM/Confirm/Cancel은 로드 경로만 마련, 자동 재생 이벤트 미연결.
- CharacterDefinition.Portrait: 부트 캐시 Sprite 우선, 없으면 기존 SourcePortrait. 기존 이미지/ID/원화는 변경하지 않음. 캐릭터 프리팹은 로드만 하며 게임에서 생성하도록 준비.
- 기존 씬/프리팹을 수정하거나 재생성하지 않음. 새 JSON·C#을 임포트 후 기존 BootScene Play로 적용.

## 검증

- Unity 6000.3.16f1, Android 타깃 Edit Mode 최종 **89/89 통과**, skipped 0. 기존 80 + 부트 리소스 9 검사. 실제 Resources.LoadAsync로 JSON 및 필수 UI 14종과 캐릭터 정의 로딩 검사 포함.
- 비동기 대기/진행률/세션 고정, 필수 누락 후 재시도, 선택 오디오 추후 제공, 잘못된 타입, 취소 후 늦은 결과, 동시 로딩 거부, Sprite fallback, JSON 중복/종류 검증 포함.
- 최종 로그 C# 컴파일 오류·경고 없음. 변경 소스·테스트·JSON 9개 파일 해시와 검증 복사본의 일치 확인. 누락된 메타 파일 보완. HTML 로컬 링크 정적 확인.
- 로그: Client/Logs/boot-resources-editmode.log, Client/Logs/Tests/boot-resources-editmode.xml.
- 실제 에디터의 Play Mode/씬 왕복/재시도 텍스트 클릭과 실제 음원 청취는 수행하지 않음. 사용자 다음 단계는 기존 BootScene Play이며 재생성 메뉴 실행은 불필요.
- 기존 UI Foundation은 사용자가 전체 정상 동작을 확인했다. 새 부트 변경은 Play Mode/기기/음원 청취 미검증.

## 사람용 안내

- ../Human/Boot_Resources_Guide.html
