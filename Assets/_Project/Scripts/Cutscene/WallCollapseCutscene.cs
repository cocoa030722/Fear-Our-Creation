using System.Collections;
using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Cutscene
{
    /// <summary>
    /// 1스테이지 오프닝: 촉수가 벽을 흔들다 벽이 무너진다. 연출 동안 플레이어 조작을 잠근다.
    /// 재시작(R)으로 다시 열린 씬에서는 연출 없이 벽이 이미 무너진 상태로 시작한다(빠른 재시작 우선).
    /// 무너진 뒤에는 벽 콜라이더가 꺼져 길이 열린다.
    /// </summary>
    public class WallCollapseCutscene : MonoBehaviour
    {
        [SerializeField] Collider2D wallCollider;
        [SerializeField] SpriteRenderer wallRenderer;
        [SerializeField] Tentacle[] tentacles;

        [Header("파편(플레이스홀더)")]
        [SerializeField] Sprite debrisSprite;
        [SerializeField] Material debrisMaterial;
        [SerializeField] Color debrisColor = new Color32(0x1A, 0xA6, 0xA6, 0xFF);
        [SerializeField] int debrisCount = 10;

        [Header("타이밍(초)")]
        [Tooltip("연출 시작 후 벽이 무너지는 시점")]
        [SerializeField] float breakAtSeconds = 1f;
        [Tooltip("벽이 무너진 뒤 조작이 풀리기까지")]
        [SerializeField] float afterBreakSeconds = 0.8f;

        IEnumerator Start()
        {
            var player = FindAnyObjectByType<PlayerController>();

            if (SnapshotSystem.LoadedByRestart)
            {
                SetBroken();
                yield break;
            }

            if (player != null) player.ControlLocked = true;
            foreach (var t in tentacles) t.Extend();

            yield return new WaitForSeconds(breakAtSeconds);
            SetBroken();
            SpawnDebris();

            yield return new WaitForSeconds(afterBreakSeconds);
            foreach (var t in tentacles) t.Retract();
            if (player != null) player.ControlLocked = false;
        }

        void SetBroken()
        {
            if (wallCollider != null) wallCollider.enabled = false;
            if (wallRenderer != null) wallRenderer.enabled = false;
            if (SnapshotSystem.LoadedByRestart)
                foreach (var t in tentacles) t.HideImmediately();
        }

        void SpawnDebris()
        {
            if (wallRenderer == null || debrisSprite == null) return;
            Vector2 center = wallRenderer.transform.position;
            Vector2 extents = wallRenderer.transform.localScale * 0.5f;
            for (int i = 0; i < debrisCount; i++)
            {
                var go = new GameObject("Debris");
                go.transform.position = center + new Vector2(Random.Range(-extents.x, extents.x), Random.Range(-extents.y, extents.y));
                float size = Random.Range(0.12f, 0.3f);
                go.transform.localScale = new Vector3(size, size, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = debrisSprite;
                sr.sharedMaterial = debrisMaterial;
                sr.color = debrisColor;
                sr.sortingOrder = 3;
                go.AddComponent<Debris>().Launch(Random.insideUnitCircle.normalized * Random.Range(1.5f, 4f), 0.7f);
            }
        }

        /// <summary>파편: 튀어나가며 감속하고 서서히 사라진다.</summary>
        class Debris : MonoBehaviour
        {
            Vector2 _velocity;
            float _life, _age;
            SpriteRenderer _sr;

            public void Launch(Vector2 velocity, float life)
            {
                _velocity = velocity;
                _life = life;
                _sr = GetComponent<SpriteRenderer>();
            }

            void Update()
            {
                _age += Time.deltaTime;
                transform.position += (Vector3)(_velocity * Time.deltaTime);
                _velocity = Vector2.Lerp(_velocity, Vector2.zero, 4f * Time.deltaTime);
                transform.Rotate(0f, 0f, 360f * Time.deltaTime);
                var c = _sr.color;
                c.a = Mathf.Clamp01(1f - _age / _life);
                _sr.color = c;
                if (_age >= _life) Destroy(gameObject);
            }
        }
    }
}
