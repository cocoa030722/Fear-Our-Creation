# PROGRESS

마지막 갱신: 2026-09-21 · 현재 단계: **M0~M2 완료**, **M3 진행 중**(3a 적 기반+창병, 3b 투척병·척탄병·노획 코드 완료, 사용자 플레이 검증 대기)

## 마일스톤 현황

| 마일스톤                 | 상태    |
| ------------------------ | ------- |
| M0 프로젝트 셋업         | 완료    |
| M1 이동·공격·즉사·재시작 | 완료    |
| M2 무기 시스템           | 완료    |
| M3 적 AI + 스테이지 1~2  | 진행 중 (3a·3b 코드 완료) |
| M4 ~ M9                  | 미착수  |

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

## M3 진행 (4단계로 나눠 커밋: 3a 적 기반+창병 / 3b 투척병·척탄병·노획 / 3c 벽 프리팹+1스테이지 / 3d 2스테이지+재미 검증)

### 3a: 적 기반 + 가시 창병 (코드 완료, 사용자 플레이 검증 대기)
- `EnemyData`(SO, `Data/Enemies/SpearGrunt.asset`): 이동속도 배수(0.9), 시야 90도·화면 가로 0.5배, 공격 딜레이 0.2/간격 1.0, 사용 무기(가시창 = 공격 모양), 노획 무기. 기획 표 수치 그대로, 없는 값(순찰 속도 0.4배, 회전 720도/초, 경계 유지 3초, 몸 지름 1배)은 임시값
- `EnemyBase`(즉사, 시신 충돌 제거, 노획 드롭, 적끼리 피해 무시), `Perception`(시야 부채꼴 + 벽/`SightBlocker` 차단, 소리 구독은 벽 차단 없음), `EnemyMeleeAttack`(딜레이 후 판정 재계산), `EnemyAI`(Idle/순찰 → Alert → Chase → Attack → Return, 머리 위 상태 라벨)
- `MeleeHit`(Weapons): 플레이어와 적이 공용으로 쓰는 근접 판정(상자/부채꼴 + 벽 차단). `MeleeAttack`이 이를 사용하도록 리팩터
- `WallQuery`에 `IsSightBlocked`, `IsBlockedCircle`, `Overlaps` 추가(벽 질의는 여전히 이 클래스 한 곳)
- `NavGrid`(World): 8방향 그리드 A* 자체 구현(칸 0.5, 이동체 반지름 0.4), 씬 시작 후 첫 요청 때 굽기, 경로는 벽에 안 걸리는 구간을 직선으로 다듬음. 막히지 않은 직선이면 경로 없이 직진
- K 디버그 키(`GameInput.DebugKill`, `PlayerHealth`의 K 처리) 제거: 적 공격으로 사망하므로 R 재시작 검증은 적에게 죽어서 한다
- `Tools/Fear/M3 Setup` → `M3_Sandbox`(경계 벽, 기둥, 선반 벽, 창병 4명: 정면 경비/순찰/등 돌린 경비/소리 테스트, 권총·가시창 픽업). 빌드 목록 M3, M2, M1, M0 순
- 자동 스모크 테스트로 확인한 것(플레이 모드, `unity command`): 소리 → Alert → 발사 지점으로 이동, 기둥을 돌아가는 경로, 시야로 발견 → 추격 → 공격 → 플레이어 사망, 등 뒤 접근 시 미발각 + 주먹 처치 + 가시창 노획, 경계 해제 후 원위치·원래 방향 복귀
- 미확인(사람이 플레이해서 볼 것): 조작감/난이도(공격 딜레이 0.2초에 피할 수 있는지), 순찰, 시야 가림(선반 벽 뒤), 적 여럿이 몰릴 때의 겹침, 상태 라벨 가독성

### 3b: 가시 투척병 + 척탄병 + 노획 (코드 완료, 사용자 플레이 검증 대기)
- `EnemyAttack`(추상): 발동 딜레이 → `Perform()`, 간격 유지, `TriggerDistance`. 파생: `EnemyMeleeAttack`(창병), `EnemyRangedAttack`(투사체 발사)
- `Projectile`이 출처(`HitSource`)를 받도록 확장: 플레이어 발사는 Wall/Enemy/Destructible, 적 발사는 Wall/Player/Destructible에 반응하고 다른 적은 지나침(아군 오사 없음). 회수용 가시는 플레이어가 쏜 것만 남김. 속도는 무기 SO 대신 `speedOverride`(적은 플레이어 이동속도 × 배수)
- `EnemyData` 추가 필드: `projectileSpeedMultiplier`(2.5, 개발계획 6-1 임시값), `engageDistanceInPlayerDiameters`(원거리 적이 멈춰 서서 쏘는 거리). 적 SO: `ThornThrower`(발사 간격 0.5초, 몸 0.9배, 사거리 6), `Grenadier`(투척 간격 5초, 몸 1.2배, 속도 0.8배, 사거리 7). 노획: 투척 가시 6개 묶음, 폭탄알 1발
- 프리팹 `Prefabs/Enemies/ThornThrower`, `Grenadier`(앞쪽 표시: 투척병 연분홍, 척탄병 노랑=폭탄알). `M3_Sandbox`에 각 1기 추가(투척병 우측 아래, 척탄병 좌측 위)
- 자동 스모크 테스트로 확인: 투척병이 6칸 거리까지 접근해 멈추고 발사해 플레이어 사망, 척탄병 폭탄알(0.2초 딜레이 + 비행)이 플레이어를 폭발로 사망시킴, 처치 시 가시 6개 묶음/폭탄알 1발 드롭
- 미확인: 투사체 속도 2.5배(12.5 유닛/초)가 피할 만한지, 척탄병 폭발 범위와 피하기, 적 투사체가 다른 적을 지나치는지 실제 플레이 확인, 원거리 적이 벽 뒤에서 쏘는 경우

### 결정 사항 (M3)
- 적 원거리 공격의 조준은 발동 시점의 플레이어 위치로 1회 계산(선행 조준 없음). 보이지 않으면 바라보는 방향
- 길찾기는 자체 그리드 A*: 벽이 정적이라 충분하고 외부 에셋/NavMesh 의존이 없음
- 권총 어그로 지속 5초는 "경계 상태 유지 시간"으로 통일(개발계획 6-4): 소리 지점으로 이동해 도착 후 남은 시간 동안 경계, 끝나면 복귀. 추격/공격 중인 적은 소리를 무시
- 시야로 발견하면 바로 추격(Alert를 거치지 않음), 소리는 Alert. 뒤쪽은 전혀 못 봄
- 적 공격 클래스는 `EnemyMeleeAttack`(플레이어용 `MeleeAttack`과 이름 충돌 회피)
- 공격 트리거 거리 = 몸 반지름 + 사거리 × 0.9(임시), 발동 딜레이 동안 플레이어를 계속 조준

## M2 결과 (완료: 체크리스트 6항목을 사용자가 플레이로 확인)

### 1단계: 무기 시스템 뼈대 + 근접 (완료)

- `WeaponData`(구 `MeleeWeaponData`, .meta 유지해 기존 Fist 에셋 보존): 5종 전체 필드(근접/탄약/투사체/폭탄/권총 소리/보스 피해량) 정의. 무기 SO 5종 + `WeaponCatalog`(`Data/Weapons/`)
- `WeaponHolder`(플레이어): 현재 무기+잔량, 스페이스 습득/교체/드롭, 합산 습득(상한 초과분은 바닥에 남김), 소진 시 주먹 복귀, `PlayerLoadout.Current` 항상 동기화, 시작 시 로드아웃에서 장착
- `WeaponPickup`(+프리팹 `Prefabs/Weapons/WeaponPickup.prefab`): 픽업 색/라벨, 드롭 시 잔량 유지
- `MeleeAttack`(구 `FistAttack`): 부채꼴 각도 0이면 직선 상자, 0 초과면 부채꼴. 부채꼴 범위 표시는 코드로 생성한 스프라이트
- `Tools/Fear/M2 Setup` → `M2_Sandbox`(가시창 픽업 2개, 더미 4개, 벽). `M1 Setup`도 같은 플레이어 리그를 공유하며 M1_Sandbox를 재생성. 빌드 목록 M2, M1, M0 순
- 입력: `GameInput.Interact`(Space). 좌상단 디버그 HUD(OnGUI)에 무기/잔량 표시
- `M2 Setup`은 씬을 매번 새로 만들므로, 에디터에서 M2_Sandbox를 고친 내용은 재실행하면 사라진다. 배치를 바꿀 때는 `M2Setup.BuildScene`의 PlacePickup/CreateDummy를 수정할 것(픽업 배치는 이미 반영: 가시 3, 폭탄 3, 권총 2, 가시창 2)
- 임시값(기획 빈칸, SO에서 조정): 가시창 부채꼴 120도, 투사체 속도 20(총알 40) 지름/초. (투척 가시 간격은 기획 표의 0.4초로 정정: 처음 0.3초로 잘못 넣었었음) SO는 id가 빈 경우에만 초기화하므로 조정한 수치는 덮어쓰지 않음

### 2단계: 원거리 무기 (완료)

- `RangedAttack`(플레이어): 발사 간격/탄약 소모, 조준 방향 투사체 생성. 권총은 발사 시 `SoundEventBus.Publish`(수신자는 M3)
- `Projectile`(+프리팹): 매 프레임 이동 구간 레이캐스트(터널링 방지), 벽/적 명중 처리
  - 투척 가시: 명중 시 50%, 빗나감(벽/맵 끝) 시 100% 확률로 회수용 픽업 생성. 회수 픽업은 같은 무기를 들고 있을 때 위를 지나가면 자동 습득(`WeaponPickup.MakeAutoRecover`), 스페이스 교체와 분리
  - 폭탄알: 적/벽 접촉 시 폭발(`ExplosionEffect.Detonate`, 지름 4배, 벽 뒤 대상 제외, 플레이어 면역). 벽 폭발은 벽 앞으로 0.1 물려서 판정(WallQuery가 시작점이 벽 안이면 전부 막기 때문)
  - 권총: 좌클릭 누르고 있으면 연사(초당 8발), 탄이 벽/적에 닿으면 소멸
- `SoundEventBus`(Core): `SoundEvent(위치, 반경, 지속)` 정적 버스
- `WeaponHolder.HandleAttackInput(pressed, held)`: 권총만 held, 나머지는 pressed
- M2_Sandbox: 투척 가시 3, 폭탄알 3, 권총 2 픽업(합산 상한 테스트), 먼 더미 무리(폭발 범위 확인), 옆 벽

### 검증 체크리스트 (M2_Sandbox, R로 재시작 포함 — 전부 통과)

1. 가시창 부채꼴 범위 표시와 판정 일치, 벽 뒤 더미 불사
2. 스페이스: 습득, 교체 시 이전 무기 그 자리에 드롭(잔량 유지), 투척 가시 합산(상한 12, 초과분 바닥에 남음)
3. 투척 가시 소진 → 주먹 복귀, 빗나간 가시 위를 지나가면 회수
4. 폭탄알: 더미 무리 폭발, 벽 뒤 더미 무사, 플레이어 무사, 상한 2
5. 권총: 연사, 16발 소진 후 주먹 복귀(빈 총 사라짐)
6. 무기 든 채 K(사망) → R: 진입 시점의 무기/탄약으로 복원

### 결정 사항 (M2)

- `MeleeWeaponData`를 새로 만들지 않고 파일/클래스를 `WeaponData`로 개명(.meta 유지): 기존 Fist 에셋의 스크립트 GUID가 보존되어 수치를 잃지 않음
- 무기 5종을 SO 하나(`WeaponData` + `kind`)로 통합: 수치를 SO에서만 바꾸면 동작이 바뀌는 구조, 종류별 SO 분리는 과분할이라 채택하지 않음
- 부채꼴 범위 표시는 코드로 스프라이트를 생성(부채꼴 텍스처): 별도 아트/머티리얼 없이 SO 각도·사거리 변경이 그대로 반영됨
- 발사체 벽 판정은 이동 구간 레이캐스트(터널링 방지), 폭발 중심은 벽 앞으로 0.1 물림(시작점이 벽 안이면 `WallQuery`가 전부 차단하기 때문)
- 권총만 누르고 있으면 연사, 그 외는 클릭 1회
- 픽업 라벨은 `TextMesh`(내장 폰트, 영문 표기), 디버그 HUD는 OnGUI. 정식 UI는 M6 이후로 미룸
- 배치 모드(`Unity.exe -batchmode`)는 에디터가 열려 있으면 실패하므로 세션 후반에는 `unity command menu "Tools/Fear/M2 Setup"` 사용

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
- 에디터가 백그라운드일 때 플레이 모드의 `Time`이 멈춰 있다. 자동 검증은 `unity command eval_file`에서 `EditorApplication.isPaused = true` 후 `EditorApplication.Step()`을 반복(1스텝 = 0.02초)하면 결정적으로 시뮬레이션된다. 이때 `PlayerController`가 마우스 방향으로 회전을 덮어쓰므로 `enabled = false`로 끈 뒤 위치/회전을 직접 지정한다. 상태는 리플렉션 없이 `EnemyAI.State`로 읽는다
- 소리 이벤트를 발행하면 모든 적이 그 지점으로 직선 이동하므로, 그 경로 위에 플레이어가 있으면 발견되어 죽는다(테스트 배치 주의)
- `RequireComponent`는 인자를 최대 3개까지만 받는다
- 프로젝트 루트에서 `grep -r`는 `Library/`까지 훑어 2분 넘게 걸린다. Grep 도구에 `glob`을 주거나 `docs/`, `Assets/_Project/`로 범위를 좁힌다

## 다음 세션 시작 가이드

- 현재 브랜치 main, M2까지 커밋 완료(M2 커밋 `3a38f7a`~`faa63ee`). 원격 푸시 완료(`9de3789`)
- 시작 시 할 일: M3 진행 절(3a·3b 결과/결정)을 읽고, 3c(벽 프리팹 + 1스테이지)를 이어서 한다. 1스테이지 내용은 `docs/기획.md` 스테이지 절. 적 수치는 `docs/기획.md` 표, 구조는 개발계획 3.4
- 샌드박스 씬 M0~M2가 있고 빌드 목록은 M2, M1, M0 순. 플레이 확인은 M2_Sandbox가 가장 많은 요소를 담고 있음
- 씬/에셋 구성은 `Editor/`의 메뉴 스크립트(`M0 Setup`, `M1 Setup`, `M2 Setup`)가 재생성하므로, M3도 `M3 Setup`을 만들어 씬 YAML을 직접 편집하지 않는다. 플레이어 리그와 무기 에셋은 `M2Setup`이 담당하고 `M1 Setup`이 이를 호출한다

## 다음 단계

- 3a 사용자 플레이 검증(`M3_Sandbox`): 정면 접근 시 발각/추격/공격, 등 뒤 처치, 권총 소리로 유인, 선반 벽 뒤에 숨기, R 재시작
- 3b 사용자 플레이 검증(`M3_Sandbox`의 투척병/척탄병): 피할 수 있는지, 노획 후 사용
- 3c: 벽/화분/제어 패널 프리팹, 1스테이지(오프닝 벽 붕괴 연출, 튜토리얼 순서)
- 3d: 2스테이지, 재미 검증 게이트(첫 클리어 10분 이내)
- 뚱보는 M4에서 완성

## 미해결 / 확인 필요

- 개발계획 6절의 기획 빈칸 8건은 아직 미확정(임시값 사용 중: 가시창 부채꼴 120도, 투사체 속도 20/총알 40 지름/초, 주먹 폭 0.5). 나머지는 M3 착수 전 확인
- M0~M2 커밋 모두 원격 푸시 완료(2026-09-21)
- `ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json`은 에디터가 자동 수정한 변경이라 커밋에서 제외
- `.claude/`(세션 명령어·로컬 설정)와 `메모.txt`(사용자 메모)는 커밋 대상에서 제외
