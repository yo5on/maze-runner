using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Builds the game for the Web (WebGL) platform, ready for GitHub Pages.
    /// Menu: Maze Runner > Build Web (GitHub Pages)
    /// CLI:  Unity -batchmode -quit -projectPath . -buildTarget WebGL
    ///             -executeMethod MazeRunner.EditorTools.WebBuild.BuildFromCommandLine
    ///             [-webBuildOutput Builds/WebGL]
    ///             [-webBuildDefines MAZE_AI_TEST]   (test builds only)
    /// </summary>
    public static class WebBuild
    {
        private const string DefaultOutput = "Builds/WebGL";

        // MainMenu first so the web player boots into the menu. The scene list in
        // Build Settings is left untouched; this list is only used for web builds.
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/LevelSelect.unity",
            "Assets/Scenes/Customization.unity",
            "Assets/Scenes/Levels/Level1.unity",
            "Assets/Scenes/Levels/Level2.unity",
            "Assets/Scenes/Levels/Level3.unity",
        };

        [MenuItem("Maze Runner/Build Web (GitHub Pages)")]
        public static void BuildFromMenu()
        {
            Build(DefaultOutput);
        }

        public static void BuildFromCommandLine()
        {
            string output = GetArg("-webBuildOutput") ?? DefaultOutput;
            // Optional, for test builds only, e.g. -webBuildDefines MAZE_AI_TEST (comma-separated).
            string defines = GetArg("-webBuildDefines");
            bool ok = Build(output, string.IsNullOrEmpty(defines) ? null : defines.Split(','));
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool Build(string outputPath, string[] extraDefines = null)
        {
            ApplyWebSettings();

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
                extraScriptingDefines = extraDefines,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"[WebBuild] Result: {summary.result}, size: {summary.totalSize / (1024f * 1024f):F1} MB, " +
                      $"errors: {summary.totalErrors}, time: {summary.totalTime}, output: {Path.GetFullPath(outputPath)}");
            return summary.result == BuildResult.Succeeded;
        }

        private static void ApplyWebSettings()
        {
            // Custom page: responsive scaling, loading bar and on-screen touch controls.
            PlayerSettings.WebGL.template = "PROJECT:MazeRunner";

            // GitHub Pages cannot send Content-Encoding headers for pre-compressed files,
            // so ship gzip with the JavaScript decompression fallback.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            // Content-hashed file names: a page cached from an earlier deploy never
            // mixes with new build files (the page reloads once if its files are gone).
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.showDiagnostics = false;

            // Smaller wasm and faster loading.
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);

            PlayerSettings.runInBackground = false;
        }

        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }
    }
}
