using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Banc d'essai des 60 FPS dans un build, lancé par l'argument « -redmet-benchmark » (sans lui, ne fait rien) :
// vague du mannequin, lame de test, gore complet, démo de combo enchaînée pendant la durée de mesure,
// synchronisation verticale coupée pour mesurer la marge réelle. Le temps CPU de chaque image vient de
// FrameTimingManager (actif dans les builds quand « Frame Timing Stats » est coché, ce que fait l'outil de build).
// Rapport : benchmark.txt à côté de l'exécutable, puis le jeu se ferme.
public class BenchmarkRunner : MonoBehaviour
{
    private const string Argument = "-redmet-benchmark";

    [SerializeField] private float warmup = 2f;
    [SerializeField] private float duration = 30f;
    [SerializeField] private int mannequinWave = 6;

    private readonly FrameTiming[] timings = new FrameTiming[1];

    private void Start()
    {
        if (System.Environment.GetCommandLineArgs().Contains(Argument))
            StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        GameOptions.Gore = GoreLevel.Full;

        yield return new WaitForSecondsRealtime(warmup);

        var waves = FindAnyObjectByType<EnemyWaveManager>();
        var player = FindAnyObjectByType<PlayerCombat>();
        var demo = FindAnyObjectByType<ComboDemo>();
        waves.StartWave(mannequinWave);
        player.TestBlade = true;

        yield return new WaitForSecondsRealtime(1f);

        var cpu = new List<double>();
        var frame = new List<double>();
        float end = Time.realtimeSinceStartup + duration;
        int combos = 0;

        while (Time.realtimeSinceStartup < end)
        {
            if (!demo.IsPlaying)
            {
                demo.Play();
                combos++;
            }

            FrameTimingManager.CaptureFrameTimings();

            if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
                cpu.Add(timings[0].cpuMainThreadFrameTime);

            frame.Add(Time.unscaledDeltaTime * 1000.0);
            yield return null;
        }

        string report = Report(cpu, frame, combos);
        string path = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "benchmark.txt");
        File.WriteAllText(path, report);
        Debug.Log(report);
        Application.Quit();
    }

    private string Report(List<double> cpu, List<double> frame, int combos)
    {
        string Stats(List<double> values)
        {
            if (values.Count == 0)
                return "pas de mesure";

            var sorted = values.OrderBy(v => v).ToArray();
            return $"{values.Count} images ; moyenne {values.Average():F2} ms ; 99e centile {sorted[(int)(sorted.Length * 0.99)]:F2} ms ; " +
                   $"pire {sorted[^1]:F2} ms ; au-delà de 16,6 ms : {values.Count(v => v > 16.6)}";
        }

        return $"RED MET — banc d'essai ({System.DateTime.Now:yyyy-MM-dd HH:mm})\n" +
               $"Matériel : {SystemInfo.processorType} ; {SystemInfo.graphicsDeviceName} ; {Screen.width}×{Screen.height}\n" +
               $"Scène : vague du mannequin, lame de test, gore complet, {combos} combos en {duration:F0} s, vsync coupée\n" +
               $"Thread principal (CPU) : {Stats(cpu)}\n" +
               $"Durée d'image : {Stats(frame)}\n";
    }
}
