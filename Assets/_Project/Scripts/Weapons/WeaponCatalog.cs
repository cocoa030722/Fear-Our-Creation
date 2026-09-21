using UnityEngine;

namespace Game.Weapons
{
    /// <summary>모든 무기 SO 목록. PlayerLoadout의 무기 ID로 SO를 찾는 데 쓴다.</summary>
    [CreateAssetMenu(menuName = "Fear/Weapon Catalog", fileName = "WeaponCatalog")]
    public class WeaponCatalog : ScriptableObject
    {
        public WeaponData[] weapons;

        public WeaponData Find(string id)
        {
            if (string.IsNullOrEmpty(id) || weapons == null) return null;
            foreach (var w in weapons)
                if (w != null && w.id == id) return w;
            return null;
        }
    }
}
