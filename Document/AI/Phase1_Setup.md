# Phase 1 적용과 확인

> 최신 UI 구조/프리팹 적용 및 검증 상태: [UI_Foundation_Status.md](UI_Foundation_Status.md). 아래 이전 단계 기록보다 최신 내용을 우선한다.

실제 저장소: `D:\Project PA`  
Unity 프로젝트: `D:\Project PA\Client`

Phase 1 코드, Edit Mode 테스트, 씬/데이터 생성용 에디터 메뉴를 준비했다. 씬과 ScriptableObject 에셋은 아래 메뉴를 실행할 때 생성된다. 현재 작업에서 씬 생성 메뉴, Play Mode, Android 빌드를 실행하지 않았다.

## Unity Editor 적용 순서

1. Unity `6000.3.16f1`에서 `Client` 프로젝트를 연다. 이 버전으로 코드를 검증했으며, 명세 기준 버전으로의 업그레이드는 별도 미완료 항목이다.
2. `Window > TextMeshPro > Import TMP Essential Resources`로 TMP 기본 리소스를 가져온다. 이미 가져왔다면 생략한다. 새 패키지를 설치하는 단계는 아니다.
3. Phase 0 적용을 하지 않았다면 `Pocket Arcade > Phase 0 > 1. Apply Foundation`을 실행한다. Input System 변경 후 재시작 안내가 나오면 먼저 재시작한다.
4. `Pocket Arcade > Phase 0 > 2. Switch To Android`, `3. Validate Foundation`을 실행한다.
5. `Pocket Arcade > Phase 1 > 1. Create Hub And Data`를 실행한다.
6. `Pocket Arcade > Phase 1 > 2. Validate Hub`를 실행한다.
7. `Assets/_Project/Scenes/BootScene.unity`를 열고 Play 한다.

씬 생성/Play Mode를 자동 실행하지 않은 근거는 [프로젝트 명세](MiniGameHeaven_Unity_MVP_Spec.md) 0절의 다음 지시다.

> MCP가 연결되지 않았거나 작업 도중 실패하면 임의의 우회 자동화를 만들지 말고, C#과 에디터 스크립트까지 준비한 뒤 내가 Unity Editor에서 수행할 정확한 절차를 알려줘.

## 메뉴가 생성하는 것

- 캐릭터 정의 3개: char_momo, char_bori, char_nabi.
- 미니게임 정의 3개: block_gravity, coming_soon_1, coming_soon_2.
- 위 정의를 참조하는 GameCatalog.
- BootScene의 초기화 컴포넌트와 로딩 문구.
- SelectScene의 허브, 캐릭터 카드, 게임 카드, 설정 패널과 항상 접근 가능한 계정 연동 버튼.
- GameScene의 진입 안내와 허브 복귀 버튼.

기존 에셋은 덮어쓰지 않으며, 같은 씬에 해당 Phase 1 컴포넌트가 있으면 다시 만들지 않는다. 따라서 재실행해도 카드를 중복 생성하지 않는다. 기존에 만들어진 화면을 변경하려면 Inspector에서 편집한다. 설정 메뉴 재실행을 화면 재생성 기능으로 사용하지 않는다.

Momo/Bori/Nabi는 각각 모모/보리/나비다. 기본 TMP 글꼴로 즉시 표시할 수 있도록 임시 UI는 영문으로 작성했다. 한국어 UI 적용 시 재배포 가능한 한글 글꼴의 TMP 에셋과 텍스트를 함께 바꾼다.

## 수동 스모크 체크리스트

| 동작 | 기대 결과 |
| --- | --- |
| BootScene Play | 로그인 요구 없이 허브 진입 |
| 캐릭터 이미지 또는 Characters 클릭 | 캐릭터 카드 3개 표시 |
| Bori/Nabi 선택 | 즉시 허브 복귀, 이름/이미지 갱신 |
| Play 종료 후 BootScene 재실행 | 선택 캐릭터 유지 |
| Settings에서 Music/Sound effects/Vibration 변경 | 토글 값 저장, 재실행해도 유지 |
| Choose a game → Block Gravity | GameScene 진입, 선택 캐릭터 이름 표시 |
| GameScene → Back to hub | 허브 복귀 |
| Coming Soon 카드 두 개 클릭 | 준비 중 안내, 씬 전환 없음 |
| 각 패널에서 Link Google account 클릭 | 준비 중 안내, Guest 상태 유지 |
| 16:9 / 19.5:9 / 20:9 Game View | 버튼과 문구가 잘리지 않는지 확인 |
| 각 동작 후 Console | 새 오류가 없는지 확인 |

Google 실제 로그인, 실제 음원 배치·기본 BGM 자동 시작, 진동 출력, Block Gravity 보드 플레이는 이번 단계에 포함되지 않는다. SoundManager의 재생 API와 저장 설정에 따른 음소거 반영은 구현했다. 실제 청취는 별도 확인이 필요하다.

## 원화 교체

`Assets/_Project/ScriptableObjects/Characters/`의 각 CharacterDefinition에서 `Portrait`를 바꾼다. 더미 이미지는 ComfyUI 작업에서 생성한 `Art/Characters/Dummy/{ID}_dummy.png`가 존재하면 초기 설정 시 자동 연결한다. 파일이 없으면 캐릭터 대표 색 사각형이 표시된다.

최종 원화 파일명이나 크기를 코드에 넣을 필요는 없다. 설정 메뉴를 실행하기 전에 더미가 없었거나, 생성 이후 원화를 교체하는 경우 Inspector에서 Portrait를 지정한다.

## 저장 위치와 복구

- 실제 경로: `Application.persistentDataPath/player-save.json`.
- Windows 경로는 Unity의 Company Name/Product Name에 따라 달라진다.
- 기존 파일 교체 시 직전 저장을 `.bak`으로 보관한다.
- 쓰기는 같은 폴더의 `.tmp` 파일을 완전히 기록한 뒤 교체한다.
- 손상된 본 파일은 백업으로 복구하고, 둘 다 읽을 수 없으면 기본값으로 시작한다.
- 알 수 없는 미래 버전 저장 파일은 덮어쓰지 않고 시작 화면에 오류를 표시한다.
- 파일 쓰기에 실패하면 이전 선택/설정을 유지하고 재시도 안내를 보여준다.

## 확인 결과

실행한 테스트와 남은 검증은 [Phase 1 작업 결과](Phase1_Status.md)에 기록한다.

## 싱글톤 매니저 적용 (2026-09-18)

- 이미 Phase 1 씬을 생성했다면 Create Hub And Data를 다시 실행한다. 기존 GameBootstrap 루트에 DataManager, SoundManager, SceneFlowManager, UIManager를 보충하며 기존 UI와 정의 에셋은 보존한다.
- Validate Hub는 네 매니저가 Bootstrap과 같은 루트에 각각 하나씩 있는지도 검사한다.
- Game Services의 SoundManager에서 Ui Click Clip을 선택적으로 지정한다. 미지정 시 무음이다. BGM은 PlayBgm 호출에 클립을 넘겨야 하며 기본 BGM은 아직 연결하지 않았다.
- Play 후 허브 → 게임 → 허브를 이동해도 Game Services가 하나만 남는지 확인한다.
- 클립 연결 후 설정의 Music/Sound effects 변경과 재실행 시 설정 유지, 실제 출력 여부를 확인한다.