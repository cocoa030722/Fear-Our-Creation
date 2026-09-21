using Game.Core;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>폭발 판정과 표시. 판정은 정적 Detonate, 표시는 짧게 퍼졌다 사라지는 반투명 원(노란 위험 표식).</summary>
    public class ExplosionEffect : MonoBehaviour
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] float lifetimeSeconds = 0.25f;

        float _age;
        Color _startColor;

        /// <summary>
        /// center에서 폭발 판정을 수행한다. 반경 = 폭발 지름의 절반. 벽 뒤 대상은 WallQuery로 걸러낸다.
        /// 아군 오사 없음: 플레이어가 낸 폭발은 플레이어를 해치지 않는다(환경 오브젝트는 그대로 피해).
        /// </summary>
        public static void Detonate(WeaponData data, Vector2 center, HitSource source, ExplosionEffect effectPrefab)
        {
            float diameter = GameConstants.FromPlayerDiameters(data.explosionDiameters);
            Show(effectPrefab, center, diameter);

            int mask = source == HitSource.Player
                ? Layers.Mask(Layers.Enemy, Layers.Destructible)
                : Layers.Mask(Layers.Player, Layers.Destructible);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = mask, useTriggers = true };
            var results = new Collider2D[32];
            int count = Physics2D.OverlapCircle(center, diameter * 0.5f, filter, results);
            for (int i = 0; i < count; i++)
            {
                var col = results[i];
                if (!col.TryGetComponent<IDamageable>(out var target) || target.IsDead) continue;
                Vector2 point = col.ClosestPoint(center);
                if (WallQuery.IsBlocked(center, point)) continue;
                Vector2 dir = (point - center).sqrMagnitude > 0.0001f ? (point - center).normalized : Vector2.up;
                target.TakeHit(new HitInfo(source, HitKind.Explosion, point, dir, isBombShell: true));
            }
        }

        /// <summary>판정 없이 표시만 생성한다. color/lifetime은 프리팹 값을 덮어쓸 때만 지정(폭발 오브젝트용).</summary>
        public static void Show(ExplosionEffect prefab, Vector2 center, float worldDiameter, Color? color = null, float lifetime = 0f)
        {
            if (prefab == null) return;
            var fx = Instantiate(prefab, center, Quaternion.identity);
            fx.Play(worldDiameter, color, lifetime);
        }

        void Play(float worldDiameter, Color? color = null, float lifetime = 0f)
        {
            transform.localScale = new Vector3(worldDiameter, worldDiameter, 1f);
            if (body != null && color.HasValue) body.color = color.Value;
            if (lifetime > 0f) lifetimeSeconds = lifetime;
            if (body != null) _startColor = body.color;
        }

        void Update()
        {
            _age += Time.deltaTime;
            if (body != null)
            {
                var c = _startColor;
                c.a *= Mathf.Clamp01(1f - _age / lifetimeSeconds);
                body.color = c;
            }
            if (_age >= lifetimeSeconds) Destroy(gameObject);
        }
    }
}
