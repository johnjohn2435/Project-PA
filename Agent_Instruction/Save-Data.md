# 저장·보상·튜토리얼 계약

> [색인](README.md) · [구조](../ARCHITECTURE.md)

- 순수 모델은 `Client/Assets/_Project/Scripts/Core/Save/`, 파일 어댑터는 `Scripts/Runtime/Save/JsonSaveRepository.cs`, 앱 진입점은 `Scripts/Runtime/Managers/DataManager.cs`다. 아래 Scripts 경로는 `Client/Assets/_Project/` 기준이다.
- `PlayerProfile`이 상태를 소유한다. 변경은 복사본 → repository.Save 성공 → 현재 상태 교체 → Changed 순이다. 저장 실패 시 선택·코인·설정·튜토리얼을 먼저 반영하지 않는다.
- 기본 저장은 `Application.persistentDataPath/player-save.json`이다. 임시 파일과 `.bak`을 사용한 파일 교체/복구, 미래 스키마 거부를 보존한다. 테스트에 실제 사용자 저장 경로를 사용하지 않는다.
- 현재 `SaveData.CurrentVersion=3`. `completedResults`는 결과 중복 지급 방지, `completedTutorials`는 안내 완료 이력이다. 같은 resultId의 일치하는 재요청은 영수증을 반환하며 내용이 다르면 거부한다. 점수·코인·영수증은 하나의 저장으로 갱신한다.
- 튜토리얼은 끝까지 완료하고 저장에 성공했을 때만 완료 처리한다. 나중에 보기·취소는 다음 안내 대상으로 남긴다.
- `char_momo/char_bori/char_nabi`는 `CharacterIds.FromLegacy`와 `SaveData.Normalize`의 호환 경로에서만 새 ID로 변환한다. 새 자산/코드는 새 ID를 사용하고 코인·최고점·지급 이력을 보존한다.
- 캐릭터 외형/표시 이름 교체로 저장 ID를 바꾸지 않는다. `char_hamster` 슬롯은 현재 다람이이며 `char_poodle`도 기본 해금에 포함된다. 신규/기존 저장의 해금 처리를 함께 확인한다.
- `MiniGameDefinition.ScoreKey` 미지정 시 Id를 사용한다. DataManager는 등록된 ScoreKey만 기록한다. 급정거 실험의 `udada_brake.v1`을 이전 배달 점수와 섞지 않는다. 현재 카탈로그에서 제외된 게임은 실행 경로와 저장 허용 여부를 먼저 확인한다.
- 원판/시간 분신의 현재판 점수·목표와 영구 보상은 별개다. 별도 제품 결정 없이 코인·최고점 저장을 연결하지 않는다. 계정/클라우드 병합 규칙도 미정이다.

검증은 `PlayerProfileTests`, `SaveRepositoryTests`, `GameResultTests`, `OnboardingTests`, `ResultControllerTests` 중 해당 범위를 선택한다. 실제 파일 복구와 메모리 대역 테스트를 구분한다. [검증 안내](Verification.md).
