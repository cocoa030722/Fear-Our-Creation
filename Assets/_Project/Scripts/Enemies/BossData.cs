using Game.Weapons;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// 보스 수치(5스테이지, 개발계획 M6). 체력은 무기별 WeaponData.bossDamage의 합으로 소진된다
    /// (근접 25 / 가시 25÷6 / 권총 25÷8 / 폭탄알 25, 체력 100 = 근접 4번 분량).
    /// </summary>
    [CreateAssetMenu(menuName = "Fear/Boss Data", fileName = "BossData")]
    public class BossData : ScriptableObject
    {
        [Header("공통")]
        public string id;
        public string displayName;
        public float maxHealth = 100f;
        [Tooltip("플레이어를 향해 도는 속도(도/초). 기획에 수치 없음, 임시값")]
        public float turnDegreesPerSecond = 360f;

        [Header("원거리 사격(가시)")]
        [Tooltip("사용 무기(투사체 종류). 기획: 가시 원거리 사격")]
        public WeaponData spikeWeapon;
        [Tooltip("사격 예고선 시간(초). 기획: 0.8초")]
        public float fireTelegraphSeconds = 0.8f;
        [Tooltip("발사 후 다음 예고까지의 간격(초). 기획에 수치 없음, 개발계획 6-2 임시값(예고 → 1발 → 쿨다운)")]
        public float fireCooldownSeconds = 1.5f;

        [Header("잡몹 호출")]
        [Tooltip("호출 간격(초). 기획: 쿨다운 12초")]
        public float summonCooldownSeconds = 12f;
        [Tooltip("호출 모션 시간(초). 이 동안 사격이 중단된다. 기획: 1.5초")]
        public float summonMotionSeconds = 1.5f;
        [Tooltip("동시 소환 수(생존 중인 잡몹이 이 수보다 적을 때만 보충). 기획: 동시 4기")]
        public int summonCount = 4;
        [Tooltip("소환된 잡몹이 플레이어를 향해 경계하는 시간(초). TankAmbush와 같은 방식")]
        public float summonAlertSeconds = 30f;

        [Header("방호복 손상 표시(플레이스홀더)")]
        [Tooltip("손상 단계별(25% 단위, 0~3) 촉수 노출 오버레이 알파")]
        public float[] armorStageAlpha = { 0f, 0.28f, 0.56f, 0.85f };
    }
}
