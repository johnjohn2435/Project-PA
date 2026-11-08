# 튜토리얼·게임 사운드·캐릭터·Android 후속 구현 완료 (2026-11-05)

- 사용자 승인: 남은 순서대로 계속 진행. 최종 확정 char_cat=고영이/Goyeongi, char_dog=가나지/Ganaji, char_hamster=햄쥐/Hamjwi.
- SaveData v3: completedTutorials, 이전 char_momo/char_bori/char_nabi 선택·해금 ID를 새 ID로 변환·중복 정리. 코인/최고 기록/지급 이력 보존. 새 코드·카탈로그·리소스 경로와 StringTable 이름 키 모두 새 ID 사용.
- 첫 게임 튜토리얼 3단계, 완료 저장, 저장 실패 재시도·나중에 보기, 일시정지 메뉴 ? 다시 보기. 완료 저장 성공 전에는 완료로 표시하지 않음. UIBase 수명·FIFO/Fixed 규칙 및 ILocalizedDynamicText 사용.
- 게임 Place/Clear/Combo/Gravity/GameOver 사운드 및 허브/게임 BGM 이벤트 연결. 실제 음원은 미제공이므로 선택 리소스 경로와 코드만 준비. 부트 캐시만 조회하고 런타임 추가 로딩 없음.
- 준비된 3종 5파츠를 rigid SpriteRenderer 리그로 통합. 공유 Idle/Walk Animator, 좌우 루트 반전·상하 마지막 좌우 유지. 허브 실제 리그 미리보기/새 초상화, CharacterPreview 레이어, RenderTexture 카메라 연결. 숨긴 홈 패널에서는 스폰하지 않고 숨김/종료 때 인스턴스 해제.
- Approved 원본과 ArtPipeline 준비 패키지는 보존. 리깅 준비 시안의 Unity 통합이며 새 원화 최종 승인이나 SpriteSkin 메시/가중치 제작을 의미하지 않는다.
- MCP로 원본 프리팹·카탈로그·리소스 및 폰트 적용/검증 완료. 실제 허브 세 캐릭터와 한국어/영어 튜토리얼 GPU 화면 확인. 실제 저장 설정 변경 없이 UI Present로 세 캐릭터만 확인했고 실제 데이터에서 튜토리얼 완료 버튼은 누르지 않았다. 한국어·runInBackground=false로 복원 후 Play 종료.
- Edit Mode 240/240, Play Mode 23/23, skipped 0. 변경 Scripts/Tests 전체 해시가 검증 복사본과 일치. 저장 테스트는 메모리/임시 저장소.
- 10개 시드 무작위 합법 배치의 장기 게임 진행 검사: 각 종료 또는 500턴. 진행/프레젠테이션 완료/점수 비음수 확인. 재미·난이도나 실제 사람 10판 평가를 대체하지 않는다.
- Android IL2CPP ARM64 개발 빌드 최종 build_44284486be23 성공, buildTimeMs=71275(최초 네이티브 빌드 후 최종 증분), 경고/오류 0. com.johnjohn2435.pocketarcade, versionCode 1/versionName 1.0, minSdk25/targetSdk36, APK v2 서명 확인.
- APK Client/Builds/Android/PocketArcade-20261105.apk. 실제 바이트·SHA256·검사 결과 Client/Logs/onboarding-build.json.
- 테스트 보고서 Client/Logs/Tests/onboarding-edit.xml, onboarding-play.xml. 로그 Client/Logs/onboarding-edit.log, onboarding-play.log. 화면 Client/Logs/Screenshots/cat-hub.png, characters-hub.png, hamster-hub.png, tutorial-start.png, tutorial-gravity-en.png.
- ADB 연결 Android 기기 없음: 실제 기기 터치/앱 복귀/진동/음원 출력 미검증. 실제 음원 파일과 사용자 난이도 피드백은 후속이다. 코드/프리팹 적용에 남은 수동 메뉴 작업 없음.
- 사람용 ../Human/Onboarding_Characters_Android.html.
