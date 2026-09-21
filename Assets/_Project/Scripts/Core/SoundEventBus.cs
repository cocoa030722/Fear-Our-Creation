using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>소리 이벤트. 권총 발사 등이 발행하고, 반경 내 적이 경계 상태로 전환되는 처리는 M3에서 구독한다.</summary>
    public readonly struct SoundEvent
    {
        public readonly Vector2 Position;
        /// <summary>어그로 반경(월드 유닛). 벽 차단 없음.</summary>
        public readonly float Radius;
        /// <summary>어그로 지속(초).</summary>
        public readonly float DurationSeconds;

        public SoundEvent(Vector2 position, float radius, float durationSeconds)
        {
            Position = position;
            Radius = radius;
            DurationSeconds = durationSeconds;
        }
    }

    /// <summary>소리 이벤트 버스(정적). 발행자와 수신자(적 AI)를 분리한다.</summary>
    public static class SoundEventBus
    {
        public static event Action<SoundEvent> Emitted;

        public static void Publish(SoundEvent e) => Emitted?.Invoke(e);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Emitted = null;
    }
}
