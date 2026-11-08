# Unity 클라이언트 진입점

- 이 폴더는 `Assets/`, `Packages/`, `ProjectSettings/`가 있는 Unity 프로젝트 루트다.
- 작업 전 상위 [AGENTS.md](../AGENTS.md), [ARCHITECTURE.md](../ARCHITECTURE.md), 해당 [기능 계약](../Agent_Instruction/README.md)을 읽는다. 상위 규칙을 여기 복제하지 않는다.
- Unity 명령의 프로젝트 인자는 이 폴더, Git 명령의 기준은 상위 저장소다. 문서의 `Client/Assets/...` 경로는 저장소 기준이다.
- 에디터 버전은 [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt), 패키지는 [manifest.json](Packages/manifest.json)과 [packages-lock.json](Packages/packages-lock.json)이 원본이다. 지침 적용만을 이유로 버전을 올리지 않는다.
- 신규 에이전트 계약·사람용 문서는 Assets 안에 만들지 않고 상위 문서 구조를 따른다.
