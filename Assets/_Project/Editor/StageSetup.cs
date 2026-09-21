using Game.Core;
using Game.Cutscene;
using Game.UI;
using Game.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 스테이지 씬 구성(1~2스테이지)과 벽 계열 프리팹(벽/화분/제어 패널). 여러 번 실행해도 안전(씬은 매번 새로 만든다).
    /// 벽 규칙은 Wall 레이어 하나로 통일되므로 화분·제어 패널도 프리팹의 겉모습만 다르다.
    /// 레이아웃을 바꿀 때는 이 스크립트를 수정한다(에디터에서 직접 고친 씬은 재실행 시 사라짐).
    /// </summary>
    public static class StageSetup
    {
        const string Root = "Assets/_Project";
        const string PropDir = Root + "/Prefabs/Props";
        const string WallPrefabPath = PropDir + "/Wall.prefab";
        const string PlanterPrefabPath = PropDir + "/Planter.prefab";
        const string PanelPrefabPath = PropDir + "/ControlPanel.prefab";
        internal const string Stage1Path = Root + "/Scenes/Stage1.unity";
        internal const string Stage2Path = Root + "/Scenes/Stage2.unity";

        static readonly Color PlanterColor = new Color32(0x3F, 0xBF, 0x9F, 0xFF);
        static readonly Color PanelColor = new Color32(0x0E, 0x7C, 0x7C, 0xFF);
        static readonly Color TextColor = new Color32(0x2B, 0x3A, 0x42, 0xFF);

        [MenuItem("Tools/Fear/Stage 1-2 Setup")]
        public static void Run()
        {
            if (!M3Setup.EnsureAssets()) return;
            EnsurePropPrefabs();
            BuildStage1();
            BuildStage2();
            M2Setup.SetBuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Stage] 셋업 완료: Stage1, Stage2");
        }

        // ---- 프리팹 ----------------------------------------------------------------------------------------

        static void EnsurePropPrefabs()
        {
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Square.png");
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Circle.png");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/SpriteUnlit.mat");

            SaveProp(WallPrefabPath, "Wall", square, unlit, PlaceholderPalette.LabObject, false);
            SaveProp(PlanterPrefabPath, "Planter", circle, unlit, PlanterColor, true);
            SaveProp(PanelPrefabPath, "ControlPanel", square, unlit, PanelColor, false);
        }

        static void SaveProp(string path, string name, Sprite sprite, Material unlit, Color color, bool round)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var go = M1Setup.CreateSprite(name, sprite, unlit, color, Vector2.zero, Vector2.one, Layers.Wall);
            if (round) go.AddComponent<CircleCollider2D>().radius = 0.5f;
            else go.AddComponent<BoxCollider2D>().size = Vector2.one;
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        // ---- 1스테이지 -------------------------------------------------------------------------------------

        static void BuildStage1()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var player = M2Setup.CreateRig();
            player.transform.position = new Vector3(-13f, 0f, 0f); // 숙직실

            var wall = Load(WallPrefabPath);
            var planter = Load(PlanterPrefabPath);
            new GameObject("NavGrid").AddComponent<NavGrid>();
            Boundary(wall);

            // 숙직실(좌측): 위/아래 벽 + 오른쪽 벽. 오른쪽 벽 가운데가 오프닝에서 무너진다
            Prop(wall, "Dorm Wall Top", new Vector2(-11.5f, 4.25f), new Vector2(7f, 0.5f));
            Prop(wall, "Dorm Wall Bottom", new Vector2(-11.5f, -4.25f), new Vector2(7f, 0.5f));
            Prop(wall, "Dorm Wall Upper", new Vector2(-8.25f, 3f), new Vector2(0.5f, 3f));
            Prop(wall, "Dorm Wall Lower", new Vector2(-8.25f, -3f), new Vector2(0.5f, 3f));
            var breakable = Prop(wall, "Dorm Wall Breakable", new Vector2(-8.25f, 0f), new Vector2(0.5f, 3f));

            // 침대(장식, 충돌 없음)
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Square.png");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/SpriteUnlit.mat");
            var bed = M1Setup.CreateSprite("Bed", square, unlit, new Color32(0x8F, 0xD6, 0xD6, 0xFF), new Vector2(-13.5f, 2.6f), new Vector2(2.4f, 1.2f), 0);
            bed.GetComponent<SpriteRenderer>().sortingOrder = -2;

            // 홀 오른쪽 끝의 문틈: 위/아래 벽 사이가 탕비실로 가는 통로
            Prop(wall, "Hall Wall Upper", new Vector2(6.25f, 5.5f), new Vector2(0.5f, 7f));
            Prop(wall, "Hall Wall Lower", new Vector2(6.25f, -5.5f), new Vector2(0.5f, 7f));

            // 엄폐물(화분 = 벽): 벽 뒤에 숨으면 적 시야를 피한다
            Prop(planter, "Planter 1", new Vector2(-1f, 3.2f), Vector2.one * 1.2f);
            Prop(planter, "Planter 2", new Vector2(-1f, -3.2f), Vector2.one * 1.2f);
            Prop(planter, "Planter 3", new Vector2(2.5f, 1.6f), Vector2.one * 1.2f);
            Prop(planter, "Planter 4", new Vector2(2.5f, -1.6f), Vector2.one * 1.2f);
            Prop(planter, "Planter 5", new Vector2(0f, 6.2f), Vector2.one * 1.2f);

            // 적: 가시 창병만. 튜토리얼 순서 = 혼자 있는 적의 뒤 → 주먹 처치 → 가시창 노획, 이후 엄폐하며 접근
            var grunt = Load(M3Setup.SpearGruntPrefabPath);
            M3Setup.PlaceEnemy(grunt, "Grunt Tutorial", new Vector2(-5f, 0f), 270f);        // 오른쪽을 보고 서 있음(등이 플레이어 쪽)
            M3Setup.PlaceEnemy(grunt, "Grunt Cover", new Vector2(-1f, -7f), 0f);
            M3Setup.PlaceGrunt(grunt, "Grunt Patrol", new Vector2(1f, 4.5f), 0f, new Vector2(1f, 4.5f), new Vector2(1f, 7.8f));
            M3Setup.PlaceEnemy(grunt, "Grunt Door Guard", new Vector2(5f, 0f), 90f);        // 문틈 앞

            // 오프닝 연출: 촉수가 벽을 흔들다 무너뜨린다(재시작 시 생략)
            BuildOpening(breakable, square, unlit);

            // 종점: 탕비실 전화기
            Goal("Phone", new Vector2(13f, 0f), "Phone", "Stage 1 Clear", "Stage2", square, unlit);

            Hint(new Vector2(-13f, 0f), 4.5f, "WASD: 이동    마우스: 조준    R: 즉시 재시작");
            Hint(new Vector2(-4f, 0f), 4.5f, "좌클릭: 공격 — 적의 등 뒤에서 노려 보세요\n죽은 적이 떨어뜨린 무기 위에서 스페이스: 습득/교체");
            Hint(new Vector2(0f, 0f), 3f, "화분과 벽 뒤에 숨으면 적의 시야를 피할 수 있습니다");

            EditorSceneManager.SaveScene(scene, Stage1Path);
        }

        static void BuildOpening(GameObject breakable, Sprite square, Material unlit)
        {
            var tentacles = new Tentacle[3];
            float[] ys = { -1f, 0.2f, 1.2f };
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject($"Tentacle {i + 1}");
                go.transform.position = new Vector3(-6.6f, ys[i], 0f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, 90f); // +Y가 왼쪽(-X)을 향하도록
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = unlit;
                lr.startColor = lr.endColor = PlaceholderPalette.Enemy;
                lr.sortingOrder = 2;
                lr.positionCount = 2;
                tentacles[i] = go.AddComponent<Tentacle>();
            }

            var cutscene = new GameObject("Opening Cutscene").AddComponent<WallCollapseCutscene>();
            var so = new SerializedObject(cutscene);
            so.FindProperty("wallCollider").objectReferenceValue = breakable.GetComponent<Collider2D>();
            so.FindProperty("wallRenderer").objectReferenceValue = breakable.GetComponent<SpriteRenderer>();
            so.FindProperty("debrisSprite").objectReferenceValue = square;
            so.FindProperty("debrisMaterial").objectReferenceValue = unlit;
            var arr = so.FindProperty("tentacles");
            arr.arraySize = tentacles.Length;
            for (int i = 0; i < tentacles.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = tentacles[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- 2스테이지 -------------------------------------------------------------------------------------

        static void BuildStage2()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var player = M2Setup.CreateRig();
            player.transform.position = new Vector3(-13f, 0f, 0f); // 탕비실 전화기 앞

            var wall = Load(WallPrefabPath);
            var planter = Load(PlanterPrefabPath);
            new GameObject("NavGrid").AddComponent<NavGrid>();
            Boundary(wall);

            var square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/Square.png");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Materials/SpriteUnlit.mat");

            // 탕비실(좌측): 문틈 y ±1.25
            Prop(wall, "Break Room Wall Top", new Vector2(-12.75f, 3.25f), new Vector2(4.75f, 0.5f));
            Prop(wall, "Break Room Wall Bottom", new Vector2(-12.75f, -3.25f), new Vector2(4.75f, 0.5f));
            Prop(wall, "Break Room Wall Upper", new Vector2(-10.25f, 2.4f), new Vector2(0.5f, 2.3f));
            Prop(wall, "Break Room Wall Lower", new Vector2(-10.25f, -2.4f), new Vector2(0.5f, 2.3f));
            var phone = M1Setup.CreateSprite("Phone", square, unlit, PanelColor, new Vector2(-13.7f, 2.2f), new Vector2(0.9f, 0.7f), 0);
            AddLabel(phone.transform, "Phone");

            // 통화 연출(맵 시작, 재시작 시 생략). 자막 문구는 임시 원고
            var call = new GameObject("Phone Call").AddComponent<PhoneCall>();
            SetLines(call, new (string, string, float)[]
            {
                ("소장", "…연결됐군. 자네, 무사한가.", 2.5f),
                ("소장", "상황은 통제하에 있네. 외부에는 이미 연락해 두었어.", 3.5f),
                ("소장", "최하층으로 집결하게. 유사시에는 지하터널로 퇴각해 대피하고 항전하도록.", 4f),
                ("소장", "상황은 통제하에 있네.", 2.5f),
                ("소장", "상황은 통제하에 있네…  상황은 통제하에 있네…", 3f),
            });

            // 엄폐물: 투척병의 가시는 벽에 막힌다는 것을 배우게 하는 배치
            foreach (var (name, pos) in new (string, Vector2)[]
            {
                ("Planter A", new Vector2(-6f, 3.5f)), ("Planter B", new Vector2(-6f, -3.5f)),
                ("Planter C", new Vector2(-2f, 0f)), ("Planter D", new Vector2(2f, 3f)),
                ("Planter E", new Vector2(2f, -3f)), ("Planter F", new Vector2(6.5f, 0f)),
                ("Planter G", new Vector2(9f, 3.5f)), ("Planter H", new Vector2(9f, -3.5f)),
            })
                Prop(planter, name, pos, Vector2.one * 1.2f);

            var grunt = Load(M3Setup.SpearGruntPrefabPath);
            var thrower = Load(M3Setup.ThrowerPrefabPath);
            M3Setup.PlaceEnemy(thrower, "Thrower Front", new Vector2(4f, 0f), 90f);           // 화분 뒤로 접근
            M3Setup.PlaceGrunt(grunt, "Grunt Patrol", new Vector2(-4f, 6.5f), 0f, new Vector2(-4f, 6.5f), new Vector2(-4f, -6.5f));
            M3Setup.PlaceEnemy(grunt, "Grunt Top", new Vector2(2f, 7f), 180f);
            M3Setup.PlaceEnemy(thrower, "Thrower Far", new Vector2(10f, -5f), 90f);
            M3Setup.PlaceEnemy(grunt, "Grunt Elevator Guard", new Vector2(12f, 2f), 90f);

            Goal("Elevator", new Vector2(14f, 0f), "Elevator", "Stage 2 Clear (M3 빌드의 끝)", "", square, unlit);

            Hint(new Vector2(-6f, 0f), 4.5f, "투척병의 가시는 벽과 화분에 막힙니다. 엄폐물 뒤로 붙어서 접근하세요");

            EditorSceneManager.SaveScene(scene, Stage2Path);
        }

        // ---- 공용 ------------------------------------------------------------------------------------------

        static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

        /// <summary>프리팹 인스턴스를 놓고 크기를 맞춘다(벽/화분/패널 공용).</summary>
        static GameObject Prop(GameObject prefab, string name, Vector2 pos, Vector2 size)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            return go;
        }

        /// <summary>맵 경계 벽(투사체가 맵 밖으로 나가지 않게).</summary>
        static void Boundary(GameObject wall)
        {
            Prop(wall, "Boundary Top", new Vector2(0f, 9.25f), new Vector2(31f, 0.5f));
            Prop(wall, "Boundary Bottom", new Vector2(0f, -9.25f), new Vector2(31f, 0.5f));
            Prop(wall, "Boundary Left", new Vector2(-15.25f, 0f), new Vector2(0.5f, 19f));
            Prop(wall, "Boundary Right", new Vector2(15.25f, 0f), new Vector2(0.5f, 19f));
        }

        static void Goal(string name, Vector2 pos, string label, string message, string nextScene, Sprite square, Material unlit)
        {
            var go = M1Setup.CreateSprite(name, square, unlit, PanelColor, pos, new Vector2(1.6f, 1.6f), 0);
            go.GetComponent<SpriteRenderer>().sortingOrder = -1;
            AddLabel(go.transform, label);
            var goal = go.AddComponent<StageGoal>();
            var so = new SerializedObject(goal);
            so.FindProperty("nextSceneName").stringValue = nextScene;
            so.FindProperty("message").stringValue = message;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Hint(Vector2 pos, float radius, string text)
        {
            var go = new GameObject("Hint");
            go.transform.position = pos;
            var hint = go.AddComponent<HintZone>();
            var so = new SerializedObject(hint);
            so.FindProperty("text").stringValue = text;
            so.FindProperty("radius").floatValue = radius;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetLines(PhoneCall call, (string speaker, string text, float seconds)[] lines)
        {
            var so = new SerializedObject(call);
            var arr = so.FindProperty("lines");
            arr.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("speaker").stringValue = lines[i].speaker;
                e.FindPropertyRelative("text").stringValue = lines[i].text;
                e.FindPropertyRelative("seconds").floatValue = lines[i].seconds;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>월드 좌표 위에 뜨는 영문 라벨(임시. 정식 UI 전까지 TextMesh 사용).</summary>
        static void AddLabel(Transform parent, string text)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.75f / Mathf.Max(0.01f, parent.localScale.y), 0f);
            go.transform.localScale = new Vector3(1f / parent.localScale.x, 1f / parent.localScale.y, 1f);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = font;
            tm.fontSize = 48;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = TextColor;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            mr.sortingOrder = 5;
        }
    }
}
