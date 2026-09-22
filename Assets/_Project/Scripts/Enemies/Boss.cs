using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Player;
using Game.Weapons;
using Game.World;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 5스테이지 보스. 별도의 보스전 기믹 없이 두 패턴만 반복한다: 원거리 사격(예고선 → 발사 → 쿨다운, 제어 패널로 막히면 보류)과
    /// 잡몹 호출(쿨다운 12초, 호출 모션 1.5초 동안 사격 중단). 체력바 대신 방호복 손상 단계(25% 단위)로 진행 상황을 표시한다.
    /// 이동하지 않고 제자리에서 플레이어를 향해 돈다(배치: 플레이어-제어 패널-잡몹-보스).
    /// BossIntro가 진입 연출을 마친 뒤 Activate()를 호출하기 전까지는 피격도, 공격도 하지 않는다.
    /// </summary>
    public class Boss : MonoBehaviour, IDamageable
    {
        [SerializeField] BossData data;
        [SerializeField] SpriteRenderer body;
        [Tooltip("손상 단계마다 짙어지는 촉수 노출(플레이스홀더 오버레이)")]
        [SerializeField] SpriteRenderer armorOverlay;
        [SerializeField] Collider2D hitCollider;
        [SerializeField] Projectile projectilePrefab;
        [SerializeField] LineRenderer telegraphLine;
        [Tooltip("사망(보스전 클리어) 시 발동할 종점")]
        [SerializeField] StageGoal goalOnDeath;

        [Header("잡몹 호출")]
        [Tooltip("소환할 잡몹 프리팹 목록(가시 창병 위주 + 척탄병 최대 1기는 이 배열 구성으로 정한다)")]
        [SerializeField] GameObject[] summonPrefabs;
        [Tooltip("소환 지점(캣워크 경로). 순서대로 돌려 가며 사용")]
        [SerializeField] Transform[] summonPoints;

        static readonly Color TelegraphColor = new Color32(0xF5, 0xC5, 0x18, 0xC0); // 노란 위험 표식(경고)
        static readonly Color HitFlashColor = Color.white;

        public bool IsDead { get; private set; }
        public event Action Died;

        PlayerHealth _player;
        Color _baseColor;
        bool _active;
        float _damage;
        int _armorStage = -1;

        bool _telegraphing;
        Vector2 _telegraphDir;
        float _telegraphEnd;
        float _nextFireTime;

        bool _summoning;
        float _nextSummonTime;
        int _summonCursor;
        int _spawnCursor;
        readonly List<EnemyBase> _minions = new List<EnemyBase>();

        PlayerHealth Player
        {
            get
            {
                if (_player == null) _player = FindAnyObjectByType<PlayerHealth>();
                return _player;
            }
        }

        void Awake()
        {
            if (telegraphLine != null) telegraphLine.enabled = false;
            if (armorOverlay != null)
            {
                var c = armorOverlay.color;
                c.a = 0f;
                armorOverlay.color = c;
            }
        }

        /// <summary>진입 연출이 끝난 뒤(또는 재시작으로 생략된 뒤) 호출: 사격/호출을 시작한다.
        /// 이 시점의 몸 색(BossIntro가 공개 연출로 바꾼 붉은색)을 기준색으로 잡는다(피격 플래시 복귀용).</summary>
        public void Activate()
        {
            if (IsDead) return;
            if (body != null) _baseColor = body.color;
            _active = true;
            _nextFireTime = Time.time;
            _nextSummonTime = Time.time + data.summonCooldownSeconds;
        }

        public void TakeHit(HitInfo hit)
        {
            if (IsDead || !_active) return;
            if (hit.Source != HitSource.Player || hit.Weapon == null) return; // 보스 피해량은 플레이어 무기(WeaponData.bossDamage)만 인정

            _damage += hit.Weapon.bossDamage;
            StartCoroutine(FlashHit()); // 여러 번 겹쳐 걸려도 매번 흰색 → 원래색으로 돌아가므로 무해하다
            UpdateArmorStage();
            if (_damage >= data.maxHealth) Kill();
        }

        IEnumerator FlashHit()
        {
            if (body == null) yield break;
            body.color = HitFlashColor;
            yield return new WaitForSeconds(0.06f);
            if (!IsDead) body.color = _baseColor;
        }

        void UpdateArmorStage()
        {
            float ratio = Mathf.Clamp01(_damage / data.maxHealth);
            int stage = Mathf.Clamp(Mathf.FloorToInt(ratio * 4f), 0, data.armorStageAlpha.Length - 1);
            if (stage == _armorStage) return;
            _armorStage = stage;
            if (armorOverlay != null)
            {
                var c = armorOverlay.color;
                c.a = data.armorStageAlpha[stage];
                armorOverlay.color = c;
            }
        }

        void Kill()
        {
            if (IsDead) return;
            IsDead = true;
            _active = false;
            _telegraphing = false;
            if (telegraphLine != null) telegraphLine.enabled = false;
            if (body != null) body.color = new Color(0.3f, 0.1f, 0.1f, 1f);
            if (hitCollider != null) hitCollider.enabled = false;
            Died?.Invoke();
            if (goalOnDeath != null) goalOnDeath.Trigger();
        }

        void Update()
        {
            if (IsDead || !_active) return;
            var player = Player;
            if (player == null || player.IsDead) return;

            FaceTowards(player.transform.position);
            UpdateSummon();
            if (!_summoning) UpdateFire(player);
        }

        void FaceTowards(Vector2 targetPos)
        {
            Vector2 dir = targetPos - (Vector2)transform.position;
            if (dir.sqrMagnitude < 0.0001f) return;
            float target = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.z, target, data.turnDegreesPerSecond * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // ---- 원거리 사격: 예고선(0.8초) → 발사 → 쿨다운. 시야가 막히면(제어 패널 엄폐) 예고를 걸지 않는다 ----

        void UpdateFire(PlayerHealth player)
        {
            if (_telegraphing)
            {
                if (telegraphLine != null)
                {
                    telegraphLine.SetPosition(0, transform.position);
                    telegraphLine.SetPosition(1, (Vector2)transform.position + _telegraphDir * 30f);
                }
                if (Time.time >= _telegraphEnd)
                {
                    _telegraphing = false;
                    if (telegraphLine != null) telegraphLine.enabled = false;
                    Fire(_telegraphDir);
                    _nextFireTime = Time.time + data.fireCooldownSeconds;
                }
                return;
            }

            if (Time.time < _nextFireTime) return;
            Vector2 origin = transform.position;
            Vector2 playerPos = player.transform.position;
            if (WallQuery.IsSightBlocked(origin, playerPos)) return; // 제어 패널 뒤에 숨으면 사격 보류(접근 기회)

            _telegraphDir = (playerPos - origin).sqrMagnitude > 0.0001f ? (playerPos - origin).normalized : (Vector2)transform.up;
            _telegraphEnd = Time.time + data.fireTelegraphSeconds;
            _telegraphing = true;
            if (telegraphLine != null)
            {
                telegraphLine.enabled = true;
                telegraphLine.startColor = telegraphLine.endColor = TelegraphColor;
                telegraphLine.SetPosition(0, origin);
                telegraphLine.SetPosition(1, origin + _telegraphDir * 30f);
            }
        }

        void Fire(Vector2 dir)
        {
            if (projectilePrefab == null || data.spikeWeapon == null) return;
            var p = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            p.Init(data.spikeWeapon, dir, HitSource.Enemy);
        }

        // ---- 잡몹 호출: 쿨다운마다 호출 모션(사격 중단) 후 부족한 만큼 보충 소환 ----

        void UpdateSummon()
        {
            if (_summoning) return;
            if (Time.time < _nextSummonTime) return;
            _minions.RemoveAll(e => e == null || e.IsDead);
            if (_minions.Count >= data.summonCount || summonPrefabs == null || summonPrefabs.Length == 0 || summonPoints == null || summonPoints.Length == 0)
            {
                _nextSummonTime = Time.time + data.summonCooldownSeconds;
                return;
            }
            _nextSummonTime = Time.time + data.summonCooldownSeconds;
            StartCoroutine(SummonRoutine());
        }

        IEnumerator SummonRoutine()
        {
            _summoning = true;
            if (telegraphLine != null) telegraphLine.enabled = false;
            _telegraphing = false;

            // 호출 모션(플레이스홀더): 몸이 부풀었다 가라앉으며 잡몹을 부른다
            float t = 0f;
            Vector3 baseScale = transform.localScale;
            while (t < data.summonMotionSeconds)
            {
                t += Time.deltaTime;
                float pulse = 1f + 0.12f * Mathf.Sin(t / data.summonMotionSeconds * Mathf.PI);
                transform.localScale = baseScale * pulse;
                yield return null;
            }
            transform.localScale = baseScale;

            SpawnWave();
            _summoning = false;
        }

        void SpawnWave()
        {
            _minions.RemoveAll(e => e == null || e.IsDead);
            int need = data.summonCount - _minions.Count;
            var player = Player;
            for (int i = 0; i < need; i++)
            {
                var prefab = summonPrefabs[_summonCursor % summonPrefabs.Length];
                var point = summonPoints[_spawnCursor % summonPoints.Length];
                _summonCursor++;
                _spawnCursor++;

                var go = Instantiate(prefab, point.position, point.rotation);
                if (go.TryGetComponent<EnemyBase>(out var enemy)) _minions.Add(enemy);
                if (player != null && go.TryGetComponent<EnemyAI>(out var ai)) ai.AlertTo(player.transform.position, data.summonAlertSeconds);
            }
        }
    }
}
