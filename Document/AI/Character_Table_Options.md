# 캐릭터 테이블 형식 검토 — 2026-09-18

상태: 추천안. 사용자가 형식을 확정하거나 구현을 지시한 것은 아니다.

- 질문: 기존 HTML 캐릭터 테이블을 JSON으로 관리하는 편이 나은지, 비기획자 입장에서 쉬운 방식은 무엇인지.
- 현재 코드 확인: CharacterDefinition은 ScriptableObject이며 id, displayName, portrait(Sprite), themeColor, defaultUnlocked, abilityId를 가진다. GameCatalog가 목록을 참조한다.
- 현재 3종 규모에는 기존 ScriptableObject를 기준 데이터로 유지하는 방식을 추천했다. Unity Inspector에서 수정하고, 사람이 전체 목록을 볼 필요가 있으면 에셋에서 HTML을 생성하는 도구를 추후 만든다.
- HTML은 사람용 표시·편집 화면, JSON은 외부 데이터 교환·저장 형식으로 구분했다. JSON 원본으로 전환한다면 한 방향 가져오기와 ID 검증·이미지 참조 변환이 필요하다.
- 여러 행의 일괄 수치 편집이 필요해지면 Excel/CSV도 검토할 수 있다. 특정 개수 기준으로 전환을 의무화하지 않는다.
- 캐릭터 정의와 player-save.json의 선택·보유·코인 저장을 구분했다.
- defaultUnlocked 필드는 존재하지만 현재 MVP 저장은 3종을 기본 해금하므로 필드 변경만으로 잠금 기능이 완성된다고 설명하지 않았다. abilityId는 게임 효과 미구현이다.
- 이번 작업은 설명 문서 작성만 했다. 캐릭터 데이터 형식 전환, JSON importer, HTML 자동 생성·편집 도구는 구현하지 않았다.

사람용 설명: [Character_Table_Guide.html](../Human/Character_Table_Guide.html)

근거:
- Client/Assets/_Project/Scripts/Runtime/Meta/Characters/CharacterDefinition.cs
- Client/Assets/_Project/Scripts/Runtime/Meta/GameCatalog.cs
- https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html
- https://docs.unity3d.com/6000.3/Documentation/Manual/json-serialization.html