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
            if (GameInput.Instance.TogglePause.WasPressedThisFrame())
            {
                SetPaused(!_paused);
                Game.Audio.SfxPlayer.Play(Game.Audio.Sfx.UiClick);
            }
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

            float w = 420f, h = 320f;
            var boxRect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(boxRect, string.Empty);
            GUI.Label(new Rect(boxRect.x, boxRect.y + 16f, w, 44f), "일시정지", OsFont.Style(28, PlaceholderPalette.Player));

            var buttonStyle = new GUIStyle(GUI.skin.button) { font = OsFont.Get(), fontSize = 26, normal = { textColor = PlaceholderPalette.Player } };
            buttonStyle.hover.textColor = buttonStyle.normal.textColor;
            buttonStyle.active.textColor = buttonStyle.normal.textColor;
            buttonStyle.focused.textColor = buttonStyle.normal.textColor;
            float bw = 300f, bh = 56f;
            float bx = (Screen.width - bw) * 0.5f;
            if (GUI.Button(new Rect(bx, boxRect.y + 80f, bw, bh), "계속하기", buttonStyle))
            {
                SetPaused(false);
                Game.Audio.SfxPlayer.Play(Game.Audio.Sfx.UiClick);
            }
            if (GUI.Button(new Rect(bx, boxRect.y + 148f, bw, bh), "다시 시작(R)", buttonStyle))
            {
                SetPaused(false);
                Game.Audio.SfxPlayer.Play(Game.Audio.Sfx.UiClick);
                RestartController.Restart();
            }
            if (GUI.Button(new Rect(bx, boxRect.y + 216f, bw, bh), "타이틀로", buttonStyle))
            {
                SetPaused(false);
                Game.Audio.SfxPlayer.Play(Game.Audio.Sfx.UiClick);
                SnapshotSystem.ResetForNewGame();
                SceneManager.LoadScene("Title");
            }
        }
    }
}
