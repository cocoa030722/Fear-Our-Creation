using System;

namespace Game.Core
{
    /// <summary>
    /// 스냅샷에 저장되는 플레이어 소지품. 맵 배치(적/무기/시신)는 씬 리로드로 복원되므로 이것만 직렬화한다.
    /// M1에는 무기가 주먹뿐이라 비어 있는 골격. M2에서 무기 ID/잔량을 채운다.
    /// </summary>
    [Serializable]
    public class PlayerLoadout
    {
        /// <summary>현재 무기 ID. null 또는 빈 문자열이면 주먹.</summary>
        public string WeaponId;
        public int Ammo;

        public PlayerLoadout Clone() => (PlayerLoadout)MemberwiseClone();

        /// <summary>씬 사이로 이월되는 현재 소지품. 씬 시작 시 플레이어가 읽어 간다.</summary>
        public static PlayerLoadout Current = new PlayerLoadout();
    }
}
