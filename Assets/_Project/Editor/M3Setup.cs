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
        const string SpearGruntPrefabPath = Root + "/Prefabs/Enemies/SpearGrunt.prefab";
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

            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(SpearGruntDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(data, SpearGruntDataPath);
            }
            if (string.IsNullOrEmpty(data.id))
            {
                var spear = AssetDatabase.LoadAssetAtPath<WeaponData>(SpearWeaponPath);
                data.id = "spear_grunt";
                data.displayName = "Spear Grunt";
                data.playerConfig = AssetDatabase.LoadAssetAtPath<PlayerConfig>(M2Setup.PlayerConfigPath);
                data.moveSpeedMultiplier = 0.9f;  // 기획: 플레이어의 0.9배
                data.weapon = spear;              // 기획: 공격 범위 = 가시창과 동일
                data.attackWindupSeconds = 0.2f;  // 기획: 발동 딜레이 0.2초
                data.attackIntervalSeconds = 1f;  // 기획: 공격 간격 1.0초
                data.sightAngleDegrees = 90f;     // 기획: 전방 90도
                data.sightRangeInScreenWidths = 0.5f; // 기획: 화면 가로 길이의 절반
                data.lootWeapon = spear;          // 기획: 가시창 노획
                EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();

            EnsureSpearGruntPrefab();
            return true;
        }

        static void EnsureSpearGruntPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SpearGruntPrefabPath) != null) return;
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Circle.png");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/SpriteUnlit.mat");
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(SpearGruntDataPath);
            var pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath).GetComponent<WeaponPickup>();

            var go = M1Setup.CreateSprite("SpearGrunt", circle, unlit, PlaceholderPalette.Enemy, Vector2.zero,
                Vector2.one * data.bodyDiameterInPlayerDiameters * GameConstants.PlayerDiameter, Layers.Enemy);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.linearDamping = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            // 앞쪽 표시(플레이스홀더): 짙은 붉은 작은 원. 시야 방향을 알 수 있게 한다
            var facing = M1Setup.CreateSprite("Facing", circle, unlit, new Color32(0x7A, 0x10, 0x10, 0xFF), Vector2.zero, Vector2.one * 0.24f, Layers.Enemy);
            facing.transform.SetParent(go.transform, false);
            facing.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            facing.GetComponent<SpriteRenderer>().sortingOrder = 1;

            var enemy = go.AddComponent<EnemyBase>();
            go.AddComponent<Perception>();
            go.AddComponent<EnemyMeleeAttack>();
            go.AddComponent<EnemyAI>();
            M1Setup.SetField(enemy, "data", data);
            M1Setup.SetField(enemy, "body", go.GetComponent<SpriteRenderer>());
            M1Setup.SetField(enemy, "hitCollider", col);
            M1Setup.SetField(enemy, "rb", rb);
            M1Setup.SetField(enemy, "pickupPrefab", pickupPrefab);

            PrefabUtility.SaveAsPrefabAsset(go, SpearGruntPrefabPath);
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

            // 테스트용 무기: 권총(소리), 가시창(노획 전에도 시험)
            PlacePickup(catalog.Find("pistol"), new Vector2(-2f, -2f));
            PlacePickup(catalog.Find("spear"), new Vector2(2f, -2f));

            EditorSceneManager.SaveScene(scene, ScenePath);
            M2Setup.SetBuildScenes();
        }

        static void CreateWall(string name, Sprite square, Material unlit, Vector2 pos, Vector2 size)
        {
            var wall = M1Setup.CreateSprite(name, square, unlit, PlaceholderPalette.LabObject, pos, size, Layers.Wall);
            wall.AddComponent<BoxCollider2D>().size = Vector2.one;
        }

        static void PlaceGrunt(GameObject prefab, string name, Vector2 pos, float angleZ, params Vector2[] patrol)
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
