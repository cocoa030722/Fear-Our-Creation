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
    /// <summary>M1 셋업: 플레이어/주먹 SO 생성과 M1 샌드박스 씬 구성. 여러 번 실행해도 안전(씬은 매번 새로 만든다).</summary>
    public static class M1Setup
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/M1_Sandbox.unity";
        const string PlayerConfigPath = Root + "/Data/PlayerConfig.asset";
        const string FistDataPath = Root + "/Data/Weapons/Fist.asset";

        [MenuItem("Tools/Fear/M1 Setup")]
        public static void Run()
        {
            // 에셋 생성만 먼저 하고, 로드는 NewScene 이후에 한다(NewScene이 참조 없는 에셋을 언로드해 null이 되기 때문)
            LoadOrCreate<PlayerConfig>(PlayerConfigPath);
            LoadOrCreate<MeleeWeaponData>(FistDataPath);
            if (AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Circle.png") == null)
            {
                Debug.LogError("[M1] M0 에셋이 없습니다. 먼저 Tools/Fear/M0 Setup을 실행하세요.");
                return;
            }

            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M1] 셋업 완료: " + ScenePath);
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

        static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var playerConfig = AssetDatabase.LoadAssetAtPath<PlayerConfig>(PlayerConfigPath);
            var fistData = AssetDatabase.LoadAssetAtPath<MeleeWeaponData>(FistDataPath);
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Circle.png");
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Square.png");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/SpriteUnlit.mat");

            // 재시작/스냅샷 컨트롤러
            new GameObject("RestartController").AddComponent<RestartController>();

            // 플레이어
            var player = CreateSprite("Player", circle, unlit, PlaceholderPalette.Player, Vector2.zero, Vector2.one * GameConstants.PlayerDiameter, Layers.Player);
            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.AddComponent<CircleCollider2D>().radius = 0.5f;

            // 조준 방향 표시(플레이스홀더): 앞쪽(+Y)의 작은 원
            var facing = CreateSprite("Facing", circle, unlit, Color.white, new Vector2(0f, 0.32f), Vector2.one * 0.24f, Layers.Player);
            facing.transform.SetParent(player.transform, false);
            facing.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            facing.GetComponent<SpriteRenderer>().sortingOrder = 1;

            // 공격 범위 표시(반투명). 크기/위치는 FistAttack이 SO 수치로 맞춘다
            var rangeColor = PlaceholderPalette.Player;
            rangeColor.a = 0.3f;
            var range = CreateSprite("FistRange", square, unlit, rangeColor, Vector2.zero, Vector2.one, 0);
            range.transform.SetParent(player.transform, false);
            var rangeRenderer = range.GetComponent<SpriteRenderer>();
            rangeRenderer.sortingOrder = -1;
            rangeRenderer.enabled = false;

            var health = player.AddComponent<PlayerHealth>();
            var fist = player.AddComponent<FistAttack>();
            var controller = player.AddComponent<PlayerController>();
            SetField(health, "body", player.GetComponent<SpriteRenderer>());
            SetField(fist, "data", fistData);
            SetField(fist, "rangeIndicator", rangeRenderer);
            SetField(controller, "config", playerConfig);
            SetField(controller, "fist", fist);

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

            // 더미 적(정지): 정면 / 대각선 / 벽 뒤
            CreateDummy("Dummy A", circle, unlit, new Vector2(3f, 0f));
            CreateDummy("Dummy B", circle, unlit, new Vector2(-3f, 3f));
            CreateDummy("Dummy Behind Wall", circle, unlit, new Vector2(0f, 4.2f));

            // 벽(청록 오브젝트): 벽 뒤 더미를 주먹이 관통하지 않는지 확인용
            var wall = CreateSprite("Wall", square, unlit, PlaceholderPalette.LabObject, new Vector2(0f, 3f), new Vector2(4f, 0.4f), Layers.Wall);
            wall.AddComponent<BoxCollider2D>().size = Vector2.one;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(Root + "/Scenes/M0_Sandbox.unity", true),
            };
        }

        static GameObject CreateSprite(string name, Sprite sprite, Material material, Color color, Vector2 position, Vector2 scale, int layer)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.position = position;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = material;
            sr.color = color;
            return go;
        }

        static void CreateDummy(string name, Sprite circle, Material unlit, Vector2 position)
        {
            var go = CreateSprite(name, circle, unlit, PlaceholderPalette.Enemy, position, Vector2.one * GameConstants.PlayerDiameter, Layers.Enemy);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            var dummy = go.AddComponent<DummyEnemy>();
            SetField(dummy, "body", go.GetComponent<SpriteRenderer>());
            SetField(dummy, "hitCollider", col);
        }

        static void SetField(Object target, string field, Object value)
        {
            if (value == null) Debug.LogError($"[M1] {target.GetType().Name}.{field}에 넣을 값이 null입니다.");
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
