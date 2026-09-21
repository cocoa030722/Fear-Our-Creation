using Game.Core;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 주먹 공격. 클릭 → 발동 딜레이 → 그 시점의 위치/방향으로 판정 범위를 재계산해 좁은 직선 범위 내 대상을 타격한다.
    /// 플레이어 몸 가장자리에서 앞쪽으로 뻗는 상자 판정이며, 벽 뒤 대상은 WallQuery로 걸러낸다.
    /// </summary>
    public class FistAttack : MonoBehaviour
    {
        [SerializeField] MeleeWeaponData data;

        [Header("공격 범위 표시(반투명)")]
        [Tooltip("판정 상자와 같은 크기로 그려지는 스프라이트. 플레이어의 자식이며 +Y가 전방")]
        [SerializeField] SpriteRenderer rangeIndicator;
        [Tooltip("판정 후에도 표시를 유지하는 시간(초)")]
        [SerializeField] float indicatorLingerSeconds = 0.1f;
        [Tooltip("공격하지 않아도 항상 표시(범위 확인/디버그용)")]
        [SerializeField] bool alwaysShowIndicator;

        const float BodyRadius = GameConstants.PlayerDiameter * 0.5f;
        static readonly Collider2D[] Buffer = new Collider2D[16];

        float _nextAttackTime;
        float _hitTime = -1f;
        float _indicatorUntil = -1f;
        int _targetMask;

        void Awake()
        {
            _targetMask = Layers.Mask(Layers.Enemy, Layers.Destructible);
            SetupIndicator();
        }

        /// <summary>표시 사각형을 판정 상자와 동일한 크기/위치로 맞춘다(수치는 SO에서 오므로 SO 변경이 그대로 반영됨).</summary>
        void SetupIndicator()
        {
            if (rangeIndicator == null) return;
            float reach = GameConstants.FromPlayerDiameters(data.rangeInDiameters);
            float width = GameConstants.FromPlayerDiameters(data.widthInDiameters);
            var t = rangeIndicator.transform;
            t.localRotation = Quaternion.identity;
            t.localPosition = new Vector3(0f, BodyRadius + reach * 0.5f, 0f);
            t.localScale = new Vector3(width, reach, 1f);
            rangeIndicator.enabled = alwaysShowIndicator;
        }

        /// <summary>공격 시도. 간격이 지나지 않았으면 무시한다.</summary>
        public bool TryAttack()
        {
            if (Time.time < _nextAttackTime) return false;
            _nextAttackTime = Time.time + data.intervalSeconds;
            _hitTime = Time.time + data.windupSeconds;
            _indicatorUntil = _hitTime + indicatorLingerSeconds;
            return true;
        }

        /// <summary>죽으면 대기 중인 판정을 취소한다.</summary>
        public void Cancel()
        {
            _hitTime = -1f;
            _indicatorUntil = -1f;
        }

        void Update()
        {
            if (rangeIndicator != null)
                rangeIndicator.enabled = alwaysShowIndicator || Time.time < _indicatorUntil;

            if (_hitTime >= 0f && Time.time >= _hitTime)
            {
                _hitTime = -1f;
                ResolveHit();
            }
        }

        void GetBox(out Vector2 center, out Vector2 size, out float angle)
        {
            Vector2 forward = transform.up;
            float reach = GameConstants.FromPlayerDiameters(data.rangeInDiameters);
            float width = GameConstants.FromPlayerDiameters(data.widthInDiameters);
            // 몸 가장자리(BodyRadius)부터 reach만큼 뻗는 상자
            center = (Vector2)transform.position + forward * (BodyRadius + reach * 0.5f);
            size = new Vector2(width, reach); // 로컬 +Y가 전방이므로 세로가 사거리
            angle = transform.eulerAngles.z;
        }

        void ResolveHit()
        {
            GetBox(out var center, out var size, out var angle);
            Vector2 origin = transform.position;
            Vector2 forward = transform.up;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = _targetMask, useTriggers = true };
            int count = Physics2D.OverlapBox(center, size, angle, filter, Buffer);

            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (!col.TryGetComponent<IDamageable>(out var target) || target.IsDead) continue;

                Vector2 point = col.ClosestPoint(origin);
                if (WallQuery.IsBlocked(origin, point)) continue;

                target.TakeHit(new HitInfo(HitSource.Player, HitKind.Melee, point, forward));
            }
        }

        void OnDrawGizmosSelected()
        {
            if (data == null) return;
            GetBox(out var center, out var size, out var angle);
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, angle), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0f));
        }
    }
}
