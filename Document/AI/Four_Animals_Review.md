# 캐릭터 4종 첫 시안 — 2026-11-04

- 사용자 요청: 확정 고영이 디자인풍으로 강아지·수달·햄스터·다람쥐 제작.
- 기존 FLUX 768x768 / 20 steps / Euler simple / CFG1 / guidance3.5. seed110410~110413. ComfyUI에서 생성 후 BiRefNet 전경 마스크 GrowMask11px, #24211f 외곽선 합성.
- 결과: ArtPipeline/ComfyUI/Examples/FourAnimals_20261104. 생성 그래프는 source_generation_api.json, 외곽선/4색 축약 그래프는 api.json, source_generation_workflow.json은 생성 단계만 표현한다.
- 알파 0~255 확인. 4종의 구체적 디자인은 승인 전. 첫 4종은 명암이 과해 제외하고 4종 재생성. ComfyUI ImageQuantize 4색으로 단순화. 귀/볼/몸 비율 등 종별 표현은 추가 검토 대상.
- Python은 비교판 레이아웃/배경 합성에만 사용. 원화 생성·외곽선 처리는 ComfyUI. Unity 미변경.
- 사람용 문서: ../Human/Four_Animals_Review.html.


## 2026-11-04 — 단순한 형태와 약한 명암·색 차이 유지

- 사용자 피드백: 고영이에는 그림자와 색 차이가 있었는데 새 4종은 너무 단순하다.
- 형태·외곽선·얼굴은 단순하게 유지하되 고영이 수준의 은은한 그림자와 부위별 포인트색은 남긴다. 무조건 2~4색으로 축약하지 않는다.
- 새 4종은 ComfyUI에서 색상 축약을 4색에서 16색으로 완화해 수정했다. 이는 이번 수정 설정이며 공통 색상 수 제한이 아니다.
- 결과: Examples/FourAnimals_20261104/ColorRevision. 사람용 안내: ../Human/Four_Animals_Color_Review.html. 신규 디자인은 승인 전, Client 미변경.


## 다람쥐 표정 수정 (2026-11-04)

- 사용자가 다람쥐 표정이 어둡다고 피드백. ComfyUI에서 입 영역 x301 y433 82x26만 세로 반전·합성해 웃는 입으로 수정했다. 생성 재실행 없음.
- 기존 16색 명암·외곽선 유지. SquirrelSmile 폴더에 API/history/PNG와 최신 4종 비교판 보존. 사람용 Four_Animals_Smile_Review.html.


## 2026-11-04 — 강아지·햄스터 디자인 확정, 수달·다람쥐 제외

- 사용자 결정: “다람쥐 수달 빼고 햄스터랑 강아지만 픽스하자”.
- 강아지와 햄스터는 ColorRevision의 색·명암 복원본으로 확정. Approved/Dog/v1, Approved/Hamster/v1에 원본·투명 PNG·정확한 생성/후처리 설정·실행 기록·승인 해시 보존.
- 기존 확정 고영이는 유지. 현재 확정 원화는 고영이·강아지·햄스터 3종. 강아지·햄스터의 고유 이름은 아직 미정이다.
- 수달·다람쥐는 현재 제작 대상에서 제외. 기존 후보 파일은 이력으로 보존한다.
- 승인 목록: ArtPipeline/ComfyUI/Approved/lineup.json. Unity 카탈로그·표시명·리소스 연결 변경 없음.
