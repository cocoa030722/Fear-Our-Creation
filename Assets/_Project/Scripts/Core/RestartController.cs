using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    /// <summary>씬마다 하나 배치. 진입 스냅샷 처리와 R키 재시작(페이드/지연 없이 즉시 리로드)을 담당한다.</summary>
    public class RestartController : MonoBehaviour
    {
        // 플레이어의 Awake/Start보다 먼저 스냅샷을 처리해야 하므로 Awake에서 수행
        void Awake()
        {
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
