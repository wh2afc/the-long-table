using System.IO;
using LongTable.Core;
using LongTable.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LongTable.EditorTools
{
    /// <summary>
    /// Creates the vertical slice scene. Everything visible is built at runtime by
    /// SliceBootstrap, so the scene holds only a camera and one component.
    /// </summary>
    public static class SliceSceneBuilder
    {
        public const string ScenePath = "Assets/TheLongTable/Scenes/VillageGreen.unity";

        [MenuItem("The Long Table/Rebuild Vertical Slice Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            SliceBootstrap.ConfigureCamera(cam);

            new GameObject("The Long Table").AddComponent<SliceBootstrap>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.companyName = "The Long Table";
            PlayerSettings.productName = "The Long Table";
            AssetDatabase.SaveAssets();
            Debug.Log($"[LongTable] Wrote {ScenePath} and set it as the only build scene.");
        }

        [MenuItem("The Long Table/Validate Slice Data")]
        public static void Validate()
        {
            var data = SliceData.LoadFromResources();
            Debug.Log($"[LongTable] Slice data is valid: {data.Villagers.Count} villagers, {data.Actions.Count} actions, " +
                      $"{data.DialogueTable.entries.Length} dialogue entries.");
        }

        /// <summary>Batch mode entry point: -executeMethod LongTable.EditorTools.SliceSceneBuilder.BuildFromCommandLine</summary>
        public static void BuildFromCommandLine()
        {
            Validate();
            Build();
        }

        /// <summary>Windows player build into Builds/Windows. Batch mode friendly.</summary>
        [MenuItem("The Long Table/Build Windows Player")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) Build();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/TheLongTable.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            Debug.Log($"[LongTable] Windows build: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB");
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
