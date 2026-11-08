# UI·문자열·프리팹 계약

> [색인](README.md) · [구조](../ARCHITECTURE.md)

## 문자열 원본

- 원본은 `Client/Assets/_Project/Resources/Data/StringTable.json`의 `key/ko/en`이다. 새 문구를 두 언어에 등록하고 `L10n.Text/Format`으로 조회한다. 로그·오류도 기존 사용자 결정에 따라 동일하게 처리한다.
- `StringTable.Parse`는 version 1, 중복/빈 행, 양 언어의 포맷 인자를 검증한다. JSON은 직접 읽는 원본이며 존재하지 않는 별도 생성기를 요구하지 않는다.
- 고정 TMP는 `LocalizedText`의 key, 동적 문장은 전체 포맷 키와 인자를 사용한다. `ILocalizedDynamicText`로 동적 라벨을 선언하고 고정 키 바인딩과 충돌시키지 않는다. 경로·ID·키·서식 태그는 문구와 구분한다.
- 캐릭터/게임 이름 키는 `character.{id}.name`, `game.{id}.name`이다. 누락 시 구현이 `[key]`를 반환하더라도 이를 정상 번역/fallback으로 인정하지 않는다.
- 초기 부트 라벨처럼 테이블 로드 전에 보일 텍스트는 `StringTableSetup`을 통해 테이블에서 편집 시 구워 넣는다. 프리팹의 구운 텍스트를 지우거나 별도 문장 리터럴로 대체하지 않는다.
- `Pocket Arcade/Localization/2. Validate String Table`과 `StringTableTests`를 사용한다. `1. Apply String Table And Korean Font`는 프리팹·폰트 바인딩을 변경하므로 기존 언어 키와 현재 Jua/NanumGothic 참조를 확인한다.

## 프레임과 수명

- View는 입력·표시, Controller는 저장/씬 이동 연결, UIManager는 프레임·순서·수명, ResourceManager는 부트 캐시·인스턴스 반환을 맡는다. 영속 매니저에 씬 View를 직렬화하지 않는다.
- `UIBase → UIPanel/UIPopup`, `PanelFrame`, `UIManager.Open/Close/Back/Home`을 따른다. 명령은 FIFO로 순차 await하고 Unity 메인 스레드에서 호출한다. Task는 await 또는 `UIManager.Observe`로 관찰한다.
- `IsOpen`과 `IsVisible`을 구분한다. 이전 프레임은 Hide, 복원 시 Show, 루트 패널 Close는 금지한다. 창 코드가 직접 SetActive/Destroy로 수명을 우회하지 않는다.
- Frame/Managed 또는 Independent/Fixed 조합만 사용한다. Managed sortingOrder는 10단위로 재배정하고 Fixed는 보존한다.
- 비활성 루트에서 생성한 중첩 UIBase를 전부 검증/등록한다. 키 중복/검증 실패 시 생성분 전체를 되돌린다. 중첩 창은 독립 Live 루트로 옮겨지므로 호스트 기준 RectTransform을 사용한다.
- Close는 키별 인스턴스 캐시를 유지하고 Unmount/Shutdown은 씬 캐시와 미완료 요청을 정리한다. Back은 Independent → 최상단 Popup → Panel → 루트 이벤트 순이며 닫히지 않는 오버레이도 입력을 소비한다. `OnBackRequested=true`는 기본 닫기를 막고 소비한다. 입력창 ESC는 포커스만 해제한다.
- 부트 화면은 [부트 계약](Boot-Resources.md)의 별도 수명이다. 일반 화면 코드가 UIManager 등록을 우회하는 근거로 사용하지 않는다.

## 알림·음소거

- 일회성 알림은 `UIManager.ShowToast`, 확인/선택은 `ShowMessageBox`, 대상 설명은 `ShowTooltip`을 사용한다. `ShowNotice`는 활성 View의 지속 안내다. 같은 결과를 Controller와 View에서 중복 발행하지 않는다.
- `UIFeedback`의 메시지/토스트 대기는 명령 큐 밖이다. 사용자의 응답 대기로 Back/Close를 막지 않는다. 메시지박스는 FIFO, 팝업은 프레임/스택을 유지하며 씬 종료 시 요청을 완료/취소한다.
- 게임 중 소리 설정은 일시정지 메뉴의 `PocketArcade.UI.AudioSettingIcon`을 재사용한다. 저장 성공한 실제 상태로 ON/OFF를 표시한다. 저장 실패 시 아이콘만 먼저 뒤집지 않는다.

## 작성·공용 부품

- UI 계층·컴포넌트·레이아웃·스타일·참조는 실제 `.prefab`에 저장한다. 런타임 `new GameObject/AddComponent`로 화면을 조립하거나 누락 참조를 자동 보완하지 않는다. 목록은 완성된 카드 프리팹 복제/재사용으로 확장한다.
- 임시 디자인도 프리팹으로 작성하고 교체할 이미지의 이름·크기·9-slice를 사람용 문서에 남긴다. 편집 자동화는 프리팹을 저장하는 용도로 사용할 수 있다.
- 공용 컴포넌트는 `Client/Assets/_Project/Scripts/Runtime/UI/`, 부품은 아래 표를 우선한다. 원시 Image/TMP/Button은 기존 공용 프리팹 구성에 필요한 경우 사용하며 존재하지 않는 래퍼를 새로 강제하지 않는다.

아래 경로는 `Client/Assets/_Project/` 기준이다.

| 용도 | 기존 부품/자산 | 사용 |
|---|---|---|
| 버튼·토글 | `Resources/UI/Common/Button.prefab`, `Toggle.prefab` | 기존 이벤트·사운드 연결 보존 |
| 캐릭터·게임 카드 | `Resources/UI/Common/CharacterCard.prefab`, `ParkCharacterCard.prefab`, `MiniGameCard.prefab`, `GameMedalCard.prefab` | 현재 화면의 카드 템플릿 확인 |
| 알림·확인·툴팁 | `Resources/UI/Overlays/Toast.prefab`, `MessageBox.prefab`, `Tooltip.prefab` | UIManager API 사용 |
| 배경·종이 스타일 | `Scripts/Runtime/UI/ArcadePaperGraphic.cs`, 기존 Hub/BootLoadingUI 프리팹 | 기존 직렬화 색·패턴 재사용 |
| 음소거 아이콘 | `Resources/UI/Icons/Audio/`, `Scripts/Runtime/UI/AudioSettingIcon.cs` | 저장 상태와 함께 표시 |
| 폰트 | `Fonts/Jua SDF.asset`, `Fonts/NanumGothic SDF.asset` | 현재 화면 참조와 한·영 글리프 유지 |

별도 공용 레드닷·상단바·아이템 아이콘 시스템은 현재 확인되지 않았다. 템플릿 예시를 실제 경로로 간주하지 않는다.

## 현재 화면

- 허브는 `Resources/UI/Screens/Hub.prefab`. 게임은 HubMedals가 카탈로그 순서대로 부트 캐시의 GameMedalCard를 생성하는 좌우 ScrollRect 목록이다. 스와이프/화살표는 탐색만 하며, 원형 배지를 고르고 하단 시작 버튼으로 실행한다. 새 게임의 Thumbnail을 사용하고 기존 그림은 HubMedals의 artwork 설정을 유지한다. 목록 확장 시 홈의 고정 카드 수를 편집하지 않는다. 별도 친구 선택창, 현재 친구 표시, 설정의 계정 연동 안내를 유지한다.
- 산책 공원 코드는 보관 중이며 현재 Hub에 연결하지 않는다. `HubParkSetup` 재실행으로 사용자 결정을 되돌리지 않는다.
- 최신 허브/부트 테마는 [Hub_UI.html](../Document/Human/Hub_UI.html)과 현재 프리팹을 기준으로 한다. 게임 필드는 게임별 Renderer, 조작·정보·메뉴는 Canvas/TMP를 사용한다.
- 선택 장식만 `OptionalUIReferenceAttribute`로 표시한다. 필수 직렬화 참조 누락을 optional로 숨기지 않는다.

근거: `UIManager.cs`, `UIBase.cs`, `UISceneHost.cs`, `UIFeedback.cs`, `HubController.cs`, `StringTable.cs`, `LocalizedText.cs`. 과거 [UI 프레임 기록](../Document/AI/UI_Frames_Status.md)의 결과는 당시 검증이다. 현재 검증은 [Verification](Verification.md)을 따른다.
