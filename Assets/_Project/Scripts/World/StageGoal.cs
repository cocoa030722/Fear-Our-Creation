using Game.Core;
using Game.Player;
using Game.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.World
{
    /// <summary>
    /// 스테이지 종점(전화기/엘리베이터). 플레이어가 영역에 들어오면 조작을 잠그고 잠시 뒤 다음 씬을 로드한다.
    /// 소지 무기는 PlayerLoadout.Current(정적)로 이월되고, 다음 씬의 RestartController가 진입 스냅샷을 저장한다.
    /// 다음 씬이 비어 있으면 클리어 문구만 띄운다(다음 스테이지가 아직 없는 빌드용).
    /// </summary>
    public class StageGoal : MonoBehaviour
    {
        [SerializeField] Vector2 size = new Vector2(1.6f, 1.6f);
        [Tooltip("다음 씬 이름(빌드 설정에 있어야 함). 비우면 클리어 문구만 표시")]
        [SerializeField] string nextSceneName;
        [SerializeField] string message = "Stage Clear";
        [Tooltip("도착 후 다음 씬 로드까지의 시간(초)")]
        [SerializeField] float delaySeconds = 1f;

        bool _reached;
        float _loadAt;
        int _playerMask;

        void Awake() => _playerMask = Layers.Mask(Layers.Player);

        void Update()
        {
            if (!_reached)
            {
                var col = Physics2D.OverlapBox(transform.position, size, 0f, _playerMask);
                if (col == null || !col.TryGetComponent<PlayerHealth>(out var health) || health.IsDead) return;
                _reached = true;
                _loadAt = Time.time + delaySeconds;
                if (col.TryGetComponent<PlayerController>(out var controller)) controller.ControlLocked = true;
                return;
            }
            if (!string.IsNullOrEmpty(nextSceneName) && Time.time >= _loadAt) SceneManager.LoadScene(nextSceneName);
        }

        void OnGUI()
        {
            if (!_reached) return;
            var style = OsFont.Style(40, new Color(0.17f, 0.23f, 0.26f, 1f));
            GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 80f), message, style);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.1f, 0.8f, 0.3f, 0.6f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
