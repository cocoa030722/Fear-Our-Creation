using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 뚱보 카운트다운. 첫 피격 후 EnemyData.deathCountdownSeconds 뒤에 사망하고,
    /// 추가 피격마다 countdownReductionPerHit만큼 앞당겨진다. 카운트다운 중에도 추격/공격은 계속한다(기획, 개발계획 6-6).
    /// 시작 표시는 피(붉은 방울)와 몸 깜빡임이며 남은 시간이 줄수록 빨라진다. 폭발 피해는 EnemyBase가 즉사로 처리한다.
    /// </summary>
    [RequireComponent(typeof(EnemyBase))]
    public class DelayedDeath : MonoBehaviour
    {
        [SerializeField] Color bloodColor = new Color32(0xB0, 0x10, 0x10, 0xFF);
        [Tooltip("피 방울 생성 간격(초)")]
        [SerializeField] float bleedIntervalSeconds = 0.12f;

        EnemyBase _enemy;
        float _remaining;
        float _bleedTimer;
        float _pulse;
        Color _baseColor;

        public bool IsCounting { get; private set; }
        public float Remaining => _remaining;

        void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            if (_enemy.Body != null) _baseColor = _enemy.Body.color;
        }

        /// <summary>피격을 흡수한다. true면 아직 살아 있음, false면 카운트다운이 이미 0이 되어 즉사해야 함.</summary>
        public bool Absorb()
        {
            var data = _enemy.Data;
            if (data.deathCountdownSeconds <= 0f) return false;
            if (!IsCounting)
            {
                IsCounting = true;
                _remaining = data.deathCountdownSeconds;
                return true;
            }
            _remaining -= data.countdownReductionPerHit;
            return _remaining > 0f;
        }

        void Update()
        {
            if (!IsCounting || _enemy.IsDead) return;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _enemy.Kill();
                return;
            }

            // 남은 시간이 줄수록 깜빡임이 빨라진다
            float urgency = 1f - Mathf.Clamp01(_remaining / Mathf.Max(0.01f, _enemy.Data.deathCountdownSeconds));
            _pulse += Time.deltaTime * Mathf.Lerp(6f, 22f, urgency);
            var body = _enemy.Body;
            if (body != null) body.color = Color.Lerp(_baseColor, bloodColor, 0.5f + 0.5f * Mathf.Sin(_pulse));

            _bleedTimer -= Time.deltaTime;
            if (_bleedTimer <= 0f)
            {
                _bleedTimer = bleedIntervalSeconds;
                SpawnBlood();
            }
        }

        void SpawnBlood()
        {
            var body = _enemy.Body;
            if (body == null) return;
            var go = new GameObject("Blood");
            Vector2 offset = Random.insideUnitCircle * _enemy.BodyRadius * 0.9f;
            go.transform.position = (Vector2)transform.position + offset;
            float size = Random.Range(0.1f, 0.22f);
            go.transform.localScale = new Vector3(size, size, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = body.sprite;
            sr.sharedMaterial = body.sharedMaterial;
            sr.color = bloodColor;
            sr.sortingOrder = body.sortingOrder - 1;
            go.AddComponent<BloodDrop>().Init(sr, 1.5f);
        }

        /// <summary>바닥에 남아 서서히 옅어지는 피 방울(플레이스홀더).</summary>
        class BloodDrop : MonoBehaviour
        {
            SpriteRenderer _sr;
            float _life, _age;

            public void Init(SpriteRenderer sr, float life)
            {
                _sr = sr;
                _life = life;
            }

            void Update()
            {
                _age += Time.deltaTime;
                var c = _sr.color;
                c.a = Mathf.Clamp01(1f - _age / _life);
                _sr.color = c;
                if (_age >= _life) Destroy(gameObject);
            }
        }
    }
}
