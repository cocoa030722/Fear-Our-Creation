using Game.Core;
using Game.Weapons;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 적 근접 공격(창병). 판정 시점의 위치/방향으로 범위를 재계산한다(MeleeHit 공용).
    /// 범위 모양은 EnemyData.weapon(창병은 가시창과 동일)에서 온다.
    /// </summary>
    public class EnemyMeleeAttack : EnemyAttack
    {
        int _targetMask;

        public override float TriggerDistance =>
            Enemy.BodyRadius + GameConstants.FromPlayerDiameters(Enemy.Data.weapon.rangeInDiameters) * 0.9f;

        protected override void Awake()
        {
            base.Awake();
            _targetMask = Layers.Mask(Layers.Player);
        }

        protected override void Perform()
        {
            MeleeHit.Strike(transform.position, transform.up, Enemy.BodyRadius, Enemy.Data.weapon, _targetMask, HitSource.Enemy);
        }
    }
}
