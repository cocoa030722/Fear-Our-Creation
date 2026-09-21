using System;
using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 적 감지. 시야(전방 부채꼴, 벽/시야 차단물이 막음)와 청각(SoundEventBus 구독, 벽 차단 없음)을 분리해 담당한다.
    /// 뒤에서 접근하면 시야에 잡히지 않는다(전방 부채꼴만 감지).
    /// </summary>
    [RequireComponent(typeof(EnemyBase))]
    public class Perception : MonoBehaviour
    {
        EnemyBase _enemy;
        PlayerHealth _player;

        /// <summary>반경 안에서 소리가 났을 때. 지속 시간은 이벤트에 들어 있다.</summary>
        public event Action<SoundEvent> Heard;

        public PlayerHealth Player
        {
            get
            {
                if (_player == null) _player = FindAnyObjectByType<PlayerHealth>();
                return _player;
            }
        }

        void Awake() => _enemy = GetComponent<EnemyBase>();
        void OnEnable() => SoundEventBus.Emitted += OnSound;
        void OnDisable() => SoundEventBus.Emitted -= OnSound;

        void OnSound(SoundEvent e)
        {
            if (_enemy.IsDead) return;
            Vector2 d = e.Position - (Vector2)transform.position;
            if (d.sqrMagnitude <= e.Radius * e.Radius) Heard?.Invoke(e); // 벽 차단 없음
        }

        /// <summary>플레이어가 시야 부채꼴 안에 있고 벽에 가려지지 않았으면 true.</summary>
        public bool CanSeePlayer(out Vector2 position)
        {
            position = default;
            var player = Player;
            if (player == null || player.IsDead) return false;

            Vector2 origin = transform.position;
            position = player.transform.position;
            Vector2 to = position - origin;

            float range = GameConstants.FromScreenWidths(_enemy.Data.sightRangeInScreenWidths);
            if (to.sqrMagnitude > range * range) return false;
            if (to.sqrMagnitude > 0.0001f && Vector2.Angle(transform.up, to) > _enemy.Data.sightAngleDegrees * 0.5f) return false;
            return !WallQuery.IsSightBlocked(origin, position);
        }

        void OnDrawGizmosSelected()
        {
            if (_enemy == null) _enemy = GetComponent<EnemyBase>();
            if (_enemy == null || _enemy.Data == null) return;
            float range = GameConstants.FromScreenWidths(_enemy.Data.sightRangeInScreenWidths);
            float half = _enemy.Data.sightAngleDegrees * 0.5f;
            Gizmos.color = new Color(1f, 0.6f, 0f);
            Vector3 p = transform.position;
            Vector3 a = Quaternion.Euler(0, 0, half) * transform.up * range;
            Vector3 b = Quaternion.Euler(0, 0, -half) * transform.up * range;
            Gizmos.DrawLine(p, p + a);
            Gizmos.DrawLine(p, p + b);
            Gizmos.DrawLine(p + a, p + b);
        }
    }
}
