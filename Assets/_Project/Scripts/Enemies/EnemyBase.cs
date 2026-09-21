using System;
using Game.Core;
using Game.Weapons;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 적 공통: 1회 피격 즉사, 사망 처리(충돌 제거, 색 변경, 노획 드롭).
    /// 아군 오사 없음: 적이 낸 피해(HitSource.Enemy)는 무시한다.
    /// </summary>
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        [SerializeField] EnemyData data;
        [SerializeField] SpriteRenderer body;
        [SerializeField] Collider2D hitCollider;
        [SerializeField] Rigidbody2D rb;
        [SerializeField] WeaponPickup pickupPrefab;
        [SerializeField] Color deadColor = new Color(0.45f, 0.12f, 0.12f, 0.6f);

        public EnemyData Data => data;
        public float BodyRadius => data.bodyDiameterInPlayerDiameters * GameConstants.PlayerDiameter * 0.5f;
        public bool IsDead { get; private set; }
        public event Action Died;

        public void TakeHit(HitInfo hit)
        {
            if (IsDead || hit.Source == HitSource.Enemy) return;
            IsDead = true;
            if (body != null) body.color = deadColor;
            if (hitCollider != null) hitCollider.enabled = false; // 시신은 통과 가능(시신 시스템은 이후 마일스톤)
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }
            DropLoot();
            Died?.Invoke();
        }

        void DropLoot()
        {
            if (data.lootWeapon == null || pickupPrefab == null) return;
            int ammo = data.lootAmmo > 0 ? data.lootAmmo : data.lootWeapon.pickupAmmo;
            WeaponPickup.Spawn(pickupPrefab, data.lootWeapon, ammo, transform.position);
        }
    }
}
