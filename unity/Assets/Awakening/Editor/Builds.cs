using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Awakening.Editor
{
    /// <summary>
    /// Batchmode builds. Example:
    /// Unity -batchmode -quit -projectPath unity -executeMethod Awakening.Editor.Builds.WebGL -out Build/web -compression none
    /// </summary>
    public static class Builds
    {
        const string MainScene = "Assets/Awakening/Scenes/Main.unity";

        [MenuItem("Awakening/Build WebGL")]
        public static void WebGL()
        {
            Configure();
            string compression = Argument("-compression", "gzip");
            PlayerSettings.WebGL.compressionFormat = compression switch
            {
                "none" => WebGLCompressionFormat.Disabled,
                "brotli" => WebGLCompressionFormat.Brotli,
                _ => WebGLCompressionFormat.Gzip,
            };
            // itch.io does not always send Content-Encoding for pre-compressed files.
            PlayerSettings.WebGL.decompressionFallback = compression != "none";
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;
            // WebAssembly 2023 (Chrome 91, Firefox 89, Safari 16.4). Unity recommends it for new
            // projects; it shrinks and speeds up the module. Not the experimental threading option.
            PlayerSettings.WebGL.wasm2023 = Argument("-wasm", "2023") == "2023";
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            // The title page loader: Assets/WebGLTemplates/Awakening (port of godot/web/shell.html).
            PlayerSettings.WebGL.template = "PROJECT:Awakening";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Master);
            PlayerSettings.stripEngineCode = true;
            Run(BuildTarget.WebGL, Argument("-out", "Build/web"));
        }

        [MenuItem("Awakening/Build Linux")]
        public static void Linux()
        {
            Configure();
            // Only the Mono Linux player is installed on this machine.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Run(BuildTarget.StandaloneLinux64, Path.Combine(Argument("-out", "Build/linux"), "the-awakening.x86_64"));
        }

        /// <summary>Creates the empty boot scene; the game builds its interface at runtime.</summary>
        public static void EnsureScene()
        {
            if (File.Exists(MainScene)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(MainScene));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, MainScene);
            AssetDatabase.Refresh();
        }

        static void Configure()
        {
            EnsureScene();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };
            PlayerSettings.companyName = "Lucid Labworks Studio";
            PlayerSettings.productName = "The Awakening of Cthulhu";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SplashScreen.show = false;
            // Without this the 2.7 MB logo texture still ships, even with the splash screen off.
            PlayerSettings.SplashScreen.showUnityLogo = false;
            string version = Argument("-version", null);
            if (version != null) PlayerSettings.bundleVersion = version;
        }

        static void Run(BuildTarget target, string output)
        {
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = output,
                target = target,
                // -dev makes a development player: the screenshot and layout harnesses run only there.
                options = Array.IndexOf(Environment.GetCommandLineArgs(), "-dev") >= 0 ? BuildOptions.Development : BuildOptions.None,
            });
            Debug.Log($"AWAKENING_BUILD {target} {report.summary.result} {report.summary.totalSize} bytes in {report.summary.totalTime}");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static string Argument(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }
    }
}
