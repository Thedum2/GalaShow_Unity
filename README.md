# GalaShow Unity

Unity **6.3 LTS / 6000.3.24f1** (`4e7b9b5b6244`)를 사용한다. 정확한 버전은 `ProjectSettings/ProjectVersion.txt`를 기준으로 하며 Unity Hub에서 같은 에디터의 **Web Build Support** 모듈을 설치한다.

작업 전 [공통 문서 안내](../docs/README.md)를 읽는다. 업그레이드 변경 및 검증 결과는 [Unity 전환 기록](../docs/unity-upgrade.md)에 있다.

## 프로젝트 열기

Unity Hub에서 이 디렉터리를 열고 패키지 복원과 스크립트 컴파일이 끝날 때까지 기다린다. `Packages/manifest.json`과 `Packages/packages-lock.json`을 함께 관리한다. Unity 6의 TextMesh Pro는 `com.unity.ugui` 2.0.0에 포함되어 있으므로 별도 `com.unity.textmeshpro` 패키지를 다시 추가하지 않는다.

## WebGL 빌드

- Build Profiles에서 **Web** 플랫폼을 선택한다. 스크립트와 CI의 타깃 이름은 `WebGL`이다.
- 활성 빌드 씬은 `Assets/Scenes/Tester.unity`다.
- Player Settings의 Publishing Settings에서 Compression Format은 **Disabled**, Name Files As Hashes는 꺼진 상태를 유지한다.
- 출력 폴더 이름을 `WebGL`로 지정해 `Build/WebGL.loader.js`, `WebGL.data`, `WebGL.framework.js`, `WebGL.wasm`을 만든다.

## CI/CD

PR과 `develop`/`main`/`master` push의 `Unity project checks`는 Unity 버전, 비압축·고정 파일명 설정, manifest/lockfile 일치, 다운로드된 Git LFS 파일을 검사한다. Unity 에디터 컴파일과 WebGL 생성은 별도의 수동 `Build Unity WebGL`에서 수행한다.

`Build Unity WebGL`은 `ProjectVersion.txt`의 에디터로 빌드하고, 위 네 파일이 모두 비어 있지 않은지 검사한 뒤 `Thedum2/GalaShow_UnityBuild`의 `develop`에 게시한다. 빌드 저장소의 `.gitattributes` 등 메타데이터를 보존하고 Git LFS로 바이너리를 게시한다. `build-info.json`에 소스 SHA·Unity 버전·실행 URL을 기록하며 Actions 산출물은 14일 보관한다. 같은 빌드 브랜치의 동시 게시를 직렬화한다.

Unity 저장소의 Actions secrets는 `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`, `GALASHOW_TOKEN`이다. 앞의 세 값은 [GameCI의 Personal 라이선스 활성화](https://game.ci/docs/github/activation/)에 사용한다. 로컬 에디터 빌드 성공과 GitHub의 라이선스 유효성은 별도로 확인해야 한다. `GALASHOW_TOKEN`에는 UnityBuild의 Contents 쓰기 권한이 필요하며 Client 연속 배포를 선택하면 Client의 Actions 쓰기 권한도 필요하다.

수동 실행 옵션:

- `commit_message`: 빌드 저장소의 커밋 메시지.
- `deploy_client`: 기본 `false`. 켜면 게시한 **정확한 빌드 커밋 SHA**를 Client의 `deploy.yml`에 넘긴다.
- `stage`: 연속 배포할 `dev` 또는 `prod`, 기본 `dev`.
- `client_ref`: 배포할 Client 브랜치/태그, 기본 `develop`. 새 `deploy.yml`이 이 ref와 Client 기본 브랜치에 있어야 한다.

Client 배포는 자체 Actions 실행에서 진행되므로 Unity 워크플로의 dispatch 성공만으로 웹 배포 성공을 판단하지 않는다. GitHub 실행 요약의 빌드 SHA와 Client Actions 결과를 확인한다.

Client는 평소 `build/unity` 서브모듈에 고정한 커밋을 사용한다. 새 기본 빌드는 해당 서브모듈을 갱신해 커밋한다. 또는 Client 수동 배포의 `unity_ref`에 빌드 SHA를 직접 지정하거나 위 연속 배포 옵션을 사용한다. 엔진 버전 파일만 바꾸면 기존 WebGL 바이너리는 갱신되지 않는다. Unity 저장소에는 AWS 배포 자격증명이 필요하지 않다.
