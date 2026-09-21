using Game.Core;
using Game.Weapons;
using UnityEngine;

namespace Game.Player
{
    /// <summary>WASD 이동, 마우스 방향 조준 회전, 좌클릭 공격, 스페이스 무기 습득/교체. 사망 시 조작 불가.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerHealth))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] PlayerConfig config;
        [SerializeField] WeaponHolder weapons;

        /// <summary>연출/통화/클리어 중 이동·공격·습득 입력을 막는다(조준은 허용). 재시작은 RestartController가 별도로 받는다.</summary>
        public bool ControlLocked { get; set; }

        Rigidbody2D _rb;
        PlayerHealth _health;
        Camera _camera;
        Vector2 _moveInput;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<PlayerHealth>();
            _health.Died += OnDied;
        }

        void OnDestroy()
        {
            if (_health != null) _health.Died -= OnDied;
        }

        void Start()
        {
            _camera = Camera.main;
        }

        void Update()
        {
            if (_health.IsDead) return;

            _moveInput = Vector2.ClampMagnitude(GameInput.Instance.Move.ReadValue<Vector2>(), 1f);
            Aim();
            if (ControlLocked)
            {
                _moveInput = Vector2.zero;
                return;
            }
            weapons.HandleAttackInput(GameInput.Instance.Attack.WasPressedThisFrame(), GameInput.Instance.Attack.IsPressed());
            if (GameInput.Instance.Interact.WasPressedThisFrame()) weapons.TryInteract();
        }

        void FixedUpdate()
        {
            _rb.linearVelocity = _health.IsDead ? Vector2.zero : _moveInput * config.moveSpeed;
        }

        void Aim()
        {
            if (_camera == null) return;
            Vector2 screen = GameInput.Instance.Point.ReadValue<Vector2>();
            Vector2 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            Vector2 dir = world - (Vector2)transform.position;
            if (dir.sqrMagnitude < 0.0001f) return;
            // 스프라이트의 앞쪽 = 로컬 +Y(transform.up)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        void OnDied()
        {
            _moveInput = Vector2.zero;
            weapons.CancelAttack();
        }
    }
}
