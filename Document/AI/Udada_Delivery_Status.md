# 이전 실험 보관

- 현재 허브 두 번째 게임은 tilt_floor다. 아래 배달 자산/이력은 보존하며 현재 활성 기능을 뜻하지 않는다. 최신 Tilt_Floor_Status.md 참고.

# 우다다 배달 최신: 급정거 배달 실험

- 사용자 승인: 장애물 회피가 익숙한 게임처럼 느껴진다는 피드백 후 급정거로 관성 택배를 현관에 보내는 실험 진행. 집3채, 네모 유지. 최신 규칙이 아래 장애물 코스 이력보다 우선한다.
- DeliverySession 재작성. 자동 오른쪽 달리기, 누름 가장자리에서 속도>=2.5면 1회 발사, 유지/비행 중 재발사 없음. 떼면 재가속. 택배는 발사 속도/감속을 고정 캡처하여 플레이어 재이동과 독립. 예측 착지점은 실제 감속 공식과 동일. 시각적 포물선은 표현만 담당.
- 초기 규칙:45초, 속도4, 가속12, 제동8, 택배 감속2, 첫 집9/간격14/집3채. 현관±1m/Perfect±0.3m. 성공150/정확300, 연속2번째+50/3번째+100. 모두정확1050/코인10. 적재1~3에 따라 제동/택배감속 감소, 실패시적재1. 과거 기회3/장애물/배달선 통과 점수 규칙 제거.
- House/Parcel/Player 및 교체 Playfield는 Editor API로 만든 프리팹. 모든 신규 자산 부트 등록, 캐시 공급. UI는 기존 프리팹/명령 큐 사용. 표시 문구 전부ko/en 테이블, 현재153행.
- DeliveryHouseView 색상 판정, DeliveryWorldView 네모/상자스택/택배/예상착지 표시/카메라, HUD/팝업 결과 연결. 하우스 배치/허용범위 설정은 DeliveryTuning.asset; 변경시구성기로프리팹일치확인.
- 선택ID udada_delivery 유지, ScoreKey udada_brake.v1로 이전 점수와 분리. MiniGameDefinition.ScoreKey는 미지정시Id. DataManager가 등록ScoreKey만허용. 이전 점수/코인/receipt를 삭제하지 않음. 새판고유ID/원자적 저장/실패재시도 유지.
- DeliverySetup.ApplyBrakePrototype 원본 적용, world/tuning/definition 백업 Logs/UdadaDeliveryBackup. 기본 전체 구성 메뉴도 새코스/ScoreKey유지. 수동메뉴작업 남지않음.
- 전체Edit262/262, Play29/29, skipped0. 원본Runtime/Tests와 검증복사본 SHA256일치. 최종 Editor 구성기에 기록키 대입/검사 보완 후 원본 컴파일/Validate 통과.
- 실제 Boot→Game 진입, PointerDown 발사, Flight0.4 프레임, 첫 Perfect300/적재2, 한영일시정지 검수. 화면 Logs/Screenshots/brake-ready.png, brake-flight.png, brake-perfect.png, brake-pause-en.png. 첫집까지만검수하고 실제결과/코인저장은 하지않음. 한국어/runInBackground=false복원/Play종료.
- 보고서 Logs/Tests/brake-edit.xml, brake-play.xml. Android 새빌드/기기/사용자재미검수는 별도. 아트/리소스세션 승인파일 미변경.
- 사람용 ../Human/Udada_Delivery_Prototype.html.

---
## 이전 장애물 실험 이력 — 현재 규칙 아님

# 우다다 배달 네모 프로토타입 — 2026-11-05

- 사용자 승인: 두 번째 미니게임 우다다 배달 진행. 리소스/디자인 세션의 최종 결정 전이므로 캐릭터 대신 네모만 사용. 기존 아트와 승인 파일을 변경하지 않는다.
- ID udada_delivery. 기존 허브 두 번째 coming_soon_1 슬롯을 새 MiniGameDefinition으로 교체, 세 번째 잠금 카드 유지. 씬은 기존 Boot/Select/Game 3개.
- UISceneHost.gameScreens에 게임 ID→화면 경로 추가, 기본 BlockGravity 경로는 유지. MiniGame 모듈 간 의존 없음.
- 90초/기회3, 자동 전진, 누름/Space 감속, 떼면 가속. 3종 움직이는 장애물, 길18마다 배달. 기본150점/5.5초 이내250점, 충돌 기회1/점수50 차감 및 회복. 코인 floor(score/100), 최대100. 수치는 프로토타입이며 밸런스 확정 아님.
- DeliverySession은 Unity 비의존/1/120초 고정 스텝. DeliveryTuning ScriptableObject, DeliveryWorldView, DeliveryBrakeInput, DeliveryHud, DeliveryDialog 분리.
- Resources/MiniGames/UdadaDelivery/Player.prefab은 네모 외형, Playfield.prefab에 중첩. 선택 캐릭터 외형은 읽지 않음. 후속 아트 적용은 외형과 표현부 참조 교체이며 규칙 충돌 크기와 분리.
- Resources/UI/Screens/UdadaDelivery.prefab, UI/Overlays/UdadaDeliveryDialog.prefab. 모든 UI는 에디터 제작, UIManager FIFO/캐시/생명주기 사용. 비활성 생성/등록 후 OnOpen. 런타임 UI 조립/창 직접 SetActive 없음.
- 새 Resources 전체 부트 필수 등록, 이후 캐시만 사용. Tuning은 HUD 직렬화 의존성. AudioCue Confirm/Cancel/GameOver 및 기존 BGM 사용, 실제 신규 음원 미제공.
- AudioSettingIcon을 Runtime/UI로 공통화. 기존 meta GUID 보존, MovedFrom 이전 namespace/assembly 지정. BG 및 새 게임 소리 아이콘 공유.
- 종료 시 고유 result ID와 DataManager.RecordGameResult 기존 원자적 저장/지급 이력. 실패 시 결과 유지, Back 소비, 재시도 성공 전 재시작/허브 보상 유실 방지. 진행 중 허브로 나가면 판 포기/무보상.
- 포커스/백그라운드 이탈 시 팝업 정지, 복귀 후 수동 재개. 브레이크 포인터 소유권 및 입력 정리. Start 뒤 버튼 그래픽 raycast를 끄고 아래 입력 영역으로 통과.
- 새 문자열19개 ko/en 등록, 모든 표시 문구 L10n/LocalizedText/ILocalizedDynamicText. 전체 테이블144행.
- Unity Editor API DeliverySetup.Apply 원본 적용, 백업 Logs/UdadaDeliveryBackup. 추가 수동 메뉴 작업 없음.
- 전체 Edit260/260, Play28/28, skipped0. 원본 Scripts/Tests 파일 해시가 검증 복사본과 일치. 신규 Edit19, Play5. 테스트는 메모리 저장소이며 원본 사용자 결과/코인에 쓰지 않음.
- 실제 원본 Boot→허브→새 게임/네모 주행/한영 일시정지→허브 복귀와 기존 게임 진입 확인. 화면 Logs/Screenshots/udada-ready.png, udada-pause-en.png. 테스트 Logs/Tests/udada-edit.xml, udada-play.xml.
- 이번 변경 Android 빌드/실기기 미실행. 기존 PocketArcade-20261105.apk는 이전 빌드이므로 새 게임 미포함. 디자인/음원/재미·밸런스 후속.
- 사람용 ../Human/Udada_Delivery_Prototype.html.

## 가로 진행 변경

- 사용자 결정: 왼쪽에서 오른쪽으로 달리기. 세로 화면 방향은 유지한다.
- DeliveryWorldView만 forward/lateral을 world X/Y로 매핑. 캐릭터 화면 왼쪽1/4, 오른쪽 코스 미리 보기, 장애물은 상하 이동, 배달선은 수직. 도로 프리팹을 Unity Editor API로 가로 길로 변환. 게임 규칙·점수·저장·감속 조작은 유지.
- DeliverySetup.ApplyHorizontalCourse 원본 적용 및 전체 구성 재실행 시 가로 길 유지. 안내 한/영 문구와 사람용 HTML/화면 갱신.
- StringTableSetup에서 이미 모든 글리프가 있을 때 TMP.TryAddCharacters가 false를 반환하는 재적용 문제 발견/수정. 없는 문자만 요청하여 반복 적용 가능. 회귀 테스트 추가.
- 전체 Edit260/260, Play28/28, skipped0. 원본 Scripts/Tests와 검증 복사본 해시 일치. 실제 가로 준비/주행/한영 일시정지 확인, 한국어·runInBackground=false로 복원, BootScene 편집 모드로 종료. Android 재빌드 미실행.
