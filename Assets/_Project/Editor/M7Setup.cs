using Game.Player;
using Game.Weapons;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M7 셋업: 6스테이지 전용 무노획 가시 창병(FunnelGrunt) SO/프리팹. 6스테이지 씬 구성은 StageSetup.BuildStage6이 담당한다.
    /// 여러 번 실행해도 안전(SO는 id가 비어 있을 때만 초기화, 프리팹은 없을 때만 생성).
    /// </summary>
    public static class M7Setup
    {
        const string Root = "Assets/_Project";
        const string FunnelGruntDataPath = Root + "/Data/Enemies/FunnelGrunt.asset";
        internal const string FunnelGruntPrefabPath = Root + "/Prefabs/Enemies/FunnelGrunt.prefab";
        const string SpearWeaponPath = Root + "/Data/Weapons/Spear.asset";

        internal static bool EnsureAssets()
        {
            if (!M6Setup.EnsureAssets()) return false;

            var config = AssetDatabase.LoadAssetAtPath<PlayerConfig>(M2Setup.PlayerConfigPath);
            var spear = AssetDatabase.LoadAssetAtPath<WeaponData>(SpearWeaponPath);

            // 가시 창병과 수치는 같지만 노획 불가(기획: 6스테이지 적에게서는 무기를 노획할 수 없음)
            M3Setup.InitEnemy(FunnelGruntDataPath, "funnel_grunt", "Funnel Grunt", config, d =>
            {
                d.moveSpeedMultiplier = 0.9f;
                d.weapon = spear;
                d.attackWindupSeconds = 0.2f;
                d.attackIntervalSeconds = 1f;
                d.lootWeapon = null;
            });
            AssetDatabase.SaveAssets();

            M3Setup.EnsureEnemyPrefab(FunnelGruntPrefabPath, FunnelGruntDataPath, "FunnelGrunt", new Color32(0x7A, 0x10, 0x10, 0xFF), false);
            return true;
        }
    }
}
