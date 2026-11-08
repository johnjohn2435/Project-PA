# 최신:시간 분신 게임

- 현재189행. 분신/공유물건/문/구간 안내ko/en추가. Edit300/Play38통과. 자세한 상태 Echo_Room_Status.md.

# 최신: 원판 생존전

- 현재 174행. 생존/아이템 점수 및 낙하 안내 ko/en 갱신. 전체Edit281/Play34 통과. 자세한 내용 Tilt_Floor_Status.md. 아래 수량은 이전 이력.

# 최신: 급정거 배달 실험

- 현재 153행. 한/영 급정거·관성·착지 판정 안내 포함. 전체Edit262/Play29 통과. 아래 수량은 이전 이력.

# 최신 현황 — 우다다 배달 프로토타입

- 현재 144행. 우다다 배달 표시/오류/구성 문구19행 추가, 한/영 UI 확인 및 전체 Edit259/Play28 통과. 자세한 상태 Udada_Delivery_Status.md. 아래 수량은 당시 이력이다.

> 2026-11-05 최신: 현재 125행, 캐릭터 이름 키는 character.char_cat.name / char_dog.name / char_hamster.name. 튜토리얼 키와 최신 구현은 [Onboarding_Characters_Android_Status.md](Onboarding_Characters_Android_Status.md) 참고. 아래는 이전 구현 기록이다.

# 문자열 테이블 (2026-09-19)

- 사용자 요청: 하드코딩된 표시 문구를 문자열 키 + 한국어/영어 컬럼 테이블로 분리.
- Resources/Data/StringTable.json: version 1, rows[{key,ko,en}], 53행. 키는 ordinal, 중복/빈 컬럼/형식 인자 오류를 거부. 기본 언어 Korean.
- GameBootstrap.Load 첫 단계에서 TextAsset 비동기 로딩/파싱. Manifest 필수 슬롯으로 등록하고 direct fallback으로 같은 에셋을 ResourceManager에 고정. 이후 즉석 로딩 없음. 원격 네트워크 없음.
- DataManager.Strings/SetLanguage, L10n.Text/Format. 조회 facade는 매니저 컨테이너가 아니며 IO 없음. 언어는 세션 한정; 언어 설정 UI/저장은 미구현.
- 정적 TMP는 LocalizedText. 동적 코인/설정/캐릭터/게임 카드/게임 진입은 View/Controller. 언어 변경 구독 해제 포함. 메시지박스/토스트 본문은 생성 시 값 유지.
- CharacterDefinition/MiniGameDefinition.DisplayName은 character.{id}.name / game.{id}.name 조회. 이전 serialized displayName 필드 제거. ID/아트 변경 없음.
- UI 제작 Editor 코드도 테이블 조회. 경로/리소스 ID/오브젝트명/에디터 메뉴/개발 예외 및 로그/테스트 데이터는 번역 대상 제외.
- Localization > 1. Apply String Table And Korean Font: UI 프리팹/3씬 백업, 정적 키 바인딩, 동적 바인딩 비활성화, 중첩 오버라이드 기록, 한글 TMP 폰트 생성/문자 사전 등록. 2. Validate String Table: 키/컬럼/인자/폰트/부트 등록 검증. Play 중 한국어/영어 미리보기 메뉴 제공.
- Fonts/NanumGothic-Regular.ttf 및 OFL.txt 출처: https://github.com/google/fonts/tree/main/ofl/nanumgothic . 게임과 함께 로컬 포함, 다운로드는 개발 작업 중에만 수행.
- Unity MCP 없음: 명세에 따라 C#/메뉴까지만 준비. 실제 에셋/씬 적용 메뉴를 자동 실행하지 않았다. 사용자 메뉴 실행과 양 언어 Play/기기 화면 검증 필요.
- Android Unity 6000.3.16f1 EditMode 117/117 passed, skipped 0. 문자열 테스트 11개 추가 및 기존 부트/씬 테스트 기대값 갱신. 실제 에셋 생성/Play 통과를 의미하지 않음.
- 로그: Client/Logs/string-table-editmode.log, Client/Logs/Tests/string-table-editmode.xml. 사람용 안내: ../Human/String_Table_Guide.html.

## 후속 규칙 갱신

- 앞으로 모든 사람이 읽는 코드 문구를 테이블에 저장하도록 AGENTS.md를 강화했다. 신규 로그·오류 메시지도 포함한다. 위 개발 로그/예외 제외 설명은 기존 구현 범위이며 향후 작성 규칙은 아니다. 이번 갱신은 문서만 변경했다.

## 2026-11-04 UI 프레임 진단 추가

- UIManager 프레임 전환의 새 진단 15행을 추가하여 현재 StringTable.json은 68행이다. 사람용 테이블 스냅샷도 갱신했다. 최신 UI 구조는 UI_Frames_Status.md 참조.
