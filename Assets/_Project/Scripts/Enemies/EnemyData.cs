using Game.Player;
using Game.Weapons;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 적 종류별 수치. 이동속도는 플레이어 이동속도의 배수, 시야는 화면 가로 길이의 배수로 기록한다.
    /// 근접 공격의 모양(사거리/부채꼴)은 사용 무기(WeaponData)를 그대로 따르고, 딜레이/간격은 이 SO의 값을 쓴다.
    /// </summary>
    [CreateAssetMenu(menuName = "Fear/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("공통")]
        public string id;
        public string displayName;
        [Tooltip("이동속도의 기준(플레이어 이동속도)")]
        public PlayerConfig playerConfig;
        [Tooltip("몸 지름(플레이어 지름의 배수). 창병 '중간 크기'는 기획에 수치 없음, 임시로 1배")]
        public float bodyDiameterInPlayerDiameters = 1f;

        [Header("이동")]
        [Tooltip("추격/복귀 속도(플레이어 속도의 배수). 가시 창병 0.9, 뚱보 0.8")]
        public float moveSpeedMultiplier = 0.9f;
        [Tooltip("순찰 속도(플레이어 속도의 배수). 기획에 수치 없음, 임시값")]
        public float patrolSpeedMultiplier = 0.4f;
        [Tooltip("회전 속도(도/초). 기획에 수치 없음, 임시값")]
        public float turnDegreesPerSecond = 720f;

        [Header("감지")]
        [Tooltip("시야 전체 각도(도). 기획: 전방 90도")]
        public float sightAngleDegrees = 90f;
        [Tooltip("시야 거리(화면 가로 길이의 배수). 기획: 0.5")]
        public float sightRangeInScreenWidths = 0.5f;
        [Tooltip("플레이어를 놓친 뒤 마지막 목격 지점에서 경계를 유지하는 시간(초). 기획에 수치 없음, 임시값")]
        public float alertSecondsAfterLostSight = 3f;

        [Header("공격")]
        [Tooltip("사용 무기. 근접(창병)은 공격 모양(사거리/부채꼴)을, 원거리(투척병=투척 가시, 척탄병=폭탄알)는 투사체 종류를 가져온다")]
        public WeaponData weapon;
        [Tooltip("공격 발동 딜레이(초). 기획: 0.2, 임의값")]
        public float attackWindupSeconds = 0.2f;
        [Tooltip("공격 간격(초). 기획: 1.0, 임의값")]
        public float attackIntervalSeconds = 1f;
        [Tooltip("원거리: 투사체 속도(플레이어 이동속도의 배수). 기획에 수치 없음, 개발계획 6-1의 임시값 2.5배. 폭탄알도 같은 값")]
        public float projectileSpeedMultiplier = 2.5f;
        [Tooltip("원거리: 이 거리(플레이어 지름의 배수) 이내로 다가오면 멈추고 발사한다. 기획에 수치 없음, 임시값")]
        public float engageDistanceInPlayerDiameters = 6f;

        [Header("뚱보 카운트다운")]
        [Tooltip("첫 피격 후 사망까지의 시간(초). 0이면 일반 적(1회 즉사). 기획: 뚱보 2초")]
        public float deathCountdownSeconds;
        [Tooltip("추가 피격마다 앞당겨지는 시간(초). 기획: 0.2")]
        public float countdownReductionPerHit = 0.2f;

        [Header("노획")]
        [Tooltip("사망 시 떨어뜨리는 무기(없으면 비움)")]
        public WeaponData lootWeapon;
        [Tooltip("노획 잔량. 0이면 무기의 pickupAmmo")]
        public int lootAmmo;

        public float MoveSpeed => playerConfig.moveSpeed * moveSpeedMultiplier;
        public float ProjectileSpeed => playerConfig.moveSpeed * projectileSpeedMultiplier;
        public float PatrolSpeed => playerConfig.moveSpeed * patrolSpeedMultiplier;
    }
}
