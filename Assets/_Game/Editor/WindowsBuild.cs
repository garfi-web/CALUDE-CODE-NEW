using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Menu RED MET > Build Windows : build 64 bits de l'arène dans Builds/Windows (hors dépôt).
// Active « Frame Timing Stats » pour que le banc d'essai (BenchmarkRunner, argument -redmet-benchmark)
// lise le temps CPU de chaque image dans le build.
public static class WindowsBuild
{
    public const string OutputPath = "Builds/Windows/REDMET.exe";
    public const string Output25DPath = "Builds/Windows25D/REDMET25D.exe";

    [MenuItem("RED MET/Build Windows")]
    public static void Build()
    {
        PlayerSettings.enableFrameTimingStats = true;

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/_Game/Scenes/Arena.unity" },
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"[RED MET] Build Windows : {summary.result}, {summary.totalSize / (1024 * 1024)} Mo, " +
                  $"{summary.totalTime.TotalSeconds:F0} s, {summary.totalErrors} erreur(s) → {OutputPath}");
    }

    // Étude 2,5D : build de l'arène 2,5D seule (Arena25D.unity), avec son banc d'essai (Benchmark25D,
    // arguments -redmet-benchmark-25d, -redmet-capture-25d, -redmet-showcase-25d).
    [MenuItem("RED MET/Build Windows (arène 2,5D)")]
    public static void Build25D()
    {
        PlayerSettings.enableFrameTimingStats = true;

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/_Game/Scenes/Arena25D.unity" },
            locationPathName = Output25DPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
        Debug.Log($"[RED MET] Build Windows 2,5D : {summary.result}, {summary.totalSize / (1024 * 1024)} Mo, " +
                  $"{summary.totalTime.TotalSeconds:F0} s, {summary.totalErrors} erreur(s) → {Output25DPath}");
    }
}
