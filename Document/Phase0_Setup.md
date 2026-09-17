# Phase 0 기반 설정 안내

## 현재 범위

- `Client/Assets/_Project/` 아래 아트·프리팹·씬·데이터·스크립트·테스트 폴더 준비.
- Core(순수 C#), Runtime, Editor, EditMode 테스트 어셈블리 분리.
- 60 FPS와 세로 화면을 적용하는 RuntimeSettings.
- 화면 크기 및 안전 영역 변경에 반응하는 SafeAreaPanel.
- 노치·제스처 영역, 16:9·19.5:9·20:9 비율과 잘못된 입력을 검증하는 Edit Mode 테스트.
- Unity 에디터 메뉴에서 실행하는 Phase0Setup 준비.
- 게임 로직, 씬 자동 전환, 허브 버튼과 캐릭터 선택은 Phase 1 이후 범위다.

## 에디터에서 적용해야 하는 이유

현재 Unity MCP가 연결되지 않았다. 최초 명세 0절의 다음 절차를 따른다.

> MCP가 연결되지 않았거나 작업 도중 실패하면 임의의 우회 자동화를 만들지 말고, C#과 에디터 스크립트까지 준비한 뒤 내가 Unity Editor에서 수행할 정확한 절차를 알려줘.

따라서 이번 작업에서 설정 메뉴를 대신 실행하거나 씬 YAML을 직접 생성하지 않는다. CLI는 코드 컴파일·Edit Mode 테스트·Android 타깃 임포트 검증에만 사용한다. 아래 적용을 끝내기 전에는 Phase 0 전체 완료로 보지 않는다.

## 사용자 실행 순서

1. Unity Hub에서 `D:\Project PA\Client`를 연다. 현재 검증 버전은 `6000.3.16f1`이다.
2. 컴파일이 끝나면 `Pocket Arcade > Phase 0 > 1. Apply Foundation`을 실행한다.
   - 제품명 PocketArcade, 회사명 Kang Wanseok, 임시 Android 앱 ID `com.johnjohn2435.pocketarcade` 설정.
   - Portrait, Linear, Input System (New), 기준 해상도 1080×1920 설정.
   - `Assets/_Project/Scenes/`에 Boot, MainHub, BlockGravity 씬 생성.
   - 각 씬에 카메라, Runtime Settings, Canvas, Safe Area, Input System EventSystem 추가.
   - Canvas Scaler: Scale With Screen Size / Match 0.5.
   - Build Profiles 전역 Scene List에 위 3개 씬을 순서대로 등록.
   - 템플릿 SampleScene은 파일을 보존하고 빌드 목록에서만 비활성화한다.
   - 다른 기존 씬과 이미 생성된 기본 씬은 덮어쓰지 않는다.
3. `Pocket Arcade > Phase 0 > 2. Switch To Android`를 실행하고 재임포트·컴파일 완료를 기다린다.
   - Unity 6의 내장 Android 플랫폼 프로필과 전역 Scene List를 사용한다.
   - 별도 배포용 커스텀 Build Profile과 실제 APK 빌드는 Phase 4에서 구성한다.
4. `Pocket Arcade > Phase 0 > 3. Validate Foundation`을 실행한다.
   - Android 타깃, Portrait, Linear, New Input, 씬 순서와 씬별 필수 구성요소를 검사한다.
5. `Window > General > Test Runner`의 EditMode에서 `PocketArcade.Tests` 테스트를 실행한다.
6. 각 기본 씬을 열어 Console 오류가 없는지 확인한다. Play Mode에서는 빈 배경이 정상이다.
7. Game 뷰 또는 설치된 Device Simulator로 16:9·19.5:9·20:9 및 안전 영역을 확인한다. 테스트는 좌표 계산 검증이며 실제 기기 화면 검증을 대신하지 않는다.

## 남은 완료 조건

- 명세 기준 Unity 버전 설치 및 재검증.
- 위 메뉴로 실제 씬 및 Player Settings 적용.
- 기본 씬 3개가 열리고 Android Build Profiles Scene List에 등록되는지 확인.
- Editor Console 오류 0 확인.
- 실제 화면 비율·노치 대응 수동 확인.
- Android APK 빌드와 게임 기능은 아직 미구현이다.

## 기술 참고

- [Screen.safeArea](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Screen-safeArea.html)
- [Android 타깃 전환 API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorUserBuildSettings.SwitchActiveBuildTarget.html)
