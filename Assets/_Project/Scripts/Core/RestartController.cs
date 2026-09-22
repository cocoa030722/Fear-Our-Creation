using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    /// <summary>씬마다 하나 배치. 진입 스냅샷 처리와 R키 재시작(페이드/지연 없이 즉시 리로드)을 담당한다.</summary>
    public class RestartController : MonoBehaviour
    {
        [Tooltip("켜면 스냅샷을 캡처/복원하기 전에 무기를 벗긴다(6스테이지: 진입 시 무기 제거, 재시작해도 맨손 유지)")]
        [SerializeField] bool disarmOnEntry;

        // 플레이어의 Awake/Start보다 먼저 스냅샷을 처리해야 하므로 Awake에서 수행
        void Awake()
        {
            if (disarmOnEntry)
            {
                // Begin()보다 먼저 비워야 재시작용 스냅샷도 무기 없는 상태로 저장된다(기획: 6스테이지 설계 결정)
                PlayerLoadout.Current.WeaponId = string.Empty;
                PlayerLoadout.Current.Ammo = 0;
            }
            SnapshotSystem.Begin();
        }

        void Update()
        {
            if (GameInput.Instance.Restart.WasPressedThisFrame()) Restart();
        }

        public static void Restart()
        {
            SnapshotSystem.RequestRestart();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
