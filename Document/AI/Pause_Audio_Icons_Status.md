# 일시정지 메뉴 소리 아이콘

- 사용자 요청: 게임 중 별도 설정창 제거, 소리 ON/OFF 아이콘으로 변경. 공간이 부족하면 메뉴창 자체 확대.
- Pause 카드 760×740, 배경음·효과음 각 180×180 아이콘 버튼, 계속하기·나가기 배치. OFF는 회색+빨간 사선. 창 코드의 직접 SetActive나 런타임 UI 조립 없음.
- AudioSettingIcon은 DataManager.Settings 복사본을 저장 후 갱신. 두 항목 독립, 다른 설정 보존. DataManager.Changed/L10n.Changed 구독/해제. 저장 실패 시 실제 저장 상태를 다시 표시하고 테이블 안내.
- 정적 이름·제목/버튼은 LocalizedText, ON/OFF·오류는 L10n. 기존 StringTable 키 9개 재사용. 새 문구 리터럴 없음.
- ILocalizedDynamicText를 추가해 동적 상태/안내 TMP가 Localization 도구의 정적 바인딩 대상에 들어가지 않도록 함.
- BlockGravitySetup 부분 클래스로 아이콘 생성 연결. 실제 이미지는 ComfyUI SolidMask/MaskComposite/JoinImageWithAlpha/ImageScale/SaveImage로 제작. API/실행 이력: ArtPipeline/ComfyUI/Examples/Audio_Icons_20261105.
- BlockGravityHud.SettingsKey와 Settings 팝업 호출 제거. 기존 설정 팝업은 Prefabs/Legacy로 GUID 유지 이동, 기존 스크립트는 호환용 보관. 부트 매니페스트에서 제거, 두 Sprite는 신규 필수 리소스로 등록.
- MCP로 원본 ApplyExitMenu 및 Validate 실행 완료. 수동 메뉴 적용 대기 없음. 원본 Boot→허브→GameScene→Pause, 한국어/영어 전환 및 GPU Game View 캡처 확인. 한국어로 복원 후 Play 종료, GameScene 편집 상태로 복귀.
- 격리 복사본 Edit Mode 222/222, Play Mode 18/18, 실패/스킵 0. 변경 소스 해시 일치. 저장 테스트는 메모리 저장소. 실제 플레이어 설정 파일 변경 없음.
- 테스트 보고서 Client/Logs/Tests/audio-icons-edit.xml, audio-icons-play.xml. 로그 Client/Logs/audio-icons-edit.log, audio-icons-play.log. 화면 Client/Logs/Screenshots/audio-icons-menu.png.
- Android 실제 음원 출력/진동/기기 확인은 별도. 사람용 안내 ../Human/Pause_Audio_Icons.html.
