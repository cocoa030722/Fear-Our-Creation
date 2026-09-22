using Game.Core;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M8 셋업: 타이틀 씬만 다룬다. 일시정지/화면 흔들림/피격 플래시는 M2Setup.CreateRig에 넣어
    /// 모든 스테이지/샌드박스가 공유한다(단일 배선 지점 유지).
    /// </summary>
    public static class M8Setup
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Title.unity";

        [MenuItem("Tools/Fear/Title Setup")]
        public static void Run()
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

            new GameObject("TitleScreen").AddComponent<TitleScreen>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            M2Setup.SetBuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M8] 타이틀 셋업 완료: " + ScenePath);
        }
    }
}
