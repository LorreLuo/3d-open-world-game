using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    public static class ProjectVerifier
    {
        [MenuItem("Game/Verify/Compile Check")]
        public static void CompileCheck()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("COMPILE_CHECK_DONE");
        }

        [MenuItem("Game/Verify/Configure Build Settings")]
        public static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Game/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/_Game/Scenes/CharacterCreation.unity", true),
                new EditorBuildSettingsScene("Assets/_Game/Scenes/GameWorld.unity", true),
            };
            Debug.Log("BUILD_SETTINGS_DONE");
        }

        [MenuItem("Game/Verify/Build Windows Player")]
        public static void BuildWindows()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var outDir = "Builds/SparkUISDemo";
            Directory.CreateDirectory(outDir);
            var report = BuildPipeline.BuildPlayer(scenes, outDir + "/SparkUISDemo.exe",
                BuildTarget.StandaloneWindows64, BuildOptions.None);
            Debug.Log("BUILD_RESULT: " + report.summary.result + " totalErrors=" + report.summary.totalErrors);
            if (report.summary.result != BuildResult.Succeeded) { EditorApplication.Exit(1); }
        }
    }
}
