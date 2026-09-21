using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>정지 더미 적. 1회 피격 즉사. M3에서 EnemyBase/AI로 대체된다.</summary>
    public class DummyEnemy : MonoBehaviour, IDamageable
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] Collider2D hitCollider;
        [SerializeField] Color deadColor = new Color(0.45f, 0.12f, 0.12f, 0.6f);

        public bool IsDead { get; private set; }

        public void TakeHit(HitInfo hit)
        {
            if (IsDead) return;
            IsDead = true;
            if (body != null) body.color = deadColor;
            // 시신은 통과 가능해야 하므로 충돌 제거(시신 시스템은 이후 마일스톤)
            if (hitCollider != null) hitCollider.enabled = false;
        }
    }
}
