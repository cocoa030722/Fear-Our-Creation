using System.Collections;
using Game.Core;
using Game.Enemies;
using Game.Player;
using Game.UI;
using UnityEngine;

namespace Game.Cutscene
{
    /// <summary>
    /// 5스테이지 진입 연출: 소장(=보스) 등장 → 또 다른 직원을 살해 → 방호복 얼굴 판 너머로 촉수 공개 → 보스전 개시(Boss.Activate()).
    /// 재시작(R)으로 열린 씬에서는 연출을 생략하고 곧바로 보스가 활성화된 최종 상태로 시작한다(개발계획, 무전투 연출과 동일 규칙).
    /// 대사 자막은 OfficeBroadcast/PhoneCall과 같은 임시 OnGUI 방식.
    /// </summary>
    public class BossIntro : MonoBehaviour
    {
        [SerializeField] Boss boss;
        [Tooltip("보스 몸에 붙은 촉수(공개 순간 뻗는다). Boss와 같은 프리팹의 자식")]
        [SerializeField] Tentacle[] tentacles;
        [Tooltip("소장에게 살해당하는 다른 직원(플레이스홀더 그래픽)")]
        [SerializeField] SpriteRenderer employeeBody;
        [SerializeField] Color employeeDeadColor = new Color(0.45f, 0.12f, 0.12f, 0.6f);

        [System.Serializable]
        public struct Caption
        {
            [TextArea] public string text;
            public float seconds;
        }

        [SerializeField] Caption[] captions =
        {
            new Caption { text = "소장이 엘리베이터 앞에 서 있다.", seconds = 1.8f },
            new Caption { text = "소장이 또 다른 직원을 뒤돌아보지도 않고 베어 넘겼다.", seconds = 2.2f },
            new Caption { text = "무언가 잘못됐다…", seconds = 1.4f },
            new Caption { text = "방호복 얼굴 판 너머, 전부 촉수였다.", seconds = 2f },
        };

        Color _bossBaseColor;
        Caption _current;
        bool _showing;

        IEnumerator Start()
        {
            var player = FindAnyObjectByType<PlayerController>();
            var body = boss != null ? boss.GetComponentInChildren<SpriteRenderer>() : null;
            if (body != null) _bossBaseColor = body.color;

            if (SnapshotSystem.LoadedByRestart)
            {
                if (employeeBody != null) employeeBody.color = employeeDeadColor;
                foreach (var t in tentacles) t.Extend();
                if (body != null) body.color = PlaceholderPalette.Enemy;
                if (boss != null) boss.Activate();
                yield break;
            }

            if (player != null) player.ControlLocked = true;

            // 1) 등장
            yield return Say(captions[0]);

            // 2) 살해
            if (employeeBody != null) employeeBody.color = employeeDeadColor;
            yield return Say(captions[1]);

            // 3) 얼굴 판 너머 공개: 촉수가 뻗고 몸 색이 인간(임시 슬레이트)에서 적(붉은색)으로 바뀐다
            yield return Say(captions[2]);
            foreach (var t in tentacles) t.Extend();
            if (body != null) body.color = PlaceholderPalette.Enemy;
            yield return Say(captions[3]);

            if (boss != null) boss.Activate();
            if (player != null) player.ControlLocked = false;
        }

        IEnumerator Say(Caption c)
        {
            _current = c;
            _showing = true;
            yield return new WaitForSeconds(c.seconds);
            _showing = false;
        }

        void OnGUI()
        {
            if (!_showing) return;
            var box = new Rect(Screen.width * 0.1f, Screen.height - 130f, Screen.width * 0.8f, 90f);
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + 16f, box.y + 8f, box.width - 32f, box.height - 16f),
                _current.text, OsFont.Style(26, Color.white, TextAnchor.MiddleCenter));
        }
    }
}
