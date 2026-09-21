using System;
using System.Collections;
using Game.Core;
using Game.Player;
using Game.UI;
using UnityEngine;

namespace Game.Cutscene
{
    /// <summary>
    /// 맵 시작 통화 연출(2스테이지). 자막 대사를 순서대로 보여 주며 그동안 플레이어 조작을 잠근다.
    /// 무전투 구간 연출은 재시작 시 생략한다(기획): 재시작으로 열린 씬에서는 바로 조작 가능.
    /// 임시 OnGUI 자막이며 정식 UI/대사 시스템은 이후 마일스톤에서 교체한다.
    /// </summary>
    public class PhoneCall : MonoBehaviour
    {
        [Serializable]
        public struct Line
        {
            public string speaker;
            [TextArea] public string text;
            [Tooltip("이 줄을 보여 주는 시간(초)")]
            public float seconds;
        }

        [SerializeField] Line[] lines;
        [Tooltip("연출 시작 전 대기(초)")]
        [SerializeField] float startDelaySeconds = 0.6f;

        Line _current;
        bool _showing;

        IEnumerator Start()
        {
            if (SnapshotSystem.LoadedByRestart) yield break;

            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) player.ControlLocked = true;

            yield return new WaitForSeconds(startDelaySeconds);
            foreach (var line in lines)
            {
                _current = line;
                _showing = true;
                yield return new WaitForSeconds(line.seconds);
            }
            _showing = false;
            if (player != null) player.ControlLocked = false;
        }

        void OnGUI()
        {
            if (!_showing) return;
            var box = new Rect(Screen.width * 0.1f, Screen.height - 150f, Screen.width * 0.8f, 110f);
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + 16f, box.y + 6f, box.width - 32f, 30f),
                _current.speaker, OsFont.Style(20, new Color(0.1f, 0.55f, 0.55f), TextAnchor.MiddleLeft));
            GUI.Label(new Rect(box.x + 16f, box.y + 36f, box.width - 32f, 66f),
                _current.text, OsFont.Style(24, Color.white, TextAnchor.UpperLeft));
        }
    }
}
