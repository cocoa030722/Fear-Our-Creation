using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 폭발 오브젝트 수치. 탄약 상자와 세척수 탱크는 이 SO만 다르다(공용 프리팹).
    /// 폭발 지름은 플레이어 지름의 배수로 기록한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Fear/Explosive Data", fileName = "ExplosiveData")]
    public class ExplosiveData : ScriptableObject
    {
        public string id;
        public string displayName;
        [Tooltip("폭발 지름(플레이어 지름의 배수). 기획: 탄약 상자 4, 세척수 탱크 6")]
        public float explosionDiameters = 4f;
        [Tooltip("파손 후 폭발까지의 지연(초). 연쇄가 한 프레임에 재귀하지 않도록 큐처럼 처리한다")]
        public float chainDelaySeconds = 0.08f;
        [Tooltip("폭발 표시 색(플레이스홀더). 붉은색은 적 전용이라 쓰지 않는다")]
        public Color effectColor = new Color32(0xFF, 0xC8, 0x00, 0xB0);
        [Tooltip("폭발 표시 지속(초). 세척수는 더 오래 퍼지는 물결")]
        public float effectSeconds = 0.25f;
        [Tooltip("몸통 크기(플레이어 지름의 배수)")]
        public float bodyDiameters = 0.8f;
        [Tooltip("몸통 색")]
        public Color bodyColor = new Color32(0x1A, 0xA6, 0xA6, 0xFF);
    }
}
