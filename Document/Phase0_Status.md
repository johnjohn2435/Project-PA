# Phase 0 작업 결과

## 상태

기반 코드와 에디터 설정 도구 준비 및 CLI 검증 완료. Phase 0 전체 완료는 아직 아니다.
Unity MCP 미연결 시 사용자에게 에디터 스크립트 실행 절차를 전달하라는 명세에 따라, 씬 생성과 Player Settings 적용은 사용자가 메뉴에서 실행해야 한다.

## 변경 파일

- `Client/Assets/_Project/`: 공통 폴더 구조와 Unity 생성 `.meta`, 빈 폴더 보존용 `.gitkeep`.
- `Scripts/Core/ProjectDefaults.cs`: 화면·프레임·씬 이름의 공통 상수.
- `Scripts/Runtime/Bootstrap/RuntimeSettings.cs`: 60 FPS·Portrait 런타임 설정.
- `Scripts/Runtime/UI/SafeAreaLayout.cs`, `SafeAreaPanel.cs`: 안전 영역 좌표 및 패널 적용.
- `Scripts/Editor/Phase0Setup.cs`: 기존 씬을 보존하는 기본 씬 생성·설정·검증 메뉴.
- Core, Runtime, Editor, Tests.EditMode의 asmdef 4개.
- `Tests/EditMode/SafeAreaLayoutTests.cs`: Safe Area 테스트 11개.
- `Client/Assets/Settings/UniversalRP.asset`: 설치된 URP가 임포트 중 수행한 포맷 버전 12→13 직렬화 갱신.
- `AGENTS.md`: 한글 Git 커밋 규칙과 최신 결정 문서 참조.
- `Document/Project_Decisions.md`: Addressables 패치 방향, 미정인 Supabase·웹서버 검토안.
- `Document/Phase0_Setup.md`, 최초 명세, `README.md`: 적용 절차 및 최신 결정 반영.

위 Scripts/Tests 경로는 `Client/Assets/_Project/` 기준이다.

## 실제 검증

| 항목 | 결과 |
| --- | --- |
| Unity 버전 | 6000.3.16f1 |
| 기본 타깃 Edit Mode | 11/11 통과, Unity 프로세스 종료 코드 0 |
| Android 타깃 임포트·컴파일 및 Edit Mode | 11/11 통과, Unity 프로세스 종료 코드 0 |
| 두 로그의 C# 컴파일 오류·경고 | 없음 |
| 소스·문서 git diff --check | 통과 |
| Play Mode | 미실행 |
| Editor UI Console 전체 확인 | 미실행 |
| APK/AAB 빌드 및 실제 기기 테스트 | 미실행 |
| 설정 메뉴 실행·생성된 씬 3개 검증 | 사용자 실행 대기 |

두 실행은 같은 테스트 11개를 서로 다른 타깃 환경에서 실행한 것이다.
로그와 NUnit 결과는 로컬 `Client/Logs/phase0-editmode.log`, `phase0-android.log`, `Tests/phase0-editmode.xml`, `Tests/phase0-android-editmode.xml`에 있으며 Git에서는 제외한다.
새 패키지 및 서버 연동은 추가하지 않았다.

## Unity Editor에서 필요한 작업

1. [설정 안내](Phase0_Setup.md)에 따라 `Pocket Arcade > Phase 0`의 1, 2, 3 메뉴를 순서대로 실행.
2. 기본 씬 3개, Android Scene List, Console 오류와 화면 비율 확인.
3. 명세 기준 Unity 6000.3.23f1 이상 설치 및 재검증. 이번 작업에서 에디터 업그레이드를 수행하지 않았다.

## 현재 플레이 가능한 범위

기본 Universal 2D 템플릿만 있다. 설정 메뉴 실행 후에도 새 씬은 기반 오브젝트만 있는 빈 화면이며 허브·캐릭터·게임 기능은 아직 없다.

## 다음 단계

Phase 0의 사용자 적용과 검증을 완료한 뒤 Phase 1에서 Bootstrap/SceneFlow, 로컬 저장, 허브, 게임 선택, 캐릭터 선택을 구현한다.
