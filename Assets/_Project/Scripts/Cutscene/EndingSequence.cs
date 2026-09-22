using Game.Core;
using Game.Enemies;
using Game.Player;
using Game.UI;
using Game.World;
using UnityEngine;

namespace Game.Cutscene
{
    /// <summary>
    /// 6스테이지 자폭 버튼(개발계획 6-7 임시값: 스페이스 상호작용). 반경 안에서 스페이스를 누르면
    /// 조작을 잠그고 화면이 불길색으로 번지며 엔딩 문구를 보여 준다. 다음 씬이 없다(게임의 끝).
    /// 버튼을 누르는 순간 모든 적이 그 자리에서 멈춘다(추격/공격 중단, 무한 스폰도 멈춤).
    /// 자폭 전에 사망하면 R로 재시작(RestartController가 항상 처리하므로 별도 구현 없음).
    /// </summary>
    public class EndingSequence : MonoBehaviour
    {
        [SerializeField] float triggerRadius = 2f;
        [SerializeField] string promptText = "스페이스: 비상 소각 버튼을 누른다";
        [TextArea] [SerializeField] string endingText = "그녀는 끝내 소각 구역을 빠져나오지 못했다.";
        [SerializeField] float fadeSeconds = 3f;
        [SerializeField] Color fireColor = new Color32(0xE0, 0x3A, 0x0E, 0xFF);

        bool _triggered;
        float _fadeT;
        bool _showPrompt;

        PlayerController _player;
        PlayerHealth _health;

        void Start()
        {
            _player = FindAnyObjectByType<PlayerController>();
            _health = _player != null ? _player.GetComponent<PlayerHealth>() : null;
        }

        void Update()
        {
            if (_triggered)
            {
                _fadeT = Mathf.Min(1f, _fadeT + Time.deltaTime / fadeSeconds);
                return;
            }
            if (_player == null || _health == null || _health.IsDead)
            {
                _showPrompt = false;
                return;
            }

            float dist = Vector2.Distance(_player.transform.position, transform.position);
            _showPrompt = dist <= triggerRadius;
            if (_showPrompt && GameInput.Instance.Interact.WasPressedThisFrame()) Trigger();
        }

        void Trigger()
        {
            _triggered = true;
            _showPrompt = false;
            _player.ControlLocked = true;
            FreezeEnemies();
        }

        /// <summary>모든 적을 그 자리에 멈춰 세운다(추격/공격 중단). 무한 스폰도 함께 멈춘다.</summary>
        static void FreezeEnemies()
        {
            foreach (var ai in FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                ai.enabled = false;
                if (ai.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
                if (ai.TryGetComponent<EnemyAttack>(out var attack))
                {
                    attack.Cancel();
                    attack.enabled = false;
                }
            }
            var spawner = FindAnyObjectByType<FunnelSpawner>();
            if (spawner != null) spawner.enabled = false;
        }

        void OnGUI()
        {
            if (_showPrompt)
                GUI.Label(new Rect(0, Screen.height * 0.75f, Screen.width, 40f), promptText, OsFont.Style(24, Color.white));

            if (!_triggered) return;

            var prevColor = GUI.color;
            var c = fireColor;
            c.a = _fadeT;
            GUI.color = c;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prevColor;

            if (_fadeT >= 1f)
                GUI.Label(new Rect(0, Screen.height * 0.42f, Screen.width, 100f), endingText, OsFont.Style(32, Color.white));
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
}
