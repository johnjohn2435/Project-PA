# 고영이 외곽선 시안 — 2026-09-18

## 최신 사용자 기준
- 첨부한 고양이 예시처럼 간단해야 하며 외곽선이 반드시 있어야 한다.
- 굵은 갈색 외곽선, 작은 광택 없는 점눈, 단순한 입, 짧고 통통한 회갈색 고등어 고양이.
- 이전 시안은 여전히 너무 자세하다고 사용자 피드백. 기존 디자인은 승인되지 않았다.

## 실제 작업
- ComfyUI만 사용. OpenAI 이미지 생성 도구/API 미사용.
- 기존 02_refine에 사용자 이미지 입력하여 DreamShaper 2회 실행, 기존 01_character로 3회 추가 실행. 소품 잔존·세부 묘사·눈 형태 문제로 제외.
- 기존 설치된 flux1-dev-fp8.safetensors, clip_l.safetensors, t5xxl_fp16.safetensors, ae.safetensors를 확인해 재사용. 새 다운로드·패키지 설치 없음.
- 기존 그래프의 배경 제거 연결을 재사용하고 FLUX 로더/FluxGuidance를 구성해 2회 실행. 사용자 이미지는 스타일 분석 참고; 최종 FLUX 두 장은 텍스트 기반 생성.
- A seed 182901, B seed 182902. 768x768, Euler/simple, 20 steps, CFG 1, guidance 3.5.
- API/UI 그래프·history·raw/master/sprite_512 보존. master 알파 0~255 검사. 미세 경계와 최종 디자인 정리는 미완료.
- 파일: ArtPipeline/ComfyUI/Examples/Goyeongi_Outline_20260918.
- 사람용 문서: ../Human/Goyeongi_Outline_Review.html.
- 기존 4종 기본 워크플로우와 Client 미변경. 새 모델 후보의 배포용 사용조건은 이번 시안 제작에서 검증하지 않았으므로 최종 제작 채택 시 별도 확인할 것.
