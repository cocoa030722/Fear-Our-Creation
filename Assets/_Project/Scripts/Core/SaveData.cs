using System;

namespace Game.Core
{
    /// <summary>디스크에 직렬화되는 저장 데이터(이어하기용). 맵 배치는 씬 자체가 기억하므로 씬 이름 + 소지품만 저장한다.</summary>
    [Serializable]
    public class SaveData
    {
        public string sceneName;
        public string weaponId;
        public int ammo;
    }
}
