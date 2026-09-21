using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 임시 UI(OnGUI)용 한글 폰트. 정식 UI(TMP + 한글 폰트)가 들어오기 전까지 OS 폰트를 쓴다.
    /// 내장 폰트에는 한글이 없어 글자가 깨지므로 맑은 고딕을 우선 요청한다.
    /// </summary>
    public static class OsFont
    {
        static Font _font;

        public static Font Get() =>
            _font != null ? _font : (_font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 24));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _font = null;

        /// <summary>글자 크기/색을 지정한 스타일을 만든다.</summary>
        public static GUIStyle Style(int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            return new GUIStyle(GUI.skin.label)
            {
                font = Get(),
                fontSize = size,
                alignment = anchor,
                wordWrap = true,
                normal = { textColor = color },
            };
        }
    }
}
