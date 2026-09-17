# Pocket Arcade 원화 제작 워크플로우

작업 파일의 기준 경로: `ArtPipeline/ComfyUI/`.

## 목표와 파일

'고양이 스낵바'에서 참고할 것은 둥근 비율, 작은 얼굴 요소, 부드러운 파스텔, 화면이 작아도 읽히는 단순한 실루엣이다. 캐릭터·소품·UI는 Pocket Arcade의 별도 디자인으로 만든다.

| 파일 | 용도 | 결과 |
| --- | --- | --- |
| Workflows/01_character.json | 텍스트로 캐릭터 후보 생성 | 1024 원본, 1024 RGBA, 512 RGBA |
| Workflows/02_refine.json | 채택한 원화를 바탕으로 작은 수정 | 같은 크기의 원본·RGBA |
| Workflows/03_cutout.json | 이미 만든 원화의 배경 제거만 재실행 | 1024 또는 입력 크기 RGBA, 512 RGBA |
| Workflows/04_background.json | 세로 게임 배경 콘셉트 | 768×1344 RGB 원본 |
| prompt_presets.json | 모모·보리·나비 및 소품 프롬프트 | 텍스트 프리셋 |
| Api/ | 로컬 API 자동 실행용 그래프 | UI에는 Workflows 파일을 가져온다 |

실제 생성 검수에서 입체감이 강한 초기 프롬프트를 평면 일러스트 가중치로 조정했다. 기본 노드만 사용한다. 설치된 ComfyUI 0.33.0의 BiRefNet 기본 노드가 필요하다. 구버전에서 빨간 노드가 생기면 임의의 이름이 같은 커스텀 노드를 연결하지 말고 기본 노드 지원 버전을 확인한다.

## 이 PC에서 사용하기

1. 기존 ComfyUI를 실행한다. 검증용 로컬 서버가 켜져 있으면 http://127.0.0.1:8188 을 사용할 수 있다.
2. Workflows의 JSON을 ComfyUI 화면으로 끌어 넣는다. C:\ComfyUI\user\default\workflows\ProjectPA에도 복사해 두므로 저장된 워크플로우에서 열 수 있다.
3. Checkpoint Loader에서 DreamShaperXL_Turbo_v2_1.safetensors, 배경 제거 모델에서 birefnet.safetensors를 선택한다.
4. Positive에서 캐릭터 설명을 수정하고 Run을 누른다. 처음에는 01_character부터 사용한다.
5. 채택한 원본을 02_refine의 Load Image에 넣고 작은 표정·의상 수정을 진행한다. 투명 PNG보다는 01의 흰 배경 원본을 입력한다.
6. 결과는 C:\ComfyUI\output\ProjectPA 아래에 저장된다. 프로젝트의 ArtPipeline/ComfyUI/Examples에는 확인용 결과만 보관한다.
7. 최종 승인 전까지 생성 결과를 Unity 더미 PNG에 덮어쓰지 않는다.

모델은 공유 폴더에 설치된다.

- C:\Users\wsuk2\ComfyUI-Shared\models\checkpoints\DreamShaperXL_Turbo_v2_1.safetensors
- C:\Users\wsuk2\ComfyUI-Shared\models\background_removal\birefnet.safetensors

모델 파일 자체는 Git에 넣지 않는다. 다운로드 원본·고정 리비전·크기·SHA-256은 models.lock.json에 기록한다. 기존 ComfyUI 소스, 기존 모델 및 패키지는 교체하지 않는다.

## 시작 설정

RTX 3080 10GB에서 배치 1을 기준으로 구성했다. 실제 생성 결과와 소요 시간은 ComfyUI_Validation.md에서 확인한다.

| 항목 | 캐릭터 후보 | 작은 수정 | 배경 콘셉트 |
| --- | --- | --- | --- |
| 해상도 | 1024×1024 | 1024×1024 정사각 입력 | 768×1344 |
| Steps | 10 | 12 | 10 |
| CFG | 2.5 | 2.5 | 2.5 |
| Sampler | dpmpp_sde | dpmpp_sde | dpmpp_sde |
| Scheduler | karras | karras | karras |
| Denoise | 1.0 | 0.30 | 1.0 |
| Batch | 1 | 1 | 1 |

이 설정은 Turbo 체크포인트용이다. 일반 SDXL 모델에 그대로 적용하지 않는다. 수정용 denoise는 0.20~0.35에서 시작하고, 실루엣까지 바꿀 때만 0.4 이상을 시도한다. 낮은 denoise라도 동일 캐릭터를 완벽하게 보장하지는 않는다.

VRAM 부족 시 다른 GPU 작업을 닫고, 캐릭터를 768×768로 낮추거나 생성 원본만 먼저 저장한 뒤 03_cutout에서 따로 배경을 제거한다. Refiner, 추가 LoRA, 4배 업스케일은 기본 구성에 넣지 않았다.

## 아트 방향

- 실루엣: 머리와 몸을 큰 둥근 덩어리 2개 정도로 읽히게 구성.
- 외곽선: 짙은 갈색, 일정한 두께. 작은 털·금속 질감·복잡한 주름을 줄임.
- 색: 크림 바탕, 코랄 #F5A69B, 민트 #B9DEC9, 라벤더 #C6B8E8, 선색 #66534C.
- 명암: 기본색과 그림자색 1개 중심. 강한 광택·실사 털·3D 조명 제외.
- 캐릭터 시점: 정면, 전신, 발끝 포함. 카메라·크기·발 기준선을 고정.
- 소품 시점: 정면 또는 약한 3/4 중 하나를 품목별로 고정.
- 배경: 단순한 큰 면과 빈 공간 우선. 버튼·글자·숫자·UI를 이미지에 그리지 않음.
- 작은 아이콘: 64px·128px로 축소해 눈·귀·색 구분이 유지되는지 확인.

프롬프트는 대상·색상 문장 + 스타일·구도 순으로 사용한다. 색상 누락을 줄이기 위해 대상 설명을 앞쪽에 배치했다. 게임 제목 대신 위 시각적 특성을 적어 반복 생성 시 조절하기 쉽게 했다. 프롬프트의 종·장식은 탐색용 제안이며 최종 캐릭터 설정이 아니다.

## 일관성을 유지하는 제작 순서

1. 한 캐릭터로 seed만 바꿔 후보를 4~8장 생성한다. 배치 1로 순차 실행한다.
2. 한 장을 승인 원화로 고른다. 모델·seed·프롬프트·샘플링 설정을 함께 보관한다.
3. 02_refine으로 원화를 재입력하고 한 번에 한 요소만 변경한다.
4. 나머지 캐릭터는 같은 스타일 문장과 구도를 유지하고 색·귀·장식만 구분한다.
5. 채택본은 모양·외곽선·팔레트를 사람이 정리한 후 게임에 넣는다.
6. 옆·뒤·동작 프레임은 이 워크플로우만으로 자동 정합되지 않는다. 원화 확정 후 수작업 리깅/프레임 정리 또는 별도 포즈 제어 워크플로우를 구성한다.

seed 고정은 재현에 도움을 주지만 프롬프트가 바뀐 캐릭터의 정체성을 고정하는 기능은 아니다. 이 구성은 원화·단일 스프라이트 제작용이며 자동 스프라이트 시트 생성기는 아니다.

## 투명 배경 처리

워크플로우 연결은 다음과 같다.

원본 → BiRefNet의 전경 마스크 → InvertMask → JoinImageWithAlpha → RGBA PNG 저장

설치된 JoinImageWithAlpha는 입력 마스크를 반전해 알파로 사용하므로, BiRefNet 전경 마스크를 먼저 반전하는 연결이 필요하다. 누락하면 캐릭터가 투명해지고 배경이 남는다.

흰 배경이나 'transparent background' 프롬프트만으로는 투명 PNG가 되지 않는다. 저장 결과의 RGBA 모드, 알파값 범위, 검은/흰 배경 위에서의 외곽선을 확인한다. 귀·꼬리·흰색 몸통 일부가 빠지면 마스크를 수동 보정한다. 512 출력은 리사이즈한 미리보기이며, 발 기준선과 캐릭터 점유율을 자동으로 맞춰 주지는 않는다.

## Unity 더미 및 최종 교체

현재 더미는 Client/Assets/_Project/Art/Characters/Dummy의 256×256 투명 PNG 3개다. 모모/보리/나비 ID는 유지하며, 아직 캐릭터 선택 UI와 연결하지 않았다.

더미에는 전용 임포터가 적용된다: Sprite (2D and UI), Single, PPU 256, Bottom Center, mipmap 끔, Bilinear, 압축 없음. 이 규칙은 Dummy 폴더의 *_dummy.png에만 적용된다.

최종 원화는 별도 Final 폴더에 넣고 512/1024 해상도, 동일 캔버스 점유율, Bottom Center 피벗을 맞춘다. 텍스처 해상도가 달라지면 PPU도 조정해 월드 크기를 유지한다. 개발 중에는 원본을 보존하고, 모바일 압축과 Atlas 구성은 실제 기기 품질 확인 후 설정한다.

## 모델 출처

- [DreamShaper XL Turbo 모델 카드](https://huggingface.co/Lykon/dreamshaper-xl-v2-turbo)
- [ComfyUI용 BiRefNet](https://huggingface.co/Comfy-Org/BiRefNet)
- [BiRefNet 원본](https://huggingface.co/ZhengPeng7/BiRefNet)
- [공식 SDXL 예제](https://comfyanonymous.github.io/ComfyUI_examples/sdxl/)
- [공식 Img2Img 예제](https://comfyanonymous.github.io/ComfyUI_examples/img2img/)
- [공식 배경 제거 안내](https://docs.comfy.org/tutorials/utility/remove-background-birefnet)
- [ComfyUI 워크플로우 스키마](https://github.com/Comfy-Org/docs/blob/main/specs/workflow_json_0.4.mdx)
- [시각적 참고: 고양이 스낵바](https://play.google.com/store/apps/details?id=com.tree.idle.catsnackbar)

모델의 라이선스 원문은 위 모델 카드에서 확인할 수 있다. 생성 이미지 품질과 게임 적용 여부는 별도 검수한다.
