using Game.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>타이틀 화면. "시작"을 누르면 새 게임으로 1스테이지를 연다.</summary>
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] string titleText = "Fear Our Creation";
        [SerializeField] string firstScene = "Stage1_1";

        void OnGUI()
        {
            // 배경이 검정 계열(PlaceholderPalette.Background)이므로 밝은 색(Player)으로 대비를 준다
            GUI.Label(new Rect(0, Screen.height * 0.26f, Screen.width, 120f), titleText, OsFont.Style(72, PlaceholderPalette.Player));

            var buttonStyle = new GUIStyle(GUI.skin.button) { font = OsFont.Get(), fontSize = 36, normal = { textColor = PlaceholderPalette.Player } };
            buttonStyle.hover.textColor = buttonStyle.normal.textColor;
            buttonStyle.active.textColor = buttonStyle.normal.textColor;
            buttonStyle.focused.textColor = buttonStyle.normal.textColor;
            float w = 260f, h = 64f;
            float bx = (Screen.width - w) * 0.5f;
            float by = Screen.height * 0.55f;

            bool hasSave = SaveSystem.HasSave();
            if (hasSave && GUI.Button(new Rect(bx, by, w, h), "이어하기", buttonStyle))
            {
                Game.Audio.SfxPlayer.Play(Game.Audio.Sfx.UiClick);
                ContinueGame();
            }
            if (hasSave) by += 76f;
            if (GUI.Button(new Rect(bx, by, w, h), hasSave ? "새로 시작" : "시작", buttonStyle))
            {
                Game.Audio.SfxPlayer.Play(Game.Audio.Sfx.UiClick);
                StartGame();
            }
            by += 76f;
            if (GUI.Button(new Rect(bx, by, w, h), "종료", buttonStyle))
            {
                Game.Audio.SfxPlayer.Play(Game.Audio.Sfx.UiClick);
                Application.Quit();
            }
        }

        void StartGame()
        {
            SnapshotSystem.ResetForNewGame();
            SceneManager.LoadScene(firstScene);
        }

        void ContinueGame()
        {
            if (!SaveSystem.TryLoad(out string sceneName)) { StartGame(); return; }
            SnapshotSystem.PrepareContinue();
            SceneManager.LoadScene(sceneName);
        }
    }
}
