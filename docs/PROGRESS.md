# PROGRESS

마지막 갱신: 2026-09-21 · 현재 단계: **M0 완료**, **M1 완료**(사용자 플레이 검증 통과), 다음은 M2

## 마일스톤 현황

| 마일스톤                 | 상태            |
| ------------------------ | --------------- |
| M0 프로젝트 셋업         | 완료            |
| M1 이동·공격·즉사·재시작 | 완료            |
| M2 무기 시스템           | 미착수 (다음)   |
| M3 ~ M9                  | 미착수          |

## M0 결과 (완료 기준 충족: 빈 씬에서 플레이어(원)가 카메라 안에 표시됨)

Game 뷰 캡처로 확인. 컴파일 에러 없음. 플레이어 지름이 화면 높이 10유닛의 1/10로 표시됨.

### 만든 것

- 폴더 구조: `Assets/_Project/` 하위 전체(개발계획 2절)
- 어셈블리: `Game.Runtime`(`Scripts/`), `Game.Editor`(`Editor/`)
- `Scripts/Core/GameConstants.cs`: `PlayerDiameter`=1, `CameraOrthographicSize`=5, `TargetAspect`=16:9, `ScreenWidth`≈17.78, 환산 헬퍼
- `Scripts/Core/Layers.cs`: 레이어 이름/인덱스/마스크
- `Scripts/Core/PlaceholderPalette.cs`: 색 규칙
- `Scripts/Core/FixedCamera.cs`: Orthographic Size 고정, 16:9 레터박스
- `Editor/M0ProjectSetup.cs`: 메뉴 `Tools/Fear/M0 Setup`. 폴더·레이어·충돌 매트릭스·스프라이트·머티리얼·씬 생성(재실행 안전)
- 에셋: `Art/Sprites/Circle.png`, `Square.png`(256px, 256PPU), `Art/Materials/SpriteUnlit.mat`
- 씬: `Scenes/M0_Sandbox.unity`(Main Camera + Player). 빌드 설정의 씬 목록은 이 씬 하나

### 프로젝트 설정 변경

- 레이어 6~12: Wall, Player, Enemy, Projectile, Destructible, Pickup, SightBlocker
- Physics2D 중력 0 (탑뷰)
- 충돌 매트릭스(커스텀 레이어 기준)
  - Player: Wall, Enemy, Destructible
  - Enemy: Wall, Enemy, Destructible, Player, Projectile
  - Projectile: Wall, Destructible, Enemy, Player
  - Pickup, SightBlocker: 충돌 없음(오버랩/레이캐스트 전용)
  - 커스텀 레이어와 Default 레이어 간 충돌도 끔

## 결정 사항 / 메모

- 패키지(Input System 1.20, Cinemachine 6.6, uGUI/TMP, 2D Tilemap)와 Git/LFS(.gitattributes 템플릿)는 프로젝트에 이미 있어 M0에서 추가하지 않음
- Cinemachine 추적은 M1에서 적용(`M1_Sandbox`의 `CM Player Follow`, 댐핑 0, 크기 5 고정). M0_Sandbox는 고정 카메라 그대로
- 기존 `Assets/Scenes/SampleScene`은 삭제하지 않고 빌드 목록에서만 제외
- 플레이어 색은 흰 배경 대비를 위해 짙은 슬레이트(#2B3A42)로 임의 지정(기획에 규정 없음)
- 캡처의 상하 검은 띠는 캡처 창 비율에 의한 레터박스이며 결함이 아님
- 에디터가 자동 생성한 변경: `ProjectSettings.asset`의 `APP_UI_EDITOR_ONLY` 정의, `SceneTemplateSettings.json`

## M1 결과 (완료: 이동·조준·주먹 처치·벽 차단·K 즉사·R 재시작을 사용자가 플레이로 확인)

### 만든 것
- `Core/`: `IDamageable`+`HitInfo`(출처/종류/폭탄알), `WallQuery`(벽 차단 판정 단일 진입점), `GameInput`(InputAction: WASD/마우스/좌클릭/R/K), `PlayerLoadout`, `SnapshotSystem`(Begin/Capture/Restore/RequestRestart, IsRestart), `RestartController`(씬 시작 시 Begin, R키 즉시 씬 리로드)
- `Player/`: `PlayerConfig`(SO, 이동속도 5), `PlayerController`(이동·마우스 조준·공격 입력), `PlayerHealth`(1회 피격 즉사, 임시 K키 즉사)
- `Weapons/`: `MeleeWeaponData`(SO, 주먹 수치), `FistAttack`(클릭 → 0.1초 뒤 판정 재계산, 간격 0.5초, 벽 뒤 대상 제외)
- `Enemies/DummyEnemy`: 정지, 1회 피격 즉사(색 변경 + 충돌 제거)
- `Editor/M1Setup.cs`: 메뉴 `Tools/Fear/M1 Setup`. SO 에셋(`Data/PlayerConfig.asset`, `Data/Weapons/Fist.asset`)과 `Scenes/M1_Sandbox.unity`(Cinemachine 추적, 더미 3개, 벽 1개) 생성. 빌드 목록은 M1, M0 순
- 공격 범위 표시: `FistAttack.rangeIndicator`(플레이어 자식 `FistRange`, 슬레이트 알파 0.3). 클릭 시점~판정 후 0.1초 표시, 크기/위치는 SO 수치에서 계산해 판정 상자와 일치. `alwaysShowIndicator` 체크 시 상시 표시
- 어셈블리 참조: `Game.Runtime`→`Unity.InputSystem`, `Game.Editor`→`Unity.Cinemachine`

### 검증 (M1_Sandbox 재생, 사용자 확인 완료)
WASD 이동, 마우스 조준, 좌클릭으로 더미 처치, 벽 뒤 더미는 벽에 붙어 쳐도 안 죽음, K로 플레이어 즉사, R로 즉시 재시작. 공격 범위 표시는 추가 후 사용자 재확인은 미실시

## 결정 사항 (M1)
- 주먹 사거리 1배는 **몸 가장자리 기준**으로 해석, 판정 폭은 기획에 없어 임시 0.5배(SO에서 조정)
- 입력은 `.inputactions` 에셋 대신 코드로 InputAction 정의(에셋 YAML 수작업 회피, 리바인딩은 이후 필요할 때 전환)
- R키는 사망 여부와 무관하게 언제나 재시작 가능
- 재시작 상태는 정적 `SnapshotSystem`이 보관하고 `SubsystemRegistration`에서 초기화(도메인 리로드 비활성 대비)
- `EditorSceneManager.NewScene(Single)`은 참조 없이 로드된 SO를 언로드해 fake-null로 만든다 → 에셋 로드는 NewScene 이후에 하고, SetField는 null이면 에러를 로그. (실패 사례: 처음 M1 Setup에서 `PlayerController.config`/`FistAttack.data`가 씬에 null로 저장돼 이동·공격 불가, 콘솔 NRE `PlayerController.cs:48`)
- 이번 세션 초반엔 에디터가 꺼져 있어 `Unity.exe -batchmode -executeMethod`로 컴파일/셋업 실행

## 다음 단계
- M2(무기 시스템) 착수
- M2에서 `MeleeWeaponData`를 `WeaponData`로 확장/통합, `PlayerLoadout`에 무기 ID/잔량 채우기
- K 디버그 키는 적 공격이 들어오는 M3 이후 제거

## 미해결 / 확인 필요

- 개발계획 6절의 기획 빈칸 8건은 아직 미확정(임시값 사용 예정)
- M0 커밋 `13bd181`(원격 푸시 완료). M1은 로컬 커밋, 푸시는 미실시
- `ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json`은 에디터가 자동 수정한 변경이라 M1 커밋에서 제외
- `.claude/`(세션 명령어·로컬 설정)와 `메모.txt`(사용자 메모)는 커밋 대상에서 제외
