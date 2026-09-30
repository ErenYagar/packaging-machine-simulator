using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WaferSaw.Editor
{
    public static class WaferSawSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/WaferSawDigitalTwin.unity";
        [MenuItem("Tools/Build Wafer Saw Digital Twin")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }
        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }
        public static void OpenDemo()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.isPlaying = true;
        }
        private static void Build()
        {
            Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory("Assets/Materials"); Directory.CreateDirectory("Assets/Prefabs"); AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Wafer Saw Digital Twin"); root.AddComponent<WaferSawBootstrap>().Build();
            var view = root.GetComponent<MachineView>();
            for (int index = 0; index < view.Materials.Length; index++)
            {
                Material material = view.Materials[index];
                string path = "Assets/Materials/" + material.name.Replace(" ", "-") + ".mat";
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing == null) AssetDatabase.CreateAsset(material, path);
                else
                {
                    EditorUtility.CopySerialized(material, existing);
                    foreach (var renderer in root.GetComponentsInChildren<Renderer>()) if (renderer.sharedMaterial == material) renderer.sharedMaterial = existing;
                    view.Materials[index] = existing;
                    UnityEngine.Object.DestroyImmediate(material);
                }
            }
            RenderSettings.ambientLight = new Color(0.34f, 0.4f, 0.49f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }.Concat(EditorBuildSettings.scenes.Where(item => item.path != ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Wafer Saw scene built, all references wired: " + ScenePath);
        }
    }
}
