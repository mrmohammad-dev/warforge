using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WarForge.EditorTools
{
    /// <summary>
    /// One-click project setup and Android build. Run once after opening the project:
    /// menu WarForge > Setup Project. Also callable headless: Unity -batchmode -executeMethod WarForge.EditorTools.WarForgeProjectSetup.BuildAndroidDebug
    /// </summary>
    public static class WarForgeProjectSetup
    {
        const string ScenePath = "Assets/WarForge/Scenes/Main.unity";
        const string SettingsDir = "Assets/WarForge/Settings";

        [MenuItem("WarForge/Setup Project")]
        public static void SetupProject()
        {
            Directory.CreateDirectory(SettingsDir);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

            ConfigurePlayer();
            CreateMainScene();
            ConfigureBuildSettings();
            TryEnsureUrp();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WarForge] Setup complete. Next: WarForge > Build Android Debug APK (or Build Settings > Android > Build).");
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "mrmohammad-dev";
            PlayerSettings.productName = "WarForge";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.mrmohammad.warforge");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.gpuSkinning = true;
            PlayerSettings.allowUnsafeCode = false;
            PlayerSettings.runInBackground = false;
            // Touch controls use the legacy Input API: Active Input Handling must be 'Input Manager (Old)' or 'Both'.
            // ProjectSettings.asset already sets activeInputHandler: 0 for this.
        }

        static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // GameRoot creates the main camera and AudioListener at runtime.
            // Keep the scene empty to avoid duplicate cameras and AudioListeners.
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void TryEnsureUrp()
        {
            // URP assets are created by the URP package's own menu. We call it when available and report otherwise.
            var existing = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
            if (existing.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(existing[0]);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = asset as UnityEngine.Rendering.RenderPipelineAsset;
                return;
            }
            Debug.LogWarning("[WarForge] No URP asset found. Create one: right-click Assets/WarForge/Settings > Create > Rendering > URP Asset (with Universal Renderer), " +
                             "then drag it into Project Settings > Graphics and Project Settings > Quality. Then run Setup Project again.");
        }

        [MenuItem("WarForge/Build Android Debug APK")]
        public static void BuildAndroidDebug() => Build(false);

        [MenuItem("WarForge/Build Android Release APK")]
        public static void BuildAndroidRelease() => Build(true);

        static void Build(bool release)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = !release;
            EditorUserBuildSettings.androidBuildType = release ? AndroidBuildType.Release : AndroidBuildType.Debug;
            string dir = "Builds";
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, release ? "WarForge-release.apk" : "WarForge-debug.apk");
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = file,
                target = BuildTarget.Android,
                options = release ? BuildOptions.None : BuildOptions.Development,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[WarForge] Build result: " + report.summary.result + " -> " + file);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
