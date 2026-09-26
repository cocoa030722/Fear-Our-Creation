using Game.Core;
using Game.Weapons;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 적 원거리 공격(가시 투척병, 척탄병). 발동 딜레이 후 그 시점의 플레이어 위치를 향해 투사체를 발사한다.
    /// 투사체 종류(가시/폭탄알)는 EnemyData.weapon, 속도는 EnemyData의 플레이어 속도 배수, 간격은 EnemyData.
    /// 적의 투사체는 다른 적을 지나치고(아군 오사 없음) 벽과 플레이어에만 반응한다.
    /// </summary>
    public class EnemyRangedAttack : EnemyAttack
    {
        [SerializeField] Projectile projectilePrefab;

        Perception _perception;

        public override float TriggerDistance => GameConstants.FromPlayerDiameters(Enemy.Data.engageDistanceInPlayerDiameters);

        protected override void Awake()
        {
            base.Awake();
            _perception = GetComponent<Perception>();
        }

        protected override void Perform()
        {
            Vector2 origin = transform.position;
            // 발동 시점에 플레이어가 보이면 그 위치로, 아니면 바라보는 방향으로 발사(점사 전체가 이 방향 하나를 공유)
            Vector2 dir = _perception.CanSeePlayer(out Vector2 playerPos) && (playerPos - origin).sqrMagnitude > 0.0001f
                ? (playerPos - origin).normalized
                : (Vector2)transform.up;

            int count = Mathf.Max(1, Enemy.Data.burstCount);
            if (count <= 1) { Fire(dir); return; }
            StartCoroutine(FireBurst(dir, count));
        }

        System.Collections.IEnumerator FireBurst(Vector2 dir, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Fire(dir);
                if (i < count - 1) yield return new WaitForSeconds(Enemy.Data.burstIntervalSeconds);
            }
        }

        void Fire(Vector2 dir)
        {
            var p = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            p.Init(Enemy.Data.weapon, dir, HitSource.Enemy, Enemy.Data.ProjectileSpeed);
        }
    }
}
