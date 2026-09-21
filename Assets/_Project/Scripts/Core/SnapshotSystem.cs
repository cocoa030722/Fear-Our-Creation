using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 맵 진입 스냅샷과 R키 재시작 (개발계획 3.3). 씬 리로드 + 진입 시 PlayerLoadout 주입 방식.
    /// 흐름: 씬 시작 시 Begin() → 재시작이 아니면 Capture, 재시작이면 Restore. R키 → RequestRestart() → 씬 리로드.
    /// </summary>
    public static class SnapshotSystem
    {
        static PlayerLoadout _snapshot;

        /// <summary>이번 씬 로드가 R키 재시작으로 인한 것인지. 무전투 연출 스킵 판단에 쓴다.</summary>
        public static bool IsRestart { get; private set; }

        public static bool HasSnapshot => _snapshot != null;
        public static PlayerLoadout Snapshot => _snapshot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _snapshot = null;
            IsRestart = false;
            PlayerLoadout.Current = new PlayerLoadout();
        }

        /// <summary>맵 진입 시점에 한 번 호출. 재시작이면 저장된 상태를 복원하고, 아니면 현재 상태를 캡처한다.</summary>
        public static void Begin()
        {
            if (IsRestart && _snapshot != null) Restore();
            else Capture();
            IsRestart = false;
        }

        public static void Capture()
        {
            _snapshot = PlayerLoadout.Current.Clone();
        }

        public static void Restore()
        {
            if (_snapshot == null) return;
            PlayerLoadout.Current = _snapshot.Clone();
        }

        /// <summary>재시작 요청. 다음 Begin()이 Restore로 동작하도록 표시한다(씬 리로드는 호출 측 책임).</summary>
        public static void RequestRestart()
        {
            IsRestart = true;
        }
    }
}
