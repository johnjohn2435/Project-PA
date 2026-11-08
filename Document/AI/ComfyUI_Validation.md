# 워크플로우 검증 결과

검증일: 2026-09-18. RTX 3080 10GB, 설치된 ComfyUI 0.33.0 / PyTorch 2.10.0+cu130에서 실행했다.

## 실제 실행

| 워크플로우·프리셋 | 결과 | 로컬 API 대기 포함 시간 |
| --- | --- | --- |
| 01_character / char_bori | 성공 | 18.08초 |
| 01_character / char_momo | 성공 | 10.05초 |
| 01_character / char_nabi | 성공 | 10.05초 |
| 02_refine / char_momo | 성공 | 10.16초 |
| 03_cutout / char_momo | 성공 | 2.03초 |
| 04_background / char_momo | 성공 | 8.03초 |

시간은 이 PC의 단일 검증 기록이며 캐시·모델 로딩·다른 GPU 작업에 따라 달라진다. 최초 생성 후 스타일과 색상 가중치를 조정했으며 표는 최종 실행 기록이다.

## 검사

- UI 워크플로우 4개: 공식 0.4 JSON 스키마 통과.
- 실제 ComfyUI 노드 존재 여부, 포트 타입, 링크 양방향 연결, API 필수 입력 검사 통과.
- 체크포인트 6,939,220,250바이트 및 배경 제거 모델 444,473,596바이트: 공식 저장소 SHA-256과 일치. models.lock.json 참조.
- 더미 PNG 3개: RGBA 256×256, 알파 0~255.
- Unity Android 타깃 더미 임포트 및 컴파일: 종료 코드 0, C# 오류 없음.
- 더미 텍스처의 Sprite/Single, PPU 256, mipmap 끔, alphaIsTransparency 설정 확인.

## 투명 이미지

- 01_character_char_bori_sprite_512.png: RGBA 512×512, 알파 0~255, 전경 면적 38.2%
- 01_character_char_momo_sprite_512.png: RGBA 512×512, 알파 0~255, 전경 면적 31.5%
- 01_character_char_nabi_sprite_512.png: RGBA 512×512, 알파 0~255, 전경 면적 27.4%
- 02_refine_char_momo_sprite_512.png: RGBA 512×512, 알파 0~255, 전경 면적 31.5%
- 03_cutout_char_momo_sprite_512.png: RGBA 512×512, 알파 0~255, 전경 면적 31.5%

## 시각적 검수와 적용 범위

초기 결과가 3D 장난감처럼 보여 평면 스타일 가중치를 높였다. 이후 갈색 편향과 스카프 색 누락을 줄이기 위해 대상·색상 문장을 앞에 배치했다. 캐릭터 원본과 투명 PNG, 배경 원화, 후보 비교 이미지를 Examples에 보관한다.

예제는 스타일 탐색용이다. 아직 최종 캐릭터 디자인이나 게임에 연결된 스프라이트가 아니다. 눈 크기·정확한 색값·캐릭터 비율·포즈·발 기준선은 최종 제작에서 정리해야 한다. 원화 수정 워크플로우도 캐릭터 정체성이나 애니메이션 프레임 정합을 보장하지 않는다.

Unity에는 더미 3종만 추가했으며, 캐릭터 선택 UI 연결은 다음 개발 단계다. 기존 씬 및 인증 기능은 변경하지 않았다.


## 2026-09-18 — 모모 디자인 시안 추가 검증

기존 01_character 그래프로 20회 생성 성공. 제시 후보 A/B/C/D의 1024 RGBA·알파 0~255, 512 출력, 비교용 64/128px 및 흰/어두운 배경 시각 검수를 수행했다. 원화 확정·최종 마스크 승인·Unity 적용 검증은 미실행이다. 모델 파일명과 크기를 재확인했으며 모델 전체 해시는 이번에 재계산하지 않았다. 상세 설정과 실행 ID는 Examples/Momo_Design_20260918/review_manifest.json, 검토 내용은 [Momo_Design_Review.md](Momo_Design_Review.md)에 기록했다.


## 2026-11-05 — 고영이 새 원화와 실제 5파츠 검토본

- ComfyUI FLUX로 자연스러운 오른쪽 3/4 전신을 새로 그리고 실제 원화의 몸·양팔·양다리를 분리했다. 꼬리 없음. 기존 얼굴 이동/타원 파츠 시안은 미채택 이력으로 유지한다.
- 새 패키지: ArtPipeline/Rigging/Goyeongi_Redraw_20261105. 사용자 디자인 승인 전이며 Approved 및 다른 두 종을 교체하지 않았다.
- 5 PNG·관절 겹침·메시 가중치 검사와 작은 ±12도 동작 검토 렌더 완료. Unity 실행/통합 및 큰 동작은 미검증. 가중치 변형 검토본이며 기존 rigid 리그에 바로 넣어 검증한 결과가 아니다.
- 상세: Document/AI/Goyeongi_Redraw_Status.md, Document/Human/Goyeongi_Redraw_Review.html.


## 2026-11-05 — 같은 체형의 세 친구, 최신 디자인 목표

- 사용자 첨부 호텔 재즈바 썸네일처럼 단순하고 서로 닮은 동물 친구들. 고영이·가나지·햄쥐는 같은 키/몸통/팔다리/관절 위치를 목표로 귀·색·고등어무늬만 구분해 공통 스켈레톤 애니메이션을 재사용한다. 기존 길고 세밀한 고영이 재원화는 최신 목표의 확정 기준이 아니다.
- ComfyUI 기존 FLUX로 나란한 시안 생성 및 img2img, 마스크 정리 완료. ArtPipeline/ComfyUI/Examples/Shared_Lineup_20261105/lineup_final.png. 사용자 승인 전이며 Approved 교체 없음.
- 최종은 B→E→마스크 정리. 꼬리 없음, 가나지/햄쥐의 잘못된 이마 줄무늬 제거, 볼터치 제거, 눈썹/반사광 추가. 원화 생성과 픽셀 수정은 ComfyUI; Python은 입력 마스크/색 준비·파일 처리. API/시드/실행 이력 보존.
- 제한: 아직 정면에 가깝고 팔 위치·몸 폭·외곽선 색의 차이가 남는다. 배 색면·고양이 수염·손발 선도 남아 있어 엄밀한 공통체형 제작 완료나 공통 본 호환 검증으로 표현하지 않는다. 파츠/리그/Unity 적용 미실행.
- 사람용 검토: Document/Human/Shared_Lineup_Review.html.


## 2026-11-05 — 머리/몸통 분리와 추가 참고 화풍

- 사용자 최신 요청: 같은 체형의 세 친구 방향은 좋지만 머리와 몸통을 나누고 머리를 더 크게 해 귀엽게. 추가 레퍼런스 3개를 통해 약간 흔들리는 짙은 손그림 외곽선·담백한 색면·큰 머리 아래 작은 몸통을 기준으로 명확히 했다.
- 향후 파츠는 head/body/arm_L/arm_R/leg_L/leg_R의 6개. 과거 머리 포함 body 5파츠 규칙보다 우선한다. 공통 본/애니메이션 재사용과 꼬리 없음은 유지.
- ComfyUI에서 추가 레퍼런스 구도 입력을 만들고 FLUX img2img로 새 시안 생성. ArtPipeline/ComfyUI/Examples/BigHead_Lineup_20261105/lineup_final.png. seed115630,denoise0.82,steps30,guidance3.5. 원화 생성·수정은 ComfyUI만 사용, Python은 입력 준비/파일 처리.
- 꼬리·잘못된 가나지 이마 줄무늬 제거, 윤곽 보완 및 작은 눈썹/반사광 편집 완료. 일부 볼터치/수염/배 색면/가나지 입 주변 무늬는 남은 검토 디테일이며 확정 사항이 아님.
- 새 비율 검토용이며 사용자 승인·Approved 교체·실제 6파츠 PNG/메시 분리·Unity 적용·공통 본 검증은 미실행. 세 종 머리 폭·팔 위치와 정면에 가까운 방향은 추가 조정 필요.
- 사람용: Document/Human/BigHead_Lineup_Review.html. 추가 레퍼런스 3개와 실제 API/이력/편집 입력 보존.


## 2026-11-05 — 큰 머리 디자인 방향 승인 및 공통 6파츠/애니메이션

- 사용자: 현재 큰 머리·작은 몸통 비율과 디자인 방향 승인, 자세한 디자인은 나중에 직접 수정. 공통 본 제작 진행 승인. 원본 승인 기록 Approved/SharedFamily/v1.
- ArtPipeline/Rigging/SharedCharacters6_20261105 및 ZIP: 3종×6개 실제 원화 PNG, 같은 본/피벗/몸통·팔다리 실루엣, 공통 Idle/Walk JSON·네이티브 .anim·단일 Controller·3 Prefab·unitypackage. 기존 Client 파일 교체 없음.
- 원본 실제 파츠를 ComfyUI에서 분리·배치. 고영이 몸통/팔다리 템플릿을 공통화하고 가나지/햄쥐는 색 합성. 숨겨진 팔 뒤는 기존 깨끗한 색면으로 복원. 생성 인페인트는 팔 선이 남아 미채택. 타원 팔다리 합성 없음.
- 별도 Unity6000.3.16f1에서18Sprite·3Prefab·공통Controller·198자세 샘플의 작성 위치/회전 일치와16개 실제 화면 캡처 확인. Y곡선만 쓸 때 X기본값이 초기화되는 문제를 고정 X/Z 곡선 명시로 수정.
- 현재는 rigid cutout Transform 본 방식, SpriteSkin/Spine 메시 아님. 작은 대기/걷기만 검증. 정면에 가까운 소스이며 측면/후면·점프/공격/큰 동작과 최종 관절 선 다듬기는 별도.
- 사람용 Document/Human/SharedCharacters6_Review.html. 게임에 적용 완료한 것으로 보고하지 않는다.


## 2026-11-05 — 푸들형 가나지 추가 후보

- 사용자 요청: 공통 리그 작업을 먼저 끝낸 뒤 첨부 강아지 사진을 참고해 가나지 한 마리 더 제작. 기존 가나지 교체 지시는 아님.
- 공통6파츠 작업 완료 후 사진 오른쪽 살구색 푸들 참고 후보를 ComfyUI로 생성. Examples/Ganaji_Poodle_20261105/preview.png 및 comparison.png. seed115801,denoise0.86,steps30,guidance3.5.
- 사진은 형상 참고 입력. 원화 생성·눈 축소·실제 머리 분리·공통 몸통/팔다리 색 합성과 최종 PNG 출력은 ComfyUI. Python은 입력 마스크 준비/파일검사/비교 배치.
- 6개768RGBA PNG와 동일 피벗 공유, 기존 가나지 유지. 새 후보 사용자 승인 및 Unity 재검증 전. 기본3종 검증 결과를 이 후보까지 실행한 것으로 확대해 말하지 않는다.
- 사람용 Document/Human/Ganaji_Poodle_Review.html.
