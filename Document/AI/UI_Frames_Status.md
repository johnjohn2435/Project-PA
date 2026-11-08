# UI 프레임·FIFO 명령 큐 (2026-11-04)

- 사용자 승인: 제시한 9개 UIManager 요구사항으로 기존 구현을 교체한다. 본 문서가 기존 UI_Foundation_Status/UI_Manager 설명의 내비게이션·팝업 수명 규칙보다 우선한다.
- Open/Close/Back/Home 모두 하나의 FIFO 큐. 로더 await 완료까지 다음 명령 실행 금지, 명령별 오류 격리, 씬 수명 취소 및 세션 번호 검증.
- UIBase 추상 → UIPanel/UIPopup. protected OnOpen(object)/OnHide/OnShow/OnClose/bool OnBackRequested, internal 실행 메서드. true Back 훅은 소비, false는 기본 처리. IsOpen/IsVisible 별도.
- PanelFrame = Panel + Popup 목록. 이전 프레임 Hide, 상위 Close 시 아래 Show. 루트 Close 금지. Home은 오버레이·상위 프레임·루트 팝업 정리.
- Frame/Managed, Independent/Fixed만 허용. Independent는 Popup만 허용. 현재 프레임 Canvas sortingOrder 기본 100부터 10단위 재계산. Fixed 값 보존.
- 키별 인스턴스 캐시: Close는 파괴 안 함. 이미 열린 키 재Open 거부. 호스트 Unmount/Shutdown에서는 해당 씬 인스턴스 캐시 파괴, ResourceManager의 부트 에셋 캐시는 유지.
- Instantiate는 비활성 staging 하위. 중첩 UIBase 전체 검증 후 원자 등록, 실패 전체 롤백. 각 UIBase는 등록 후 Live 자식으로 분리되므로 host-relative RectTransform으로 제작. 요청 키만 Open.
- Back: Independent 높은 고정 order(동률 최근) → top popup → panel → root event. closeOnBack=false도 소비. ESC 입력창 선택 시 포커스만 해제.
- runtime UI 조립/창 직접 SetActive 제거. 활성 제어는 UIBase 내부·UIManager만. 부팅 시스템은 명시적인 예외이며 별도 BootStatusView/BootDialogSystem으로 관리.
- 실제 공급자는 ResourceManager.GetLoaded의 부트 캐시만 사용. Task<GameObject> 공급자 주입은 지연/실패/취소 검증과 향후 API 대응용이며 즉석 리소스 로딩/원격 다운로드는 추가하지 않음.
- 기존 UIPanel enum → UIPage. HubView/HubController/GameEntryController/UISceneHost/메시지박스/토스트/툴팁 호출부 이관. UIFeedback은 사용자 응답 대기를 명령 큐 바깥에서 처리.
- UIManager는 Unity 메인 스레드에서 사용. API Task를 await하거나 UIManager.Observe로 오류 관찰.
- UIFrameSetup 메뉴: 1. Migrate Prefabs And Hosts, 2. Validate Frames. UI 프리팹과 3씬을 Logs/UIFramesBackup에 백업. Root/Popup Canvas·정책·키 및 Live/Inactive 호스트 설정, 별도 BootDialog 프리팹 준비.
- 실제 에셋/씬 메뉴 자동 실행 안 함: Unity MCP 미연결 프로젝트 규칙 준수. 사용자에게 1번 → Localization 적용 → 각 Validate → Boot Play 순서 안내.
- 신규 진단은 StringTable.json에 기록. 기존 개발 로그 전체 이전은 별도 작업으로 남음.
- 테스트 결과: Unity 6000.3.16f1 Android Edit Mode 187/187 passed, skipped 0. UIManager 계약 검사 30개 포함. 다른 작업에서 추가된 BlockGravityTests 53개도 포함되며 이 UI 작업에서 게임 코드는 수정하지 않았다. 컴파일 오류·경고 없음. 전체 Scripts 소스 해시가 검증 복사본과 일치. 로그 Client/Logs/ui-frames-editmode.log, XML Client/Logs/Tests/ui-frames-editmode.xml.
- 사람용 문서: ../Human/UI_Frames_Guide.html.


## 2026-11-04 추가 — 실제 Canvas의 Fixed 순서 보존

- 프리팹을 Canvas 하위 staging/live로 옮길 때 Unity가 고정 sortingOrder를 0으로 바꾸는 문제를 수정했다. Resolve는 Instantiate 전에 프리팹 정의의 순서를 보존하고 Bind에 전달하며, UIBase.ShowInternal은 활성화 후 Fixed 순서를 복원한다. Managed/FIFO/입력 차단 규칙은 그대로다.
- Block Gravity 메뉴는 Independent/Fixed 31000, 중첩 설정은 31010. 기존 툴팁 10010 및 부트 전용 안내 30000보다 위다. Shade는 전체 화면 Image+Button, Card는 형제 Image로 배경 클릭과 내부 클릭을 분리한다. 신규 게임 모달을 추가할 때 이 예약 순서를 따른다.
- 원본 MCP 적용 완료, 최신 전체 Edit Mode 222/222 및 실제 CanvasScaler Play Mode 16/16 통과. 상세는 BlockGravity_Combo_And_Menu_Status.md와 ../Human/BlockGravity_Combo_And_Menu_Guide.html.
