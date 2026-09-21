using System.Collections;
using Game.Core;
using Game.Weapons;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 폭발 오브젝트(탄약 상자/세척수 탱크). 공격(근접, 투사체, 폭발)을 받으면 파손되어 범위 내 모든 대상을 즉사시킨다.
    /// 플레이어도 포함(폭탄알 면역은 폭탄알에만 적용). Wall이 아니므로 시야는 막지 않지만 폭발은 Wall이 막는다.
    /// 연쇄: 폭발 범위 안의 다른 폭발 오브젝트가 TakeHit를 받으면 자기 지연 뒤에 폭발하므로 재귀 없이 순차 처리된다.
    /// </summary>
    public class ExplosiveProp : MonoBehaviour, IDamageable
    {
        [SerializeField] ExplosiveData data;
        [SerializeField] SpriteRenderer body;
        [SerializeField] Collider2D hitCollider;
        [SerializeField] ExplosionEffect effectPrefab;

        public ExplosiveData Data => data;
        public bool IsDead { get; private set; }

        public void TakeHit(HitInfo hit)
        {
            if (IsDead) return;
            IsDead = true;
            if (hitCollider != null) hitCollider.enabled = false;
            if (body != null) body.enabled = false;
            foreach (var r in GetComponentsInChildren<SpriteRenderer>()) r.enabled = false; // 노란 경고 표식 포함
            StartCoroutine(Explode());
        }

        IEnumerator Explode()
        {
            if (data.chainDelaySeconds > 0f) yield return new WaitForSeconds(data.chainDelaySeconds);

            Vector2 center = transform.position;
            float diameter = GameConstants.FromPlayerDiameters(data.explosionDiameters);
            ExplosionEffect.Show(effectPrefab, center, diameter, data.effectColor, data.effectSeconds);

            var filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = Layers.Mask(Layers.Player, Layers.Enemy, Layers.Destructible),
                useTriggers = true
            };
            var results = new Collider2D[64];
            int count = Physics2D.OverlapCircle(center, diameter * 0.5f, filter, results);
            for (int i = 0; i < count; i++)
            {
                var col = results[i];
                if (!col.TryGetComponent<IDamageable>(out var target) || target.IsDead) continue;
                Vector2 point = col.ClosestPoint(center);
                if (WallQuery.IsBlocked(center, point)) continue;
                Vector2 dir = (point - center).sqrMagnitude > 0.0001f ? (point - center).normalized : Vector2.up;
                target.TakeHit(new HitInfo(HitSource.Environment, HitKind.Explosion, point, dir));
            }
            Destroy(gameObject, 0.05f);
        }
    }
}
