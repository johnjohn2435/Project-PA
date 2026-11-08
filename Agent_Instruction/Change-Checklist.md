# 동시 수정 체크리스트

> [기능별 색인](README.md) · [반복 작업 절차](Common-Changes.md)
> 경로는 `Client/Assets/_Project/` 기준이다. 기준일: 2026-11-06, 코드와 테스트의 검사 범위를 대조했다.

한 곳을 바꿀 때 함께 확인할 지점이다. 개수는 실제 파일 수를 보장하지 않는 논리적 확인 지점 수이며 변경 시 소비처를 다시 검색한다. 줄 번호 대신 파일·심볼을 사용한다.
완전한 자동 검사 없이 빠지기 쉬운 항목을 먼저 둔다. 검사로 잡히는 부분은 명시하고, 새로 확인한 누락을 추가한다. 경고만 늘리지 말고 구현을 한곳에 모으거나 의미 있는 검사를 붙이면 해당 행을 축소/제거한다. 이를 이유로 요청 밖 리팩터링을 시작하지 않는다.

| 변경 | 함께 확인할 곳 | 잡아 주는 장치와 한계 | 빠뜨리면 생기는 일 |
|---|---|---|---|
| <a id="a1"></a>**A1. 신규 기본 해금 캐릭터** | 5지점: `ScriptableObjects/GameCatalog.asset`, 캐릭터 정의/리소스/문자열, `Scripts/Core/Save/SaveData.cs`의 초기 unlockedCharacterIds, `SaveData.Normalize`, `Tests/EditMode/OnboardingTests.cs`의 리그 검사 대상 | GameCatalog.Validate/문자열/부트 검사는 일부 누락을 잡는다. 현재 Normalize와 리그 테스트는 ID 목록이 고정되어 새 ID의 기존 저장 해금·리그 검사를 자동으로 보장하지 않는다. | 새 친구가 표시돼도 기존 저장에서 선택 불가, 신규 스킨이 테스트 대상에서 빠짐. [절차](Common-Changes.md#character) |
| <a id="a2"></a>**A2. 게임 카탈로그·화면 라우팅** | 5지점: MiniGameDefinition/GameCatalog, `Scripts/Runtime/UI/UISceneHost.cs`의 gameScreens와 GameScene 직렬화 값, 화면 프리팹/부트 목록, 문자열, 영구 기록을 쓰는 경우 ScoreKey/DataManager | GameCatalog.Validate는 ID/씬 필드, ValidateInventory는 등록을 검사한다. `UISceneHost.ScreenFor`는 매칭이 없으면 기본 screenPath를 반환하므로 모든 신규 ID의 올바른 화면을 보장하지 않는다. | 새 게임 선택 후 기본 게임 화면이 열리거나 다른 규칙 기록과 혼합. [절차](Common-Changes.md#game) |
| <a id="a3"></a>**A3. 저장 필드·튜토리얼·설정 추가** | 4지점: `Scripts/Core/Save/SaveData.cs` 기본값/Normalize, Copy와 설정 Copy, `PlayerProfile`의 변경/Commit, 저장·복구·이전 버전·실패 테스트 | 기존 SaveRepositoryTests/PlayerProfileTests/OnboardingTests가 기존 필드를 검사한다. 새 필드의 복사·마이그레이션 누락까지 자동 탐지한다고 가정하지 않는다. | 복사/저장 후 값 소실, 실패했는데 UI만 변경. 상세 [저장 계약](Save-Data.md) |
| <a id="a4"></a>**A4. UI 키·창·씬 수명** | 5지점: 프리팹의 UIBase 키/참조, 호출 Controller, 부트 등록, 중첩 UIBase/프레임, 구독/Unmount/취소 | UIManagerTests/UIFrameSetup.Validate가 중복 키·참조·프레임을 일부 검사한다. 실제 버튼 경로·모달 입력·씬 복귀 시나리오는 별도 확인한다. | 열리지 않는 창, 이전 씬 이벤트/늦은 콜백, 입력 가로채기. [절차](Common-Changes.md#ui) |
| <a id="a5"></a>**A5. 문자열·Resources 항목** | 4지점: StringTable key/ko/en, 호출·LocalizedText/동적 라벨, 필요 시 프리팹에 구운 초기 문구, BootResources 목록/타입 | `StringTableTests.EveryLiteralLookupKeyExistsInProductionTable`는 리터럴 호출 키, `StringTableSetup.Validate`는 선언된 바인딩/이름, `BootResourceSetup.ValidateInventory`는 등록을 검사한다. 동적 키 전체·초기 구운 문구의 최신성·레이아웃은 별도다. | 키 노출, 오래된 부트 문구, 로딩 실패. [절차](Common-Changes.md#strings) |
| <a id="a6"></a>**A6. 캐릭터 렌더러·분신 표현** | 4지점: 캐릭터 프리팹의 authored 활성 상태, `Scripts/Runtime/MiniGames/EchoRoom/Unity/EchoWorldView.cs`의 표시/틴트, 재시작/정리, 모든 선택 스킨 테스트 대상 | `EchoPlayTests.UnspawnedGhostsHideEveryRendererIncludingSkinnedBodies`, `RestartHidesRecordedSkinnedGhostsAgain`, `EverySelectedSkinHidesUnusedGhostsAndKeepsTheirBodyWithTheirHead`가 기존 회귀를 검사한다. 새 렌더러 유형/새 스킨의 포함 여부는 재확인한다. | Sprite만 숨기고 메시 몸통이 남거나 공유 재질 색상 오염. [사례](Pitfalls.md) |

## 기존 장치를 재사용할 곳

- Resources 등록 누락은 `BootResourceSetup.ValidateInventory`와 Play/Build guard가 잡는다. 같은 내용을 새 수동 검사기로 중복 구현하지 않는다.
- UIManager의 명령 큐와 PlayerProfile.Commit은 수명/저장 순서를 한곳에서 관리한다. 새 View에서 별도 큐·선저장 상태를 구현하지 않는다.
- 다중 사본·설정 위치는 [Pitfalls](Pitfalls.md#copies), 구체적인 검사 실행 방법은 [Verification](Verification.md)이 원본이다.
