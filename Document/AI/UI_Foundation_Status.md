> 2026-11-06 최신 요청: 게임 고르기를 기존 원형 배지 3개 + 시작 버튼으로 복원했다. Hub.prefab은 HubMedals를 사용하고 park 참조는 비워 산책 맵/조이스틱을 생성하지 않는다. 별도 CharacterRosterView와 현재 친구 변경 버튼, 부트 단계 텍스트는 유지한다. 관련 EditMode 40개, 세 게임 시작/복귀·친구 선택·설정 PlayMode 1개 통과. [화면](../Human/Hub_UI.html#hub).

> 2026-11-06 부트 상태 표시 복원: 사용자 요청에 따라 제목 아래에 현재 로딩 단계를 민트색 주아체로 표시한다. BootStatusView의 기존 진행 이벤트를 사용하며 진행 바/퍼센트는 숨긴다. 문자열 테이블 로드 전에는 테이블에서 구운 준비 문구를 표시한다. 오류·재시도 유지. 별도 PlayMode 1/1에서 초기 문구, 실제 진행 이벤트, 실패→재시도→허브 진입 및 한국어/영문 화면 확인. [부트 화면](../Human/Hub_UI.html#boot).

> 2026-11-06 최신 허브: 인라인 친구/게임 배지 선택을 놀이공원 산책 맵과 별도 캐릭터 스크롤 목록으로 대체했다. HubParkView/Controller/World 및 HubJoystick, 카탈로그 기반 CharacterRosterView. 입구 접근 후 명시적으로 시작하며 세 게임 복귀 위치를 보존한다. 전체 EditMode 321, 실제 이동·선택·입장·복귀 PlayMode 1 통과. 12항목 목록 확장·잠금, 9대16/20대9 화면 확인. Android 실기기 미검증. [화면과 사용법](../Human/Hub_UI.html#park).

> 2026-11-06 최신 요청: 일반 부트 화면은 중앙 제목만 표시한다. 캐릭터를 제거하고 상태·진행 바·퍼센트는 비활성화했다. 실패 팝업과 재시도는 유지한다. 독립 프로젝트 PlayMode 1/1에서 실제 실패→취소→재시도→허브 진입, 한국어/영문 화면을 확인했다. Android 실기기는 미검증. [화면](../Human/Hub_UI.html#boot).

> 2026-11-06 부트 로딩·실패/재시도 UI에 크림·민트·주황색과 주아체를 적용했다. 메인 허브에는 세 게임의 원형 배지를 모두 표시한다. 관련 EditMode 53/53, 부트 실패→재시도→허브→세 번째 게임 진입/복귀 PlayMode 1/1 통과. 화면과 검증 범위는 [부트·허브 UI](../Human/Hub_UI.html#boot).

> 2026-11-06 승인된 원형 배지·크림색 종이·민트 간판 시안을 실제 허브와 블록 그래비티·원판 생존전·10초 전의 나 UI에 적용했다. 허브는 친구/게임 선택 후 시작, 추가 게임은 전체 게임에서 선택한다. 주아체·네이티브 uGUI 그래픽과 프리팹을 사용한다. EditMode 304, 관련 PlayMode 32 통과. Android 실기기 미검증. [적용 화면](../Human/Hub_UI.html).

> 2026-11-06 허브 네 화면에 Crisp Kit Free Theme를 적용했다. 홈·캐릭터·게임·설정의 세로 화면 캡처와 검증 범위는 [허브 UI](../Human/Hub_UI.html)에 있다. PNG는 허브 프리팹이 직접 참조하며 출처는 빌드의 StreamingAssets/CrispKit-Credits.txt에 포함된다.

> 2026-11-04 최신 UI 내비게이션·캐시·Back 규칙은 [UI_Frames_Status.md](UI_Frames_Status.md)가 우선한다. 아래는 이전 구현 기록이다.

# UI Foundation 구현 기록

> 최신 매니저 구조: [Independent_Managers_Status.md](Independent_Managers_Status.md). 이전 통합 루트 기록을 대체한다.


> 사용자가 기존 UI의 Play 동작을 정상 확인했다. 이후 추가된 부트 사전 로딩은 [Boot_Resources_Status.md](Boot_Resources_Status.md)가 최신 기록이다.

## 최신 상태 — Transform 중복 링크 경고 수정

- 사용자 로그: 생성/검증 메뉴가 실행되어 저장됨. 생성 중 Common의 SerializedObject.ApplyModifiedPropertiesWithoutUndo에서 Transform child is linked multiple times 경고 반복.
- 원인: 전체 Component의 모든 직렬화 참조를 재매핑하여 Transform.m_Children까지 변경. 이미 부모에 붙인 replacement가 중복 등록됨.
- UIReferenceRemapper로 분리: MonoBehaviour가 선언한 Unity 직렬화 필드만 재매핑. 배열·중첩·상속 필드 지원. Transform, GameObject 소유권, 프리팹 메타데이터 제외. 부모·자식 중복/불일치 검사를 생성 후 및 Validate에 추가.
- Android 타깃 Edit Mode **80/80 통과, skipped 0**. 기존 64 + 참조 교체 회귀 2 + 실제 생성 프리팹 읽기 전용 검사 14. C# 오류·경고 및 Transform 중복 링크 로그 없음.
- 사용자 생성 UI 프리팹 14개의 계층, 필수 UI 참조, Hub 카드/토글 개수 검사 통과. 검사한 복사본과 원본 프리팹 파일 내용 일치 확인. 실제 씬/프리팹은 수정하거나 재생성하지 않음.
- 로그: Client/Logs/ui-hierarchy-editmode.log, Client/Logs/Tests/ui-hierarchy-editmode.xml.
- 사용자 다음 단계: 컴파일 후 UI Foundation > 2. Validate Prefabs And Scenes, BootScene Play 확인. 현재 결과상 삭제·재생성 불필요. 전체 수정 생성 메뉴 및 Play Mode 재실행 검증은 미실행.

아래는 최초 구현 당시의 기록이다. 생성 미실행 및 64개 검사 표기는 위 최신 상태로 대체한다.

## 범위

- HubView/GameEntryView에서 저장·씬 이동을 HubController/GameEntryController로 분리. View는 내부 참조·표시·입력 이벤트와 시작 시 Controller 구성 담당.
- ResourceManager 싱글톤: Resources 프리팹 로드·캐시, Spawn/Release, 살아 있는 인스턴스가 있는 경로의 Evict 거부, TrimUnused로 미사용 참조 정리. 프리팹 에셋에 UnloadAsset 사용하지 않음.
- 초기화 Resource → Data → Sound → SceneFlow → UI. 종료 시 UI와 나머지 매니저를 정리한 뒤 ResourceManager 정리.
- BootStatusView가 GameBootstrap.Failed 이벤트를 구독. 영속 GameBootstrap의 bootStatus 씬 참조 제거.
- UISceneHost가 화면 프리팹 로드. UIManager가 씬 View 등록/해제와 패널 이동 관리. UIFeedback은 씬별 오버레이 수명 관리.
- 메시지박스: 1개 재사용 + FIFO 요청 큐, Task 결과 Confirmed/Cancelled/SceneClosed. 응답 전에 이벤트/내용 초기화. 씬 종료 시 모든 미완료 요청 해결.
- 팝업: 스택 최상위만 조작, 닫으면 파괴. 메시지박스 활성 중 새 팝업 거부. 모달 뒤쪽 CanvasGroup.interactable=false, 이전 입력 상태 복구.
- 토스트: 1개 재사용, unscaledTime, 대기 최대 16개. 툴팁: 대상 위치 추적, 화면 범위 보정, 대상 비활성/패널 이동/모달 열기 시 숨김.
- ToggleView 공통 입력 이벤트/무통지 표시/재사용 초기화. 설정 화면에서 실제 사용. 기본 버튼은 Unity Button 사용.
- 범용 풀, 카드 가상화, Addressables/원격 다운로드, 실제 게임 로직은 범위 밖. 캐릭터 카드는 기존 고정 카탈로그 수만큼 구성.

## 프리팹 생성 도구 (아직 미실행)

- Pocket Arcade > UI Foundation > 1. Create Prefabs And Migrate Scenes
- Pocket Arcade > UI Foundation > 2. Validate Prefabs And Scenes
- 기존 Phase 1 씬을 먼저 Client/Logs/UIFoundationBackup/<timestamp>/에 백업.
- Common/Button, Toggle, CharacterCard, MiniGameCard; Panels/Home, Characters, Games, Settings; Screens/Hub, GameEntry; Overlays/Tooltip, Toast, MessageBox, Popup을 Assets/_Project/Resources/UI/ 아래 생성.
- GameManagers는 Assets/_Project/Prefabs/Common에 저장. 기존 프리팹 파일은 덮어쓰지 않음.
- 같은 형태의 기존 UI를 공통 프리팹 인스턴스로 교체하며 화면 내 참조 재매핑. 원본의 위치·문구·색을 override로 유지. 자식 계층이 다르면 중단.
- Select/Game 씬에서 화면을 UISceneHost로 교체. Phase 1 도구는 Host가 있으면 기존 화면을 다시 만들지 않도록 보완.
- Unity MCP 미연결 규칙 때문에 생성 메뉴 및 Play Mode를 실행하지 않음. 실제 .prefab 생성 완료로 보고하지 않는다.

## 검증

- Unity 6000.3.16f1, Android 타깃 Edit Mode 최종 **64/64 통과**. 기존 55개 + UI 기반 검사 9개.
- 실제 프로젝트에서 복사한 별도 검증 프로젝트 사용. 변경한 소스·테스트 20개 파일의 SHA-256 일치 확인. 최종 로그 C# 컴파일 오류/경고 없음.
- 캐시 재사용/다중 인스턴스 해제 조건/외부 파괴 정리/중복 매니저/누락 프리팹, 메시지박스 큐와 응답, 씬 종료 취소, 팝업 입력 순서, 토글 이벤트 중복 방지, 툴팁·토스트 재사용, 모달이 있는 화면의 파괴 직후 탐색을 검사.
- 검증 초기 실행에서 테스트의 Task.Result 동기 대기가 멈췄다. 결과 완료 여부를 먼저 검사하게 바꾸고 View 표시 시 이벤트를 명시적으로 연결하도록 보완했다. 최종 결과는 보완 후 전체 검사다.
- 결과: Client/Logs/Tests/ui-foundation-editmode.xml, Client/Logs/ui-foundation-editmode.log.
- HTML 내부 앵커와 로컬 문서 링크 정적 검사 완료. 브라우저 렌더링 검사 미실행.
- 실제 에디터 생성 메뉴/프리팹 참조 검사 메뉴/Play Mode/Android 기기/터치·Raycast 검증은 미실행. 생성 도구는 컴파일 검사만 통과했으며 실제 프리팹 생성 및 마이그레이션 완료로 보고하지 않는다.

## 사용자 문서

- ../Human/UI_Foundation_Guide.html
