using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vault.EditorTools
{
    /// <summary>
    /// One-shot project setup and command-line builds.
    ///   unity build --target WebGL --execute-method Vault.EditorTools.VaultBuild.BuildWebGL -o Build/WebGL
    ///   unity build --target StandaloneWindows64 --execute-method Vault.EditorTools.VaultBuild.BuildWindows -o Build/Windows
    /// </summary>
    public static class VaultBuild
    {
        const string ScenePath = "Assets/Vault/Scenes/Vault.unity";

        [MenuItem("Vault/Set Up Project")]
        public static void Setup()
        {
            ImportTmpEssentials();
            CreateScene();
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("Vault setup done.");
        }

        static void ImportTmpEssentials()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return;
            var pkg = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            if (!File.Exists(pkg)) throw new FileNotFoundException(pkg);
            AssetDatabase.ImportPackage(pkg, false);
            AssetDatabase.Refresh();
        }

        static void CreateScene()
        {
            if (File.Exists(ScenePath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cam.tag = "MainCamera";
            var c = cam.GetComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.027f, 0.035f, 0.059f);
            c.orthographic = true;

            new GameObject("Bootstrap", typeof(Bootstrap));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Vo Tran Quoc Cuong";
            PlayerSettings.productName = "The Vault";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);

            PlayerSettings.WebGL.template = "PROJECT:Vault";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            // VAULT_DEBUG=1 gives full stack traces in the browser console (bigger, slower build).
            PlayerSettings.WebGL.exceptionSupport = Environment.GetEnvironmentVariable("VAULT_DEBUG") == "1"
                ? WebGLExceptionSupport.FullWithStacktrace
                : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
        }

        public static void BuildWebGL() => Build(BuildTarget.WebGL, "Build/WebGL");
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Build/Windows/TheVault.exe");

        static void Build(BuildTarget target, string defaultOutput)
        {
            Setup();
            var output = ArgAfter("-buildOutput") ?? defaultOutput;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = target,
                locationPathName = output,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"Build {summary.result}: {summary.totalSize / (1024 * 1024)} MB -> {summary.outputPath} ({summary.totalErrors} errors)");
            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Build failed with " + summary.totalErrors + " errors");
        }

        static string ArgAfter(string flag)
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, flag);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
