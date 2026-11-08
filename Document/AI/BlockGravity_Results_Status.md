# Block Gravity 결과 팝업과 영구 저장

2026-11-04 사용자가 결과 팝업, 최고 기록·코인 저장, 저장 실패 재시도와 중복 지급 방지를 승인했다. 코드·검증·원본 Unity 적용을 완료했다.

## 완료된 동작

- 게임 오버 연출이 끝나면 결과 팝업이 열린다. 최종 점수, 최고 기록, 코인, 저장 상태, 저장 재시도, 다시 하기와 허브 이동을 제공한다.
- 결과 팝업의 최초 열기에서 자동 저장한다. 실패하면 예상 보상만 표시하고 다시 하기·허브 버튼은 비활성화한다. ESC/Back도 결과창을 닫지 않는다.
- 같은 결과 ID로 재시도한다. 성공하면 저장 완료와 실제 지급 코인을 표시하고 이동 버튼을 활성화한다.
- 다시 하기는 새 세션과 새 결과 ID를 만든다. 결과창 재열기, 같은 요청 재제출, 저장 후 재실행으로 같은 코인이 다시 지급되지 않는다.
- 원본 프로젝트에서 Localization 적용, 플레이필드·결과 프리팹 생성, GameScene 연결, 부트 리소스 목록 동기화 및 참조 검증을 MCP로 실행했다. 사용자 수동 메뉴 적용은 현재 작업에 남아 있지 않다.

## 저장 구조와 호환성

- SaveData schemaVersion은 2다. 기존 v0/v1의 캐릭터·설정·코인·기록은 유지하며 결과 이력 기본값을 추가한다. 실제 파일 갱신은 다음 저장 시 수행한다.
- 최고 점수는 long으로 저장하여 게임 점수와 범위를 맞췄다. 기존 int 점수 JSON도 읽는다.
- completedResults에는 resultId, gameId, 점수, 계산된 보상, 실제 지급 코인, 결과 시점의 최고 기록을 보존한다.
- PlayerProfile.RecordGameResult는 기록·코인·지급 이력을 한 SaveData로 복사한 뒤 기존 JsonSaveRepository의 임시 파일→원자 교체 방식으로 저장한다. 디스크 저장이 성공하기 전에는 현재 프로필 상태를 교체하지 않는다.
- 같은 ID와 같은 결과는 저장/지급/Changed 알림 없이 기존 영수증을 반환한다. 같은 ID에 다른 점수·게임·보상을 넣으면 거부한다.
- 지급 이력은 삭제하거나 최근 N개로 잘라내지 않는다. 따라서 오래된 ID 재제출도 막지만, 플레이 횟수에 따라 로컬 저장 파일은 커진다.
- 코인 보상은 기존 규칙 min(100, floor(score/250))이다. 기존 int 코인 잔액 한계에 도달하면 넘치는 코인은 더하지 않고 실제 지급액을 결과창에 표시한다.
- 저장 실패 중 강제 종료된 미저장 판을 복원하는 기능은 포함하지 않는다. 판 중간 저장·강제 종료 복구는 별도 범위다.

## 코드 책임

- GameResultRequest/Receipt: 변경할 수 없는 제출 요청과 직렬화 가능한 지급 기록. 외부에 반환하는 기록과 Snapshot은 복사본이다.
- PlayerProfile: 결과 ID 중복·충돌 검사, 최고 기록과 잔액, 지급 이력의 원자 저장 경계.
- DataManager.RecordGameResult: 등록된 게임인지 검사한 뒤 프로필 저장 API로 연결한다.
- BlockGravityResultController: 동일 요청 보존, 재진입 차단, 성공 결과 캐시, 실패 상태와 재시도. 저장 중 재호출은 추가 저장을 시작하지 않는다.
- BlockGravityPresenter: 세션별 Guid 생성, 게임 오버 후 결과 컨트롤러 생성, 저장 완료된 결과만 재시작하도록 제한한다. 결과 저장 경로는 도메인의 일회성 TryClaimReward를 미리 소비하지 않는다.
- BlockGravityResultPopup: UIManager의 UIPopup/프레임/FIFO/키별 캐시 수명을 사용한다. 프리팹으로 제작하며 표시 문구는 StringTable bg.result.* 키로 관리한다.
- BlockGravitySetup: 기존 생성/검증 메뉴가 결과 프리팹까지 생성·등록·검사한다. 기존 프리팹은 보존하며 새 결과 프리팹을 추가했다.

## 검증과 원본 적용

- Unity 6000.3.16f1 별도 검증 프로젝트에서 Edit Mode 213/213 통과(이전 196 + 저장·변환·중복 지급 14 + 결과 컨트롤러 3).
- Play Mode 12/12 통과(이전 9 + 결과 저장/재열기/새 판, 실패 시 이동 차단과 재시도, 데이터 매니저 준비 전 복구 3).
- JSON 실제 파일 왕복, v1→v2 변환, int 범위 초과 점수, 재실행 후 중복 제출, 저장 실패 시 원본 유지, 코인 상한, 이벤트 재진입·저장 후 구독자 예외를 검사했다.
- 원본 Unity에서는 MCP로 코드 컴파일 완료, Localization, 플레이필드/결과 생성, GameScene·부트 연결 및 Validate를 확인했다.
- BootScene → SelectScene → GameScene 진입, 게임 HUD, 결과 프리팹 사전 로딩을 실제 에디터에서 확인했다.
- 성공/실패 결과 화면은 1250점·5코인 샘플과 메모리 저장 대리자를 사용해 GPU 캡처하고 시각 검사했다. 이 샘플은 실제 게임 결과나 플레이 보상으로 기록하지 않았다.
- 원본 화면 검증 전후 실제 코인 0, 지급 이력 0, 최고 기록 0으로 동일했다. 일시 변경한 runInBackground는 원래 값으로 복원하고 Play Mode를 종료했다.
- 원본 에디터 최종 Console error/warning 0, compilationFailed=false. 검증 복사본 최초 임포트에는 기존 Collab Proxy 패키지의 CS0618 경고가 있었다.
- Unity CLI는 사용자 승인 후 1.0.0-beta.16으로 갱신했다. 콘솔 조회에는 최신 CLI, 씬·프리팹 적용과 화면 확인에는 연결된 Unity MCP를 사용했다.
- Android 빌드·실제 기기 터치는 이번에 수행하지 않았다.

## 플레이 방법과 다음 범위

Unity에서 BootScene부터 Play하고 Block Gravity를 선택한다. 게임 오버 뒤 자동 저장 완료를 확인한 후 다시 하기 또는 허브로 이동한다. 저장 실패 시 저장 재시도를 누른다.

현재 결과·저장 작업은 완료했다. 다음 코드 작업은 사운드·진동 호출 연결, 첫 플레이 안내, Android 검증에서 발견되는 문제 수정이다. 기존 동시 삭제 규칙의 다단 연쇄 제약은 별도 게임 규칙 설계 사항으로 유지한다.

사람용 안내: [결과와 저장 안내](../Human/BlockGravity_Results_Guide.html).
검증 로그: Client/Logs/blockgravity-results-editmode.log, blockgravity-results-playmode.log와 Client/Logs/Tests의 대응 XML.
원본 생성 메뉴 백업: Client/Logs/BlockGravityBackup/. 생성된 결과 프리팹: Client/Assets/_Project/Resources/UI/Overlays/BlockGravityResult.prefab.
