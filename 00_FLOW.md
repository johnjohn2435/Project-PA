# 지침 읽기와 적용 흐름

```text
CLAUDE.md (@AGENTS.md)
  → AGENTS.md: 공통 작업·보호·검증·문서·Git 정책
  → ARCHITECTURE.md: 작업 영역 → 코드 위치
  → Agent_Instruction/README.md: 필요한 계약만 선택
  → 반복 작업이면 Common-Changes → 해당 Change-Checklist 행
  → 실제 코드·자산·설정 확인 → 변경 → 영향에 맞는 검증 → 해당 문서 갱신
```

`Client/AGENTS.md`는 Unity 폴더에서 시작할 때 상위 지침으로 연결한다. 세션마다 전체 기능 문서를 자동으로 읽지 않는다.

- 기능 계약의 원본은 `Agent_Instruction/`. UI·부트·저장·게임·캐릭터의 현재 규칙을 구분한다.
- `Pitfalls.md`는 재현 근거가 있는 환경·자산 함정, `Verification.md`는 재실행 방법, `Refactoring-Review.md`는 별도 리뷰 체크리스트다.
- 새 기능의 계약이 필요하면 `_Feature-Template.md`를 복사해 질문→전체 흐름→불변식→시나리오→외부 결합→확장→검증 순으로 작성한다. 코드 목록을 복제하지 않고 함정·소유권·실패 처리를 보존한다. 기존 문서는 실질적으로 손댈 때만 전환한다.
- [반복 작업 절차](Agent_Instruction/Common-Changes.md)는 두 번 이상 반복한 작업에만, [체크리스트](Agent_Instruction/Change-Checklist.md)는 실제 누락/결합 근거가 있는 지점에만 추가한다. 자동 검사가 생기거나 원본이 통합되면 항목을 줄인다.
- 중요한 구조 결정의 이유·기각 대안은 [ADR](Document/AI/adr/README.md)에 남긴다. 새 구조 결정을 임의로 만드는 지시가 아니다.
- `Document/AI/`와 `Document/Human/`은 기존 배치를 유지한다. `Docs/`를 중복 생성하지 않는다. [문서 분류](Document/AI/README.md)와 [TODO](Document/AI/TODO.md)를 사용한다.
- 과거 상태 문서의 테스트 숫자·초기 프로토타입 설명은 현재 상태를 보장하지 않는다. 최신 사용자 결정과 실제 코드를 대조한다.

## AgentInstructionKit2 적용 방식

두 번째 묶음의 신규 3종(반복 작업 절차·동시 수정 체크리스트·ADR 틀)과 기존 8종의 변경을 프로젝트에 맞게 반영했다. 기능 계약은 필요한 문서만 읽고 현재 코드와 대조한다.

- 사용자 변경을 분리할 수 없다고 자동으로 함께 커밋하지 않는다. 푸시는 실제 대상 브랜치를 확인하며 기본 브랜치 갱신을 강제하지 않는다.
- 별도 저작 HTML과 실제 변환 생성물을 구분한다. 기존 문서를 `_archive/`/`_generated/`로 일괄 이동하지 않는다.
- 자동·정적·수동 검증을 구분하고 변경 위험에 맞는 범위를 수행한다. 문서 변경에도 전체 게임 테스트를 강제하지 않는다.
- 하위 에이전트는 명시적으로 요청/지정된 범위에 사용한다. 자동 위임을 기본으로 켜지 않는다.
- 서버 분리·ECS·Addressables·존재하지 않는 로그 래퍼/이벤트 버스는 현재 프로젝트 규칙으로 추가하지 않는다.

[적용 안내](Document/Human/Agent_Instruction_Guide.html) · [구조](ARCHITECTURE.md)
