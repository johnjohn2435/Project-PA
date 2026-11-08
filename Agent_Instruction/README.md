# 기능별 지침 색인

공통 규칙은 [AGENTS](../AGENTS.md), 코드 위치는 [ARCHITECTURE](../ARCHITECTURE.md)가 원본이다. 해당 영역만 읽고 실제 코드·설정과 대조한다. 코드 경로는 별도 명시가 없으면 저장소 기준, 링크는 문서 기준이다.

| 작업 영역 | 현재 계약 |
|---|---|
| 부팅·씬 이동·매니저·캐시 | [Boot-Resources.md](Boot-Resources.md) |
| UI·허브·문자열·알림·소리 아이콘 | [UI.md](UI.md) |
| 저장·보상·튜토리얼·ID | [Save-Data.md](Save-Data.md) |
| 게임 규칙·게임별 표현·보류 실험 | [MiniGames.md](MiniGames.md) |
| 캐릭터·아트·리그 | [Characters-Art.md](Characters-Art.md) |
| 반복 작업 순서·본보기 | [Common-Changes.md](Common-Changes.md) |
| 동시 수정 지점·누락 검사 | [Change-Checklist.md](Change-Checklist.md) |
| 실행 검증 | [Verification.md](Verification.md) |
| 환경·자산 함정 | [Pitfalls.md](Pitfalls.md) |
| 별도 리뷰 | [Refactoring-Review.md](Refactoring-Review.md) |

- 반복 작업은 Common-Changes의 해당 절차와 연결된 Change-Checklist 행을 읽는다. 구조 결정의 이유는 [ADR](../Document/AI/adr/README.md)에서 찾는다.
- UI/프리팹/문구: UI + 해당 기능. 리소스를 추가하면 Boot-Resources도 읽는다.
- 씬 이동/재시도/재입장: Boot-Resources + UI. 결과 저장이 엮이면 Save-Data도 읽는다.
- 캐릭터 선택/스킨: Characters-Art + Save-Data + UI. 승인 원본과 실제 게임 프리팹을 구분한다.
- Git/프리팹 재생성/검증 복사본: Pitfalls. 실행 결과는 Verification 기준으로 구분한다.
- 새 기능 계약은 [_Feature-Template.md](_Feature-Template.md)에서 시작한다. 공통 규칙을 복제하지 않는다.
- 계획·과거 기록은 [Document/AI](../Document/AI/README.md), 남은 일은 [TODO](../Document/AI/TODO.md), 사람용 안내는 [HTML](../Document/Human/Agent_Instruction_Guide.html)로 연결한다.
