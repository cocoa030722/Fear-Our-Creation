using System.Collections;
using Game.Core;
using Game.Player;
using Game.UI;
using UnityEngine;

namespace Game.Cutscene
{
    /// <summary>
    /// 4-1 사무실 연출: 플레이어가 반경 안에 들어오면 모든 모니터에 같은 송신문이 동시에 뜨고,
    /// 전화기/컴퓨터에 얽힌 촉수가 키보드를 치듯 움직인다. 조작은 잠그지 않는다(무전투 구간, 걸어 다니며 볼 수 있음).
    /// 재시작(R)으로 열린 씬에서는 연출 없이 최종 상태로 시작한다(무전투 연출 생략 규칙).
    /// 화면 글자는 런타임에 OS 한글 폰트로 만든다(폰트 에셋을 씬에 저장할 수 없어서). 임시 그래픽.
    /// </summary>
    public class OfficeBroadcast : MonoBehaviour
    {
        [SerializeField] SpriteRenderer[] screens;
        [SerializeField] Tentacle[] tentacles;
        [Tooltip("플레이어가 이 반경(월드 유닛) 안에 들어오면 시작")]
        [SerializeField] float triggerRadius = 5f;
        [Tooltip("모니터에 뜨는 짧은 문구(모든 모니터 공통)")]
        [SerializeField] string screenText = "상황은 통제하에\n있음";
        [Tooltip("화면 아래 자막으로 보여 주는 송신문 전문")]
        [TextArea] [SerializeField] string subtitle = "상황은 통제하에 있음. 지원은 필요하지 않음. 전 직원 안전 확인 완료";
        [SerializeField] float subtitleSeconds = 6f;
        [Tooltip("글자가 다 찍히기까지의 시간(초)")]
        [SerializeField] float typeSeconds = 1.2f;
        [SerializeField] Color offColor = new Color32(0x1E, 0x2B, 0x31, 0xFF);
        [SerializeField] Color onColor = new Color32(0xC8, 0xF0, 0xF0, 0xFF);

        TextMesh[] _texts;
        bool _showSubtitle;

        IEnumerator Start()
        {
            _texts = new TextMesh[screens.Length];
            var font = OsFont.Get();
            for (int i = 0; i < screens.Length; i++)
            {
                screens[i].color = offColor;
                _texts[i] = CreateText(screens[i].transform, font);
            }

            if (SnapshotSystem.LoadedByRestart)
            {
                Apply(screenText.Length);
                foreach (var t in tentacles) t.Extend();
                yield break;
            }

            var player = FindAnyObjectByType<PlayerHealth>();
            while (player == null || player.IsDead
                   || Vector2.Distance(player.transform.position, transform.position) > triggerRadius)
                yield return null;

            // 모든 모니터가 동시에 켜지고, 같은 글자가 같은 속도로 찍힌다
            foreach (var t in tentacles) t.Extend();
            _showSubtitle = true;
            for (float t = 0f; t < typeSeconds; t += Time.deltaTime)
            {
                Apply(Mathf.CeilToInt(screenText.Length * (t / typeSeconds)));
                yield return null;
            }
            Apply(screenText.Length);

            yield return new WaitForSeconds(subtitleSeconds);
            _showSubtitle = false;
        }

        void Apply(int visibleChars)
        {
            string shown = screenText.Substring(0, Mathf.Clamp(visibleChars, 0, screenText.Length));
            for (int i = 0; i < screens.Length; i++)
            {
                screens[i].color = visibleChars > 0 ? onColor : offColor;
                _texts[i].text = shown;
            }
        }

        static TextMesh CreateText(Transform parent, Font font)
        {
            var go = new GameObject("Screen Text");
            go.transform.SetParent(parent, false);
            // 부모(모니터)의 스케일을 상쇄해 글자가 찌그러지지 않게 한다
            go.transform.localScale = new Vector3(1f / parent.lossyScale.x, 1f / parent.lossyScale.y, 1f);
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.fontSize = 48;
            tm.characterSize = 0.034f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color32(0x2B, 0x3A, 0x42, 0xFF);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            mr.sortingOrder = 6;
            return tm;
        }

        void OnGUI()
        {
            if (!_showSubtitle) return;
            var box = new Rect(Screen.width * 0.1f, Screen.height - 130f, Screen.width * 0.8f, 90f);
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + 16f, box.y + 8f, box.width - 32f, box.height - 16f),
                subtitle, OsFont.Style(26, Color.white, TextAnchor.MiddleCenter));
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
}
