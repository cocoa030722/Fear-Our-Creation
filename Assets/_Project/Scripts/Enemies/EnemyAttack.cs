using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 적 공격 공통 타이밍: 예고 모션 없이 발동 딜레이 후 판정(수행 시점의 위치/방향으로 계산), 공격 간격 유지.
    /// 딜레이/간격은 EnemyData의 값이고, 무엇을 하는지(근접 판정/투사체 발사)는 파생 클래스가 정한다.
    /// </summary>
    [RequireComponent(typeof(EnemyBase))]
    public abstract class EnemyAttack : MonoBehaviour
    {
        protected EnemyBase Enemy { get; private set; }

        float _nextAttackTime;
        float _hitTime = -1f;

        public bool IsBusy => _hitTime >= 0f;
        public bool CanAttack => !IsBusy && Time.time >= _nextAttackTime;

        /// <summary>플레이어 중심까지 이 거리 이내면 공격을 시작한다(월드 유닛).</summary>
        public abstract float TriggerDistance { get; }

        protected virtual void Awake() => Enemy = GetComponent<EnemyBase>();

        public void StartAttack()
        {
            if (!CanAttack) return;
            _nextAttackTime = Time.time + Enemy.Data.attackIntervalSeconds;
            _hitTime = Time.time + Enemy.Data.attackWindupSeconds;
        }

        public void Cancel() => _hitTime = -1f;

        void Update()
        {
            if (_hitTime < 0f) return;
            if (Enemy.IsDead) { Cancel(); return; }
            if (Time.time < _hitTime) return;

            _hitTime = -1f;
            Perform();
        }

        /// <summary>발동 딜레이가 끝난 시점의 공격 수행.</summary>
        protected abstract void Perform();
    }
}
