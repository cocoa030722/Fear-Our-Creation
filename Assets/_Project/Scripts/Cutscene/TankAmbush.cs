using System;
using System.Collections;
using Game.Core;
using Game.Enemies;
using Game.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.Cutscene
{
    /// <summary>
    /// 4-2 배양실 기습 연출. 순서: 시작 시 양옆 유리 수조 안에 적이 잠들어 있음(공격받지도 공격하지도 않음)
    /// → 플레이어가 기준 x를 넘어 전진 → 카메라가 수조를 비추고 슬로우모션(플레이어가 대비할 시간)
    /// → 유리가 깨지며 적이 나와 플레이어에게 경계 상태로 달려듦 → 카메라가 플레이어로 돌아오면 즉시 정상 속도.
    /// 재시작(R)으로 열린 씬에서도 같은 카메라/슬로우 연출을 그대로 재생한다.
    /// </summary>
    public class TankAmbush : MonoBehaviour
    {
        [Serializable]
        public class Tank
        {
            public SpriteRenderer glass;
            public Collider2D glassCollider;
            public EnemyBase[] enemies;
        }

        [SerializeField] Tank[] tanks;
        [Tooltip("플레이어가 이 x를 넘으면 기습 시작(수조에서 충분히 먼 곳)")]
        [SerializeField] float triggerX = -7f;

        [Header("카메라/슬로우모션 (실시간 초)")]
        [Tooltip("수조를 비추는 가상 카메라(평소엔 꺼 둔다)")]
        [SerializeField] CinemachineCamera focusCamera;
        [Tooltip("수조를 비추기 시작한 뒤 유리가 깨지기까지")]
        [SerializeField] float focusSeconds = 1.8f;
        [Tooltip("깨진 뒤 카메라가 머무는 시간")]
        [SerializeField] float holdAfterBreakSeconds = 0.8f;
        [Tooltip("슬로우모션 시간 배율")]
        [SerializeField] float slowScale = 0.3f;
        [SerializeField] float cameraBlendSeconds = 0.8f;

        [Header("깨질 때")]
        [Tooltip("수조마다 깨지는 시간 차(초, 실시간)")]
        [SerializeField] float breakStaggerSeconds = 0.12f;
        [Tooltip("깨진 수조에서 나온 적이 플레이어 위치를 향해 경계하는 시간(초)")]
        [SerializeField] float alertSeconds = 10f;
        [SerializeField] int shardsPerTank = 14;
        [SerializeField] Color shardColor = new Color32(0x8F, 0xD6, 0xD6, 0xC0);
        [SerializeField] Sprite shardSprite;
        [SerializeField] Material shardMaterial;

        PlayerController _player;
        PlayerHealth _health;

        void Awake()
        {
            // 수조 안의 적은 잠든 상태: AI/감지를 끄고 충돌·물리도 꺼서 공격받지도 않는다(수조 유리는 Wall이라 투사체/폭발도 막음)
            foreach (var tank in tanks)
                foreach (var e in tank.enemies)
                    SetDormant(e, true);
        }

        static void SetDormant(EnemyBase enemy, bool dormant)
        {
            enemy.GetComponent<EnemyAI>().enabled = !dormant;
            enemy.GetComponent<Perception>().enabled = !dormant;
            enemy.GetComponent<Collider2D>().enabled = !dormant;
            enemy.GetComponent<Rigidbody2D>().simulated = !dormant;
        }

        void OnDestroy()
        {
            // 연출 도중 R 재시작/씬 이동으로 파괴돼도 시간 배율이 남지 않게
            Time.timeScale = 1f;
        }

        IEnumerator Start()
        {
            _player = FindAnyObjectByType<PlayerController>();
            _health = _player != null ? _player.GetComponent<PlayerHealth>() : null;
            if (_player == null) yield break;

            while (_health.IsDead || _player.transform.position.x < triggerX) yield return null;

            yield return PlayShow();
        }

        IEnumerator PlayShow()
        {
            var brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
            if (brain != null)
            {
                brain.IgnoreTimeScale = true; // 슬로우 중에도 카메라 전환은 같은 속도
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, cameraBlendSeconds);
            }

            _player.ControlLocked = true;
            Time.timeScale = slowScale;
            if (focusCamera != null) focusCamera.gameObject.SetActive(true);

            // 유리에 금이 가듯 떨린다
            for (float t = 0f; t < focusSeconds; t += Time.unscaledDeltaTime)
            {
                float shake = Mathf.Clamp01(t / focusSeconds);
                foreach (var tank in tanks)
                {
                    if (tank.glass == null) continue;
                    var c = tank.glass.color;
                    c.a = 0.35f + 0.25f * Mathf.Abs(Mathf.Sin(t * 18f)) * shake;
                    tank.glass.color = c;
                }
                yield return null;
            }

            yield return BreakAll();
            yield return WaitUnscaled(holdAfterBreakSeconds);

            if (focusCamera != null) focusCamera.gameObject.SetActive(false); // 플레이어 카메라로 복귀
            yield return WaitUnscaled(cameraBlendSeconds);
            // 카메라가 플레이어로 돌아오면 슬로우 없이 즉시 정상 속도로 복귀하고 조작을 돌려준다
            Time.timeScale = 1f;
            if (!_health.IsDead) _player.ControlLocked = false;
        }

        /// <summary>슬로우모션과 무관하게 흐르는 대기(실시간 초).</summary>
        static IEnumerator WaitUnscaled(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null;
        }

        IEnumerator BreakAll()
        {
            foreach (var tank in tanks)
            {
                Break(tank);
                if (breakStaggerSeconds > 0f) yield return WaitUnscaled(breakStaggerSeconds);
            }
        }

        void Break(Tank tank)
        {
            if (tank.glass != null) tank.glass.enabled = false;
            if (tank.glassCollider != null) tank.glassCollider.enabled = false;
            SpawnShards(tank.glass != null ? tank.glass.transform : transform);

            foreach (var e in tank.enemies)
            {
                if (e == null || e.IsDead) continue;
                SetDormant(e, false);
                // 나오자마자 플레이어 위치를 향해 경계(시야에 잡히면 곧바로 추격)
                if (_player != null) e.GetComponent<EnemyAI>().AlertTo(_player.transform.position, alertSeconds);
            }
        }

        void SpawnShards(Transform tank)
        {
            if (shardSprite == null) return;
            Vector2 center = tank.position;
            Vector2 extents = tank.lossyScale * 0.5f;
            for (int i = 0; i < shardsPerTank; i++)
            {
                var go = new GameObject("Glass Shard");
                go.transform.position = center + new Vector2(UnityEngine.Random.Range(-extents.x, extents.x), UnityEngine.Random.Range(-extents.y, extents.y));
                float size = UnityEngine.Random.Range(0.1f, 0.28f);
                go.transform.localScale = new Vector3(size, size, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = shardSprite;
                sr.sharedMaterial = shardMaterial;
                sr.color = shardColor;
                sr.sortingOrder = 4;
                go.AddComponent<Shard>().Launch(UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(2f, 6f), 0.9f);
            }
        }

        /// <summary>유리 파편: 튀어나가며 감속하고 사라진다(게임 시간 기준이라 슬로우 중에는 느리게 흩어진다).</summary>
        class Shard : MonoBehaviour
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
                float dt = Time.deltaTime;
                _age += dt;
                transform.position += (Vector3)(_velocity * dt);
                _velocity = Vector2.Lerp(_velocity, Vector2.zero, 4f * dt);
                transform.Rotate(0f, 0f, 400f * dt);
                var c = _sr.color;
                c.a = Mathf.Clamp01(1f - _age / _life) * 0.75f;
                _sr.color = c;
                if (_age >= _life) Destroy(gameObject);
            }
        }
    }
}
