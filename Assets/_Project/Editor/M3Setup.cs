using Game.Core;
using Game.Enemies;
using Game.Player;
using Game.Weapons;
using Game.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M3 셋업: 적 SO/프리팹과 M3 샌드박스 씬. 여러 번 실행해도 안전
    /// (SO는 id가 비어 있을 때만 초기화하므로 조정한 수치는 덮어쓰지 않고, 씬은 매번 새로 만든다).
    /// 씬 배치를 바꿀 때는 이 스크립트를 수정한다(에디터에서 직접 고친 씬은 재실행 시 사라짐).
    /// </summary>
    public static class M3Setup
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/M3_Sandbox.unity";
        const string EnemyDir = Root + "/Data/Enemies";
        const string SpearGruntDataPath = EnemyDir + "/SpearGrunt.asset";
        const string ThrowerDataPath = EnemyDir + "/ThornThrower.asset";
        const string GrenadierDataPath = EnemyDir + "/Grenadier.asset";
        internal const string SpearGruntPrefabPath = Root + "/Prefabs/Enemies/SpearGrunt.prefab";
        internal const string ThrowerPrefabPath = Root + "/Prefabs/Enemies/ThornThrower.prefab";
        internal const string GrenadierPrefabPath = Root + "/Prefabs/Enemies/Grenadier.prefab";
        const string ProjectilePrefabPath = Root + "/Prefabs/Projectiles/Projectile.prefab";
        const string ThornWeaponPath = Root + "/Data/Weapons/ThrownThorn.asset";
        const string BombWeaponPath = Root + "/Data/Weapons/BombShell.asset";
        const string SpearWeaponPath = Root + "/Data/Weapons/Spear.asset";
        const string PickupPrefabPath = Root + "/Prefabs/Weapons/WeaponPickup.prefab";

        [MenuItem("Tools/Fear/M3 Setup")]
        public static void Run()
        {
            if (!EnsureAssets()) return;
            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M3] 셋업 완료: " + ScenePath);
        }

        internal static bool EnsureAssets()
        {
            if (!M2Setup.EnsureAssets()) return false;

            var config = AssetDatabase.LoadAssetAtPath<PlayerConfig>(M2Setup.PlayerConfigPath);
            var spear = AssetDatabase.LoadAssetAtPath<WeaponData>(SpearWeaponPath);
            var thorn = AssetDatabase.LoadAssetAtPath<WeaponData>(ThornWeaponPath);
            var bomb = AssetDatabase.LoadAssetAtPath<WeaponData>(BombWeaponPath);

            // 기획 표의 수치는 그대로, 기획에 없는 값은 임시값(개발계획 6절). id가 비어 있을 때만 초기화하므로 조정한 값은 유지된다
            InitEnemy(SpearGruntDataPath, "spear_grunt", "Spear Grunt", config, d =>
            {
                d.moveSpeedMultiplier = 0.9f;      // 기획: 플레이어의 0.9배
                d.weapon = spear;                  // 기획: 공격 범위 = 가시창과 동일
                d.attackWindupSeconds = 0.2f;      // 기획: 발동 딜레이 0.2초
                d.attackIntervalSeconds = 1f;      // 기획: 공격 간격 1.0초
                d.lootWeapon = spear;              // 기획: 가시창 노획
            });
            InitEnemy(ThrowerDataPath, "thorn_thrower", "Thrower", config, d =>
            {
                d.bodyDiameterInPlayerDiameters = 0.9f; // 기획: 마른 체형(수치 없음, 임시)
                d.moveSpeedMultiplier = 1f;             // 기획에 없음, 임시
                d.weapon = thorn;
                d.attackWindupSeconds = 0.2f;           // 일반 적 발동 딜레이(임의값)
                d.attackIntervalSeconds = 0.5f;         // 기획: 투척병 발사 간격 0.5초
                d.projectileSpeedMultiplier = 2.5f;     // 개발계획 6-1 임시값
                d.engageDistanceInPlayerDiameters = 6f;
                d.lootWeapon = thorn;                   // 기획: 투척 가시 노획(6개 묶음)
            });
            InitEnemy(GrenadierDataPath, "grenadier", "Grenadier", config, d =>
            {
                d.bodyDiameterInPlayerDiameters = 1.2f; // 기획: 한쪽 팔이 비대(수치 없음, 임시)
                d.moveSpeedMultiplier = 0.8f;           // 기획에 없음, 임시
                d.weapon = bomb;
                d.attackWindupSeconds = 0.2f;
                d.attackIntervalSeconds = 5f;           // 기획: 척탄병 투척 간격 5초
                d.projectileSpeedMultiplier = 2.5f;     // 기획: 폭탄알 속도 = 투척 가시와 같음
                d.engageDistanceInPlayerDiameters = 7f;
                d.lootWeapon = bomb;                    // 기획: 폭탄알 노획
            });
            AssetDatabase.SaveAssets();

            EnsureEnemyPrefab(SpearGruntPrefabPath, SpearGruntDataPath, "SpearGrunt", new Color32(0x7A, 0x10, 0x10, 0xFF), false);
            EnsureEnemyPrefab(ThrowerPrefabPath, ThrowerDataPath, "ThornThrower", new Color32(0xFF, 0x9A, 0x9A, 0xFF), true);
            EnsureEnemyPrefab(GrenadierPrefabPath, GrenadierDataPath, "Grenadier", PlaceholderPalette.Hazard, true); // 노란 표식 = 폭탄알을 든 손
            return true;
        }

        static void InitEnemy(string path, string id, string displayName, PlayerConfig config, System.Action<EnemyData> setValues)
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(data, path);
            }
            if (!string.IsNullOrEmpty(data.id)) return;
            data.id = id;
            data.displayName = displayName;
            data.playerConfig = config;
            setValues(data);
            EditorUtility.SetDirty(data);
        }

        static void EnsureEnemyPrefab(string prefabPath, string dataPath, string name, Color facingColor, bool ranged)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Circle.png");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/SpriteUnlit.mat");
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
            var pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath).GetComponent<WeaponPickup>();

            float diameter = data.bodyDiameterInPlayerDiameters * GameConstants.PlayerDiameter;
            var go = M1Setup.CreateSprite(name, circle, unlit, PlaceholderPalette.Enemy, Vector2.zero, Vector2.one * diameter, Layers.Enemy);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.linearDamping = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            // 앞쪽 표시(플레이스홀더): 시야 방향을 알 수 있게 한다. 자식이므로 몸 지름에 비례해 커진다
            var facing = M1Setup.CreateSprite("Facing", circle, unlit, facingColor, Vector2.zero, Vector2.one * 0.24f, Layers.Enemy);
            facing.transform.SetParent(go.transform, false);
            facing.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            facing.GetComponent<SpriteRenderer>().sortingOrder = 1;

            var enemy = go.AddComponent<EnemyBase>();
            go.AddComponent<Perception>();
            M1Setup.SetField(enemy, "data", data);
            M1Setup.SetField(enemy, "body", go.GetComponent<SpriteRenderer>());
            M1Setup.SetField(enemy, "hitCollider", col);
            M1Setup.SetField(enemy, "rb", rb);
            M1Setup.SetField(enemy, "pickupPrefab", pickupPrefab);

            if (ranged)
            {
                var attack = go.AddComponent<EnemyRangedAttack>();
                M1Setup.SetField(attack, "projectilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath).GetComponent<Projectile>());
            }
            else go.AddComponent<EnemyMeleeAttack>();
            go.AddComponent<EnemyAI>();

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

            // 길찾기 격자(경계 벽 안쪽 전체)
            new GameObject("NavGrid").AddComponent<NavGrid>();

            // 경계 벽(투사체가 맵 밖으로 나가지 않게) + 내부 벽
            CreateWall("Wall Top", square, unlit, new Vector2(0f, 9.25f), new Vector2(31f, 0.5f));
            CreateWall("Wall Bottom", square, unlit, new Vector2(0f, -9.25f), new Vector2(31f, 0.5f));
            CreateWall("Wall Left", square, unlit, new Vector2(-15.25f, 0f), new Vector2(0.5f, 19f));
            CreateWall("Wall Right", square, unlit, new Vector2(15.25f, 0f), new Vector2(0.5f, 19f));
            CreateWall("Wall Pillar", square, unlit, new Vector2(3f, 3f), new Vector2(0.5f, 4f));   // 길찾기: 돌아가야 함
            CreateWall("Wall Shelf", square, unlit, new Vector2(-6f, 2.5f), new Vector2(8f, 0.5f)); // 시야 차단: 뒤에 숨기

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpearGruntPrefabPath);
            // 정면 경비: 멀리서 왼쪽(플레이어 쪽)을 보고 서 있음. 접근하면 시야에 걸림
            PlaceGrunt(prefab, "Grunt Guard", new Vector2(11f, 0f), 90f);
            // 순찰: 두 지점을 왕복
            PlaceGrunt(prefab, "Grunt Patrol", new Vector2(-9f, -6f), 0f, new Vector2(-9f, -6f), new Vector2(-9f, -1f));
            // 등 돌린 경비: 뒤로 접근해 주먹으로 처치(시야 90도라 뒤는 안 보임)
            PlaceGrunt(prefab, "Grunt Back Turned", new Vector2(0f, -6f), 180f);
            // 권총 소리 테스트: 위쪽 구석에서 벽 너머를 보지 못하고 서 있다가 소리를 들으면 발사 지점으로 이동
            PlaceGrunt(prefab, "Grunt Sound Test", new Vector2(10f, 6.5f), 0f);

            // 가시 투척병: 오른쪽 아래에서 왼쪽을 보고 서 있음. 접근하면 멈춰 서서 발사
            PlaceEnemy(AssetDatabase.LoadAssetAtPath<GameObject>(ThrowerPrefabPath), "Thrower", new Vector2(12f, -5f), 90f);
            // 척탄병: 왼쪽 위에서 오른쪽을 보고 서 있음. 5초 간격으로 폭탄알
            PlaceEnemy(AssetDatabase.LoadAssetAtPath<GameObject>(GrenadierPrefabPath), "Grenadier", new Vector2(-12f, 5.5f), 270f);

            // 테스트용 무기: 권총(소리), 가시창(노획 전에도 시험)
            PlacePickup(catalog.Find("pistol"), new Vector2(-2f, -2f));
            PlacePickup(catalog.Find("spear"), new Vector2(2f, -2f));

            EditorSceneManager.SaveScene(scene, ScenePath);
            M2Setup.SetBuildScenes();
        }

        internal static void CreateWall(string name, Sprite square, Material unlit, Vector2 pos, Vector2 size)
        {
            var wall = M1Setup.CreateSprite(name, square, unlit, PlaceholderPalette.LabObject, pos, size, Layers.Wall);
            wall.AddComponent<BoxCollider2D>().size = Vector2.one;
        }

        internal static void PlaceEnemy(GameObject prefab, string name, Vector2 pos, float angleZ) => PlaceGrunt(prefab, name, pos, angleZ);

        internal static void PlaceGrunt(GameObject prefab, string name, Vector2 pos, float angleZ, params Vector2[] patrol)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angleZ);

            if (patrol.Length == 0) return;
            var points = new Transform[patrol.Length];
            for (int i = 0; i < patrol.Length; i++)
            {
                var p = new GameObject($"{name} Patrol {i + 1}");
                p.transform.position = patrol[i];
                points[i] = p.transform;
            }
            var so = new SerializedObject(go.GetComponent<EnemyAI>());
            var prop = so.FindProperty("patrolPoints");
            prop.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            so.ApplyModifiedPropertiesWithoutUndo();
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
