using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 근접 무기 수치. M1에서는 주먹만 사용. M2에서 WeaponData로 확장/통합한다.
    /// 거리는 플레이어 지름의 배수로 기록하고 GameConstants.FromPlayerDiameters로 환산한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Fear/Melee Weapon Data", fileName = "MeleeWeaponData")]
    public class MeleeWeaponData : ScriptableObject
    {
        [Tooltip("사거리(플레이어 몸 가장자리 기준, 플레이어 지름의 배수). 주먹 1배")]
        public float rangeInDiameters = 1f;

        [Tooltip("판정 폭(플레이어 지름의 배수). 좁은 직선 범위. 기획에 수치 없음, 임시값 0.5")]
        public float widthInDiameters = 0.5f;

        [Tooltip("공격 발동 딜레이(초). 클릭 후 이 시간이 지난 시점에 판정")]
        public float windupSeconds = 0.1f;

        [Tooltip("공격 간격(초). 클릭 시점부터 다음 클릭까지")]
        public float intervalSeconds = 0.5f;
    }
}
