using UnityEngine;

namespace Game.Weapons
{
    public enum WeaponKind
    {
        Melee,  // 주먹, 가시창
        Thrown, // 투척 가시
        Bomb,   // 폭탄알
        Gun,    // 권총
    }

    /// <summary>
    /// 무기 수치 전체(주먹/가시창/투척 가시/폭탄알/권총). 수치는 여기서만 바꾼다.
    /// 거리는 플레이어 지름의 배수, 어그로 반경은 화면 가로 배수로 기록하고 GameConstants로 환산한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Fear/Weapon Data", fileName = "WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [Header("공통")]
        [Tooltip("PlayerLoadout에 저장되는 식별자. 비워 두면 안 됨")]
        public string id;
        [Tooltip("화면 표시 이름(디버그 HUD/픽업 라벨)")]
        public string displayName;
        public WeaponKind kind;
        [Tooltip("공격 발동 딜레이(초). 클릭 후 이 시간이 지난 시점에 발동")]
        public float windupSeconds = 0.1f;
        [Tooltip("공격 간격(초). 클릭 시점부터 다음 공격까지")]
        public float intervalSeconds = 0.5f;
        [Tooltip("픽업 표시 색(붉은색은 적 전용, 노란색은 폭발물 전용)")]
        public Color pickupColor = new Color32(0x1A, 0xA6, 0xA6, 0xFF);

        [Header("탄약 (근접은 무한: 소지 상한 0)")]
        [Tooltip("소지 상한. 0이면 탄약 개념 없음(근접)")]
        public int maxAmmo;
        [Tooltip("노획/기본 배치 시 잔량(투척 가시 6개 묶음, 권총 16발 탄창 등)")]
        public int pickupAmmo;
        [Tooltip("같은 무기를 들고 있을 때 습득하면 잔량을 합산(투척 가시, 폭탄알)")]
        public bool stackable;

        [Header("근접")]
        [Tooltip("사거리(플레이어 몸 가장자리 기준, 지름 배수)")]
        public float rangeInDiameters = 1f;
        [Tooltip("직선 판정 폭(지름 배수). 부채꼴 각도가 0일 때만 사용. 기획에 수치 없음, 임시값")]
        public float widthInDiameters = 0.5f;
        [Tooltip("부채꼴 전체 각도(도). 0이면 직선 상자 판정. 기획에 수치 없음, 임시값")]
        [Range(0f, 360f)] public float arcDegrees;

        [Header("투사체 (투척 가시, 폭탄알, 권총탄)")]
        [Tooltip("비행 속도(지름/초). 폭탄알은 투척 가시와 같음. 기획에 수치 없음, 임시값")]
        public float projectileSpeedInDiameters = 20f;
        [Tooltip("적에게 명중한 가시를 회수할 수 있는 확률")]
        [Range(0f, 1f)] public float hitRecoverChance = 0.5f;
        [Tooltip("명중하지 않은(벽 등) 가시를 회수할 수 있는 확률")]
        [Range(0f, 1f)] public float missRecoverChance = 1f;

        [Header("폭탄알")]
        [Tooltip("폭발 지름(플레이어 지름 배수)")]
        public float explosionDiameters = 4f;

        [Header("권총 소리")]
        [Tooltip("발사 시 어그로 반경(화면 가로 배수). 벽 차단 없음")]
        public float aggroRadiusInScreenWidths = 1f;
        [Tooltip("어그로 지속(초)")]
        public float aggroSeconds = 5f;

        [Header("보스전")]
        [Tooltip("보스 체력 100 기준 피해량. 근접 25 / 가시 25÷6 / 권총 25÷8 / 폭탄알 25")]
        public float bossDamage = 25f;

        /// <summary>탄약을 소모하는 무기인가(근접은 무한).</summary>
        public bool UsesAmmo => maxAmmo > 0;
    }
}
