using UnityEngine;

namespace Game.Core
{
    /// <summary>벽 차단 판정의 유일한 진입점. 시야·투사체·폭발·근접 판정·길찾기가 모두 이 규칙(Wall 레이어)을 쓴다.</summary>
    public static class WallQuery
    {
        /// <summary>from에서 to까지 직선 사이에 Wall이 있으면 true.</summary>
        public static bool IsBlocked(Vector2 from, Vector2 to)
        {
            return Physics2D.Linecast(from, to, Layers.WallMask).collider != null;
        }

        /// <summary>시야 차단 판정: 벽 + 시야만 막는 오브젝트(SightBlocker).</summary>
        public static bool IsSightBlocked(Vector2 from, Vector2 to)
        {
            return Physics2D.Linecast(from, to, Layers.SightBlockMask).collider != null;
        }

        /// <summary>반지름 radius인 몸이 from에서 to로 직선 이동할 때 벽에 걸리면 true(이동 경로 판정).</summary>
        public static bool IsBlockedCircle(Vector2 from, Vector2 to, float radius)
        {
            Vector2 delta = to - from;
            float dist = delta.magnitude;
            if (dist < 0.0001f) return Overlaps(from, radius);
            return Physics2D.CircleCast(from, radius, delta / dist, dist, Layers.WallMask).collider != null;
        }

        /// <summary>반지름 radius인 원이 point에서 벽과 겹치면 true(길찾기 격자 차단 판정).</summary>
        public static bool Overlaps(Vector2 point, float radius)
        {
            return Physics2D.OverlapCircle(point, radius, Layers.WallMask) != null;
        }
    }
}
