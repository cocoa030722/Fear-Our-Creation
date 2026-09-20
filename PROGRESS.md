# PROGRESS

마지막 갱신: 2026-09-20 · 현재 단계: **M0 완료**, 다음은 M1

## 마일스톤 현황

| 마일스톤 | 상태 |
| --- | --- |
| M0 프로젝트 셋업 | 완료 |
| M1 이동·공격·즉사·재시작 | 미착수 (다음) |
| M2 ~ M9 | 미착수 |

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
- Cinemachine 추적은 M1 이동 구현 시 붙이기로 미룸. 현재는 고정 카메라
- 기존 `Assets/Scenes/SampleScene`은 삭제하지 않고 빌드 목록에서만 제외
- 플레이어 색은 흰 배경 대비를 위해 짙은 슬레이트(#2B3A42)로 임의 지정(기획에 규정 없음)
- 캡처의 상하 검은 띠는 캡처 창 비율에 의한 레터박스이며 결함이 아님
- 에디터가 자동 생성한 변경: `ProjectSettings.asset`의 `APP_UI_EDITOR_ONLY` 정의, `SceneTemplateSettings.json`

## 다음 단계 (M1)
- Input System Action Map(WASD/마우스/R), 이동·마우스 조준 회전
- 주먹 공격(발동 딜레이 0.1초, 간격 0.5초, 좁은 직선 범위), `IDamageable`/`HitInfo`
- 더미 적 1회 피격 즉사, 플레이어 즉사
- `SnapshotSystem` 골격 + R키 재시작(0.5초 이내). 완료 기준은 개발계획 M1 참조
- 개발계획 6절의 임시값(플레이어 이동속도 5유닛/초 등)을 SO 기본값으로 반영

## 미해결 / 확인 필요
- 개발계획 6절의 기획 빈칸 8건은 아직 미확정(임시값 사용 예정)
- M0 변경 사항은 커밋 완료(`13bd181`). 원격 푸시는 하지 않음
