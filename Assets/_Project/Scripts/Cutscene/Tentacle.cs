using UnityEngine;

namespace Game.Cutscene
{
    /// <summary>
    /// 붉은 촉수 모티프(플레이스홀더). LineRenderer를 사인파로 흔들어 뻗었다 움츠린다.
    /// transform.up 방향으로 뻗는다. 붉은색은 적 전용이라 촉수(=적의 일부)에만 쓴다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class Tentacle : MonoBehaviour
    {
        [SerializeField] int segments = 16;
        [Tooltip("최대 길이(월드 유닛)")]
        [SerializeField] float length = 4f;
        [SerializeField] float baseWidth = 0.3f;
        [SerializeField] float waveAmplitude = 0.4f;
        [SerializeField] float waveFrequency = 1.6f;
        [SerializeField] float waveSpeed = 7f;
        [Tooltip("뻗기/움츠리기 속도(길이 비율/초)")]
        [SerializeField] float extendSpeed = 2.5f;

        LineRenderer _line;
        float _extend;
        float _target;
        float _phase;

        void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.15f));
            _line.widthMultiplier = baseWidth;
            _phase = Random.value * 10f;
            _line.enabled = false;
        }

        public void Extend() => _target = 1f;
        public void Retract() => _target = 0f;

        /// <summary>재시작 등으로 연출을 건너뛸 때 즉시 숨긴다.</summary>
        public void HideImmediately()
        {
            _extend = _target = 0f;
            if (_line != null) _line.enabled = false;
        }

        void Update()
        {
            _extend = Mathf.MoveTowards(_extend, _target, extendSpeed * Time.deltaTime);
            _line.enabled = _extend > 0.001f;
            if (!_line.enabled) return;

            Vector2 origin = transform.position;
            Vector2 up = transform.up;
            Vector2 right = transform.right;
            _line.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float along = length * _extend * t;
                float wave = Mathf.Sin(Time.time * waveSpeed + _phase + t * waveFrequency * Mathf.PI * 2f) * waveAmplitude * t;
                _line.SetPosition(i, origin + up * along + right * wave);
            }
        }
    }
}
