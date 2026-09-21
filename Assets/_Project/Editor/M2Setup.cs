using Game.Core;
using Game.Enemies;
using Game.Player;
using Game.Weapons;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M2 셋업: 무기 SO 5종 + 카탈로그 + 픽업 프리팹 + M2 샌드박스 씬. 여러 번 실행해도 안전
    /// (SO는 id가 비어 있을 때만 초기값을 넣으므로 조정한 수치는 덮어쓰지 않고, 씬은 매번 새로 만든다).
    /// 플레이어 리그(카메라/추적/플레이어/무기)는 M1 샌드박스도 공유한다.
    /// </summary>
    public static class M2Setup
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/M2_Sandbox.unity";
        const string WeaponDir = Root + "/Data/Weapons";
        const string CatalogPath = WeaponDir + "/WeaponCatalog.asset";
        const string PickupPrefabPath = Root + "/Prefabs/Weapons/WeaponPickup.prefab";
        const string ExplosionPrefabPath = Root + "/Prefabs/Projectiles/ExplosionEffect.prefab";
        const string ProjectilePrefabPath = Root + "/Prefabs/Projectiles/Projectile.prefab";
        public const string PlayerConfigPath = Root + "/Data/PlayerConfig.asset";

        [MenuItem("Tools/Fear/M2 Setup")]
        public static void Run()
        {
            if (!EnsureAssets()) return;
            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M2] 셋업 완료: " + ScenePath);
        }

        /// <summary>SO/프리팹 생성(씬에 넣기 전 단계). 성공 여부 반환.</summary>
        internal static bool EnsureAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Circle.png") == null)
            {
                Debug.LogError("[M2] M0 에셋이 없습니다. 먼저 Tools/Fear/M0 Setup을 실행하세요.");
                return false;
            }

            LoadOrCreate<PlayerConfig>(PlayerConfigPath);
            var fist = LoadOrCreate<WeaponData>(WeaponDir + "/Fist.asset");
            var spear = LoadOrCreate<WeaponData>(WeaponDir + "/Spear.asset");
            var thorn = LoadOrCreate<WeaponData>(WeaponDir + "/ThrownThorn.asset");
            var bomb = LoadOrCreate<WeaponData>(WeaponDir + "/BombShell.asset");
            var pistol = LoadOrCreate<WeaponData>(WeaponDir + "/Pistol.asset");

            // 기획 수치는 그대로, 기획에 없는 값은 임시값(개발계획 6절). id가 비어 있을 때만 초기화
            Init(fist, "fist", "Fist", WeaponKind.Melee, d =>
            {
                d.rangeInDiameters = 1f; d.widthInDiameters = 0.5f; d.arcDegrees = 0f;
                d.windupSeconds = 0.1f; d.intervalSeconds = 0.5f; d.bossDamage = 25f;
            });
            Init(spear, "spear", "Spear", WeaponKind.Melee, d =>
            {
                d.rangeInDiameters = 2f; d.arcDegrees = 120f; // 부채꼴 각도는 임시값
                d.windupSeconds = 0.1f; d.intervalSeconds = 0.3f; d.bossDamage = 25f;
                d.pickupColor = PlaceholderPalette.LabObject;
            });
            Init(thorn, "thorn", "Thorn", WeaponKind.Thrown, d =>
            {
                d.maxAmmo = 12; d.pickupAmmo = 6; d.stackable = true;
                d.windupSeconds = 0f; d.intervalSeconds = 0.3f; // 투척 간격은 임시값
                d.projectileSpeedInDiameters = 20f; d.hitRecoverChance = 0.5f; d.missRecoverChance = 1f;
                d.bossDamage = 25f / 6f;
                d.pickupColor = new Color32(0x5C, 0xC8, 0xC8, 0xFF);
            });
            Init(bomb, "bomb", "Bomb", WeaponKind.Bomb, d =>
            {
                d.maxAmmo = 2; d.pickupAmmo = 1; d.stackable = true;
                d.windupSeconds = 0f; d.intervalSeconds = 1f;
                d.projectileSpeedInDiameters = 20f; d.explosionDiameters = 4f;
                d.bossDamage = 25f;
                d.pickupColor = PlaceholderPalette.Hazard;
            });
            Init(pistol, "pistol", "Pistol", WeaponKind.Gun, d =>
            {
                d.maxAmmo = 16; d.pickupAmmo = 16; d.stackable = false;
                d.windupSeconds = 0f; d.intervalSeconds = 1f / 8f; // 초당 8발
                d.projectileSpeedInDiameters = 40f; // 총알 속도는 임시값
                d.aggroRadiusInScreenWidths = 1f; d.aggroSeconds = 5f;
                d.bossDamage = 25f / 8f;
                d.pickupColor = new Color32(0x0E, 0x6B, 0x6B, 0xFF);
            });

            var catalog = LoadOrCreate<WeaponCatalog>(CatalogPath);
            catalog.weapons = new[] { fist, spear, thorn, bomb, pistol };
            EditorUtility.SetDirty(catalog);

            EnsurePickupPrefab();
            EnsureProjectilePrefabs();
            AssetDatabase.SaveAssets();
            return true;
        }

        static void Init(WeaponData d, string id, string displayName, WeaponKind kind, System.Action<WeaponData> setValues)
        {
            if (!string.IsNullOrEmpty(d.id)) return;
            d.id = id;
            d.displayName = displayName;
            d.kind = kind;
            setValues(d);
            EditorUtility.SetDirty(d);
        }

        static void EnsurePickupPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath) != null) return;
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Square.png");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/SpriteUnlit.mat");

            var go = M1Setup.CreateSprite("WeaponPickup", square, unlit, PlaceholderPalette.LabObject, Vector2.zero, Vector2.one * 0.6f, Layers.Pickup);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            col.isTrigger = true;
            var pickup = go.AddComponent<WeaponPickup>();
            M1Setup.SetField(pickup, "body", go.GetComponent<SpriteRenderer>());
            PrefabUtility.SaveAsPrefabAsset(go, PickupPrefabPath);
            Object.DestroyImmediate(go);
        }

        static void EnsureProjectilePrefabs()
        {
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Circle.png");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/SpriteUnlit.mat");

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPrefabPath) == null)
            {
                // 폭발 표시: 노란 위험 표식, 반투명 원(지름은 Play에서 SO 수치로 맞춘다)
                var color = PlaceholderPalette.Hazard;
                color.a = 0.4f;
                var fx = M1Setup.CreateSprite("ExplosionEffect", circle, unlit, color, Vector2.zero, Vector2.one, 0);
                fx.GetComponent<SpriteRenderer>().sortingOrder = 3;
                var effect = fx.AddComponent<ExplosionEffect>();
                M1Setup.SetField(effect, "body", fx.GetComponent<SpriteRenderer>());
                PrefabUtility.SaveAsPrefabAsset(fx, ExplosionPrefabPath);
                Object.DestroyImmediate(fx);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath) == null)
            {
                // 루트는 이동 기준, 자식 Body가 무기별 크기/색을 받는다
                var root = new GameObject("Projectile") { layer = Layers.Projectile };
                var body = M1Setup.CreateSprite("Body", circle, unlit, PlaceholderPalette.LabObject, Vector2.zero, Vector2.one * 0.2f, Layers.Projectile);
                body.transform.SetParent(root.transform, false);
                body.GetComponent<SpriteRenderer>().sortingOrder = 2;
                var proj = root.AddComponent<Projectile>();
                M1Setup.SetField(proj, "body", body.GetComponent<SpriteRenderer>());
                M1Setup.SetField(proj, "explosionPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPrefabPath).GetComponent<ExplosionEffect>());
                M1Setup.SetField(proj, "pickupPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath).GetComponent<WeaponPickup>());
                PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
                Object.DestroyImmediate(root);
            }
        }

        static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateRig();

            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Circle.png");
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Square.png");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/SpriteUnlit.mat");
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalog>(CatalogPath);

            // 근접 테스트용 더미 + 벽
            M1Setup.CreateDummy("Dummy A", circle, unlit, new Vector2(4f, 0f));
            M1Setup.CreateDummy("Dummy B", circle, unlit, new Vector2(4f, 1.5f));
            M1Setup.CreateDummy("Dummy C", circle, unlit, new Vector2(-4f, 2f));
            M1Setup.CreateDummy("Dummy Behind Wall", circle, unlit, new Vector2(0f, 4.2f));
            // 원거리/폭발 테스트: 먼 더미 무리(폭발 지름 4배 안에 여러 개) + 벽 뒤 더미
            M1Setup.CreateDummy("Far Dummy 1", circle, unlit, new Vector2(8f, -0.5f));
            M1Setup.CreateDummy("Far Dummy 2", circle, unlit, new Vector2(8f, 0.8f));
            M1Setup.CreateDummy("Far Dummy 3", circle, unlit, new Vector2(8.5f, 2.1f));
            M1Setup.CreateDummy("Far Dummy Behind Wall", circle, unlit, new Vector2(-8f, 0f));
            var wall = M1Setup.CreateSprite("Wall", square, unlit, PlaceholderPalette.LabObject, new Vector2(0f, 3f), new Vector2(4f, 0.4f), Layers.Wall);
            wall.AddComponent<BoxCollider2D>().size = Vector2.one;
            var wall2 = M1Setup.CreateSprite("Wall Side", square, unlit, PlaceholderPalette.LabObject, new Vector2(-6f, 0f), new Vector2(0.4f, 4f), Layers.Wall);
            wall2.AddComponent<BoxCollider2D>().size = Vector2.one;

            // 무기 픽업(교체/드롭 테스트를 위해 같은 무기를 2개 이상 둔다)
            PlacePickup(catalog.Find("spear"), 0, new Vector2(-3f, -2f));
            PlacePickup(catalog.Find("spear"), 0, new Vector2(3f, -2f));
            // 투척 가시 6개 묶음 3개: 합산 습득(상한 12, 초과분 잔류) 테스트
            PlacePickup(catalog.Find("thorn"), 0, new Vector2(-6f, -3.5f));
            PlacePickup(catalog.Find("thorn"), 0, new Vector2(-4.33f, -3.49f));
            PlacePickup(catalog.Find("thorn"), 0, new Vector2(-3.08f, -3.49f));
            // 폭탄알 3개: 상한 2 클램프 테스트
            PlacePickup(catalog.Find("bomb"), 0, new Vector2(6f, -2f));
            PlacePickup(catalog.Find("bomb"), 0, new Vector2(6f, -3.5f));
            PlacePickup(catalog.Find("bomb"), 0, new Vector2(6f, -5.18f));
            PlacePickup(catalog.Find("pistol"), 0, new Vector2(0f, -3.5f));    // 16발 탄창
            PlacePickup(catalog.Find("pistol"), 0, new Vector2(1.5f, -3.5f));  // 교체/잔량 유지 테스트

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildScenes();
        }

        /// <summary>NewScene 이후에 호출해야 한다(에셋 로드 시점 문제). 플레이어/카메라/추적/재시작 컨트롤러를 만든다.</summary>
        internal static GameObject CreateRig()
        {
            var playerConfig = AssetDatabase.LoadAssetAtPath<PlayerConfig>(PlayerConfigPath);
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalog>(CatalogPath);
            var fistData = catalog.Find("fist");
            var pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath).GetComponent<WeaponPickup>();
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Circle.png");
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Square.png");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/SpriteUnlit.mat");

            // 재시작/스냅샷 컨트롤러
            new GameObject("RestartController").AddComponent<RestartController>();

            // 플레이어
            var player = M1Setup.CreateSprite("Player", circle, unlit, PlaceholderPalette.Player, Vector2.zero, Vector2.one * GameConstants.PlayerDiameter, Layers.Player);
            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.AddComponent<CircleCollider2D>().radius = 0.5f;

            // 조준 방향 표시(플레이스홀더): 앞쪽(+Y)의 작은 원
            var facing = M1Setup.CreateSprite("Facing", circle, unlit, Color.white, new Vector2(0f, 0.32f), Vector2.one * 0.24f, Layers.Player);
            facing.transform.SetParent(player.transform, false);
            facing.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            facing.GetComponent<SpriteRenderer>().sortingOrder = 1;

            // 공격 범위 표시(반투명). 모양/크기는 MeleeAttack이 무기 SO 수치로 맞춘다
            var rangeColor = PlaceholderPalette.Player;
            rangeColor.a = 0.3f;
            var range = M1Setup.CreateSprite("MeleeRange", square, unlit, rangeColor, Vector2.zero, Vector2.one, 0);
            range.transform.SetParent(player.transform, false);
            var rangeRenderer = range.GetComponent<SpriteRenderer>();
            rangeRenderer.sortingOrder = -1;
            rangeRenderer.enabled = false;

            var health = player.AddComponent<PlayerHealth>();
            var melee = player.AddComponent<MeleeAttack>();
            var ranged = player.AddComponent<RangedAttack>();
            var holder = player.AddComponent<WeaponHolder>();
            var controller = player.AddComponent<PlayerController>();
            M1Setup.SetField(health, "body", player.GetComponent<SpriteRenderer>());
            M1Setup.SetField(melee, "rangeIndicator", rangeRenderer);
            M1Setup.SetField(holder, "catalog", catalog);
            M1Setup.SetField(holder, "fist", fistData);
            M1Setup.SetField(holder, "pickupPrefab", pickupPrefab);
            M1Setup.SetField(holder, "melee", melee);
            M1Setup.SetField(holder, "ranged", ranged);
            M1Setup.SetField(ranged, "projectilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath).GetComponent<Projectile>());
            M1Setup.SetField(ranged, "holder", holder);
            M1Setup.SetField(controller, "config", playerConfig);
            M1Setup.SetField(controller, "weapons", holder);

            // 카메라(고정 크기) + Cinemachine 추적
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PlaceholderPalette.Background;
            cam.orthographic = true;
            cam.orthographicSize = GameConstants.CameraOrthographicSize;
            camGo.AddComponent<FixedCamera>();
            camGo.AddComponent<CinemachineBrain>();

            var vcamGo = new GameObject("CM Player Follow");
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            vcam.Follow = player.transform;
            var lens = vcam.Lens;
            lens.OrthographicSize = GameConstants.CameraOrthographicSize;
            vcam.Lens = lens;
            var composer = vcamGo.AddComponent<CinemachinePositionComposer>();
            composer.Damping = Vector3.zero;

            return player;
        }

        static void PlacePickup(WeaponData data, int ammoOverride, Vector2 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = "Pickup " + data.displayName;
            go.transform.position = position;
            var pickup = go.GetComponent<WeaponPickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("data").objectReferenceValue = data;
            so.FindProperty("ammo").intValue = ammoOverride > 0 ? ammoOverride : data.pickupAmmo;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(Root + "/Scenes/M1_Sandbox.unity", true),
                new EditorBuildSettingsScene(Root + "/Scenes/M0_Sandbox.unity", true),
            };
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return asset;
        }
    }
}
