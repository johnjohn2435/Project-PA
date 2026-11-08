# 검증 진입점

> [색인](README.md) · [구조](../ARCHITECTURE.md)

## 종류와 보고

| 종류 | 확인 범위 | 대신하지 못하는 것 |
|---|---|---|
| 문서·정적 참조 검사 | 경로·링크·코드/설정 대조 | 실행 동작 |
| 순수 로직/대역 테스트 | 상태 전이·실패·중복 처리 | 실제 Unity 수명·자산 |
| 컴파일 | 문법·타입·asmdef 결합 | 게임 동작 |
| Unity EditMode/PlayMode | 엔진 결합·프리팹·검증 시나리오 | 기기 터치·성능 |
| 실제 게임 화면 | 선택한 씬·조작·렌더링 | 다른 해상도·기기 |
| Android 빌드/실기기 | 각각 패키징/실제 조작·성능 | 서로를 대체하지 않음 |

실행한 명령·대상 프로젝트·결과·대역·미실행 항목을 보고한다. 과거 문서 수치나 호출/할당 감소를 현재 통과 수/FPS 향상으로 쓰지 않는다.

## 명령

아래는 저장소 루트 `D:\Project PA`에서 PowerShell로 실행하는 재현 예시다. CLI 옵션은 설치된 `unity test --help`로 확인했다. 이 문서 작성 때 게임 테스트는 실행하지 않았다.

```powershell
unity status --json
unity test --help
$qaOutput = 'D:\Unity\Project PA\.instruction-validation'
New-Item -ItemType Directory -Path $qaOutput -Force | Out-Null
unity test '.\Client' --mode EditMode --filter 'PocketArcade.Tests.StringTableTests' --output "$qaOutput\strings.xml" --timeout 600
```

전체 실행이 필요한 코드 변경에는 `--filter` 없이 EditMode/PlayMode를 각각 실행한다. 출력 이름을 다르게 지정한다. `unity test`는 배치 Editor를 실행하므로 동일 원본을 연 에디터와 동시 실행하지 않는다. 실행 중 Editor를 강제로 종료하지 말고 가능한 Test Runner/연결 CLI를 쓰거나 동기화된 독립 검증 프로젝트를 사용한다. 복사본 사용 시 해시·Packages·ProjectSettings 차이를 보고한다.

| 변경 영역 | EditMode 선택 | PlayMode/한계 |
|---|---|---|
| 문자열 | `PocketArcade.Tests.StringTableTests` | 한·영 글리프/부트 최초 표시 별도 |
| 부트·매니저 | `BootResourceTests`, `BootLoadingUITests`, `IndependentManagerTests`, `ManagerTests`, `CatalogAndSceneFlowTests` | 정상 부트·실패→재시도·씬 왕복 |
| UI 프레임·참조 | `UIManagerTests`, `UIFoundationTests`, `UIReferenceRemapperTests`, `HubParkTests`, `HubCarouselTests` | 현재 허브 연결과 실제 Back/모달 입력 확인; 클래스명은 과거 공원 이름을 유지함 |
| 저장·보상 | `PlayerProfileTests`, `SaveRepositoryTests`, `GameResultTests`, `OnboardingTests`, `ResultControllerTests` | 메모리 저장 대역과 실제 파일 검사를 구분 |
| 블록 그래비티 | `Tests/EditMode/BlockGravity`, `BlockGravityPresentation` | 필터 `PocketArcade.Tests.BlockGravityPlayMode` |
| 원판 생존전 | `PocketArcade.Tests.TiltFloor.FloorTests` | 필터 `PocketArcade.Tests.FloorPlay` |
| 시간 분신 | `PocketArcade.Tests.EchoRoom.EchoTests` | 필터 `PocketArcade.Tests.EchoPlay` |
| 보관 배달 | `Tests/EditMode/UdadaDelivery` | 필터 `PocketArcade.Tests.UdadaDeliveryPlay`; 정상 허브 진입 아님 |

표의 폴더는 `Client/Assets/_Project/` 기준이다. 클래스명만 쓴 행은 해당 테스트 파일의 namespace를 확인해 `--filter`를 지정한다. 독립 .NET 테스트 실행기는 현재 저장소에 확정된 것이 없으므로 있다고 가정하지 않는다.

<a id="layers"></a>
## 빠른 피드백과 엔진 통합 검사

- 작업 중에는 바뀐 규칙의 작은 테스트부터 실행하고, 자산·수명·씬 결합을 바꿨으면 해당 엔진 검사를 추가한다. 완료 전에는 영향받는 계약을 모두 확인한다. 공용 기반의 넓은 변경이나 원인 불명 회귀에는 전체 EditMode/PlayMode로 넓힌다. 문서 변경마다 전체 게임 테스트를 강제하지 않는다.
- 순수 로직 예시는 `PocketArcade.Tests.BlockGravity.BlockGravityTests`다. 현재 이 검사도 Unity Test Runner를 사용하므로 독립 .NET 실행이나 Editor 기동 없는 검사라고 표현하지 않는다. EditMode 전체가 빠른/순수 테스트인 것도 아니다. BootResourceTests처럼 실제 자산 로딩을 포함하는 클래스는 통합 검사다.
- 현재 Tests 소스에 빠름/느림용 Category 분류가 없고, 확인한 저장소의 `.github/workflows`·`.gitlab-ci.yml`·`azure-pipelines.yml`에도 강제 CI 구성이 없다. 새 검사를 추가하면 이 문서의 영역 표에 실제 fixture와 분류/선행 조건을 적는다. 실행 체계 변경이 승인되기 전에는 CI가 강제한다고 주장하지 않는다.
- 자동·수동·정적 검사를 방법별로 보고한다. 자동화가 없다는 이유로 실제 수행한 수동 검증을 숨기지 않되, 확인하지 않은 실행 동작까지 통과했다고 쓰지 않는다.

아래 예시는 저장소 루트에서 실행한다. 같은 원본을 연 Editor와 배치 테스트를 동시에 실행하지 않는 기존 제약을 먼저 확인한다.

```powershell
$qaOutput = 'D:\Unity\Project PA\.instruction-validation'
New-Item -ItemType Directory -Path $qaOutput -Force | Out-Null
# 관련 순수 규칙 한 fixture (Unity 기동 비용은 있음)
unity test '.\Client' --mode EditMode --filter 'PocketArcade.Tests.BlockGravity.BlockGravityTests' --output "$qaOutput\block-rules.xml" --timeout 600
# 공용 변경 등 전체 검증이 필요한 경우에만
unity test '.\Client' --mode EditMode --output "$qaOutput\editmode.xml" --timeout 600
unity test '.\Client' --mode PlayMode --output "$qaOutput\playmode.xml" --timeout 900
```

<a id="prerequisites"></a>
## 데이터 선행 조건과 도구 자체의 검증

- 테스트 전에 실제 변경 파일이 임포트/컴파일됐는지 확인한다. Resources 추가는 현재 목록 등록/타입을, 문자열은 key/ko/en과 필요한 초기 프리팹 값을, 캐릭터는 실제 적용 프리팹을 확인한다. 선행 갱신이 필요한 검사는 해당 실행 명령과 산출물 위치를 함께 보고한다.
- 구성/생성 메뉴는 검증 명령이 아니다. [다중 반영 지점](Pitfalls.md#copies)과 변경 범위를 확인해 필요한 갱신만 수행한다. 오래된 복사본을 통과한 결과를 최신 원본 통과로 보고하지 않는다.
- 도구·생성기·검사기를 변경하면 의미 있는 기존 단위 검사와 작은 실제 입력 실행을 수행한다. 대상/출력 경로와 재실행 시 변경 범위를 확인한다. 정상 입력과 변경한 실패 조건을 구분하고 테스트 중 사용자 원본을 덮어쓰지 않는다.
- 실제 실행이 불가능하면 정적 확인만 완료했다고 보고하고 그 도구의 실행 검증은 미완료로 남긴다. 문서에 적힌 명령을 읽은 것은 실행 검증이 아니다.

## 에디터 검증 메뉴

- `Pocket Arcade/Boot/2. Validate Local Resource Inventory`
- `Pocket Arcade/Localization/2. Validate String Table`
- `Pocket Arcade/UI Frames/2. Validate Frames`
- `Pocket Arcade/Block Gravity/2. Validate Playfield`
- `Pocket Arcade/Tilt Floor/2. Validate Experiment`
- `Pocket Arcade/Echo Room/2. Validate Prototype`

메뉴의 실제 구현과 현재 실험 대응 여부를 확인한다. Configure/Apply/Migrate 메뉴를 검증 대신 실행하지 않는다. 고빈도 로그 공통 스위치는 현재 정의하지 않았으며 기능의 실제 진단 옵션을 먼저 찾는다.

## 실제 확인 진입

- 시작 씬: `Client/Assets/_Project/Scenes/BootScene.unity`. 제목/단계 문구 → 허브 → 게임 배지 선택 → 시작 → 일시정지/재개 → 허브 복귀를 확인한다.
- 게임 화면: `Resources/UI/Screens/BlockGravity.prefab`, `TiltFloor.prefab`, `EchoRoom.prefab` (`Client/Assets/_Project/` 기준). 직접 프리팹 실행은 매니저/캐시/선택 게임이 있는 정상 진입과 다르다.
- 친구 선택: 목록에서 변경 → 저장 성공한 선택 표시 → 게임 캐릭터 일치. 저장 실패 시 기존 상태 보존.
- 원판: 착지 적 접촉 Hit, 공중 예고 비치명적, 경계 밖 Fell, 재시작 정상화. 시간 분신: 길막·중앙 겹침·새 분신·재시작 후 메시 잔상 없음.
- 한국어/영어, 작은 세로 화면, safe area, 터치, 재실행 후 저장 유지, Android 실제 기기 성능은 해당 변경 시 별도로 확인한다.
