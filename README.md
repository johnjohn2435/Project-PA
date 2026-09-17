# Project PA

Pocket Arcade 모바일 미니게임 허브 프로젝트입니다.

## 폴더 구조

- `Document/`: 기획 및 개발 명세
- `Client/`: Unity 클라이언트 프로젝트 (Universal 2D), 내부에 `Assets/`, `Packages/`, `ProjectSettings/` 위치

## 프로젝트 열기

1. Unity Hub에서 로그인하고 유효한 Unity Editor 라이선스를 활성화합니다.
2. Unity Editor `6000.3.16f1`과 Android Build Support, SDK/NDK, OpenJDK를 설치합니다.
3. Unity Hub의 프로젝트 추가에서 `D:\Project PA\Client` 폴더를 선택합니다.
4. 첫 실행의 패키지 설치와 임포트가 끝날 때까지 기다립니다.

현재 생성에 사용한 에디터는 PC에 설치된 `6000.3.16f1`입니다. 개발 명세의 기준 버전은 `6000.3.23f1` 이상이므로 본격적인 개발 전에 버전을 맞춰야 합니다.

## 초기 상태

Universal 2D 템플릿으로 프로젝트를 생성하고 Unity Editor의 초기 임포트와 배치 실행 정상 종료를 확인했습니다. Play Mode 테스트와 Android 빌드는 아직 수행하지 않았습니다. 초기 변환에서 자동 추가된 광고·결제·분석·내비게이션·멀티플레이 센터·XR 보조 패키지는 MVP 범위에 맞게 제외했습니다.

Git은 저장소 루트에서 문서와 클라이언트를 함께 관리합니다. Unity 캐시, 사용자별 설정, IDE 생성 파일과 빌드 결과물은 제외합니다.

## 기반 설정 및 결정 기록

- [Phase 0 설정 절차](Document/Phase0_Setup.md): Unity 메뉴에서 기본 씬과 모바일 설정 적용.
- [최신 결정 기록](Document/Project_Decisions.md): Addressables 패치 방향 및 미정인 서버 검토안.
- 현재 씬 생성과 Player Settings 적용은 위 절차의 사용자 실행을 기다리고 있습니다.
- [Phase 0 작업 결과](Document/Phase0_Status.md): 실제 테스트 결과와 남은 완료 조건.

## 씬과 플레이 진입

`BootScene → SelectScene → GameScene` 흐름을 사용합니다. 로그인 없이 게스트로 플레이하며, Google 계정 연동은 SelectScene에서 선택적으로 제공할 예정입니다. 실제 인증 기능은 아직 구현하지 않았습니다.
