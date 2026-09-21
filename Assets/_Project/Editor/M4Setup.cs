using Game.Core;
using Game.Enemies;
using Game.Weapons;
using Game.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M4 셋업: 뚱보(SO/프리팹), 폭발 오브젝트(SO/프리팹), M4 샌드박스 씬. 여러 번 실행해도 안전
    /// (SO는 id가 비어 있을 때만 초기화, 프리팹은 없을 때만 생성, 씬은 매번 새로 만든다).
    /// </summary>
    public static class M4Setup
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/M4_Sandbox.unity";
        const string FattyDataPath = Root + "/Data/Enemies/Fatty.asset";
        internal const string FattyPrefabPath = Root + "/Prefabs/Enemies/Fatty.prefab";
        const string ExplosiveDir = Root + "/Data/Explosives";
        const string AmmoCrateDataPath = ExplosiveDir + "/AmmoCrate.asset";
        const string WashTankDataPath = ExplosiveDir + "/WashTank.asset";
        internal const string AmmoCratePrefabPath = Root + "/Prefabs/Props/AmmoCrate.prefab";
        internal const string WashTankPrefabPath = Root + "/Prefabs/Props/WashTank.prefab";
        const string ExplosionPrefabPath = Root + "/Prefabs/Projectiles/ExplosionEffect.prefab";
        const string PickupPrefabPath = Root + "/Prefabs/Weapons/WeaponPickup.prefab";

        [MenuItem("Tools/Fear/M4 Setup")]
        public static void Run()
        {
            if (!EnsureAssets()) return;
            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M4] 셋업 완료: " + ScenePath);
        }

        internal static bool EnsureAssets()
        {
            if (!M3Setup.EnsureAssets()) return false;

            var config = AssetDatabase.LoadAssetAtPath<Game.Player.PlayerConfig>(M2Setup.PlayerConfigPath);
            var spear = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "/Data/Weapons/Spear.asset");

            // 기획 표: 뚱보 이동속도 0.8배, 카운트다운 2초/0.2초 감소, 공격 간격은 창병과 동일(1.0초). 몸 크기는 수치 없음(임시 1.5배)
            M3Setup.InitEnemy(FattyDataPath, "fatty", "Fatty", config, d =>
            {
                d.bodyDiameterInPlayerDiameters = 1.5f;
                d.moveSpeedMultiplier = 0.8f;
                d.weapon = spear;
                d.attackWindupSeconds = 0.2f;
                d.attackIntervalSeconds = 1f;
                d.deathCountdownSeconds = 2f;
                d.countdownReductionPerHit = 0.2f;
                d.lootWeapon = spear;
            });
            AssetDatabase.SaveAssets();

            M3Setup.EnsureEnemyPrefab(FattyPrefabPath, FattyDataPath, "Fatty", new Color32(0x7A, 0x10, 0x10, 0xFF), false);
            var root = PrefabUtility.LoadPrefabContents(FattyPrefabPath);
            if (root.GetComponent<DelayedDeath>() == null)
            {
                root.AddComponent<DelayedDeath>();
                PrefabUtility.SaveAsPrefabAsset(root, FattyPrefabPath);
            }
            PrefabUtility.UnloadPrefabContents(root);

            if (!AssetDatabase.IsValidFolder(ExplosiveDir)) AssetDatabase.CreateFolder(Root + "/Data", "Explosives");
            // 기획 표: 탄약 상자 폭발 지름 4배, 세척수 탱크 6배(지름 기준, 반경 아님)
            InitExplosive(AmmoCrateDataPath, "ammo_crate", "Ammo Crate", d =>
            {
                d.explosionDiameters = 4f;
                d.effectColor = new Color32(0xFF, 0xC8, 0x00, 0xB0);
                d.effectSeconds = 0.25f;
                d.bodyDiameters = 0.8f;
            });
            InitExplosive(WashTankDataPath, "wash_tank", "Wash Tank", d =>
            {
                d.explosionDiameters = 6f;
                d.effectColor = new Color32(0x9B, 0xE0, 0x6A, 0x90); // 독성 세제의 물결(연두, 플레이스홀더)
                d.effectSeconds = 0.6f;
                d.bodyDiameters = 1.2f;
            });
            AssetDatabase.SaveAssets();

            EnsureProp(AmmoCratePrefabPath, AmmoCrateDataPath, "AmmoCrate", false);
            EnsureProp(WashTankPrefabPath, WashTankDataPath, "WashTank", true);
            return true;
        }

        static void InitExplosive(string path, string id, string displayName, System.Action<ExplosiveData> setValues)
        {
            var data = AssetDatabase.LoadAssetAtPath<ExplosiveData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ExplosiveData>();
                AssetDatabase.CreateAsset(data, path);
            }
            if (!string.IsNullOrEmpty(data.id)) return;
            data.id = id;
            data.displayName = displayName;
            setValues(data);
            EditorUtility.SetDirty(data);
        }

        static void EnsureProp(string prefabPath, string dataPath, string name, bool round)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;
            var data = AssetDatabase.LoadAssetAtPath<ExplosiveData>(dataPath);
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Circle.png");
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Square.png");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/SpriteUnlit.mat");

            float size = data.bodyDiameters * GameConstants.PlayerDiameter;
            var go = M1Setup.CreateSprite(name, round ? circle : square, unlit, data.bodyColor, Vector2.zero, Vector2.one * size, Layers.Destructible);
            Collider2D col;
            if (round) { var c = go.AddComponent<CircleCollider2D>(); c.radius = 0.5f; col = c; }
            else { var b = go.AddComponent<BoxCollider2D>(); b.size = Vector2.one; col = b; }

            // 노란 경고 표식: 보급품/벽과 혼동되지 않게 한다(색 규칙)
            var mark = M1Setup.CreateSprite("HazardMark", circle, unlit, PlaceholderPalette.Hazard, Vector2.zero, Vector2.one * 0.4f, Layers.Destructible);
            mark.transform.SetParent(go.transform, false);
            mark.GetComponent<SpriteRenderer>().sortingOrder = 1;

            var prop = go.AddComponent<ExplosiveProp>();
            M1Setup.SetField(prop, "data", data);
            M1Setup.SetField(prop, "body", go.GetComponent<SpriteRenderer>());
            M1Setup.SetField(prop, "hitCollider", col);
            M1Setup.SetField(prop, "effectPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPrefabPath).GetComponent<ExplosionEffect>());

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            M2Setup.CreateRig();

            var square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Square.png");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/SpriteUnlit.mat");
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalog>(Root + "/Data/Weapons/WeaponCatalog.asset");

            new GameObject("NavGrid").AddComponent<NavGrid>();
            M3Setup.CreateWall("Wall Top", square, unlit, new Vector2(0f, 9.25f), new Vector2(31f, 0.5f));
            M3Setup.CreateWall("Wall Bottom", square, unlit, new Vector2(0f, -9.25f), new Vector2(31f, 0.5f));
            M3Setup.CreateWall("Wall Left", square, unlit, new Vector2(-15.25f, 0f), new Vector2(0.5f, 19f));
            M3Setup.CreateWall("Wall Right", square, unlit, new Vector2(15.25f, 0f), new Vector2(0.5f, 19f));
            // 폭발 차단 확인용 벽(벽 뒤의 상자/대상은 폭발에 안전)
            M3Setup.CreateWall("Wall Blast Shield", square, unlit, new Vector2(-8f, -5f), new Vector2(0.5f, 5f));

            var fatty = AssetDatabase.LoadAssetAtPath<GameObject>(FattyPrefabPath);
            // 뚱보 두 명: 카운트다운(2초)과 추가 공격 단축 확인, 폭탄알/폭발은 즉사 확인
            M3Setup.PlaceGrunt(fatty, "Fatty A", new Vector2(11f, 5f), 270f);
            M3Setup.PlaceGrunt(fatty, "Fatty B", new Vector2(11f, -5f), 270f);
            // 일반 창병 한 명: 폭발 즉사 확인
            M3Setup.PlaceGrunt(AssetDatabase.LoadAssetAtPath<GameObject>(M3Setup.SpearGruntPrefabPath), "Grunt", new Vector2(12f, 0f), 90f);

            var crate = AssetDatabase.LoadAssetAtPath<GameObject>(AmmoCratePrefabPath);
            var tank = AssetDatabase.LoadAssetAtPath<GameObject>(WashTankPrefabPath);
            // 연쇄: 탄약 상자 3개가 폭발 범위(지름 4) 안에 이어져 있음
            PlaceProp(crate, "Crate Chain 1", new Vector2(-4f, 4f));
            PlaceProp(crate, "Crate Chain 2", new Vector2(-2f, 4f));
            PlaceProp(crate, "Crate Chain 3", new Vector2(0f, 4f));
            // 벽 뒤 상자: 벽 반대편에서 터진 폭발은 이 상자를 건드리지 못함
            PlaceProp(crate, "Crate Behind Wall", new Vector2(-8.9f, -5f));
            PlaceProp(crate, "Crate Blast Source", new Vector2(-7f, -5f));
            // 세척수 탱크: 더 넓은 범위(지름 6)
            PlaceProp(tank, "Wash Tank", new Vector2(-4f, -1f));

            // 테스트 무기: 가시창, 폭탄알(뚱보 즉사 확인), 투척 가시(원거리 추가 공격)
            PlacePickup(catalog.Find("spear"), new Vector2(2f, -1f));
            PlacePickup(catalog.Find("bomb"), new Vector2(2f, 1f));
            PlacePickup(catalog.Find("thorn"), new Vector2(2f, 2.5f));

            EditorSceneManager.SaveScene(scene, ScenePath);
            M2Setup.SetBuildScenes();
        }

        internal static void PlaceProp(GameObject prefab, string name, Vector2 pos)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.position = pos;
        }

        static void PlacePickup(WeaponData data, Vector2 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = "Pickup " + data.displayName;
            go.transform.position = position;
            var so = new SerializedObject(go.GetComponent<WeaponPickup>());
            so.FindProperty("data").objectReferenceValue = data;
            so.FindProperty("ammo").intValue = data.pickupAmmo;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
