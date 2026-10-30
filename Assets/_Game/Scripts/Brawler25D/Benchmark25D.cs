using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Banc d'essai de l'arène 2,5D, lancé par un argument (sans lui, ne fait rien) :
// « -redmet-benchmark-25d » : 8 ennemis à l'écran en permanence (remplacés quand ils tombent), Nathan qui se bat
// tout seul au milieu (commandes injectées dans GameInput, vie remise au plein), gore complet, vsync coupée.
// Rapport benchmark_25d.txt à côté de l'exécutable, puis le jeu se ferme.
// « -redmet-capture-25d » : même combat encerclé, puis une suite de captures (Captures25D/).
// « -redmet-showcase-25d » : partie normale (vagues de la zone 1, Nathan piloté) avec une capture de chaque type
// d'arrivée et de la flèche GO, puis en zone 2 : saut (ombre au sol), esquive vers le fond, barre de fer, tir de fusil,
// cône du Dealer et combat encerclé. Captures dans Captures25D/, puis le jeu se ferme.
public class Benchmark25D : MonoBehaviour
{
    private const string Argument = "-redmet-benchmark-25d";
    private const string CaptureArgument = "-redmet-capture-25d";
    private const string ShowcaseArgument = "-redmet-showcase-25d";

    [SerializeField] private ArenaDirector25D director;
    [SerializeField] private Body25D player;
    [SerializeField] private float warmup = 2f;
    [SerializeField] private float duration = 30f;
    [SerializeField] private int enemies = 8;
    [SerializeField] private int captures = 16;
    [SerializeField] private float captureInterval = 0.2f;

    private readonly FrameTiming[] timings = new FrameTiming[1];
    private float nextLight;
    private float nextHeavy;
    private float nextDodge;
    private readonly HashSet<ArenaDirector25D.Entry> announced = new();
    private string folder;

    private void Start()
    {
        string[] args = System.Environment.GetCommandLineArgs();

        // Banc d'essai : Nathan doit rester en vie (vie remise au plein à chaque image) ; un tir à bout portant
        // le pulvériserait, il encaisse donc le coup normal.
        if (args.Contains(Argument) || args.Contains(CaptureArgument) || args.Contains(ShowcaseArgument))
        {
            // Sans focus, le build se met en pause : le test ne doit pas dépendre de la fenêtre au premier plan.
            Application.runInBackground = true;
            var gibs = player.GetComponent<GibSource>();

            if (gibs != null)
                gibs.Configure(gibs.Set, false, gibs.Height);
        }

        if (args.Contains(Argument))
            StartCoroutine(Run(false));
        else if (args.Contains(CaptureArgument))
            StartCoroutine(Run(true));
        else if (args.Contains(ShowcaseArgument))
            StartCoroutine(Showcase());
    }

    private IEnumerator Run(bool capture)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = capture ? 60 : -1;
        GameOptions.Gore = GoreLevel.Full;

        yield return new WaitForSecondsRealtime(0.5f);
        director.StopWaves();
        director.SpawnAround(enemies);
        yield return new WaitForSecondsRealtime(warmup + 1.5f);

        var cpu = new List<double>();
        var frame = new List<double>();
        var onScreen = new List<int>();
        string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Captures25D");
        float end = Time.realtimeSinceStartup + (capture ? 4f + captures * captureInterval : duration);
        float nextCapture = Time.realtimeSinceStartup + 4f;
        int shot = 0;

        if (capture)
            Directory.CreateDirectory(folder);

        while (Time.realtimeSinceStartup < end)
        {
            Fight();

            if (director.Alive < enemies)
                director.SpawnAround(enemies - director.Alive);

            if (capture && shot < captures && Time.realtimeSinceStartup >= nextCapture)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, $"combat_encercle_{shot:00}.png"));
                shot++;
                nextCapture += captureInterval;
            }

            FrameTimingManager.CaptureFrameTimings();

            if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
                cpu.Add(timings[0].cpuMainThreadFrameTime);

            frame.Add(Time.unscaledDeltaTime * 1000.0);
            onScreen.Add(CountOnScreen());
            yield return null;
        }

        GameInput.HorizontalOverride = null;
        GameInput.VerticalOverride = null;

        if (!capture)
        {
            string scene = $"arène 2,5D, combat encerclé : {enemies} ennemis demandés, à l'écran {onScreen.Min()} à {onScreen.Max()} " +
                           $"(moyenne {onScreen.Average():F1}), Nathan se bat seul, gore complet, {duration:F0} s, vsync coupée";
            string report = BenchmarkRunner.Report(cpu, frame, scene);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "benchmark_25d.txt"), report);
            Debug.Log(report);
        }

        yield return new WaitForSecondsRealtime(0.5f);
        Application.Quit();
    }

    private IEnumerator Showcase()
    {
        Application.targetFrameRate = 60;
        GameOptions.Gore = GoreLevel.Full;
        folder = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Captures25D");
        Directory.CreateDirectory(folder);
        director.ArrivalAnnounced += OnArrival;
        var combat = player.GetComponent<BrawlerCombat25D>();
        float end = Time.realtimeSinceStartup + 180f;

        // Zone 1, déroulé normal.
        while (!director.GoVisible && Time.realtimeSinceStartup < end)
        {
            Fight();
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
        Capture("12_fleche_go");
        GameInput.VerticalOverride = 0f;

        while (director.GoVisible && Time.realtimeSinceStartup < end)
        {
            Heal();
            GameInput.HorizontalOverride = 1f;
            yield return null;
        }

        GameInput.HorizontalOverride = 0f;
        yield return new WaitForSeconds(0.4f);
        GameInput.Inject(GameAction.Jump);
        yield return new WaitForSeconds(0.28f);
        Capture("13_saut_ombre_au_sol");
        yield return new WaitForSeconds(0.8f);

        GameInput.VerticalOverride = 1f;
        GameInput.Inject(GameAction.Dodge);
        yield return new WaitForSeconds(0.08f);
        Capture("14_esquive_vers_le_fond");
        yield return new WaitForSeconds(0.3f);
        GameInput.VerticalOverride = 0f;

        // Barre de fer de la zone 2 (la plus proche encore au sol).
        Pickup25D pickup = Pickup25D.Nearest(player.Ground, 30f, Pickup25D.Kind.Weapon, anyDepth: true);
        Vector2 bar = pickup != null ? pickup.Ground : player.Ground;

        while ((player.Ground - bar).magnitude > 0.3f && Time.realtimeSinceStartup < end)
        {
            Heal();
            Vector2 d = bar - player.Ground;
            GameInput.HorizontalOverride = Mathf.Abs(d.x) > 0.15f ? Mathf.Sign(d.x) : 0f;
            GameInput.VerticalOverride = Mathf.Abs(d.y) > 0.1f ? Mathf.Sign(d.y) : 0f;
            yield return null;
        }

        GameInput.HorizontalOverride = 0f;
        GameInput.VerticalOverride = 0f;
        GameInput.Inject(GameAction.Light);
        yield return new WaitForSeconds(0.3f);
        bool barShot = false, gunShot = false, cone = false, circle = false;

        while (Time.realtimeSinceStartup < end && !(barShot && gunShot && cone && circle))
        {
            Fight();

            if (!barShot && combat.HasWeapon && combat.IsAttacking && combat.CurrentFrame == combat.CurrentAttack.startup + 1)
            {
                barShot = true;
                Capture("15_barre_de_fer");
            }

            if (!gunShot && !combat.HasWeapon && NearestInBand(3.5f) && !combat.IsAttacking)
            {
                gunShot = true;
                GameInput.Inject(GameAction.Shotgun);
                StartCoroutine(CaptureLater("16_tir_fusil_cone", 0.13f));
            }

            if (!cone && Enemy25D.Living.Any(e => e.IsAimingShotgun && e.PhaseFrame == 30))
            {
                cone = true;
                Capture("17_cone_du_dealer");
            }

            if (!circle && Enemy25D.Living.Count(e => !e.IsArriving && (e.Body.Ground - player.Ground).magnitude < 3.5f) >= 4)
            {
                circle = true;
                Capture("18_encercle");
            }

            yield return null;
        }

        GameInput.HorizontalOverride = null;
        GameInput.VerticalOverride = null;
        yield return new WaitForSecondsRealtime(0.5f);
        Application.Quit();
    }

    private void OnArrival(ArenaDirector25D.Entry entry)
    {
        if (!announced.Add(entry))
            return;

        // Au moment où l'annonce se voit le mieux : repère, porte ouverte, ombre qui grandit.
        float delay = entry switch
        {
            ArenaDirector25D.Entry.Roof => 0.6f,
            ArenaDirector25D.Entry.Door => 0.55f,
            ArenaDirector25D.Entry.Window or ArenaDirector25D.Entry.Alley => 0.6f,
            _ => 0.35f,
        };

        StartCoroutine(CaptureLater($"0{(int)entry + 1}_arrivee_{entry}", delay));

        if (entry is ArenaDirector25D.Entry.Roof or ArenaDirector25D.Entry.Window)
            StartCoroutine(CaptureLater($"0{(int)entry + 1}_arrivee_{entry}_chute", entry == ArenaDirector25D.Entry.Roof ? 1.05f : 0.95f));
    }

    private IEnumerator CaptureLater(string name, float delay)
    {
        yield return new WaitForSeconds(delay);
        Capture(name);
    }

    private void Capture(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));

    private void Heal() => player.Health.Heal(player.Health.MaxHealth);

    private bool NearestInBand(float range) =>
        Enemy25D.Living.Any(e => !e.IsArriving && Mathf.Abs(e.Body.Depth - player.Depth) < 0.3f &&
                                 Mathf.Abs(e.Body.Ground.x - player.Ground.x) < range &&
                                 Mathf.Sign(e.Body.Ground.x - player.Ground.x) == player.Facing);

    // Nathan reste au milieu du groupe : il se tourne vers l'ennemi le plus proche, frappe, esquive de temps en temps.
    private void Fight()
    {
        Heal();
        Enemy25D nearest = null;
        float best = float.MaxValue;

        foreach (Enemy25D enemy in Enemy25D.Living)
        {
            float d = (enemy.Body.Ground - player.Ground).sqrMagnitude;

            if (!enemy.IsArriving && d < best)
            {
                best = d;
                nearest = enemy;
            }
        }

        if (nearest == null)
        {
            GameInput.HorizontalOverride = 0f;
            GameInput.VerticalOverride = 0f;
            return;
        }

        Vector2 delta = nearest.Body.Ground - player.Ground;
        GameInput.HorizontalOverride = Mathf.Abs(delta.x) > 0.9f ? Mathf.Sign(delta.x) : 0f;

        // Nathan reste vers le milieu de la zone, au cœur du groupe, au lieu de suivre un ennemi jusqu'au bord.
        Playfield25D field = Playfield25D.Instance;
        float middle = (field.MinX + field.MaxX) * 0.5f;

        if (Mathf.Abs(player.Ground.x - middle) > 2.5f)
            GameInput.HorizontalOverride = Mathf.Sign(middle - player.Ground.x);
        GameInput.VerticalOverride = Mathf.Abs(delta.y) > 0.2f ? Mathf.Sign(delta.y) : 0f;

        if (Time.time >= nextLight && Mathf.Abs(delta.x) < 1.3f)
        {
            GameInput.Inject(GameAction.Light);
            nextLight = Time.time + 0.12f;
        }

        if (Time.time >= nextHeavy && Mathf.Abs(delta.x) < 1.3f)
        {
            GameInput.Inject(GameAction.Heavy);
            nextHeavy = Time.time + 2.5f;
        }

        if (Time.time >= nextDodge)
        {
            GameInput.Inject(GameAction.Dodge);
            nextDodge = Time.time + 4f;
        }
    }

    private static int CountOnScreen()
    {
        Camera cam = Camera.main;
        int count = 0;

        foreach (Enemy25D enemy in Enemy25D.Living)
        {
            Vector3 v = cam.WorldToViewportPoint(enemy.Body.Center);

            if (v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f)
                count++;
        }

        return count;
    }
}
