using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 물리 레이어 이름/인덱스/마스크. 인덱스는 TagManager 설정과 일치해야 한다(Tools/Fear/M0 Setup이 동기화).
    /// 벽 규칙(시야·투사체·폭발 차단)은 Wall 레이어 하나로 통일한다.
    /// </summary>
    public static class Layers
    {
        public const string WallName = "Wall";
        public const string PlayerName = "Player";
        public const string EnemyName = "Enemy";
        public const string ProjectileName = "Projectile";
        public const string DestructibleName = "Destructible";
        public const string PickupName = "Pickup";
        public const string SightBlockerName = "SightBlocker";

        public const int Wall = 6;
        public const int Player = 7;
        public const int Enemy = 8;
        public const int Projectile = 9;
        public const int Destructible = 10;
        public const int Pickup = 11;
        public const int SightBlocker = 12;

        /// <summary>(인덱스, 이름) 목록. 에디터 셋업에서 사용.</summary>
        public static readonly (int index, string name)[] All =
        {
            (Wall, WallName),
            (Player, PlayerName),
            (Enemy, EnemyName),
            (Projectile, ProjectileName),
            (Destructible, DestructibleName),
            (Pickup, PickupName),
            (SightBlocker, SightBlockerName),
        };

        public static int Mask(params int[] layers)
        {
            int mask = 0;
            foreach (int l in layers) mask |= 1 << l;
            return mask;
        }

        /// <summary>이동/시야/투사체/폭발을 모두 막는 마스크.</summary>
        public static readonly int WallMask = Mask(Wall);

        /// <summary>시야 판정 차단 마스크(벽 + 시야만 막는 오브젝트).</summary>
        public static readonly int SightBlockMask = Mask(Wall, SightBlocker);
    }
}
