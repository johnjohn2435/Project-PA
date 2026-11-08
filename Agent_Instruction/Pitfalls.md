# 프로젝트 함정·환경

> [색인](README.md) · 공통 보호 규칙은 [AGENTS](../AGENTS.md)

## 2026-11-06 — 작업 폴더와 실제 원본이 다르다

- 이 채팅의 `D:\Unity\Project PA`에는 과거 작성/설치 스크립트와 검증 복사본이 있다. Git 저장소·실제 Unity 원본은 `D:\Project PA/Client`다.
- `.ui-validation`, `.echo-validation`, `.blockgravity-*`, `BlockGravityResultsValidation`의 상태를 원본 상태로 간주하지 않는다. 예전 install/finish 스크립트를 무조건 실행하면 최신 자산을 과거 버전으로 덮어쓸 수 있다.
- 실제 쓰기 전 프로젝트 루트·Git 상태·필요 권한을 확인한다. 원본과 복사본을 대조할 때 수정 대상 파일의 해시/설정 일치를 확인한다.

## 2026-09-18 — UI 참조 재매핑이 계층을 손상시킨 사례

- 전체 SerializedObject의 모든 ObjectReference를 바꾸면 Transform의 `m_Father/m_Children`과 GameObject 소유권까지 재매핑할 수 있다.
- `Scripts/Editor/UIReferenceRemapper.cs`처럼 컴포넌트가 선언한 직렬화 필드만 대상으로 한다. 계층 변경은 Transform/PrefabUtility API로 처리한다.
- [기록](../Document/AI/UI_Foundation_Status.md), `UIReferenceRemapperTests`를 확인한다.

## 2026-11-06 — 분신의 머리 없는 몸통 잔상

- SpriteRenderer만 숨기면 SkinnedMeshRenderer 몸통은 계속 보인다. 모든 Renderer와 authored 활성 상태를 보존하고 틴트는 공유 재질을 수정하지 않는다.
- `EchoWorldView.cs`와 [메시 QA](../Document/Human/Character_Render_Play_QA.html), `EchoPlayTests`가 진입점이다.

## 생성/구성 메뉴는 읽기 전용 검증이 아니다

- Boot/UI/게임의 Configure, Apply, Migrate, Build 메뉴는 씬·프리팹·JSON을 쓴다. `HubParkSetup`은 현재 해제된 공원을 다시 연결한다.
- 과거 도구에는 `Client/Logs/` 백업 경로가 남아 있다. 신규 검증 출력은 별도 작업 폴더에 두고 도구를 수정하는 작업에서 백업 경로도 검토한다. 이 문서 적용이 해당 도구 코드 수정 완료를 뜻하지 않는다.
- 실행 전 실제 MenuItem/메서드의 쓰기 범위와 기존 자산 보존 동작을 읽고 필요한 기능만 실행한다.

<a id="copies"></a>
## 원본과 여러 반영 지점을 확인할 설정·자산

| 대상 | 원본/입력 | 반영 위치와 갱신 시 주의 |
|---|---|---|
| 부트 등록 목록 | `Client/Assets/_Project/Resources/`와 GameCatalog | `Resources/Boot/BootResources.json`은 BootResourceSetup의 목록 동기화로 반영한다. Configure는 목록뿐 아니라 BootScene/매니저도 건드리므로 쓰기 범위를 먼저 읽는다. |
| 최초 표시 문자열 | `Client/Assets/_Project/Resources/Data/StringTable.json` | 프리팹에 구운 초기 문구와 LocalizedText 키. JSON만 바꿔도 초기 직렬화 문구가 자동으로 최신이 되는 것은 아니다. StringTableSetup.Apply는 여러 자산/폰트를 변경한다. |
| 과거 캐릭터 가져오기 | `ArtPipeline/Rigging/Characters_20261105/`를 읽는 `SharedCharacterSetup.Apply` | 기존 파츠/Portrait가 있으면 복사를 생략하고 Character.prefab이 있으면 생성을 건너뛴다. 원본을 바꿨다는 이유로 게임 자산도 갱신됐다고 보고하지 않는다. 이 과거 경로를 현재 승인 원본으로 간주하지 않는다. |
| 원판 크기 | `Client/Assets/_Project/ScriptableObjects/TiltFloor/FloorTuning.asset`의 halfSize | `Scripts/Runtime/MiniGames/TiltFloor/Unity/FloorTuning.cs` 기본값, Rules 변환과 월드/카메라 소비처. 실제 직렬화 값과 코드 기본값은 독립적이다. |
| 검증용 복사본 | 실제 `D:\Project PA/Client` | 보조 작업 폴더의 복사본은 빌드/동기화가 자동으로 갱신해 주는 배포본이 아니다. 수정 파일·패키지·설정 차이를 확인한다. |

별도 서버 설정·배포용 사본 경로는 현재 확정된 것이 없다. 실제 원본/생성기/출력/갱신 조건을 확인한 경우만 추가한다. 동시 수정 확인은 [체크리스트](Change-Checklist.md)를 사용한다.

## Git·로컬 파일

- 현재 저장소는 기존 변경·미추적 자산이 많다. 모든 미추적 파일을 새로 만든 임시물로 간주하거나 한꺼번에 스테이징하지 않는다.
- 공유 checkout의 브랜치는 다른 세션도 바꿀 수 있다. 커밋 직전 `git branch --show-current`, `git diff --cached --name-only`를 확인한다. 타인의 변경을 stash/restore하지 않는다.
- 캐시/빌드/로그 제외는 [실제 .gitignore](../.gitignore)를 기준으로 한다. 필수 `.meta`, 프리팹, Resources 데이터까지 생성물이라는 이유로 제외하지 않는다.
- 외부 이슈/CI 주소는 현재 확정한 것이 없다. 도구 주소·프로젝트 ID·인증 정보를 추측해 추가하지 않는다.

## 이 PC에서 재확인할 것

- PowerShell에서는 공백이 있는 경로를 인자로 인용한다. 경로 이동/삭제는 먼저 실제 절대 경로와 범위를 확인한다.
- Unity CLI는 `unity --help`, `unity status --json`으로 확인한다. 2026-11-06 이 적용 세션에서는 연결된 Editor가 발견되지 않았다. 상시 미연결로 간주하지 말고 실행 시 재조회한다.
- 에디터 버전은 프로젝트 설정을 따르고 패키지 도구는 현재 CLI 도움말을 확인한다. 연결/프로세스 조회 실패를 Unity가 종료됐다는 증거로 사용하지 않는다.
