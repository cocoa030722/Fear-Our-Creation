using Game.Player;
using UnityEngine;

namespace Game.UI
{
    /// <summary>플레이어가 반경 안에 있으면 화면 아래에 안내 문구를 띄운다(튜토리얼 힌트, 임시 OnGUI).</summary>
    public class HintZone : MonoBehaviour
    {
        [TextArea] [SerializeField] string text;
        [SerializeField] float radius = 3f;

        PlayerHealth _player;

        void OnGUI()
        {
            if (string.IsNullOrEmpty(text)) return;
            if (_player == null) _player = FindAnyObjectByType<PlayerHealth>();
            if (_player == null || _player.IsDead) return;
            if (((Vector2)_player.transform.position - (Vector2)transform.position).sqrMagnitude > radius * radius) return;

            var style = OsFont.Style(22, new Color(0.17f, 0.23f, 0.26f, 1f));
            var rect = new Rect(Screen.width * 0.15f, Screen.height - 110f, Screen.width * 0.7f, 70f);
            GUI.Label(rect, text, style);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
