using System.IO;
using EchoShift.Game;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EchoShift.EditorTools
{
    /// <summary>
    /// One-click project setup: Android player settings and the boot scene.
    /// Runs automatically the first time the project is opened.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Boot.unity";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath) && !Application.isBatchMode) SetUp();
            };
        }

        [MenuItem("Echo Shift/1. Set Up Project", priority = 1)]
        public static void SetUp()
        {
            PlayerSettings.companyName = GameConfig.StudioName;
            PlayerSettings.productName = GameConfig.GameName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, GameConfig.PackageId);
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "1.0" || PlayerSettings.bundleVersion == "0.1")
                PlayerSettings.bundleVersion = "0.1.0";
            if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;

            // Portrait only (decision D1).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Google Play requires 64-bit builds; IL2CPP is needed for ARM64.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.059f, 0.086f, 0.149f);

            EnsureScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Echo Shift: project set up. Press Play to run the game in the editor. " +
                      "Set the Game view to a portrait size such as 1080x1920.");
        }

        static void EnsureScene()
        {
            // Never drop unsaved edits in whatever scene is open.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var light = GameObject.Find("Directional Light");
                if (light != null) Object.DestroyImmediate(light);
                var cam = Camera.main;
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.059f, 0.086f, 0.149f);
                    cam.orthographic = true;
                }
                // The game builds itself from code at start-up (GameApp), so the scene stays empty.
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            if (EditorSceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
