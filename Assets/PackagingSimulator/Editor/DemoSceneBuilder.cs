using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace PackagingSim.Editor
{
    public static class DemoSceneBuilder
    {
        public const string ScenePath = "Assets/PackagingSimulator/Scenes/PackagingDemo.unity";
        private const string ResourcePath = "Assets/PackagingSimulator/Resources";

        [MenuItem("Tools/Packaging Simulator/Create or Open Demo Scene")]
        public static void OpenDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareResources();
        }

        public static void PrepareBatch()
        {
            try { PrepareResources(); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void PrepareResources()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset") == null)
            {
                string package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly).resolvedPath;
                AssetDatabase.importPackageCompleted += OnResourcesImported;
                AssetDatabase.importPackageFailed += OnImportFailed;
                AssetDatabase.ImportPackage(Path.Combine(package, "Package Resources", "TMP Essential Resources.unitypackage"), false);
                return; // ImportPackage completes on a later editor update.
            }
            CreateScene();
        }

        private static void OnResourcesImported(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnResourcesImported;
            AssetDatabase.importPackageFailed -= OnImportFailed;
            EditorApplication.delayCall += CreateScene;
        }

        private static void OnImportFailed(string packageName, string error)
        {
            AssetDatabase.importPackageCompleted -= OnResourcesImported;
            AssetDatabase.importPackageFailed -= OnImportFailed;
            Debug.LogError("TMP Essential Resources import failed: " + error);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }

        private static void CreateScene()
        {
            try { CreateSceneAndAssets(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void CreateSceneAndAssets()
        {
            Directory.CreateDirectory(ResourcePath);
            Directory.CreateDirectory("Assets/PackagingSimulator/Scenes");
            AssetDatabase.Refresh();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ResourcePath + "/ChineseFont.asset");
            if (font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>(ResourcePath + "/Fonts/NotoSansCJKtc-Regular.otf");
                if (source == null) throw new FileNotFoundException("Missing bundled Noto Sans CJK TC font.");
                font = TMP_FontAsset.CreateFontAsset(source, 48, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
                font.name = "ChineseFont";
                font.isMultiAtlasTexturesEnabled = true;
                AssetDatabase.CreateAsset(font, ResourcePath + "/ChineseFont.asset");
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(ResourcePath + "/MachineSurface.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, ResourcePath + "/MachineSurface.mat");
            }
            // Repeated menu calls open the existing scene and preserve user modifications.
            if (File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var ui in root.GetComponentsInChildren<PackagingUI>(true))
                        if (ui.font == null) { ui.font = font; EditorUtility.SetDirty(ui); }
                    foreach (var view in root.GetComponentsInChildren<MachineView>(true))
                        if (view.surfaceMaterial == null) { view.surfaceMaterial = material; EditorUtility.SetDirty(view); }
                }
                EditorSceneManager.SaveScene(scene);
            }
            else
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("Packaging Machine Demo");
                root.AddComponent<MachineController>();
                root.AddComponent<PackagingUI>().font = font;
                root.AddComponent<MachineView>().surfaceMaterial = material;
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            if (!EditorBuildSettings.scenes.Any(scene => scene.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            PlayerSettings.companyName = "PackagingDemo";
            PlayerSettings.productName = "Packaging Machine Simulator";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            AssetDatabase.SaveAssets();
            Debug.Log("Packaging demo ready: " + ScenePath);
        }
    }
}
