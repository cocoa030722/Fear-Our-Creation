using UnityEngine;

namespace Game.Core
{
    /// <summary>벽 차단 판정의 유일한 진입점. 시야·투사체·폭발·근접 판정이 모두 이 규칙(Wall 레이어)을 쓴다.</summary>
    public static class WallQuery
    {
        /// <summary>from에서 to까지 직선 사이에 Wall이 있으면 true.</summary>
        public static bool IsBlocked(Vector2 from, Vector2 to)
        {
            return Physics2D.Linecast(from, to, Layers.WallMask).collider != null;
        }
    }
}
