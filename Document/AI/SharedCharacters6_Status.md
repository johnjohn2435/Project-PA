# 공통 6파츠 제작 결과


## 2026-11-05 — 큰 머리 디자인 방향 승인 및 공통 6파츠/애니메이션

- 사용자: 현재 큰 머리·작은 몸통 비율과 디자인 방향 승인, 자세한 디자인은 나중에 직접 수정. 공통 본 제작 진행 승인. 원본 승인 기록 Approved/SharedFamily/v1.
- ArtPipeline/Rigging/SharedCharacters6_20261105 및 ZIP: 3종×6개 실제 원화 PNG, 같은 본/피벗/몸통·팔다리 실루엣, 공통 Idle/Walk JSON·네이티브 .anim·단일 Controller·3 Prefab·unitypackage. 기존 Client 파일 교체 없음.
- 원본 실제 파츠를 ComfyUI에서 분리·배치. 고영이 몸통/팔다리 템플릿을 공통화하고 가나지/햄쥐는 색 합성. 숨겨진 팔 뒤는 기존 깨끗한 색면으로 복원. 생성 인페인트는 팔 선이 남아 미채택. 타원 팔다리 합성 없음.
- 별도 Unity6000.3.16f1에서18Sprite·3Prefab·공통Controller·198자세 샘플의 작성 위치/회전 일치와16개 실제 화면 캡처 확인. Y곡선만 쓸 때 X기본값이 초기화되는 문제를 고정 X/Z 곡선 명시로 수정.
- 현재는 rigid cutout Transform 본 방식, SpriteSkin/Spine 메시 아님. 작은 대기/걷기만 검증. 정면에 가까운 소스이며 측면/후면·점프/공격/큰 동작과 최종 관절 선 다듬기는 별도.
- 사람용 Document/Human/SharedCharacters6_Review.html. 게임에 적용 완료한 것으로 보고하지 않는다.


이음새 수정본은 ArtPipeline/Rigging/SharedCharacters6_SeamFix. 목 아래 몸통 겹침, 턱선, 팔 뒤 몸통 경계 및 골반 내부 절단선을 수정했다. 원래 Idle/Walk Unity 샘플 검수 및 전후 비교는 Document/Human/SharedCharacters6_SeamFix.html. 푸들 후보는 별도 파츠만 보정했다.

최신 윤곽 검수본 ArtPipeline/Rigging/SharedCharacters6_Continuous. 원화의 연속 몸통을 ComfyUI에서 복원하고 기존 본에 가중치를 준 SkinnedMeshRenderer로 구동한다. 기존 Idle/Walk Unity 캡처 비교 Document/Human/SharedCharacters6_Continuous.html. 몸 파츠 수정 시 torso_mesh 갱신 필요.

SharedCharacters6_ArmSwing에서 몸통 메시의 팔 가중치를 0으로 하고 양팔 스프라이트를 독립 회전하도록 수정. Unity 기본 3종 대기/걷기 확인. Document/Human/SharedCharacters6_ArmSwing.html.

SharedCharacters6_ShoulderBlend: 두 팔에 독립된 body/arm 가중치 메시 적용. 팔 뿌리에서 몸통 가중치 1, 손끝에서 팔 가중치 1. 3개 프리팹의 기존 Idle/Walk 검수 및 주기당 32프레임 캡처.

SharedCharacters6_Ink: 기존 어깨 보간 리그의 턱과 몸통·팔 외곽선 보강. 3종 Unity Idle/Walk 검수. 비교 Document/Human/SharedCharacters6_Ink.html.

## 2026-11-05 캐릭터 게임 적용
고영이·가나지·햄쥐의 기본 스킨을 허브, 선택 카드, 흔들리는 바닥 및 보관 중인 우다다 배달에 적용했다. 이 세 프리팹은 공용 Idle/Walk를 사용한다. 현재 허브에서 플레이하는 이동 게임은 흔들리는 바닥이다. 기존 네모 외형 지침은 이 적용 기록으로 갱신한다. 푸들 후보는 별도 검토 상태다.

승인 SharedFamily/v1 원화의 짙은 외곽선 전체 폭/키(수염·귀 포함)를 기준으로 프리팹 X 배율과 카드 합성을 맞춤. 고영이 0.844, 가나지 0.887, 햄쥐 0.827. 게임 스폰은 프리팹 배율에 월드 크기를 곱한다.

햄쥐의 게임용 머리·몸통·양팔과 선택 카드는 SharedFamily/v1 원화를 직접 분리한 SharedCharacters6_Game/char_hamster/approved_body/workflow.json 결과가 최신이다. 별도 torso와 양팔 메시 범위를 조정했다. 햄쥐 및 가나지의 가로 비율 보정은 하위 파츠에서 프리팹 루트로 옮겼다.

2026-11-06 고영이·가나지도 승인 원화에서 직접 분리한 head, torso_mesh, arm_L, arm_R과 선택 카드를 사용한다. 각 char_*/approved_body/workflow.json 및 마스크가 해당 스킨의 최신 복원 워크플로우다.

2026-11-06 기존 햄쥐 슬롯(char_hamster)에 다람이/Darami 연결. 다람이만 body/tail 파츠를 사용하며 오른쪽을 볼 때 꼬리가 왼쪽에 보인다. 공용 걷기와 루트 좌우 반전을 따른다.

2026-11-06 푸들 원화 승인 및 네 번째 캐릭터 char_poodle 추가. 표시 이름 푸들/Poodle은 작업 기본값. 공용 Idle/Walk와 6본·몸통/팔 보간 메시를 연결했다. 홈 배지 4개와 캐릭터 목록 4행, 부트 사전 로딩을 갱신했다. SaveData.Normalize에서 신규·기존 저장 모두 기본 해금. 선택/기존 코인 123 유지 및 허브→원판 게임 통합 검증1, EditMode320, PlayMode43 통과. Android 실기기 미실행. 상세 ../Human/Poodle_Redraw_Review.html.
