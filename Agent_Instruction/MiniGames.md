# 미니게임 계약

> [색인](README.md) · [구조](../ARCHITECTURE.md)

## 진입과 공통 책임

현재 GameCatalog에는 `block_gravity`, `tilt_floor`, `echo_room`이 있다. Hub에서 선택 후 시작하고 SceneFlowManager → GameScene의 UISceneHost.gameScreens → 해당 화면으로 연결한다. `udada_delivery` 코드/자산/저장 이력은 보존하지만 현재 허브 카탈로그에는 없다.
순수 세션 규칙과 Unity 입력/렌더링/메뉴를 분리한다. 화면/팝업은 UIManager, 사운드/캐릭터는 부트 캐시를 사용한다. 정지·재시작·허브 복귀에서 입력, Animator, 타이머와 구독을 함께 정리한다.

아래 경로는 `Client/Assets/_Project/Scripts/Runtime/MiniGames/` 기준이다.

## BlockGravity

- `BlockGravity/Application/BlockGravitySession.cs`가 상태, `Domain/`이 보드/조각/해결 규칙, `Presentation/Unity/`가 입력·연출·결과 UI를 담당한다.
- 조각 3개 세트 중 한 번이라도 삭제하면 콤보를 유지하고, 세 조각 모두 삭제 없이 끝나면 초기화한다. 줄을 지운 배치마다 1 증가하며 같은 배치의 다중 줄/연쇄로 중복 증가시키지 않는다.
- `ProgressivePieceSource`는 6/18/36회 배치 경계의 난이도를 사용한다. 큰 조각은 최대 3×3이며 새 판은 첫 단계로 시작한다. 조각 풀을 바꾸면 표시 풀/HUD/튜토리얼을 함께 확인한다. 시간 제한을 임의로 추가하지 않는다.
- 결과 팝업/Controller는 [저장 계약](Save-Data.md)의 저장 실패 재시도와 중복 지급 방지를 따른다. Place/Clear/Combo/Gravity/GameOver 및 게임 BGM은 부트 캐시를 사용한다.
- 상세 설계/당시 검증: [규칙·배치](../Document/AI/BlockGravity_Layout_And_Rules.md), [결과](../Document/AI/BlockGravity_Results_Status.md), [난이도](../Document/AI/BlockGravity_Difficulty_Status.md).

## TiltFloor — 원판 생존전

- `TiltFloor/FloorSession.cs`와 `Unity/FloorTuning.cs`, `Unity/FloorWorldView.cs`를 함께 읽는다. 최신 규칙은 원판 위 생존·낙하 적·주기적 보너스다. 과거 목표 수집/기울기 자체 종료 규칙으로 되돌리지 않는다.
- 예고→공중 낙하→착지 때 세션이 한 번만 각속도 충격을 적용한다. 가장자리일수록 기울기 충격이 크며, View는 착지 기록으로 약 0.32초 원판 눌림과 카메라 흔들림을 표시한다. 메뉴 정지 중에는 Present를 호출하지 않고 재시작의 새 세션에서 연출을 초기화한다. 적은 플레이어 추격과 경사 미끄러짐을 함께 적용한다.
- 착지한 적 접촉은 즉시 Hit, 원판 밖 추락은 Fell이다. 예고/공중 적은 착지 전 비치명적이다. 선택 캐릭터와 공용 Idle/Walk를 유지한다.
- 반지름 기본값 4는 실제 `Client/Assets/_Project/ScriptableObjects/TiltFloor/FloorTuning.asset`과 코드 기본값을 함께 확인한다. 크기 변경 시 카메라·그림자·경계·스폰 범위를 맞춘다.
- 생존시간 점수/아이템 보너스는 현재판에만 적용한다. 영구 최고점/코인 연결은 별도 결정이다. [현재 기록](../Document/AI/Tilt_Floor_Status.md).

## EchoRoom — 10초 전의 나

- `EchoRoom/EchoSession.cs`는 기록/상호작용, `Unity/EchoTuning.cs`는 10초×6구간 등 설정, `Unity/EchoWorldView.cs`는 실제 표현을 담당한다.
- 분신 최대 5, 과거 경로는 불변이고 현재 물건/발판과 상호작용한다. 물건은 공유하며 중앙 안전구역은 겹침을 허용한다.
- 분신은 길막만 한다. 원판 적의 접촉 사망 규칙을 복사하지 않는다. 현재판 회수 목표만 있으며 영구 기록/코인 지급은 추가하지 않는다.
- 분신 숨김·표시·틴트는 SpriteRenderer와 SkinnedMeshRenderer를 포함한 모든 Renderer 및 authored 활성 상태를 처리한다. 공유 머티리얼을 직접 변경하지 않는다. [현재 기록](../Document/AI/Echo_Room_Status.md).

## UdadaDelivery — 보관 중인 급정거 배달

- `UdadaDelivery/DeliverySession.cs`와 `Unity/`는 오른쪽 진행·급정거 관성 택배를 집 현관에 보내는 실험이다. 이전 장애물 회피/기회 3회/배달선 통과 규칙을 현재로 취급하지 않는다.
- 선택 ID는 `udada_delivery`, 기록 키는 `udada_brake.v1`. 현재 허브에서 제외되어도 코드·자산·기존 코인은 삭제하지 않는다. 재활성화는 카탈로그·라우팅·저장 계약을 함께 검토한다.
- 현재 리소스에는 선택 캐릭터 적용이 반영되어 있으므로 오래된 네모 전용 지침을 다시 강제하지 않는다. [실험 기록](../Document/AI/Udada_Delivery_Status.md).

게임별 EditMode와 PlayMode 위치·필터·실제 진입은 [Verification](Verification.md). 튜닝 값은 현재 ScriptableObject와 코드를 우선하고 문서 수치만 수정해 구현 완료로 보고하지 않는다.
