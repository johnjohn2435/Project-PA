# 고영이 추가 단순화 — 2026-11-04

- 최신 사용자 요청: 첨부한 게임 이미지처럼 기존 B보다 더 간단하게. 이 요청이 이전 B 형태 고정 규칙보다 우선한다.
- 이름과 고등어 고양이 설정은 유지. 새 시안은 미승인. 이전 승인 원본은 이력으로 보존.
- 목표: 매우 굵은 짙은 외곽선, 점눈과 단순한 입, 적은 색면·무늬, 세부 묘사 최소화.
- 실제 제작: 꺼진 로컬 ComfyUI를 기존 실행 구성으로 시작. 기존 05_character_flux로 6장 생성. 기존 FLUX, 768x768, steps 20, CFG 1, guidance 3.5, Euler/simple 재사용. 새 모델 설치·OpenAI 이미지 생성 API 사용 없음.
- F(seed 110406)의 둥근 형태를 선택해 ComfyUI에서 BiRefNet 전경 마스크를 GrowMask 11px 확장하고 #24211f 외곽선을 합성. 이는 에이전트의 제시안 선택이며 사용자 승인 아님.
- 새 시안에서는 볼터치·독립적인 배 무늬·수염·손발 세부선을 생략. 이마와 꼬리 무늬를 축약. 입은 생성 결과의 작은 고양이 입이 남았으며 일자 입 목표와 다름. 원본의 미세한 톤 변화는 남아 있어 엄격한 단색화 완료로 보지 않는다.
- 산출물: ArtPipeline/ComfyUI/Examples/Goyeongi_Minimal_20261104. F는 생성 원본과 API/UI/실행 기록, Outlined는 ComfyUI 후처리 결과와 API/실행 기록. outline.py는 재실행 스크립트.
- Python은 검토용 배경 합성에만 사용. 원화 생성·외곽선 처리는 ComfyUI.
- RGBA 768x768, 알파 0~255 및 바깥 모서리 투명 확인. 배경 합성 시 외곽선 시각 확인, 저장본 해시 일치. 최종 마스크·게임 가독성 검증과 Unity 적용 미실행.
- 사람용 문서: ../Human/Goyeongi_Minimal_Review.html.


## 후속 승인 및 수정 완료

같은 날 사용자가 이 디자인풍을 승인하고 고영이의 꼬리 제거를 요청했다. 위 미승인 상태는 과거 탐색 기록이며 현재 기준은 Approved/Goyeongi/v2/master.png다. Approved_Character_Style.md 참조.
