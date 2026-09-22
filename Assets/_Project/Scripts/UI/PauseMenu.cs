using Game.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>ESC로 토글되는 일시정지 메뉴. 재개/재시작/타이틀로 이동을 제공한다.</summary>
    public class PauseMenu : MonoBehaviour
    {
        bool _paused;

        void Update()
        {
            if (GameInput.Instance.TogglePause.WasPressedThisFrame()) SetPaused(!_paused);
        }

        void SetPaused(bool paused)
        {
            _paused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        void OnDestroy()
        {
            if (_paused) Time.timeScale = 1f;
        }

        void OnGUI()
        {
            if (!_paused) return;

            float w = 320f, h = 240f;
            var boxRect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(boxRect, string.Empty);
            GUI.Label(new Rect(boxRect.x, boxRect.y + 12f, w, 32f), "일시정지", OsFont.Style(22, Color.white));

            var buttonStyle = new GUIStyle(GUI.skin.button) { font = OsFont.Get(), fontSize = 18 };
            float bx = (Screen.width - 220f) * 0.5f;
            if (GUI.Button(new Rect(bx, boxRect.y + 60f, 220f, 44f), "계속하기", buttonStyle))
                SetPaused(false);
            if (GUI.Button(new Rect(bx, boxRect.y + 112f, 220f, 44f), "다시 시작(R)", buttonStyle))
            {
                SetPaused(false);
                RestartController.Restart();
            }
            if (GUI.Button(new Rect(bx, boxRect.y + 164f, 220f, 44f), "타이틀로", buttonStyle))
            {
                SetPaused(false);
                SnapshotSystem.ResetForNewGame();
                SceneManager.LoadScene("Title");
            }
        }
    }
}
