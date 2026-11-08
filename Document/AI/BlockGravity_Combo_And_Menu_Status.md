# Block Gravity 콤보·나가기 메뉴 (2026-11-04)

- 사용자 결정: 새로 받는 조각 3개를 고정된 한 세트로 본다. 세트 안에서 한 번이라도 줄을 지우면 콤보 유지, 세 조각을 모두 썼는데 삭제가 없으면 0으로 초기화한다. 최근 3회 배치의 이동 창 방식이 아니다.
- 줄을 지운 배치마다 콤보를 한 번 증가. 동시 여러 줄·같은 배치의 중력 변경 삭제는 추가 콤보 증가 없이 기존 연쇄 배수를 적용한다.
- 삭제 점수 = 기존 기본 삭제 점수 × 같은 턴 연쇄 단계 × 콤보. 배치 점수는 셀당 10점 그대로다. 1줄 삭제는 1콤보 100, 2콤보 200, 3콤보 300점이다.
- 2콤보부터 줄 삭제 연출 시작에 진동 1회와 약한 카메라 셰이킹 1회. hapticsEnabled를 매회 읽으며 Android 시스템 햅틱 설정도 존중한다. Android View.performHapticFeedback(CONFIRM, API30 이상 / VIRTUAL_KEY, 이전 버전), 추가 권한·플러그인 없음. Editor/PC에서는 진동 없음.
- 카메라 셰이킹은 0.20초 감쇠, 강도 최대 0.12월드 유닛. UI Canvas는 고정. 정지·숨김·재시작·화면 재배치·종료 시 원위치 복구, 연출 완료 후 새 입력 전에 복구한다.
- 게임 HUD 버튼은 나가기 1개. 클릭/시스템 뒤로가기/앱 포커스 손실 시 일시정지 메뉴. 메뉴에는 나가기(허브 이동), 계속하기, 설정. 진행 중 허브로 나가면 해당 판을 종료하며 게임오버 보상은 지급하지 않는다.
- 메뉴와 설정은 UIManager가 관리하는 Independent/Fixed UIPopup 프리팹이다. 전체 화면 Shade Image+Button으로 음영 및 뒤쪽 입력 차단. Card는 Shade의 형제이며 raycast를 받아 카드 내부 클릭으로 닫히지 않는다.
- 메뉴 바깥 클릭/계속하기/Back은 닫기 후 게임 재개. 설정 바깥 클릭/뒤로가기는 설정만 닫고 메뉴·일시정지를 유지한다.
- 설정은 배경음·효과음·진동을 기존 DataManager에 즉시 저장. 실패하면 기존 값 유지 및 오류 문구 표시. 신규 표시 문구는 StringTable.json의 ko/en 키로 관리한다.
- 고정 순서: 기존 툴팁 10010, 부트 전용 안내 30000, 게임 나가기 메뉴 31000, 설정 31010. UIManager는 프리팹의 고정 Canvas 순서를 Instantiate 전 보존하고 UIBase가 Show 시 복원한다. Canvas 아래 재부모화로 순서가 0으로 초기화되던 실행 환경 문제를 수정했다.
- 에디터 적용은 Pocket Arcade/Block Gravity/3. Apply Exit Menu. 기존 HUD의 Pause/Home/Restart만 제거하고 Exit를 연결, 기존 Pause 카드 확장, Settings 프리팹 추가. 변경 프리팹을 Logs/BlockGravityBackup에 백업하고 부트 목록을 갱신한다. 런타임 UI 생성 없음.
- 검증: Unity 6000.3.16f1 전체 Edit Mode 222/222, Play Mode 16/16 통과. 실제 CanvasScaler에서 Shade/Card raycast 분리, 중첩 설정의 입력·순서, 설정 저장 실패, 콤보당 진동 1회, 셰이킹·일시정지·HUD 고정을 검증했다. 테스트 저장소는 메모리이며 실제 코인에 영향을 주지 않는다.
- 원본 Unity MCP 적용 및 메뉴/설정/허브 복귀 확인 완료. Android 기기 빌드 및 실제 진동 감각은 미검증이다.
- 로그: Client/Logs/Tests/blockgravity-combo-editmode2.xml, blockgravity-combo-playmode2.xml. 사람용 안내: ../Human/BlockGravity_Combo_And_Menu_Guide.html.
- Android API 근거: https://developer.android.com/develop/ui/views/haptics/haptic-feedback
