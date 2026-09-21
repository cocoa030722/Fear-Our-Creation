using Game.Core;
using Game.Weapons;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 적 근접 공격. 예고 모션 없이 발동 딜레이 후 판정하며, 판정 시점의 위치/방향으로 범위를 재계산한다(MeleeHit 공용).
    /// 딜레이/간격은 EnemyData, 범위 모양은 EnemyData.weapon(창병은 가시창과 동일)에서 온다.
    /// </summary>
    [RequireComponent(typeof(EnemyBase))]
    public class EnemyMeleeAttack : MonoBehaviour
    {
        EnemyBase _enemy;
        float _nextAttackTime;
        float _hitTime = -1f;
        int _targetMask;

        public bool IsBusy => _hitTime >= 0f;
        public bool CanAttack => !IsBusy && Time.time >= _nextAttackTime;

        /// <summary>몸 가장자리부터의 사거리(월드 유닛).</summary>
        public float Reach => GameConstants.FromPlayerDiameters(_enemy.Data.weapon.rangeInDiameters);

        void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            _targetMask = Layers.Mask(Layers.Player);
        }

        public void StartAttack()
        {
            if (!CanAttack) return;
            _nextAttackTime = Time.time + _enemy.Data.attackIntervalSeconds;
            _hitTime = Time.time + _enemy.Data.attackWindupSeconds;
        }

        public void Cancel() => _hitTime = -1f;

        void Update()
        {
            if (_hitTime < 0f) return;
            if (_enemy.IsDead) { Cancel(); return; }
            if (Time.time < _hitTime) return;

            _hitTime = -1f;
            MeleeHit.Strike(transform.position, transform.up, _enemy.BodyRadius, _enemy.Data.weapon, _targetMask, HitSource.Enemy);
        }
    }
}
