using Game.Core;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 근접 공격 판정 공용 로직(플레이어와 적이 함께 사용). 부채꼴 각도 0이면 몸 가장자리에서 앞으로 뻗는 직선 상자,
    /// 0보다 크면 부채꼴. 벽 뒤 대상은 WallQuery로 걸러낸다. 판정 시점의 위치/방향으로 매번 재계산한다.
    /// </summary>
    public static class MeleeHit
    {
        static readonly Collider2D[] Buffer = new Collider2D[16];

        /// <summary>직선 상자 판정 범위(부채꼴이 아닐 때).</summary>
        public static void GetBox(Vector2 origin, Vector2 forward, float bodyRadius, WeaponData data,
            out Vector2 center, out Vector2 size, out float angle)
        {
            float reach = GameConstants.FromPlayerDiameters(data.rangeInDiameters);
            float width = GameConstants.FromPlayerDiameters(data.widthInDiameters);
            center = origin + forward * (bodyRadius + reach * 0.5f);
            size = new Vector2(width, reach); // 로컬 +Y가 전방이므로 세로가 사거리
            angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - 90f;
        }

        /// <summary>판정을 수행하고 피해를 입힌다. 맞은 대상 수를 반환.</summary>
        public static int Strike(Vector2 origin, Vector2 forward, float bodyRadius, WeaponData data, int targetMask, HitSource source)
        {
            bool isArc = data.arcDegrees > 0f;
            float reach = GameConstants.FromPlayerDiameters(data.rangeInDiameters);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = targetMask, useTriggers = true };

            int count;
            if (isArc)
                count = Physics2D.OverlapCircle(origin, bodyRadius + reach, filter, Buffer);
            else
            {
                GetBox(origin, forward, bodyRadius, data, out var center, out var size, out var angle);
                count = Physics2D.OverlapBox(center, size, angle, filter, Buffer);
            }

            float halfArc = data.arcDegrees * 0.5f;
            int hits = 0;
            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (!col.TryGetComponent<IDamageable>(out var target) || target.IsDead) continue;

                Vector2 point = col.ClosestPoint(origin);
                if (isArc)
                {
                    Vector2 to = point - origin;
                    // 몸에 딱 붙은 경우(거의 0)는 각도 계산이 무의미하므로 통과
                    if (to.sqrMagnitude > 0.0001f && Vector2.Angle(forward, to) > halfArc) continue;
                }
                if (WallQuery.IsBlocked(origin, point)) continue;

                target.TakeHit(new HitInfo(source, HitKind.Melee, point, forward));
                hits++;
            }
            return hits;
        }
    }
}
