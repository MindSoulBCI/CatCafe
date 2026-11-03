using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpatialCatCafe.Editor
{
    [InitializeOnLoad]
    public static class CafeBuild
    {
        const string ScenePath = "Assets/CatCafe/Scenes/Cafe.unity";
        const string Marker = "ProjectSettings/CatCafeConfigured.txt";
        static CafeBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(Marker)) return;
                Configure();
                if (!Application.isBatchMode) EditorSceneManager.OpenScene(ScenePath);
            };
        }

        [MenuItem("Cat Cafe/1. Configure project")]
        public static void Configure()
        {
            PlayerSettings.companyName = "SpatialCatCafe";
            PlayerSettings.productName = "空间猫咖";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.spatialcatcafe.app");
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.runInBackground = false;
            // This project uses the legacy Input API deliberately for touch + mouse,
            // independent of optional XR packages. A new project defaults to this mode.
            var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = playerSettings.FindProperty("activeInputHandler");
            if (input != null && input.intValue != 0) { input.intValue = 0; playerSettings.ApplyModifiedPropertiesWithoutUndo(); }
            EnsureStandardShader();
            ConfigureFont();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            File.WriteAllText(Marker, "Unity 6 / Android ARM64 / Built-in renderer / v0.1\n");
            Debug.Log("空间猫咖配置完成。打开 Cafe 场景并点击 Play；Android 构建请使用 Cat Cafe 菜单。");
        }

        static void ConfigureFont()
        {
            const string path = "Assets/CatCafe/Resources/Fonts/CafeChinese.otf";
            var importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;
            if (!importer) throw new BuildFailedException("缺少中文字体：" + path);
            if (importer.fontTextureCase != FontTextureCase.Dynamic)
            { importer.fontTextureCase = FontTextureCase.Dynamic; importer.SaveAndReimport(); }
        }
        static void EnsureStandardShader()
        {
            var shader = Shader.Find("Standard");
            if (!shader) throw new BuildFailedException("找不到 Standard Shader，请使用 Built-in 渲染管线。");
            var objects = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (objects.Length == 0) throw new BuildFailedException("GraphicsSettings 尚未生成，请等 Unity 首次导入完成后重新配置。");
            var settings = new SerializedObject(objects[0]);
            var shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            if (shaders == null) throw new BuildFailedException("无法配置运行时材质 Shader。");
            for (int i = 0; i < shaders.arraySize; i++)
                if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
            shaders.InsertArrayElementAtIndex(shaders.arraySize);
            shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = shader;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Cat Cafe/2. Open cafe scene")]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("Cat Cafe/3. Build Android APK")]
        public static void Android()
        {
            Configure();
            ConfigureAndroidTools();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("请在 Unity Hub 为此编辑器安装 Android Build Support、SDK/NDK 和 OpenJDK。");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (Application.isBatchMode)
                    throw new BuildFailedException("命令行构建需要传入 -buildTarget Android，请使用 build-android.sh。");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                    throw new BuildFailedException("无法切换到 Android 构建平台。");
            }
            EditorUserBuildSettings.buildAppBundle = false;
            string output = Environment.GetEnvironmentVariable("CATCAFE_APK_PATH");
            if (string.IsNullOrEmpty(output)) output = "Builds/SpatialCatCafe.apk";
            var parent = Path.GetDirectoryName(output); if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = output,
                target = BuildTarget.Android, options = BuildOptions.AutoRunPlayer
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Android 构建失败：" + report.summary.result);
            Debug.Log("APK 已生成：" + Path.GetFullPath(output));
        }

        static void ConfigureAndroidTools()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string sdk = Path.Combine(home, "Library/Android/sdk");
            string ndk = Path.Combine(sdk, "ndk/27.2.12479018");
            string javaRoot = Path.Combine(home, "Library/Java/JavaVirtualMachines");
            string jdk = null;
            if (Directory.Exists(javaRoot))
            {
                foreach (string dir in Directory.GetDirectories(javaRoot))
                {
                    if (dir.Contains("17"))
                    {
                        string candidate = Path.Combine(dir, "Contents/Home");
                        if (Directory.Exists(candidate)) jdk = candidate;
                    }
                }
            }
            if (Directory.Exists(sdk)) AndroidExternalToolsSettings.sdkRootPath = sdk;
            if (Directory.Exists(ndk)) AndroidExternalToolsSettings.ndkRootPath = ndk;
            if (!string.IsNullOrEmpty(jdk)) AndroidExternalToolsSettings.jdkRootPath = jdk;
        }
    }
}
