# 최신 접촉 규칙 —2026-11-06

- 사용자 피드백으로 원판 착지 적 접촉은 즉시 Hit 사망. 기존 접촉 밀침/쿨다운 제거. 추락 Fell과 구분한 ko/en 사망 안내. 시간/점수/캐릭터 정지, 재시작 시 복원.
- 예고/공중 적은 착지 전까지 비치명적. 노란 아이템은 기존 점수 획득.
- 관련 Edit20/20, Play5/5 통과. 접촉 사망·점수 고정·문구·Animator정지/재시작 포함. 보고서 Logs/Tests/contact-edit.xml,contact-play.xml. 아래 전체 검증 수치는 이전 이력.
- 분신은 사용자 답변대로 길막 유지, 중앙 안전구역 유지. Echo코드 변경 없음.

# 최신 크기 조정 — 2026-11-06

- 사용자 요청: 판을 더 크게. 반지름3.2→4(+25%), 면적+56.25%. 런타임 기본값/실제FloorTuning.asset 모두4로 갱신.
- FloorWorldView 카메라는 반지름을 고려해 여백 확보, 판은 화면에서도 확대. 그림자Scale1.25, 재구성기에서도반지름에맞춰그림자유지. 스폰/아이템/경계는이미반지름사용하므로일치.
- 관련기존Edit19/19, Play4/4 통과. 원본실제반지름4/준비화면/기울기구도확인. 전체281/34 재실행은안했으며아래는이전전체검증이력.
- 보고서 Logs/Tests/disc-size-edit.xml, disc-size-play.xml. 화면 disc-large-ready.png, disc-large-tilted.png. BootScene편집모드/runInBackground=false복원. Android미실행.

# 최신: 원판 생존전

- 사용자 확정: 원판 위에 적이 하늘에서 낙하, 내 위치/무게로 기울여 적을 낙사시키는 생존전. 난이도를 계속 올리고 시간 점수 및 주기적 아이템 보너스 추가. 이전6개수집완료/자동뒤집힘실패보다 우선.
- ID tilt_floor 유지, 선택 캐릭터/공용IdleWalk 연결 보존. 기존 사용자 저장 데이터·캐릭터 원본 미변경.
- FloorSession: 원형Radius4, Player radius+0.12밖이면Fell. 초반예고1초→낙하0.5초→추격→반경+0.15밖Ejected→0.65초뒤슬롯재사용. 적경사힘이플레이어보다강함, 접촉밀침쿨다운0.6초. 기울기만으로종료하지않음.
- 초당10점+아이템250점. 첫아이템6초/이후7초/수명9초. 현재판점수만계산, 영구최고점/코인지급미연결. 종료/정지시점수불변.
- 투하간격 max(0.18,3.2/(1+time/14)), batch min(12,1+floor(time/20)), 추격속도0.65+min(2.5,time*0.009). 일부낙하가플레이어예상이동위치를노림. 예고는max(0.28,1/(1+t/50)), 낙하는max(0.22,0.5/(1+t/100))까지단축하며개별생성시길이캡처. 최대적64+아이템3슬롯고정. Body/DropPhase 스냅샷으로뷰분리, 고정1/120초/결정적시드.
- FloorWorldView 원판64분할메시/테두리/원형클립격자, 예고/하늘낙하/낙사연출. 기존캐릭터Attach/Release/Paused보존, 균형작은root기울임, 재시작시애니메이터정지복원. 큰경사에서도UI와안겹치게카메라/투영보정.
- FloorSetup.ApplySurvival 원본UnityAPI실행. 이전코드및world/tuning백업 Logs/TiltFloorBackup. Prefab/UI참조·부트목록검증. 리소스는전부부트캐시/직렬화의존성. 추가수동메뉴없음.
- 전체Edit281/280, Play34/34, skipped0. 최종Scripts/Tests와검증복사본SHA256일치. 보고서 Logs/Tests/survival-edit.xml, survival-play.xml.
- 원본실제예고/낙하/적2낙사/아이템1개336점(86+250)확인. 단순원형이동시뮬레이션이300초까지생존해후반예고단축/이동경로예측투하보강. 보강후같은입력229.8초에서Fell확인(재미/밸런스평가아님). 한국어/runInBackground=false/BootScene편집모드복원. Android재빌드/실기기미실행.
- 문자열ko/en현재174행. 사람용 ../Human/Tilt_Floor_Experiment.html.

---
## 이전 실험 기록 — 현재 생존 규칙 아님

# 흔들리는 바닥 실험 — 최신 두 번째 게임

- 사용자 승인: 배달 실험이 단조롭다는 피드백 후 네모 이동으로 판이 기울고 물건이 쏠리는 작은 실험 진행. 이동·기울기·쏠림 우선, 점수/보상 저장 미구현.
- ID tilt_floor. 허브 두 번째 슬롯을 새 정의로 교체, 첫 BlockGravity/세 번째 잠금 유지. 기존 배달 자산/저장 데이터 보존. 3씬 구조 및 UISceneHost.gameScreens 재사용.
- FloorSession: 고정1/120초, 두 축 이동, 플레이어/물건 무게 torque, 스프링+감쇠 Tilt, 낮은쪽 가속, 물건 테두리 반사, 상자 접촉 밀침, 노란6개 수집. 플레이어 열린 가장자리 추락 또는28도 이상0.9초 실패. 시간/코인/최고점 저장 없음.
- FloorInput: 상대 드래그/키보드WASD+화살표, 첫포인터 소유권, Pause/Focus에서입력정리. FloorHud/FloorMenu는 UIManager의내부생명주기/FIFO/캐시 사용. UI.SetActive/런타임 UI조립없음.
- FloorWorldView: 규칙XY를2.5D투영, 메시정점/격자/테두리/받침/그림자/기울기화살표. Renderer표현이규칙판정에영향없음. 메시인스턴스OnDestroy정리. 안내와 겹친 화살표 위치 보정.
- FloorTuning.asset 분리. Resources/MiniGames/TiltFloor/Playfield, UI/Screens/TiltFloor, UI/Overlays/TiltFloorMenu. 새자산은Boot전체등록,Mesh/Material직렬화 의존성. 런타임 추가IO없음.
- 표시/오류/구성문구16행ko/en추가, 전체169행. FloorSetup.Apply 원본에실행/백업 Logs/TiltFloorBackup. 추가수동메뉴없음.
- 전체Edit277/277, Play33/33, skipped0. 최종 Scripts/Tests와 검증복사본SHA256일치. 실제 드래그 기울기/목표1개수집확인. 별도 입력제어시뮬레이션6개Cleared확인(재미판단아님). 한국어/runInBackground=false/BootScene편집모드복원.
- 보고서 Logs/Tests/floor-edit.xml, floor-play.xml. 화면 Logs/Screenshots/floor-ready.png, floor-tilted.png. Android재빌드/실기기/사용자재미평가미실행.
- 사람용 ../Human/Tilt_Floor_Experiment.html.
