using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command line entry point for producing a Windows player.
/// Usage: Unity.exe -quit -batchmode -projectPath &lt;path&gt; -executeMethod BuildScript.BuildWindows
/// Optional: -buildOutput &lt;folder&gt; to override the default Build/Windows folder.
/// </summary>
public static class BuildScript
{
    const string DefaultOutputDir = "Build/Windows";

    [MenuItem("Malajong/Build Windows Player")]
    public static void BuildWindows()
    {
        var scenes = CollectScenes();
        if (scenes.Length == 0)
        {
            Fail("No scenes found under Assets/Scenes.");
            return;
        }

        var outputDir = ArgValue("-buildOutput") ?? DefaultOutputDir;
        Directory.CreateDirectory(outputDir);

        var exeName = Sanitize(PlayerSettings.productName) + ".exe";
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outputDir, exeName),
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None,
        };

        Debug.Log($"Building {exeName} with {scenes.Length} scene(s):\n  " + string.Join("\n  ", scenes));

        var summary = BuildPipeline.BuildPlayer(options).summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.outputPath} ({summary.totalSize / (1024 * 1024)} MB)");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        else
        {
            Fail($"Build {summary.result} with {summary.totalErrors} error(s).");
        }
    }

    // Prefer whatever is configured in Build Settings, but fall back to every scene
    // in the project so a stale scene list cannot produce an empty player.
    static string[] CollectScenes()
    {
        var configured = EditorBuildSettings.scenes
            .Where(s => s.enabled && File.Exists(s.path))
            .Select(s => s.path)
            .ToArray();

        if (configured.Length > 0) return configured;

        return AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();
    }

    static string ArgValue(string flag)
    {
        var args = Environment.GetCommandLineArgs();
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    static string Sanitize(string name)
    {
        var cleaned = new string(name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray()).Trim();
        return string.IsNullOrEmpty(cleaned) ? "Malajong" : cleaned;
    }

    static void Fail(string message)
    {
        Debug.LogError(message);
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }
}
