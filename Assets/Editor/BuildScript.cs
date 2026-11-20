using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EchoShift.EditorTools
{
    /// <summary>
    /// Android builds from the menu or the command line, for example:
    ///   Unity -batchmode -quit -projectPath . -executeMethod EchoShift.EditorTools.BuildScript.BuildApk
    /// </summary>
    public static class BuildScript
    {
        [MenuItem("Echo Shift/2. Build Android APK (test)", priority = 20)]
        public static void BuildApk() => Build(false, true);

        [MenuItem("Echo Shift/3. Build Android App Bundle (release)", priority = 21)]
        public static void BuildAab() => Build(true, false);

        static void Build(bool appBundle, bool development)
        {
            if (!File.Exists(ProjectSetup.ScenePath)) ProjectSetup.SetUp();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                {
                    Fail("Could not switch to Android. Install Android Build Support for this Unity version in Unity Hub.");
                    return;
                }
            }

            if (!development && !PlayerSettings.Android.useCustomKeystore)
            {
                Debug.LogWarning("Echo Shift: release builds should be signed with your upload key. " +
                                 "Set it in Project Settings > Player > Android > Publishing Settings.");
            }

            EditorUserBuildSettings.buildAppBundle = appBundle;
            Directory.CreateDirectory("Builds");
            string path = appBundle ? "Builds/EchoShift.aab" : "Builds/EchoShift.apk";

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = path,
                target = BuildTarget.Android,
                options = development ? BuildOptions.Development : BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            bool ok = report.summary.result == BuildResult.Succeeded;
            if (ok)
            {
                Debug.Log("Echo Shift: built " + path + " (" + (report.summary.totalSize / (1024 * 1024)) + " MB)");
                if (!Application.isBatchMode) EditorUtility.RevealInFinder(path);
            }
            else
            {
                Fail("Build failed: " + report.summary.result + ". See the Console for details.");
            }

            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        static void Fail(string message)
        {
            Debug.LogError("Echo Shift: " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
