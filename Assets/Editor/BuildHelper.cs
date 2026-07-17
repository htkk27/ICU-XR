using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class BuildHelper
{
    [MenuItem("Tools/Build ICU Simulation (.exe)")]
    public static void BuildWindows()
    {
        string buildFolder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Build");
        string exePath = Path.Combine(buildFolder, "ICU_Simulation.exe");

        if (!Directory.Exists(buildFolder))
            Directory.CreateDirectory(buildFolder);

        string[] scenes = GetBuildScenes();
        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("Build Error",
                "No scenes found in Build Settings.\nAdd ICU_Main to File > Build Settings first.", "OK");
            return;
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            EditorUtility.DisplayDialog("Build Complete",
                $"Build successful!\n\nLocation:\n{exePath}\n\nSize: {report.summary.totalSize / (1024 * 1024)} MB",
                "Open Folder", "OK");

            if (EditorUtility.DisplayDialog("Build Complete",
                $"Build successful at:\n{exePath}", "Open Folder", "Close"))
            {
                EditorUtility.RevealInFinder(exePath);
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Build Failed",
                $"Build failed with {report.summary.totalErrors} error(s).\nCheck the Console for details.",
                "OK");
        }
    }

    private static string[] GetBuildScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }

        if (scenes.Count == 0)
        {
            string icuScene = FindScene("ICU_Main");
            if (!string.IsNullOrEmpty(icuScene))
            {
                scenes.Add(icuScene);
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(icuScene, true)
                };
                Debug.Log($"[BuildHelper] Auto-added scene: {icuScene}");
            }
        }

        return scenes.ToArray();
    }

    private static string FindScene(string sceneName)
    {
        string[] guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == sceneName)
                return path;
        }
        return null;
    }
}