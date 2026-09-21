using Game.Core;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 플레이어의 투사체(투척 가시, 폭탄알, 권총탄). 직선 비행하며 매 프레임 이동 구간을 레이캐스트해 터널링을 막는다.
    /// 벽(Wall 레이어)에 막히고, 적/파괴 가능 오브젝트에 명중하면 무기 종류에 따라 처리한다.
    ///  - 투척 가시: 명중 시 hitRecoverChance, 빗나감(벽) 시 missRecoverChance 확률로 회수용 픽업을 남김
    ///  - 폭탄알: 적/벽에 닿으면 폭발
    ///  - 권총탄: 명중하거나 벽에 닿으면 소멸
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] ExplosionEffect explosionPrefab;
        [SerializeField] WeaponPickup pickupPrefab;
        [Tooltip("이 거리(월드 유닛)를 날아가도 아무것도 없으면 사라진다(맵 끝까지 날아가는 것으로 간주)")]
        [SerializeField] float maxTravel = 60f;

        static readonly RaycastHit2D[] Hits = new RaycastHit2D[8];

        WeaponData _data;
        Vector2 _dir;
        float _speed;
        float _travelled;
        int _mask;

        public void Init(WeaponData data, Vector2 direction)
        {
            _data = data;
            _dir = direction.normalized;
            _speed = GameConstants.FromPlayerDiameters(data.projectileSpeedInDiameters);
            _mask = Layers.Mask(Layers.Wall, Layers.Enemy, Layers.Destructible);
            transform.up = _dir;

            if (body != null)
            {
                body.color = data.pickupColor;
                float size = data.kind == WeaponKind.Bomb ? 0.35f : data.kind == WeaponKind.Thrown ? 0.2f : 0.12f;
                body.transform.localScale = new Vector3(size, size * (data.kind == WeaponKind.Thrown ? 2.5f : 1f), 1f);
            }
        }

        void Update()
        {
            if (_data == null) return;
            float step = _speed * Time.deltaTime;
            Vector2 pos = transform.position;

            var filter = new ContactFilter2D { useLayerMask = true, layerMask = _mask, useTriggers = true };
            int count = Physics2D.Raycast(pos, _dir, filter, Hits, step);
            for (int i = 0; i < count; i++) // 거리순 정렬되어 있으므로 처음 막힌 것에서 끝
            {
                var col = Hits[i].collider;
                Vector2 point = Hits[i].point;
                if (col.gameObject.layer == Layers.Wall)
                {
                    OnHitWall(point);
                    return;
                }
                if (col.TryGetComponent<IDamageable>(out var target) && !target.IsDead)
                {
                    OnHitTarget(target, point);
                    return;
                }
            }

            transform.position = pos + _dir * step;
            _travelled += step;
            if (_travelled >= maxTravel) OnHitNothing();
        }

        void OnHitTarget(IDamageable target, Vector2 point)
        {
            if (_data.kind == WeaponKind.Bomb)
            {
                Explode(point - _dir * 0.05f);
                return;
            }
            target.TakeHit(new HitInfo(HitSource.Player, HitKind.Projectile, point, _dir));
            if (_data.kind == WeaponKind.Thrown && Random.value < _data.hitRecoverChance) DropRecoverable(point);
            Destroy(gameObject);
        }

        void OnHitWall(Vector2 point)
        {
            switch (_data.kind)
            {
                case WeaponKind.Bomb:
                    // 벽 안쪽에서 폭발 판정을 하면 WallQuery가 모든 대상을 막으므로 벽 앞으로 살짝 뺀다
                    Explode(point - _dir * 0.1f);
                    return;
                case WeaponKind.Thrown:
                    if (Random.value < _data.missRecoverChance) DropRecoverable(point - _dir * 0.2f);
                    break;
            }
            Destroy(gameObject);
        }

        void OnHitNothing()
        {
            if (_data.kind == WeaponKind.Thrown && Random.value < _data.missRecoverChance) DropRecoverable(transform.position);
            Destroy(gameObject);
        }

        void Explode(Vector2 center)
        {
            ExplosionEffect.Detonate(_data, center, HitSource.Player, explosionPrefab);
            Destroy(gameObject);
        }

        void DropRecoverable(Vector2 position)
        {
            if (pickupPrefab == null) return;
            var p = WeaponPickup.Spawn(pickupPrefab, _data, 1, position);
            p.MakeAutoRecover();
        }
    }
}
