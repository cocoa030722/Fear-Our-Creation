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

        [Header("맵 종료 연출용(맵 시작 연출이면 비워 둔다)")]
        [Tooltip("0 초과면 맵 시작이 아니라 플레이어가 이 반경(월드 유닛) 안에 들어왔을 때 재생한다")]
        [SerializeField] float triggerRadius;
        [Tooltip("통화가 끝난 뒤 발동할 종점. 종료 통화는 한 번 본 뒤에는 생략하고 바로 발동한다")]
        [SerializeField] Game.World.StageGoal goalAfter;

        string SeenId => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "/" + name;

        Line _current;
        bool _showing;

        IEnumerator Start()
        {
            bool atEnd = triggerRadius > 0f;
            if (!atEnd && SnapshotSystem.LoadedByRestart) yield break;

            var player = FindAnyObjectByType<PlayerController>();
            if (atEnd)
            {
                var health = player != null ? player.GetComponent<PlayerHealth>() : null;
                while (player == null || (health != null && health.IsDead)
                       || Vector2.Distance(player.transform.position, transform.position) > triggerRadius)
                    yield return null;
                // 맵 종료 통화: 처음 도달했을 때는 재시작 여부와 무관하게 반드시 보여 주고, 본 뒤에는 생략한다
                if (SnapshotSystem.HasSeenCutscene(SeenId))
                {
                    if (goalAfter != null) goalAfter.Trigger();
                    yield break;
                }
            }

            if (player != null) player.ControlLocked = true;
            yield return new WaitForSeconds(startDelaySeconds);
            foreach (var line in lines)
            {
                _current = line;
                _showing = true;
                yield return new WaitForSeconds(line.seconds);
            }
            _showing = false;
            if (atEnd) SnapshotSystem.MarkCutsceneSeen(SeenId);
            if (goalAfter != null) goalAfter.Trigger(); // 종점이 조작 잠금을 이어받는다
            else if (player != null) player.ControlLocked = false;
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
