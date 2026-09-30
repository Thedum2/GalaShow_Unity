using System.IO;
using Galashow.Trolley.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Galashow.Trolley.Editor
{
    /// <summary>
    /// 트롤리 딜레마 데모 씬 생성 메뉴
    /// </summary>
    public static class TrolleyDemoMenu
    {
        const string ScenePath = "Assets/Scenes/TrolleyDemo.unity";

        [MenuItem("Galashow/트롤리 딜레마/데모 씬 열기")]
        static void OpenDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }

            // 무대가 자체 카메라·조명을 만들지만, 라운드 시작 전 화면을 위해 기본 카메라를 둔다
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            new GameObject("TrolleyDemo").AddComponent<TrolleyDemo>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[TrolleyDemo] {ScenePath} 생성. Play를 누르면 라운드가 자동으로 진행됩니다.");
        }
    }
}
