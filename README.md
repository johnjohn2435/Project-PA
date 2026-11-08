# Project PA · Pocket Arcade

세로 화면 모바일 미니게임 허브입니다. Unity 프로젝트는 `Client/`, 실제 저장소는 `D:\Project PA`입니다.

- [프로젝트 구조와 코드 진입점](ARCHITECTURE.md)
- [에이전트 공통 지침](AGENTS.md) · [기능별 계약](Agent_Instruction/README.md) · [읽는 흐름](00_FLOW.md)
- [지침 적용·사용 안내](Document/Human/Agent_Instruction_Guide.html) · [문서 색인](Document/AI/README.md) · [남은 작업](Document/AI/TODO.md)

## 프로젝트 열기

Unity Hub에서 `D:\Project PA\Client`를 엽니다. 에디터 버전은 [ProjectVersion.txt](Client/ProjectSettings/ProjectVersion.txt), 패키지는 [manifest.json](Client/Packages/manifest.json)이 기준입니다. 현재 설정은 Unity 6000.3.16f1/URP 17.3.0이며 Android 작업에는 해당 버전의 Build Support·SDK/NDK·OpenJDK가 필요합니다.

## 현재 플레이 흐름

`BootScene → SelectScene → GameScene`. 게스트로 로컬 플레이하며 모든 게임 리소스를 부트에서 준비합니다. 허브의 원형 배지로 블록 그래비티·원판 생존전·10초 전의 나를 선택한 뒤 시작합니다. 별도 친구 선택창을 사용합니다. 산책 공원과 급정거 배달은 보관 중인 실험입니다.

원격 패치/Addressables는 사용하지 않습니다. Google 선택적 계정 연동의 실제 인증 SDK·서버·클라우드 저장은 미정입니다. 과거 최초 명세보다 [최신 사용자 결정](Document/AI/Project_Decisions.md)을 우선합니다.

## 문서·원화

- `Agent_Instruction/`: 현재 기능 계약, `Document/AI/`: 명세·계획·과거 검증, `Document/Human/`: HTML 안내.
- [현재 허브 안내](Document/Human/Hub_UI.html), [캐릭터 테이블](Document/Human/Character_Table_Guide.html), [ComfyUI 안내](Document/Human/ComfyUI_Art_Guide.html).
- 이미지 생성은 기존 ComfyUI 워크플로우를 사용합니다. 승인 원본·검토본·게임 적용 자산을 구분합니다.

과거 테스트 수치는 해당 시점의 결과입니다. 재실행 방법과 범위는 [검증 안내](Agent_Instruction/Verification.md)를 따릅니다. 지침 문서 적용이 게임 테스트/Android 실기기 검증을 의미하지 않습니다.
