using System.Collections.Generic;
using Game.Core;
using Game.World;
using UnityEngine;

namespace Game.Enemies
{
    public enum EnemyState
    {
        Idle,   // 대기/순찰
        Alert,  // 경계: 소리가 난 지점(또는 마지막으로 본 지점)으로 이동해 경계 유지
        Chase,  // 추격
        Attack, // 공격(발동 딜레이 중)
        Return, // 경계 해제 후 원위치 복귀
    }

    /// <summary>
    /// 적 상태머신(대기/순찰 → 경계 → 추격/공격 → 복귀).
    /// 시야로 플레이어를 보면 바로 추격, 소리를 들으면 그 지점으로 이동하며 경계(지속 시간은 소리 이벤트 값).
    /// 벽에 막히면 NavGrid 경로를 따라 이동한다.
    /// </summary>
    [RequireComponent(typeof(EnemyBase), typeof(Perception), typeof(EnemyAttack))]
    public class EnemyAI : MonoBehaviour
    {
        [Tooltip("순찰 지점(비우면 제자리 대기). 순서대로 왕복한다")]
        [SerializeField] Transform[] patrolPoints;
        [Tooltip("순찰 지점 도착 후 대기 시간(초). 기획에 수치 없음, 임시값")]
        [SerializeField] float patrolWaitSeconds = 1f;
        [Tooltip("머리 위에 현재 상태를 표시(디버그)")]
        [SerializeField] bool showStateLabel = true;

        const float ArriveRadius = 0.2f;
        const float RepathInterval = 0.5f;

        EnemyBase _enemy;
        Perception _perception;
        EnemyAttack _attack;
        Rigidbody2D _rb;

        EnemyState _state;
        Vector2 _home;
        float _homeAngle;
        Vector2 _alertPoint;
        float _alertUntil;
        Vector2 _lastSeen;
        int _patrolIndex;
        int _patrolDir = 1;
        float _patrolWaitUntil;

        readonly List<Vector2> _path = new List<Vector2>();
        int _pathIndex;
        Vector2 _pathGoal;
        float _nextRepath;
        Vector2 _desiredVelocity;
        TextMesh _label;

        public EnemyState State => _state;

        void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            _perception = GetComponent<Perception>();
            _attack = GetComponent<EnemyAttack>();
            _rb = GetComponent<Rigidbody2D>();
            _perception.Heard += OnHeard;
            _enemy.Died += OnDied;
        }

        void OnDestroy()
        {
            if (_perception != null) _perception.Heard -= OnHeard;
            if (_enemy != null) _enemy.Died -= OnDied;
        }

        void Start()
        {
            _home = transform.position;
            _homeAngle = transform.eulerAngles.z;
            SetState(EnemyState.Idle);
        }

        void OnDied()
        {
            _desiredVelocity = Vector2.zero;
            _attack.Cancel();
            if (_label != null) _label.text = "";
        }

        /// <summary>소리: 추격/공격 중이 아니면 발사 지점으로 이동하며 경계한다.</summary>
        void OnHeard(SoundEvent e)
        {
            if (_enemy.IsDead || _state == EnemyState.Chase || _state == EnemyState.Attack) return;
            _alertPoint = e.Position;
            _alertUntil = Time.time + e.DurationSeconds;
            SetState(EnemyState.Alert);
        }

        void SetState(EnemyState next)
        {
            _state = next;
            _path.Clear();
            if (next == EnemyState.Idle) _patrolWaitUntil = 0f;
        }

        void Update()
        {
            if (_enemy.IsDead) return;

            var player = _perception.Player;
            if (player == null || player.IsDead)
            {
                // 플레이어가 죽으면 그 자리에서 멈춘다(R 재시작으로 씬이 다시 로드됨)
                _desiredVelocity = Vector2.zero;
                _attack.Cancel();
                UpdateLabel();
                return;
            }

            bool seen = _perception.CanSeePlayer(out Vector2 playerPos);
            if (seen) _lastSeen = playerPos;

            switch (_state)
            {
                case EnemyState.Idle: TickIdle(seen); break;
                case EnemyState.Alert: TickAlert(seen); break;
                case EnemyState.Chase: TickChase(seen, playerPos); break;
                case EnemyState.Attack: TickAttack(seen, playerPos); break;
                case EnemyState.Return: TickReturn(seen); break;
            }
            UpdateLabel();
        }

        void FixedUpdate()
        {
            if (_enemy.IsDead) return;
            _rb.linearVelocity = _desiredVelocity;
        }

        void TickIdle(bool seen)
        {
            if (seen) { SetState(EnemyState.Chase); return; }
            if (patrolPoints == null || patrolPoints.Length == 0) { _desiredVelocity = Vector2.zero; return; }

            if (Time.time < _patrolWaitUntil) { _desiredVelocity = Vector2.zero; return; }
            Vector2 goal = patrolPoints[_patrolIndex].position;
            if (MoveTo(goal, _enemy.Data.PatrolSpeed))
            {
                _patrolWaitUntil = Time.time + patrolWaitSeconds;
                if (patrolPoints.Length > 1)
                {
                    if (_patrolIndex + _patrolDir < 0 || _patrolIndex + _patrolDir >= patrolPoints.Length) _patrolDir = -_patrolDir;
                    _patrolIndex += _patrolDir;
                }
            }
        }

        void TickAlert(bool seen)
        {
            if (seen) { SetState(EnemyState.Chase); return; }
            if (Time.time >= _alertUntil) { SetState(EnemyState.Return); return; }
            MoveTo(_alertPoint, _enemy.Data.MoveSpeed); // 도착하면 제자리에서 지속 시간이 끝날 때까지 경계
        }

        void TickChase(bool seen, Vector2 playerPos)
        {
            if (seen)
            {
                Vector2 to = playerPos - (Vector2)transform.position;
                if (to.magnitude <= _attack.TriggerDistance)
                {
                    _desiredVelocity = Vector2.zero;
                    Face(to);
                    if (_attack.CanAttack)
                    {
                        _attack.StartAttack();
                        SetState(EnemyState.Attack);
                    }
                    return;
                }
                MoveTo(playerPos, _enemy.Data.MoveSpeed);
                return;
            }

            // 놓쳤다: 마지막으로 본 지점까지 가서 잠시 경계한 뒤 복귀
            if (MoveTo(_lastSeen, _enemy.Data.MoveSpeed))
            {
                _alertPoint = _lastSeen;
                _alertUntil = Time.time + _enemy.Data.alertSecondsAfterLostSight;
                SetState(EnemyState.Alert);
            }
        }

        void TickAttack(bool seen, Vector2 playerPos)
        {
            _desiredVelocity = Vector2.zero;
            if (seen) Face(playerPos - (Vector2)transform.position); // 발동 딜레이 동안에도 조준을 따라간다
            if (!_attack.IsBusy) SetState(EnemyState.Chase);
        }

        void TickReturn(bool seen)
        {
            if (seen) { SetState(EnemyState.Chase); return; }
            if (MoveTo(_home, _enemy.Data.MoveSpeed))
            {
                transform.rotation = Quaternion.Euler(0f, 0f, _homeAngle);
                SetState(EnemyState.Idle);
            }
        }

        /// <summary>goal로 이동한다. 도착했으면 true. 벽에 막히면 NavGrid 경로를 따라간다.</summary>
        bool MoveTo(Vector2 goal, float speed)
        {
            Vector2 pos = _rb.position;
            if ((goal - pos).sqrMagnitude <= ArriveRadius * ArriveRadius)
            {
                _desiredVelocity = Vector2.zero;
                return true;
            }

            Vector2 waypoint = NextWaypoint(pos, goal);
            // 목표가 벽 여유 거리 안이라 경로가 가까운 빈 칸에서 끝나는 경우: 마지막 경유점에 닿으면 도착으로 본다
            if (_path.Count > 0 && _pathIndex == _path.Count - 1 && (waypoint - pos).sqrMagnitude <= ArriveRadius * ArriveRadius)
            {
                _desiredVelocity = Vector2.zero;
                return true;
            }
            Vector2 dir = (waypoint - pos).normalized;
            _desiredVelocity = dir * speed;
            Face(dir);
            return false;
        }

        Vector2 NextWaypoint(Vector2 pos, Vector2 goal)
        {
            float r = _enemy.BodyRadius * 0.9f;
            if (!WallQuery.IsBlockedCircle(pos, goal, r))
            {
                _path.Clear();
                return goal;
            }
            if (NavGrid.Instance == null) return goal;

            bool needPath = _path.Count == 0 || (goal - _pathGoal).sqrMagnitude > 0.25f || Time.time >= _nextRepath;
            if (needPath)
            {
                NavGrid.Instance.FindPath(pos, goal, _path);
                _pathGoal = goal;
                _pathIndex = 0;
                _nextRepath = Time.time + RepathInterval;
            }
            if (_path.Count == 0) return goal;

            while (_pathIndex < _path.Count - 1 && (_path[_pathIndex] - pos).sqrMagnitude < 0.15f * 0.15f) _pathIndex++;
            return _path[_pathIndex];
        }

        /// <summary>앞쪽(+Y)이 dir을 향하도록 회전 속도 제한을 두고 돌린다.</summary>
        void Face(Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            float target = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.z, target, _enemy.Data.turnDegreesPerSecond * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        void UpdateLabel()
        {
            if (!showStateLabel) return;
            if (_label == null)
            {
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) { showStateLabel = false; return; }
                var go = new GameObject("StateLabel");
                go.transform.SetParent(transform, false);
                _label = go.AddComponent<TextMesh>();
                _label.font = font;
                _label.fontSize = 40;
                _label.characterSize = 0.06f;
                _label.anchor = TextAnchor.LowerCenter;
                _label.alignment = TextAlignment.Center;
                _label.color = new Color(0.17f, 0.23f, 0.26f, 1f);
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = font.material;
                mr.sortingOrder = 5;
            }
            _label.text = _enemy.Data.displayName + "\n" + _state;
            // 적이 회전해도 글자는 위에 고정
            _label.transform.rotation = Quaternion.identity;
            _label.transform.position = transform.position + new Vector3(0f, _enemy.BodyRadius + 0.2f, 0f);
        }
    }
}
