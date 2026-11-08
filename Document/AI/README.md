# 프로젝트 문서 색인

[공통 지침](../../AGENTS.md) · [구조](../../ARCHITECTURE.md) · [현재 기능 계약](../../Agent_Instruction/README.md) · [TODO](TODO.md) · [사람용 안내](../Human/Agent_Instruction_Guide.html)

## 분류

- 현재 에이전트 계약: 루트 `Agent_Instruction/`.
- 제품 명세/결정/기존 기술 기록: 이 폴더의 기존 위치를 보존한다. 새 큰 설계는 `Document/AI/Plan/`, 새 기술 분석/진단은 `Document/AI/Reference/`에 둔다. 문서가 생길 때 폴더를 만든다.
- 사람이 읽는 안내: 기존 `Document/Human/`의 단일 HTML을 기본으로 한다. Kit의 Guide 역할을 이 폴더가 담당한다.
- 할 일의 단일 색인은 `TODO.md`. 계획/검토안을 승인된 구현으로 바꾸지 않는다.
- 기존 `*_Status.md`의 날짜별 검증은 당시 기록이다. 현재 계약은 해당 Agent_Instruction 문서와 코드로 확인한다. 과거 문서를 전부 이동하거나 내용 그대로 현재 규칙에 복제하지 않는다.

## 원본·생성물·보관 기준

- `Document/Human/`의 기존 HTML은 별도 저작하는 사람용 원본이다. 관련 Markdown과 의미를 맞추되 Markdown에서 자동 변환된 파일이라고 가정하지 않는다.
- 실제 변환기가 있는 문서는 원본·도구·재생성 명령·출력 위치를 이 색인 또는 해당 도구 README에 등록한다. 그 출력만 생성물로 취급하고 본문은 원본에서 수정한다. 아직 저장소 공통 Markdown→HTML 생성 체계는 정해져 있지 않다.
- 새 생성 체계를 도입할 때만 필요에 따라 `_generated/`를 사용한다. 완료 계획·폐기 구성 요소를 정리하는 작업에서는 참조와 최신 대체 문서를 확인한 뒤 `Document/AI/_archive/`로 옮길 수 있다. 기존 문서를 자동 일괄 이동하지 않는다. 이동한다면 링크·이미지 상대 경로·색인을 함께 갱신한다.
- 되돌리기 어려운 구조 결정은 [adr/](adr/README.md)와 [ADR 틀](ADR-Template.md)을 사용한다. 과거 사용자 결정 기록은 유지한다.

## 명세·결정

- [MVP 명세](MiniGameHeaven_Unity_MVP_Spec.md) — 제품 목표/초기 설계. 후속 결정과 현재 구현을 함께 확인.
- [사용자 결정](Project_Decisions.md) — 날짜별 결정. 최신 결정이 이전 검토안보다 우선.
- [Phase 0 설정](Phase0_Setup.md), [Phase 1 코드 안내](Phase1_Code_Guide.md) — 초기 설정/구조 기록.

- [반복 작업 절차](../../Agent_Instruction/Common-Changes.md) — 현재 코드에 맞춘 UI·문자열·캐릭터·게임 작업 순서.
- [동시 수정 체크리스트](../../Agent_Instruction/Change-Checklist.md) — 관련 반영 위치와 검사 한계.
- [구조 결정 색인](adr/README.md) — 형식 준비, 새 ADR은 아직 없음.

## 기능별 자료

| 주제 | 현재 계약 | 기존 상세/사람용 안내 |
|---|---|---|
| 부트·매니저 | [부트](../../Agent_Instruction/Boot-Resources.md) | [매니저 기록](Independent_Managers_Status.md), [부트 안내](../Human/Boot_Resources_Guide.html) |
| UI·문자열 | [UI](../../Agent_Instruction/UI.md) | [프레임](UI_Frames_Status.md), [문자열](String_Table_Status.md), [현재 허브](../Human/Hub_UI.html) |
| 저장·튜토리얼 | [저장](../../Agent_Instruction/Save-Data.md) | [온보딩 기록](Onboarding_Characters_Android_Status.md), [결과 안내](../Human/BlockGravity_Results_Guide.html) |
| 게임 | [게임](../../Agent_Instruction/MiniGames.md) | [난이도](BlockGravity_Difficulty_Status.md), [원판](Tilt_Floor_Status.md), [시간 분신](Echo_Room_Status.md), [배달](Udada_Delivery_Status.md) |
| 캐릭터·원화 | [캐릭터](../../Agent_Instruction/Characters-Art.md) | [ComfyUI](ComfyUI_Art_Pipeline.md), [캐릭터 테이블](../Human/Character_Table_Guide.html), [푸들 적용](../Human/Poodle_Redraw_Review.html) |

새 문서는 역할·현재 상태를 한 줄로 이 색인에 등록한다. 코드 계약이 바뀌면 기능 문서, 구조가 바뀌면 ARCHITECTURE, 사용 절차가 바뀌면 해당 HTML을 갱신한다.
