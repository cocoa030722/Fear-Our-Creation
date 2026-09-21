using UnityEngine;

namespace Game.Core
{
    public enum HitSource { Player, Enemy, Environment }

    public enum HitKind { Melee, Projectile, Explosion }

    /// <summary>피해 정보. 피해는 전부 IDamageable.TakeHit(HitInfo)로 통일한다.</summary>
    public readonly struct HitInfo
    {
        public readonly HitSource Source;
        public readonly HitKind Kind;
        /// <summary>폭탄알 여부(뚱보 즉사, 아군 오사 필터, 보스 피해량 가중치에 사용).</summary>
        public readonly bool IsBombShell;
        public readonly Vector2 Point;
        public readonly Vector2 Direction;

        public HitInfo(HitSource source, HitKind kind, Vector2 point, Vector2 direction, bool isBombShell = false)
        {
            Source = source;
            Kind = kind;
            Point = point;
            Direction = direction;
            IsBombShell = isBombShell;
        }
    }

    public interface IDamageable
    {
        bool IsDead { get; }
        void TakeHit(HitInfo hit);
    }
}
