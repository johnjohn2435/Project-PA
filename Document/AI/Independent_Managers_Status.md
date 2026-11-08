# 독립 DDOL 매니저

- 사용자 결정: 모든 매니저를 묶은 GameManagers 프리팹을 사용하지 않고 개별 DDOL 매니저를 BootScene에서 생성한다.
- GameBootstrap의 RequireComponent·DontDestroyOnLoad 제거. 개별 프리팹 5개를 직접 참조하고 순서대로 Instantiate/Initialize. 리소스 준비 후 Data/Sound/SceneFlow/UI 생성.
- 각 매니저는 PersistentManager를 상속하여 초기화 시 자신의 독립 루트에 DDOL 적용. 기능/Instance/Shutdown은 각 클래스에 유지. 공통 클래스는 컨테이너나 서비스 로케이터가 아님.
- Bootstrap은 정상 부트 완료 후 씬과 함께 제거. 성공한 매니저는 정리하지 않는다. 부트 재진입 시 준비된 singleton 5개와 사전 로딩 상태를 확인해 재사용. 실패/중단 시 생성한 매니저 및 부분적으로 남은 core singleton 정리 후 재시도.
- IndependentManagerSetup: Boot > 3. Separate Manager Prefabs. BootScene 백업 후 Prefabs/Managers/ 아래 매니저 5개 생성·부트 참조 연결. 구 GameManagers는 Prefabs/Legacy로 GUID 유지 이동. 소리 uiClickClip 보존, 기존 개별 프리팹 미덮어쓰기, 예상 외 스크립트/자식 존재 시 중단.
- Phase1/UIFoundation/BootResourceSetup 생성·검증 경로도 독립 구조로 변경하여 이전 메뉴가 통합 루트를 재생성하지 않도록 했다.
- Android 타깃 Unity 6000.3.16f1 Edit Mode **106/106 통과**, skipped 0. 기존 96 + Bootstrap 자동 매니저 부착 없음/5종 독립 루트/통합·중첩·비활성·누락 검증 10개. C# 컴파일 오류·경고 없음.
- 소스·테스트 12개 파일 해시가 검증 복사본과 일치. 결과: Client/Logs/independent-managers-editmode.log, Client/Logs/Tests/independent-managers-editmode.xml.
- 사용자 스크린샷의 생성·UI 참조 검증 성공 로그로 실제 분리 메뉴 적용을 확인. 실제 DDOL·씬 왕복·부트 재진입 Play Mode 검증은 별도다.
- 사람용 문서: ../Human/Independent_Managers_Guide.html

## 2026-09-19 — Bootstrap 이름 정리

- 씬 오브젝트는 Bootstrap, 컴포넌트/클래스는 GameBootstrap으로 구분한다.
- 생성 코드와 기존 오브젝트 재설정 경로를 함께 수정했다. 기존 씬 반영은 Boot > 3. Separate Manager Prefabs 재실행 필요. Unity MCP 미연결로 씬 파일은 직접 수정하지 않았다.
- 문자열 및 이름 할당만 수정했고 테스트는 재실행하지 않았다. 위 106개 결과는 이전 구조 변경 검증이다.
