using UnityEngine;

namespace Game.Player
{
    /// <summary>플레이어 수치. 다른 모든 이동속도의 기준값이다(개발계획 6절 임시값).</summary>
    [CreateAssetMenu(menuName = "Fear/Player Config", fileName = "PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Tooltip("이동속도(유닛/초). 기획에 절대값이 없어 임시값 5")]
        public float moveSpeed = 5f;
    }
}
