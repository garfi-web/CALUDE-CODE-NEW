using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Banc d'essai des 60 FPS dans un build, lancé par un argument (sans lui, ne fait rien) :
// « -redmet-benchmark » : vague du mannequin, lame de test, gore complet, démo de combo enchaînée ;
// « -redmet-benchmark-shotgun » : Nathan M4 contre 5 Voyous (vague 8), un tir de fusil à pompe à bout portant
// sur le groupe, vague relancée après chaque tir (5 corps pulvérisés à chaque fois), gore complet.
// Synchronisation verticale coupée pour mesurer la marge réelle. Le temps CPU de chaque image vient de
// FrameTimingManager (actif dans les builds quand « Frame Timing Stats » est coché, ce que fait l'outil de build).
// Rapport : benchmark.txt (ou benchmark_shotgun.txt) à côté de l'exécutable, puis le jeu se ferme.
public class BenchmarkRunner : MonoBehaviour
{
    private const string Argument = "-redmet-benchmark";
    private const string ShotgunArgument = "-redmet-benchmark-shotgun";

    [SerializeField] private float warmup = 2f;
    [SerializeField] private float duration = 30f;
    [SerializeField] private int mannequinWave = 6;
    [SerializeField] private int shotgunWave = 7;
    [Tooltip("Distance (m) du Voyou le plus proche à laquelle Nathan tire.")]
    [SerializeField] private float shotgunTriggerDistance = 2.2f;

    private readonly FrameTiming[] timings = new FrameTiming[1];

    private void Start()
    {
        string[] args = System.Environment.GetCommandLineArgs();

        if (args.Contains(ShotgunArgument))
            StartCoroutine(Run(true));
        else if (args.Contains(Argument))
            StartCoroutine(Run(false));
    }

    private IEnumerator Run(bool shotgun)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        GameOptions.Gore = GoreLevel.Full;

        yield return new WaitForSecondsRealtime(warmup);

        var waves = FindAnyObjectByType<EnemyWaveManager>();
        var player = FindAnyObjectByType<PlayerCombat>();
        var demo = FindAnyObjectByType<ComboDemo>();
        var health = player.GetComponent<Health>();
        var body = player.GetComponent<Rigidbody2D>();
        var gun = player.GetComponent<PlayerShotgun>();
        waves.StartWave(shotgun ? shotgunWave : mannequinWave);
        player.TestBlade = !shotgun;

        yield return new WaitForSecondsRealtime(1f);

        var cpu = new List<double>();
        var frame = new List<double>();
        float end = Time.realtimeSinceStartup + duration;
        int actions = 0;
        int kills = 0;
        float restartAt = -1f;

        while (Time.realtimeSinceStartup < end)
        {
            if (shotgun)
            {
                health.Heal(health.MaxHealth);
                int alive = waves.Enemies.Count(e => !e.IsDead);

                if (alive == 0 && restartAt < 0f)
                    restartAt = Time.realtimeSinceStartup + 1.5f;

                if (restartAt > 0f && Time.realtimeSinceStartup >= restartAt)
                {
                    restartAt = -1f;
                    waves.StartWave(shotgunWave);
                    body.position = new Vector2(-1f, body.position.y);
                }
                else if (alive > 0 && !gun.IsBusy && Nearest(waves, body.position.x) < shotgunTriggerDistance)
                {
                    int before = alive;
                    GameInput.Inject(GameAction.Shotgun);
                    actions++;
                    yield return null;
                    yield return new WaitForSeconds(0.3f);
                    kills += before - waves.Enemies.Count(e => !e.IsDead);
                }
            }
            else if (!demo.IsPlaying)
            {
                demo.Play();
                actions++;
            }

            FrameTimingManager.CaptureFrameTimings();

            if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
                cpu.Add(timings[0].cpuMainThreadFrameTime);

            frame.Add(Time.unscaledDeltaTime * 1000.0);
            yield return null;
        }

        string scene = shotgun
            ? $"Nathan M4 contre 5 Voyous, {actions} tirs de fusil à pompe, {kills} corps pulvérisés en {duration:F0} s, gore complet, vsync coupée"
            : $"vague du mannequin, lame de test, gore complet, {actions} combos en {duration:F0} s, vsync coupée";
        string report = Report(cpu, frame, scene);
        string path = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", shotgun ? "benchmark_shotgun.txt" : "benchmark.txt");
        File.WriteAllText(path, report);
        Debug.Log(report);
        Application.Quit();
    }

    private static float Nearest(EnemyWaveManager waves, float x)
    {
        float best = float.MaxValue;

        foreach (Enemy enemy in waves.Enemies)
        {
            if (!enemy.IsDead)
                best = Mathf.Min(best, Mathf.Abs(enemy.transform.position.x - x));
        }

        return best;
    }

    private static string Report(List<double> cpu, List<double> frame, string scene)
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
               $"Scène : {scene}\n" +
               $"Thread principal (CPU) : {Stats(cpu)}\n" +
               $"Durée d'image : {Stats(frame)}\n";
    }
}
