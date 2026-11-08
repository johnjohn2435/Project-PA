# 캐릭터·아트·리그 계약

> [색인](README.md) · [구조](../ARCHITECTURE.md)

## 게임에 적용된 상태

| 내부 ID | 현재 표시 | 주의 |
|---|---|---|
| char_cat | 고영이 / Goyeongi | 과거 char_momo는 저장 호환 경로만 |
| char_dog | 가나지 / Ganaji | 과거 char_bori는 저장 호환 경로만 |
| char_hamster | 다람이 / Darami | 기존 슬롯 유지, body/tail 예외 |
| char_poodle | 푸들 / Poodle | 승인·카탈로그 연결됨, 표시 이름은 작업 기본값 |

실제 이름은 `Resources/Data/StringTable.json`, 정의는 `ScriptableObjects/Characters/`, 런타임 프리팹은 `Resources/Characters/{id}/Character.prefab`이다. 이 절의 경로는 `Client/Assets/_Project/` 기준이다.
이전 햄쥐 이름·푸들 후보 상태·Client 미통합 기록은 과거 단계다. 신규/기존 저장 해금과 ID 보호는 [저장 계약](Save-Data.md)을 따른다.

## 런타임 소유권

- `Scripts/Runtime/Characters/CharacterRigController.cs`가 부트 캐시에서 생성하고 공용 Idle/Walk, 좌우 루트 반전, 정지 상태를 제어한다. 상하 이동은 마지막 좌우 방향을 유지한다.
- `Scripts/Runtime/UI/CharacterRigPreview.cs`의 카메라/RenderTexture 수명과 반환을 보존한다. 공용 애니메이션을 캐릭터마다 무작정 복제하지 않는다.
- 머리/몸/팔/다리 6파츠 공통 체형을 기본 방향으로 하되 실제 승인 리그를 우선한다. 다람이는 body/tail 예외이고 오른쪽을 볼 때 꼬리가 왼쪽에 보인다. 과거 전 캐릭터 꼬리 금지로 이를 제거하지 않는다.
- 메시 변형 리그가 있으므로 표시/숨김/분신 틴트를 SpriteRenderer만으로 처리하지 않는다. authored 비활성 파츠는 보존하고 `MaterialPropertyBlock`으로 색을 조정해 공유 재질을 바꾸지 않는다.

## 승인 아트와 제작

- 이미지 생성은 ComfyUI만 사용한다. `ArtPipeline/ComfyUI/Workflows`, `Api`, `approved_style_presets.json`, `models.lock.json`과 기존 설치 모델을 먼저 확인한다. 키·토큰을 문서에 기록하지 않는다.
- `ArtPipeline/ComfyUI/Approved/`는 승인 원본, `Examples/`는 검토본, `ArtPipeline/Rigging/`는 제작/리깅 자료다. 승인·검토·게임 적용은 서로 다른 상태다. Approved/lineup만으로 최신 게임 연결까지 추정하지 않는다.
- 큰 둥근 머리·작은 통통한 몸·짧고 둥근 팔/다리, 작은 보폭과 팔 동작, 공통 키/관절을 유지한다. 외형은 자체 디자인이며 참고 이미지의 비율·동작과 구분한다.
- 부드러운 단일 외곽선과 둥근 턱/팔/발, 은은한 명암·부위별 색을 유지한다. 파츠 경계에 테두리를 중복해 덧대지 않는다. 작은 점눈의 흰 반사광과 짧은 눈썹을 유지한다. 과도한 장식·세부선·일괄 2~4색 축약을 추가하지 않는다.
- 승인 원화를 보존하고 검토본을 최종본으로 자동 교체하지 않는다. 파츠 분리·피벗·본·애니메이션과 Unity 적용 여부를 각각 검증해 보고한다.

참조: [아트 파이프라인](../Document/AI/ComfyUI_Art_Pipeline.md), [공통 리그 기록](../Document/AI/SharedCharacters6_Status.md), [캐릭터 테이블](../Document/Human/Character_Table_Guide.html), [푸들 적용](../Document/Human/Poodle_Redraw_Review.html), [메시 표시 QA](../Document/Human/Character_Render_Play_QA.html). 과거 결과를 현재 검증으로 재사용하지 않는다.
