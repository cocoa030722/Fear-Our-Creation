# CLAUDE.md

Fear Our Creation: 탑뷰 2D 즉사 액션 게임 (Unity 6000.6.0f1, URP 2D). 응답과 주석은 한국어로 작성한다.

## 문서
- `기획.md`: 게임 기획서(규칙·수치의 원본). 기획이 바뀌면 여기부터 수정
- `개발계획.md`: 아키텍처, 마일스톤(M0~M9), 위험 요소, 기획 빈칸의 임시값
- `PROGRESS.md`: 마일스톤별 진척과 결정 사항. 마일스톤 작업을 끝낼 때마다 갱신

## 코드 규칙
- **수치 하드코딩 금지**: 기획서 '반복적 조절이 필요한 요소' 수치는 ScriptableObject(`Data/`)로 분리
- **거리 기준은 `Game.Core.GameConstants` 한 곳**: `PlayerDiameter`(1.0), `ScreenWidth`(카메라 Orthographic Size 5, 16:9에서 유도). 사거리·폭발 지름은 지름 배수, 어그로/시야는 화면 가로 배수로 기록하고 `FromPlayerDiameters`/`FromScreenWidths`로 환산
- **벽 규칙은 `Wall` 레이어 하나로 통일**: 이동·시야·투사체·폭발 차단 모두 동일. 레이캐스트 유틸은 한 곳에서만 사용. 레이어 인덱스/마스크는 `Game.Core.Layers`만 참조
- 피해는 `IDamageable.TakeHit(HitInfo)`로 통일 (M1에서 도입 예정)
- 색 규칙(`PlaceholderPalette`): 흰 배경 / 청록 오브젝트 / 붉은 적 / 노란 위험 표식(폭발물). 붉은색은 적 전용
- 어셈블리는 `Game.Runtime`, `Game.Editor` 두 개만 유지(과분할 금지). 네임스페이스는 폴더 기준 `Game.Core`, `Game.Player` 등
- 씬 리로드 + `PlayerLoadout` 주입 방식의 R키 재시작이 최우선 시스템 (개발계획 3.3)

## 폴더
`Assets/_Project/` 아래에 Art, Audio, Data, Prefabs, Scenes, Scripts(Core/Player/Weapons/Enemies/World/Cutscene/UI/Audio), Editor. 새 자산은 이 구조를 따른다.

## Unity 작업 방법
- 에디터가 열려 있으면 batch mode로 같은 프로젝트를 열 수 없다. 열린 에디터는 `unity command <명령>`(Pipeline)으로 제어한다
  - 스크립트 작성 후 `unity command recompile` → `recompile_status`로 컴파일 확인, `console`/`console_status`로 에러 확인
  - 반복 가능한 씬/에셋 구성은 `Editor/`의 메뉴 스크립트로 만들고 `unity command menu "<경로>"`로 실행 (예: `Tools/Fear/M0 Setup`, 여러 번 실행해도 안전하게 작성)
  - 화면 확인은 `capture_game_view`. `save_path`는 프로젝트 루트 기준이며 `Assets/` 아래에 저장되므로 확인 후 파일과 `.meta`를 지울 것
- 레이어/태그/Physics2D 설정은 `SerializedObject`나 Physics2D API로 바꾸고, 저장된 `ProjectSettings/*.asset` 결과를 확인
- `.gitattributes`는 Unity 템플릿(LFS 규칙 포함)이므로 덮어쓰지 말고 필요 시 추가만 한다
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`는 커밋 대상이 아니다. 커밋은 사용자가 요청할 때만 한다
