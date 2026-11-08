# Project PA 파츠 자세 편집용 ComfyUI

기존 그림의 각도를 보존 편집하는 로컬 노드 `PAPoseRotate`입니다. 기존 설치의 Pillow, NumPy, PyTorch만 사용하며 추가 모델·외부 API는 없습니다. 입력 마스크는 ComfyUI의 역알파 규약입니다.

기본 서버 8188과 분리해 기존 ComfyUI main.py를 `--base-directory "D:/Project PA/ArtPipeline/ComfyUI/PoseRuntime" --listen 127.0.0.1 --port 8189 --cpu --disable-auto-launch --disable-api-nodes`로 실행합니다. `custom_nodes/pa_pose`를 읽기 위해 `--disable-all-custom-nodes`를 지정하지 않습니다.

푸들 워크플로우: `../../Rigging/Poodle_20261106/PoseMatch/pose_api.json`. `generate.py`가 입력 파츠 업로드, 선택 마스크 제작, ComfyUI 실행과 결과 저장을 담당합니다. 그림의 회전·합성은 ComfyUI 노드에서 실행합니다. 승인 원본은 보존합니다.
