# PROGRESS

마지막 갱신: 2026-09-21 · 현재 단계: **M0 완료**, **M1 완료**(사용자 플레이 검증 통과), **M2 완료**(사용자 플레이 검증 통과), 다음은 M3

## 마일스톤 현황

| 마일스톤                 | 상태    |
| ------------------------ | ------- |
| M0 프로젝트 셋업         | 완료    |
| M1 이동·공격·즉사·재시작 | 완료    |
| M2 무기 시스템           | 완료    |
| M3 ~ M9                  | 미착수  |

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
- 씬: `Scenes/M0_Sandbox.unity`(Main Camera + Player). 빌드 설정의 씬 목록은 M1에서 M1_Sandbox, M0_Sandbox 순으로 바뀜

### 프로젝트 설정 변경

- 레이어 6~12: Wall, Player, Enemy, Projectile, Destructible, Pickup, SightBlocker
- Physics2D 중력 0 (탑뷰)
- 충돌 매트릭스(커스텀 레이어 기준)
  - Player: Wall, Enemy, Destructible
  - Enemy: Wall, Enemy, Destructible, Player, Projectile
  - Projectile: Wall, Destructible, Enemy, Player
  - Pickup, SightBlocker: 충돌 없음(오버랩/레이캐스트 전용)
  - 커스텀 레이어와 Default 레이어 간 충돌도 끔

## M2 결과 (완료: 체크리스트 6항목을 사용자가 플레이로 확인)

### 1단계: 무기 시스템 뼈대 + 근접 (완료)

- `WeaponData`(구 `MeleeWeaponData`, .meta 유지해 기존 Fist 에셋 보존): 5종 전체 필드(근접/탄약/투사체/폭탄/권총 소리/보스 피해량) 정의. 무기 SO 5종 + `WeaponCatalog`(`Data/Weapons/`)
- `WeaponHolder`(플레이어): 현재 무기+잔량, 스페이스 습득/교체/드롭, 합산 습득(상한 초과분은 바닥에 남김), 소진 시 주먹 복귀, `PlayerLoadout.Current` 항상 동기화, 시작 시 로드아웃에서 장착
- `WeaponPickup`(+프리팹 `Prefabs/Weapons/WeaponPickup.prefab`): 픽업 색/라벨, 드롭 시 잔량 유지
- `MeleeAttack`(구 `FistAttack`): 부채꼴 각도 0이면 직선 상자, 0 초과면 부채꼴. 부채꼴 범위 표시는 코드로 생성한 스프라이트
- `Tools/Fear/M2 Setup` → `M2_Sandbox`(가시창 픽업 2개, 더미 4개, 벽). `M1 Setup`도 같은 플레이어 리그를 공유하며 M1_Sandbox를 재생성. 빌드 목록 M2, M1, M0 순
- 입력: `GameInput.Interact`(Space). 좌상단 디버그 HUD(OnGUI)에 무기/잔량 표시
- 임시값(기획 빈칸, SO에서 조정): 가시창 부채꼴 120도, 투사체 속도 20(총알 40) 지름/초, 투척 가시 간격 0.3초. SO는 id가 빈 경우에만 초기화하므로 조정한 수치는 덮어쓰지 않음

### 2단계: 원거리 무기 (완료)

- `RangedAttack`(플레이어): 발사 간격/탄약 소모, 조준 방향 투사체 생성. 권총은 발사 시 `SoundEventBus.Publish`(수신자는 M3)
- `Projectile`(+프리팹): 매 프레임 이동 구간 레이캐스트(터널링 방지), 벽/적 명중 처리
  - 투척 가시: 명중 시 50%, 빗나감(벽/맵 끝) 시 100% 확률로 회수용 픽업 생성. 회수 픽업은 같은 무기를 들고 있을 때 위를 지나가면 자동 습득(`WeaponPickup.MakeAutoRecover`), 스페이스 교체와 분리
  - 폭탄알: 적/벽 접촉 시 폭발(`ExplosionEffect.Detonate`, 지름 4배, 벽 뒤 대상 제외, 플레이어 면역). 벽 폭발은 벽 앞으로 0.1 물려서 판정(WallQuery가 시작점이 벽 안이면 전부 막기 때문)
  - 권총: 좌클릭 누르고 있으면 연사(초당 8발), 탄이 벽/적에 닿으면 소멸
- `SoundEventBus`(Core): `SoundEvent(위치, 반경, 지속)` 정적 버스
- `WeaponHolder.HandleAttackInput(pressed, held)`: 권총만 held, 나머지는 pressed
- M2_Sandbox에 투척 가시/폭탄알/권총 픽업 각 2개, 먼 더미 무리(폭발 범위 확인), 옆 벽 추가

### 검증 체크리스트 (M2_Sandbox, R로 재시작 포함 — 전부 통과)

1. 가시창 부채꼴 범위 표시와 판정 일치, 벽 뒤 더미 불사
2. 스페이스: 습득, 교체 시 이전 무기 그 자리에 드롭(잔량 유지), 투척 가시 합산(상한 12, 초과분 바닥에 남음)
3. 투척 가시 소진 → 주먹 복귀, 빗나간 가시 위를 지나가면 회수
4. 폭탄알: 더미 무리 폭발, 벽 뒤 더미 무사, 플레이어 무사, 상한 2
5. 권총: 연사, 16발 소진 후 주먹 복귀(빈 총 사라짐)
6. 무기 든 채 K(사망) → R: 진입 시점의 무기/탄약으로 복원

## 결정 사항 / 메모

- 패키지(Input System 1.20, Cinemachine 6.6, uGUI/TMP, 2D Tilemap)와 Git/LFS(.gitattributes 템플릿)는 프로젝트에 이미 있어 M0에서 추가하지 않음
- Cinemachine 추적은 M1에서 적용(`M1_Sandbox`의 `CM Player Follow`, 댐핑 0, 크기 5 고정). M0_Sandbox는 고정 카메라 그대로
- 기존 `Assets/Scenes/SampleScene`은 삭제하지 않고 빌드 목록에서만 제외
- 플레이어 색은 흰 배경 대비를 위해 짙은 슬레이트(#2B3A42)로 임의 지정(기획에 규정 없음)
- 캡처의 상하 검은 띠는 캡처 창 비율에 의한 레터박스이며 결함이 아님
- 에디터가 자동 생성한 변경: `ProjectSettings.asset`의 `APP_UI_EDITOR_ONLY` 정의, `SceneTemplateSettings.json`

## M1 결과 (M2에서 `MeleeWeaponData`→`WeaponData`, `FistAttack`→`MeleeAttack`, `FistRange`→`MeleeRange`로 이름 변경됨. 아래는 M1 당시 기록) (완료: 이동·조준·주먹 처치·벽 차단·K 즉사·R 재시작을 사용자가 플레이로 확인)

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

## 시도했으나 실패한 것 / 함정

- `unity command`가 "No Unity Editor instances found"로 실패하면 에디터가 꺼진 것일 수 있다(`Get-Process`로 Unity 프로세스 확인). 꺼져 있으면 batch mode, 켜져 있으면 batch mode는 "다른 에디터에서 열림"으로 실패하므로 `unity command`를 쓴다
- `unity command console`은 이전 실행의 에러도 남아 있으니 `timestampUtc`/스택 시그니처로 최신 여부를 확인한다
- 프로젝트 루트에서 `grep -r`는 `Library/`까지 훑어 2분 넘게 걸린다. Grep 도구에 `glob`을 주거나 `docs/`, `Assets/_Project/`로 범위를 좁힌다

## 다음 세션 시작 가이드

- 현재 브랜치 main, M1·M2 커밋은 로컬만 있고 원격 푸시 전
- 시작 시 할 일: 개발계획 M3 절과 `docs/기획.md`의 적 항목·수치 표를 읽는다
- 씬/에셋 구성은 `Editor/`의 메뉴 스크립트(`M0 Setup`, `M1 Setup`, `M2 Setup`)가 재생성하므로, M3도 `M3 Setup`을 만들어 씬 YAML을 직접 편집하지 않는다. 플레이어 리그와 무기 에셋은 `M2Setup`이 담당하고 `M1 Setup`이 이를 호출한다

## 다음 단계

- M3(적 AI 4종 + 벽 + 1~2스테이지) 착수. `DummyEnemy`를 `EnemyBase`/AI로 대체, `SoundEventBus.Emitted` 구독(권총 어그로)
- K 디버그 키는 적 공격이 들어오는 M3에서 제거

## 미해결 / 확인 필요

- 개발계획 6절의 기획 빈칸 8건은 아직 미확정(임시값 사용 예정)
- M0 커밋 `13bd181`(원격 푸시 완료). M1은 로컬 커밋, 푸시는 미실시
- `ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json`은 에디터가 자동 수정한 변경이라 M1 커밋에서 제외
- `.claude/`(세션 명령어·로컬 설정)와 `메모.txt`(사용자 메모)는 커밋 대상에서 제외
