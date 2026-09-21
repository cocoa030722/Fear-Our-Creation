using System;
using Game.Core;
using UnityEngine;

namespace Game.Player
{
    /// <summary>플레이어는 1회 피격 즉사. 사망 이벤트로 조작 차단 등을 알린다.</summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] Color deadColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        public bool IsDead { get; private set; }
        public event Action Died;

        public void TakeHit(HitInfo hit)
        {
            if (IsDead) return;
            IsDead = true;
            if (body != null) body.color = deadColor;
            Died?.Invoke();
        }

        void Update()
        {
            // 임시 디버그: K키로 플레이어 즉사. 적 공격이 들어오는 M3 이후 제거
            if (!IsDead && GameInput.Instance.DebugKill.WasPressedThisFrame())
                TakeHit(new HitInfo(HitSource.Environment, HitKind.Melee, transform.position, Vector2.zero));
        }
    }
}
