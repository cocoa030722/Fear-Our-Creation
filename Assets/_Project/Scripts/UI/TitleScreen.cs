using Game.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>타이틀 화면. "시작"을 누르면 새 게임으로 1스테이지를 연다.</summary>
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] string titleText = "Fear Our Creation";
        [SerializeField] string firstScene = "Stage1";

        void OnGUI()
        {
            // 배경이 흰색(PlaceholderPalette.Background)이라 흰 글자는 보이지 않는다. 어두운 색으로 대비를 준다
            GUI.Label(new Rect(0, Screen.height * 0.26f, Screen.width, 120f), titleText, OsFont.Style(72, PlaceholderPalette.Player));

            var buttonStyle = new GUIStyle(GUI.skin.button) { font = OsFont.Get(), fontSize = 28, normal = { textColor = PlaceholderPalette.Player } };
            float w = 260f, h = 64f;
            float bx = (Screen.width - w) * 0.5f;
            if (GUI.Button(new Rect(bx, Screen.height * 0.55f, w, h), "시작", buttonStyle))
                StartGame();
            if (GUI.Button(new Rect(bx, Screen.height * 0.55f + 76f, w, h), "종료", buttonStyle))
                Application.Quit();
        }

        void StartGame()
        {
            SnapshotSystem.ResetForNewGame();
            SceneManager.LoadScene(firstScene);
        }
    }
}
