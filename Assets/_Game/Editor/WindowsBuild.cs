using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Menu RED MET > Build Windows : build 64 bits de l'arène dans Builds/Windows (hors dépôt).
// Active « Frame Timing Stats » pour que le banc d'essai (BenchmarkRunner, argument -redmet-benchmark)
// lise le temps CPU de chaque image dans le build.
public static class WindowsBuild
{
    public const string OutputPath = "Builds/Windows/REDMET.exe";

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
}
