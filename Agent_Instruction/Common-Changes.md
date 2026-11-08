# 반복 작업 절차

> [기능별 색인](README.md) · [구조](../ARCHITECTURE.md) · [동시 수정 체크리스트](Change-Checklist.md)
> 코드·자산 경로는 `Client/Assets/_Project/` 기준이다. 기준일: 2026-11-06, 아래 진입점과 검사 구현을 코드로 대조했다.

같은 종류의 작업을 두 번 이상 했고 순서나 누락 위험이 확인된 경우에만 절차를 남긴다. 한 번뿐인 변경·단일 파일 수정은 추가하지 않는다. 단계가 바뀌면 해당 절차를 갱신하고 상세 계약은 링크한다.
아래는 향후 승인된 작업의 실행 순서다. 이 문서가 기능 추가나 구성 메뉴 일괄 실행을 승인하지 않는다. 테스트 명령과 환경은 [검증 안내](Verification.md)를 따른다.

<a id="strings"></a>
## 문자열·고정/동적 라벨 변경

1. `Resources/Data/StringTable.json`에 `key/ko/en`과 문장 전체의 포맷 인자를 반영한다. [A5](Change-Checklist.md#a5)의 검사 범위를 확인한다.
2. 소비 코드에는 `L10n.Text/Format`, 고정 TMP에는 `LocalizedText.Key`를 연결한다. 동적 라벨은 `ILocalizedDynamicText`로 선언해 고정 키가 값을 덮어쓰지 않게 한다. 본보기는 `Scripts/Runtime/UI/AudioSettingIcon.cs`다.
3. JSON은 직접 편집 원본이라 별도 바이너리 생성은 없다. 초기 부트 문구·편집 시 구운 라벨이 바뀌면 해당 프리팹도 테이블 값에서 갱신한다. `StringTableSetup.Apply`는 여러 프리팹·세 씬·폰트를 바꾸므로 단순 한 문구 수정에 무조건 실행하지 않는다.
4. **검증:** `StringTableTests`, `Pocket Arcade/Localization/2. Validate String Table`. 초기 부트 문구, 한·영 포맷/글리프/줄바꿈을 해당 화면에서 확인한다. 리터럴 키 검사로 동적 조합 키까지 확인됐다고 보고하지 않는다.
5. 제품 의미/사용 절차가 바뀐 경우에만 관련 계약·HTML을 갱신한다. 근거 계약은 [UI](UI.md).

<a id="ui"></a>
## UI 패널·팝업 추가

1. [UI 계약](UI.md)과 [A4](Change-Checklist.md#a4)를 읽고 해당 기능의 화면/팝업을 본보기로 정한다. 공용 버튼·알림 프리팹을 우선한다.
2. `Scripts/Runtime/UI/` 또는 해당 게임의 Unity 표현부에 View를 두고 `UIBase/UIPanel/UIPopup` 수명을 따른다. 에디터에서 직렬화 참조·키·레이아웃이 연결된 프리팹을 저장한다. 런타임 조립으로 누락을 보완하지 않는다.
3. 필요한 문구를 [문자열 절차](#strings)로 등록하고 `Resources/UI/`의 프리팹을 부트 목록에 포함한다. [A5](Change-Checklist.md#a5) 및 `BootResourceSetup.DiscoverResources/ValidateInventory`를 확인한다. Configure 메뉴는 부트 씬까지 변경하므로 실제 쓰기 범위가 맞는지 먼저 확인한다.
4. Controller/버튼에서 `UIManager.Open/Close`로 연결하고 Task를 관찰한다. 게임 최상위 화면이면 `UISceneHost.gameScreens`도 확인한다. 중첩 창 키 충돌과 Unmount 정리를 확인한다.
5. **검증:** `UIManagerTests`, `UIFoundationTests`, 해당 게임 PlayMode 및 `Pocket Arcade/UI Frames/2. Validate Frames`. 정상 부트 후 열기→뒤로→재열기→씬 이동, 모달 입력 차단과 번역을 확인한다.
6. 기능 계약/색인과 사용자 안내를 갱신한다. 임시 이미지의 교체 조건·크기·9-slice도 해당 안내에 남긴다.

<a id="character"></a>
## 캐릭터 추가·승인 스킨 교체

1. [캐릭터 계약](Characters-Art.md)에서 승인 원본·게임 적용 자산·ID를 구분한다. 기존 슬롯의 외형 교체는 저장 ID를 유지한다. 신규 기본 해금 캐릭터는 [A1](Change-Checklist.md#a1), 렌더러 변경은 [A6](Change-Checklist.md#a6)를 확인한다.
2. `ScriptableObjects/Characters/`의 정의와 `ScriptableObjects/GameCatalog.asset`을 반영하고 이름 키를 두 언어에 등록한다. 최신 실제 `Resources/Characters/` 프리팹을 본보기로 삼는다.
3. 승인된 파츠/리그/초상화를 해당 `Resources/Characters/`에 연결하고 부트 등록을 확인한다. 과거 `SharedCharacterSetup.Apply`는 일부 파일이 있으면 복사를 생략하므로 기존 프리팹 업데이트 도구라고 가정하지 않는다. [다중 반영 지점](Pitfalls.md#copies)을 확인한다.
4. 신규 기본 해금 정책이면 `SaveData`의 초기 목록과 `Normalize`의 기존 저장 보정, 필요한 ID 호환 경로를 함께 확인한다. 선택·코인·기록을 보존한다. `OnboardingTests.CharacterPrefabsShareOneAnimatorAndBoneLayout`의 대상 목록도 신규 ID를 포함하는지 확인한다.
5. **검증:** `OnboardingTests`, `PlayerProfileTests`, 카탈로그/문자열/부트 검사. 실제 허브 선택→게임 진입→재실행 선택 유지, Idle/Walk/좌우 반전, 시간 분신의 모든 Renderer 표시/숨김을 확인한다. 테스트의 고정 ID 목록이 새 캐릭터를 자동 검증한다고 가정하지 않는다.
6. 해당 기능 계약과 캐릭터 사람용 안내의 승인/적용 상태를 맞춘다. 본보기는 최신 게임 프리팹이며 과거 원화 생성 패키지가 아니다.

<a id="game"></a>
## 미니게임 추가·노출 상태 변경

1. [게임 계약](MiniGames.md)과 [A2](Change-Checklist.md#a2)를 읽는다. 순수 세션/Unity 입력·표현·메뉴/Editor/Tests 분리는 기존 `Scripts/Runtime/MiniGames/EchoRoom/` 등의 asmdef 구조를 참고한다. 게임 ID와 영구 기록/보상 여부를 구분한다.
2. `ScriptableObjects/MiniGames/` 정의와 GameCatalog를 연결하고 게임 이름·안내 키를 등록한다. 허브에 보이게 할지 보관할지 명시한다. 신규 외부 패키지나 서버를 자동 추가하지 않는다.
3. 저장된 게임 화면 프리팹을 `Resources/UI/Screens/`에 만들고 `Scenes/GameScene.unity`의 `UISceneHost.gameScreens`에 ID/경로를 연결한다. 기본 screenPath로 떨어져 다른 게임이 열리지 않는지 확인한다. 새 씬을 실제로 쓰는 경우에만 빌드 씬 목록도 갱신한다.
4. 월드·UI·캐릭터·음원을 부트 목록에 반영한다. 영구 결과가 승인된 경우만 `MiniGameDefinition.ScoreKey`, `DataManager.RecordGameResult`와 [저장 계약](Save-Data.md)을 연결한다. 규칙이 다른 기존 기록 키를 재사용하지 않는다.
5. **검증:** 해당 순수 세션 EditMode + 해당 화면 PlayMode, 카탈로그/부트/문자열 검사. BootScene→게임 배지 선택→시작→정지/재개→재시작→허브 복귀를 확인한다. 결과 저장이 있으면 실패/재시도/중복 지급도 확인한다.
6. ARCHITECTURE·게임 계약·테스트 진입점·사용자 안내를 갱신한다. 노출 해제만 할 때 기존 코드·자산·저장을 자동 삭제하지 않는다.
