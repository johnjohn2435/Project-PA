# Block Gravity 플레이 화면 연결

2026-11-04 사용자가 후속 4번 작업을 승인했다. SpriteRenderer 보드·트레이·드래그·프리뷰와 uGUI HUD를 새 UIManager 프레임/FIFO 규칙에 연결했다.

## 현재 상태

- 구현 완료: 보드·블록 풀, 트레이 3슬롯, 유효/무효 프리뷰, 마우스·단일 터치, 추가 손가락 무시, 취소·무효 드롭 복귀, 중력 이동·삭제·방향 변경 연출, 점수·최고 점수·현재/다음 중력·남은 횟수 HUD.
- 일시정지 팝업/ESC, 포커스 상실 시 드래그 취소 및 수동 복귀, 모달·숨겨진 HUD에서 플레이 입력/연출 차단, 씬 종료 시 월드 반환·구독 해제.
- 최고 기록은 기존 저장값을 읽고 현재 화면에서는 갱신한다. 다시 하기 시 현재 씬의 최고 기록도 유지한다. 코인 지급·새 기록 영구 저장·완성된 결과 화면은 다음 5번 작업이다.
- 실제 원본 GameScene·프리팹 생성은 사용자 에디터 메뉴 실행이 필요하다. 원본 씬을 직접 수정하지 않는 것은 최초 명세의 Unity MCP 미연결 규칙에 따른다.
- 현재 리소스는 에디터에서 만드는 흰 단색 셀과 색상 틴트다. 최종 ComfyUI 아트는 아직 적용하지 않았다.

## Unity에서 적용

1. Play Mode를 종료하고 컴파일을 기다린다.
2. UI Frames 구조를 아직 적용하지 않았다면 `Pocket Arcade > UI Frames > 1. Migrate Prefabs And Hosts`를 먼저 실행한다. 이번에 해당 메뉴의 Canvas GetComponent null 처리 오류도 최소 수정했다.
3. `Pocket Arcade > Localization > 1. Apply String Table And Korean Font`로 새 문구와 한글 폰트를 준비한다.
4. `Pocket Arcade > Block Gravity > 1. Create Playfield And Connect GameScene`을 실행한다.
5. `Pocket Arcade > Block Gravity > 2. Validate Playfield`를 실행한다.
6. BootScene부터 Play하고 Block Gravity를 선택한다. 이미 리소스 사전 로딩이 끝난 Play 세션에서 씬만 재시작하지 않는다.

생성 메뉴는 GameScene과 BootResources.json을 `Client/Logs/BlockGravityBackup/<timestamp>/`에 백업한다. 새 게임 프리팹을 만들고 GameScene UISceneHost의 screenPath/rootKey를 바꾸며 기존 MainCamera를 비활성화한다. 새 월드 프리팹의 카메라가 렌더링한다. 기존 게임 프리팹이 있으면 덮어쓰지 않고 검증한다. 부트 목록은 기존 선택 슬롯·필수 여부를 유지하며 Resources 목록을 동기화한다.

이후 UI Frames 마이그레이션을 다시 실행하면 GameScene rootKey가 기존 GameEntry로 바뀔 수 있으므로, 마지막에 Block Gravity 1번 메뉴를 다시 실행한다.

## 코드와 수명

- `PocketArcade.BlockGravity.Unity`: 기존 순수 규칙 어셈블리와 Runtime/UIManager를 참조하는 새 표시 어셈블리. 기존 Runtime asmdef나 UIManager를 수정하지 않았다.
- `BlockGravityHud : UIPanel`: UI 표시·버튼·팝업 요청. OnOpen에서 ResourceManager의 부트 캐시로 월드 프리팹을 생성하고 OnClose에서 반환한다.
- `BlockGravityPausePopup : UIPopup`: UIManager가 열기/닫기/Back/캐시 수명을 관리한다. 런타임 UI를 new GameObject/AddComponent로 조립하지 않는다.
- `BlockGravityPresenter`: 세션과 View 사이 연결, TurnFrame 순서 재생, 입력 잠금, 표시용 점수·방향·카운트, 가시성/일시정지/포커스 상태 관리. 연출이 끝나야 CompletePresentation을 호출한다.
- `PieceDragController`: Input System의 Mouse/Touchscreen을 읽고 하나의 포인터만 캡처한다. UI GraphicRaycaster 결과를 먼저 검사한다. 터치 취소, 장치 소실, 포커스 상실, 화면 재배치, 모달 진입 시 드래그를 취소한다.
- `BlockGravityBoardView`: 미리 만든 SpriteRenderer 배열을 재사용한다. 블록 ID에 따라 위치를 보존하고 중력 이동과 삭제를 표현한다. 레이아웃 변경 시 카메라와 트레이 위치를 재계산한다.
- `BlockGravitySetup`: 에디터 전용 프리팹·씬·부트 구성 메뉴. 원본 리소스 이미지를 런타임에 로드하는 fallback은 없다.
- 새 표시 문구와 진단은 StringTable.json의 bg.* 25개 키, 한국어/영어로 관리한다. UI와 게임 규칙에 문장 리터럴을 추가하지 않았다.

## 검증

- 원본 Assets/Packages/ProjectSettings를 복사한 별도 Unity 6000.3.16f1 프로젝트에서 전체 컴파일 및 생성 메뉴 실행을 확인했다.
- 최종 Edit Mode: 196/196 통과. 기존 187개 + 신규 표시/프리팹/씬·부트 검사 9개.
- Play Mode: 9/9 통과. 실제 Update 루프의 가상 마우스 입력·멀티터치 취소, 드래그 배치, 무효/취소, 팝업 일시정지, 포커스 복귀, UI Raycast 차단, 5턴 중력 변경, 재시작·호스트 해제 정리를 확인했다.
- 테스트 환경에서만 배치 실행의 GameView 입력 포커스 제한을 완화한 후 복원한다. 앱의 실제 입력 설정은 변경하지 않는다.
- 한국어 HUD·보드·남은 트레이를 1080×1920 GPU 렌더링 이미지로 확인했다. 이는 복사본의 임시 셀 리소스 화면이며 실제 Android 캡처가 아니다.
- 최초 복사본 생성 메뉴 실행은 빈 SceneManagerSetup 및 기존 UI Frames의 Canvas null 처리로 중단됐다. 검증 시작 씬을 열고 Canvas 참조 처리를 수정한 후 메뉴 실행에 성공했다.
- 첫 UI Raycast 테스트는 Canvas 등록이 끝나기 전에 입력했고, 처음 가상 입력 테스트는 배치 GameView 포커스 제한으로 실패했다. 테스트 환경의 준비 시점·포커스를 보완한 뒤 위 최종 결과를 얻었다.
- 원본 씬 적용·사용자 Play 확인, Android 빌드·기기 터치는 아직 수행하지 않았다. 초기 패키지 임포트에서는 기존 Collab Proxy 패키지의 CS0618 경고가 있었으며 새 프로젝트 코드의 컴파일 오류는 없었다.

## 생성 에셋

- `Resources/MiniGames/BlockGravity/Cell.png`
- `Resources/MiniGames/BlockGravity/Playfield.prefab`
- `Resources/UI/Screens/BlockGravity.prefab`
- `Resources/UI/Overlays/BlockGravityPause.prefab`

검증 로그는 Client/Logs/blockgravity-presentation-editmode.log, blockgravity-playmode.log 및 Logs/Tests의 대응 XML이다. 사람용 안내는 [플레이 화면 적용 안내](../Human/BlockGravity_Play_Guide.html)다. 앞선 화면·규칙 문서의 미연결 상태는 이번 문서의 범위에서 대체한다. 기존 동시 줄 제거 규칙의 다단 연쇄 제약은 그대로다.

## 2026-11-04 후속 결과 저장 완료

위의 결과창·영구 저장 미구현 및 원본 적용 대기 표기는 [BlockGravity_Results_Status.md](BlockGravity_Results_Status.md)의 최신 범위로 대체한다. 결과창·저장·재시도·중복 지급 방지 구현과 원본 Unity 적용을 완료했다.
