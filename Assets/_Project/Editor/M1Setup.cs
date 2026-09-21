using Game.Core;
using Game.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M1 샌드박스 씬(주먹/더미/벽) 구성. 여러 번 실행해도 안전(씬은 매번 새로 만든다).
    /// 플레이어 리그와 무기 에셋은 M2Setup을 공유하며, 이 스크립트의 CreateSprite/CreateDummy/SetField는 셋업 공용 헬퍼다.
    /// </summary>
    public static class M1Setup
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/M1_Sandbox.unity";

        [MenuItem("Tools/Fear/M1 Setup")]
        public static void Run()
        {
            // 에셋 생성만 먼저 하고, 로드는 NewScene 이후에 한다(NewScene이 참조 없는 에셋을 언로드해 null이 되기 때문)
            if (!M2Setup.EnsureAssets()) return;

            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M1] 셋업 완료: " + ScenePath);
        }

        static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            M2Setup.CreateRig();

            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Circle.png");
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Square.png");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/SpriteUnlit.mat");

            // 더미 적(정지): 정면 / 대각선 / 벽 뒤
            CreateDummy("Dummy A", circle, unlit, new Vector2(3f, 0f));
            CreateDummy("Dummy B", circle, unlit, new Vector2(-3f, 3f));
            CreateDummy("Dummy Behind Wall", circle, unlit, new Vector2(0f, 4.2f));

            // 벽(청록 오브젝트): 벽 뒤 더미를 주먹이 관통하지 않는지 확인용
            var wall = CreateSprite("Wall", square, unlit, PlaceholderPalette.LabObject, new Vector2(0f, 3f), new Vector2(4f, 0.4f), Layers.Wall);
            wall.AddComponent<BoxCollider2D>().size = Vector2.one;

            EditorSceneManager.SaveScene(scene, ScenePath);
            M2Setup.SetBuildScenes();
        }

        internal static GameObject CreateSprite(string name, Sprite sprite, Material material, Color color, Vector2 position, Vector2 scale, int layer)
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

        internal static void CreateDummy(string name, Sprite circle, Material unlit, Vector2 position)
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

        internal static void SetField(Object target, string field, Object value)
        {
            if (value == null) Debug.LogError($"[Setup] {target.GetType().Name}.{field}에 넣을 값이 null입니다.");
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
