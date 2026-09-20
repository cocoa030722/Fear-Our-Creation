using System.IO;
using Game.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>M0 프로젝트 셋업: 폴더, 레이어, 충돌 매트릭스, 플레이스홀더 스프라이트, 샌드박스 씬. 여러 번 실행해도 안전.</summary>
    public static class M0ProjectSetup
    {
        const string Root = "Assets/_Project";
        const string SandboxScenePath = Root + "/Scenes/M0_Sandbox.unity";

        static readonly string[] Folders =
        {
            "Art/Sprites", "Art/Animations", "Art/Tilesets", "Art/VFX", "Art/Materials",
            "Audio/BGM", "Audio/SFX", "Audio/Announcement",
            "Data/Weapons", "Data/Enemies", "Data/Stages", "Data/Dialogue",
            "Prefabs/Player", "Prefabs/Enemies", "Prefabs/Weapons", "Prefabs/Projectiles", "Prefabs/Props", "Prefabs/UI",
            "Scenes",
            "Scripts/Core", "Scripts/Player", "Scripts/Weapons", "Scripts/Enemies", "Scripts/World",
            "Scripts/Cutscene", "Scripts/UI", "Scripts/Audio",
            "Editor",
        };

        [MenuItem("Tools/Fear/M0 Setup")]
        public static void Run()
        {
            CreateFolders();
            SetupLayers();
            SetupPhysics2D();
            var circle = CreateSprite("Circle", true);
            CreateSprite("Square", false);
            var material = CreateUnlitMaterial();
            BuildSandboxScene(circle, material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M0] 셋업 완료: " + SandboxScenePath);
        }

        static void CreateFolders()
        {
            foreach (var sub in Folders)
            {
                var parts = (Root + "/" + sub).Split('/');
                var path = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = path + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(path, parts[i]);
                    path = next;
                }
            }
        }

        static void SetupLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            foreach (var (index, name) in Layers.All)
            {
                var element = layers.GetArrayElementAtIndex(index);
                if (element.stringValue != name && !string.IsNullOrEmpty(element.stringValue))
                    Debug.LogWarning($"[M0] 레이어 {index}('{element.stringValue}')를 '{name}'으로 덮어씁니다.");
                element.stringValue = name;
            }
            tagManager.ApplyModifiedProperties();
        }

        static void SetupPhysics2D()
        {
            // 탑뷰이므로 중력 없음
            Physics2D.gravity = Vector2.zero;

            // 커스텀 레이어 간 충돌 규칙. Pickup/SightBlocker는 레이캐스트/오버랩 전용이라 어떤 것과도 충돌하지 않음.
            var custom = new[] { Layers.Wall, Layers.Player, Layers.Enemy, Layers.Projectile, Layers.Destructible, Layers.Pickup, Layers.SightBlocker };
            foreach (int a in custom)
                for (int b = 0; b < 32; b++)
                    Physics2D.IgnoreLayerCollision(a, b, true);

            Collide(Layers.Player, Layers.Wall);
            Collide(Layers.Player, Layers.Enemy);
            Collide(Layers.Player, Layers.Destructible);
            Collide(Layers.Enemy, Layers.Wall);
            Collide(Layers.Enemy, Layers.Enemy);
            Collide(Layers.Enemy, Layers.Destructible);
            Collide(Layers.Projectile, Layers.Wall);
            Collide(Layers.Projectile, Layers.Destructible);
            Collide(Layers.Projectile, Layers.Enemy);
            Collide(Layers.Projectile, Layers.Player);
        }

        static void Collide(int a, int b) => Physics2D.IgnoreLayerCollision(a, b, false);

        static Sprite CreateSprite(string name, bool circle)
        {
            const int size = 256;
            var path = $"{Root}/Art/Sprites/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                float r = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - r, dy = y + 0.5f - r;
                        // 원은 가장자리 1px 안티앨리어싱
                        float alpha = circle ? Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy)) : 1f;
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                tex.SetPixels32(pixels);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size; // 1유닛 = 스프라이트 한 변 → 플레이어 지름 1.0
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Material CreateUnlitMaterial()
        {
            var path = Root + "/Art/Materials/SpriteUnlit.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            mat = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void BuildSandboxScene(Sprite circle, Material unlit)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PlaceholderPalette.Background;
            cam.orthographic = true;
            cam.orthographicSize = GameConstants.CameraOrthographicSize;
            camGo.AddComponent<FixedCamera>();

            var player = new GameObject("Player") { layer = Layers.Player };
            var sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = circle;
            sr.sharedMaterial = unlit;
            sr.color = PlaceholderPalette.Player;
            player.transform.localScale = Vector3.one * GameConstants.PlayerDiameter;
            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = player.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            EditorSceneManager.SaveScene(scene, SandboxScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SandboxScenePath, true) };
        }
    }
}
