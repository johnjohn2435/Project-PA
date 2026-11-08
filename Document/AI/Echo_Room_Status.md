# 최신 플레이 QA: 미등장 분신의 스킨 메시 잔상 수정 —2026-11-06

- 사용자 제보: 시작 중앙에 목 없는 몸 남음. 원본 이동 화면에서 재현: 미등장 분신5개의 TorsoSurface/arm_LSurface/arm_RSurface가 표시되어 총15개 SkinnedMeshRenderer가 중앙에 겹침. 머리SpriteRenderer만숨긴코드원인.
- EchoWorldView의 parts를Renderer[][]로확장. authoredVisibility는모든Renderer기준으로보존/활성전환. SpriteRenderer.color, 메시에는material슬롯별MaterialPropertyBlock으로틴트/alpha0.48. 공유원본재질미변경.
- 관련 Play7/7 통과. 시작/분신생성/재시작 모든메시비활성,활성분신메시틴트,원본재질alpha보존,3스킨이동,기존4통합흐름. 런타임/테스트해시검증복사본일치. 전체Edit300/Play38은이전전체회귀이력.
- 원본실제드래그이동전후·분신1명생성·재시작·원판이동확인. 한국어/BootScene편집모드/runInBackground=false복원. Android실기기미실행.
- 화면Logs/Screenshots/ghost-body-before.png,ghost-body-after.png,ghost-mesh-active.png,floor-body-move.png. 보고서Logs/Tests/ghost-mesh-play.xml.
- 사람용 ../Human/Character_Render_Play_QA.html.

# 접촉 규칙 확정 —2026-11-06

- 사용자가 분신은 지금처럼 길만 막기로 선택. 분신 접촉으로 사망하지 않으며 중앙 안전구역 겹침도 유지. 코드변경없음.

# 세 번째 게임:10초 전의 나 —2026-11-06

- 사용자 승인: 혼자 반복해도 상황이 누적되는 시간 분신 운반 게임 구현. ID echo_room, 허브 세 번째 기존잠금슬롯교체. BlockGravity/TiltFloor유지. 기존3씬/UISceneHost경로재사용.
- 기본10초×6구간/최대5분신/목표18이상. 매구간 플레이어·분신시작위치/물건6개/운반상태/문초기화, 회수누적유지. 들고만있던물건미가산. 위치1/120초기록(구간당1201점), 과거경로불변. 현재물건공유, 현재플레이어먼저검사하여중복회수방지.
- 분신도물건을집고중앙에운반하며발판을누름. 현재플레이어만분신과접촉시밀림, 분신경로는변하지않음. 중앙안전구역겹침허용. 같은색발판→문4초유지. 좌우문과벽/방경계판정.
- EchoSession/Rules/Tuning와Unity입력·뷰·HUD·메뉴분리. 선택캐릭터스킨사용, 원래파츠활성보존, 분신만RGB/Alpha틴트. 캐릭터인스턴스6개사전생성/활성표시,종료시모두ResourceManager반환. 경로LineRenderer는구간변경시만갱신, HUD텍스트10Hz.
- 리소스MiniGames/EchoRoom/Playfield, UI/Screens/EchoRoom, UI/Overlays/EchoRoomMenu. 부트필수등록, 런타임캐시만사용. UI는Prefab만. EditorEchoSetup.Apply 원본UnityAPI실행, Logs/EchoRoomBackup 백업. 추가수동메뉴없음.
- Ko/en문구15행추가, 현재189행. 코인/영구기록저장없음.
- 첫오른쪽3개기록→다음왼쪽3개기록→분신자동협력으로36.29초19개/분신13개회수Complete. 동일프레임동시납품으로목표초과가능. 도달가능성회귀테스트포함(사용자재미평가아님).
- 전체Edit300/300, Play38/38, skipped0. 새규칙19/통합4. 독립검증경로 D:/Unity/Project PA/.echo-validation/Client 사용, 원본Scripts/Tests와SHA256일치. 기존공용검증폴더동시사용과간섭하지않음.
- 기존Card장식nullable선택참조를OptionalUIReferenceAttribute로명시, UIReferenceRemapperTests는이속성만선택허용. 필수참조검사유지/실제카드표시동작미변경. 기존전체검사4건의불일치해소.
- 원본Boot/새카드/실제드래그운반/기록재생/분신납품/한영Complete/허브복귀확인. 한국어/runInBackground=false/BootScene편집모드복원. Android새빌드/실기기미실행.
- 보고서Logs/Tests/echo-edit.xml,echo-play.xml. 화면echo-ready.png,echo-replay.png,echo-delivery.png,echo-complete.png,echo-complete-en.png.
- 사람용 ../Human/Echo_Room_Prototype.html.
