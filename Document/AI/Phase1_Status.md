# Phase 1 작업 결과

> 최신 UI 구조/프리팹 적용 및 검증 상태: [UI_Foundation_Status.md](UI_Foundation_Status.md). 아래 이전 단계 기록보다 최신 내용을 우선한다.

## 최신 상태 — 2026-09-18 UIManager 추가

- DataManager → SoundManager → SceneFlowManager → UIManager 순서로 초기화한다.
- UIManager는 패널 전환·뒤로가기·안내 표시를 맡고, HubView/GameEntryView는 내부 UI 참조를 유지한다.
- 실제 프로젝트의 Assets/Packages/ProjectSettings를 별도 복사한 Unity 6000.3.16f1 프로젝트에서 Android 타깃 Edit Mode 55/55 통과(기존 47 + UIManager 8). 변경한 소스와 테스트의 해시 일치를 확인했다. 최종 로그에 C# 컴파일 오류·경고 없음.
- 첫 UI 검사 실행은 에디터 전용 테스트 컴포넌트를 AddComponent 할 수 없어 8개 실패했다. 실제 HubView/GameEntryView로 검사하도록 바꾼 뒤 최종 55개가 통과했다.
- 결과: Client/Logs/Tests/ui-manager-final-editmode.xml, Client/Logs/ui-manager-final-editmode.log.
- 사용자가 기존 GameScene 진입 화면을 제공했다. 새 UIManager 구성의 메뉴 적용·Play Mode·실제 기기 검증은 아직 수행하지 않았다.
- 씬/프리팹 생성 메뉴는 자동 실행하지 않았다. 실제 프리팹 분리, 공통 팝업, ResourceManager는 미구현이다. 게임 로직보다 이 구조 작업을 먼저 진행한다.
- 사람용 최신 안내: [UI_Manager_Guide.html](../Human/UI_Manager_Guide.html).

## 이전 검증 기록

아래는 UIManager 추가 이전의 기록이며 최신 결과는 위 내용을 따른다.

## 2026-09-18 싱글톤 매니저 반영

Phase 1 코드와 설정 도구에 DataManager, SoundManager, SceneFlowManager를 추가하고 Bootstrap/UI를 연결했다. 최종 Android 타깃 Edit Mode 47/47 통과. 씬 생성 메뉴·Play Mode·실제 기기·음원 청취는 미실행이므로 Phase 1 전체 완료라고 보고하지 않는다.

## 변경 범위

- 추가: Scripts/Runtime/Managers/DataManager.cs, SoundManager.cs, SceneFlowManager.cs.
- 추가: Tests/EditMode/ManagerTests.cs (12개 검사).
- 수정: GameBootstrap.cs, HubView.cs, GameEntryView.cs, Phase1Setup.cs.
- 기준 경로는 Client/Assets/_Project/다. Unity가 새 파일과 Managers 폴더의 .meta를 생성했다.
- 규칙/설명 갱신: AGENTS.md, AI 명세·결정·코드 구조·적용 안내, Human/Phase1_Guide.html.
- 기존 Core의 저장 규칙, JSON 스키마, 캐릭터 테이블 형식은 변경하지 않았다. 새 패키지와 실제 음원은 추가하지 않았다.

## 실제 검증

| 항목 | 결과 |
| --- | --- |
| Unity | 6000.3.16f1 |
| 최종 Android 타깃 Edit Mode | 47/47 통과 |
| 구성 | 기존 검사 35개 + 매니저 검사 12개 |
| 최종 로그의 C# 컴파일 오류·경고 | 없음 |
| 중복 매니저 초기화 거부 | Edit Mode 검사 통과 |
| 명시적 Shutdown 시 이벤트·Instance 정리, 재초기화 | Edit Mode 검사 통과 |
| 저장된 사운드 설정 적용·저장 실패 시 기존 음소거 상태 유지 | Edit Mode 검사 통과 |
| 씬을 실제로 바꾸며 DontDestroyOnLoad/OnDestroy 수명 확인 | 미실행 |
| 사운드 실제 청취 | 미실행 |
| 씬 생성·참조 검사 메뉴·Play Mode | 미실행 |
| APK/AAB 빌드·기기 실행 | 미실행 |

첫 매니저 검사 실행은 46개 중 44개 통과, 2개 실패했다. Edit Mode의 객체 파괴를 Play Mode의 OnDestroy 수명과 동일하게 가정한 검사를 수정하고, 명시적 Shutdown 경계를 추가했다. 이후 이벤트 구독 해제와 재초기화 검사 1개를 더해 최종 47개가 통과했다. 실제 OnDestroy·씬 간 유지 동작을 Play Mode에서 검증했다고 해석하면 안 된다.

최종 로그와 NUnit 결과:
- Client/Logs/managers-final-editmode.log
- Client/Logs/Tests/managers-final-editmode.xml

이전 Phase 1 기준은 Edit Mode 35/35 통과였으며 로그는 phase1-editmode.log, phase1-android-editmode.log에 보존되어 있다. 이전 결과와 최신 결과를 합산해 서로 다른 테스트 수처럼 보고하지 않는다.

## 현재 제공하는 범위

코드와 설정 메뉴를 준비했다. 사용자가 메뉴를 적용하면 게스트 허브, 캐릭터 3종 선택, 설정 저장, Block Gravity 진입 안내와 허브 복귀를 구성할 수 있다. 아직 실제 보드 플레이는 없다.

사운드는 재생 API와 설정 반영까지 준비했다. Ui Click Clip을 연결하면 패널·게임 이동 클릭에 사용한다. 기본 BGM은 연결하지 않았으며 PlayBgm에 클립을 전달해야 한다. 진동 출력은 미구현이다.

## 사용자에게 남은 작업

[적용 안내](Phase1_Setup.md) 순서대로 TMP 리소스와 Phase 0/1 설정을 적용한다. 이미 Phase 1 씬을 생성했더라도 Create Hub And Data를 다시 실행해 누락된 매니저를 추가하고 Validate Hub를 실행한다.

그다음 BootScene Play, 선택·설정 저장, 게임 진입·복귀, Game Services가 하나만 유지되는지, 음원 연결 후 출력·음소거를 확인한다. 씬 자동 적용을 하지 않는 것은 Unity MCP 미연결 시 C#·에디터 스크립트까지 준비하라는 프로젝트 명세에 따른다.

사람용 설명: [Phase1_Guide.html](../Human/Phase1_Guide.html)
AI용 코드 구조: [Phase1_Code_Guide.md](Phase1_Code_Guide.md)

다음은 UI·매니저 프리팹 분리와 리소스 로딩 구조 확정이다. 구조와 실제 씬 검증을 마친 뒤 게임 로직으로 진행한다.