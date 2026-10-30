using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Déroulé de l'arène 2,5D, façon beat'em up : la caméra se bloque sur une zone, les ennemis arrivent par vagues,
// chacun par une direction tirée au sort (bords gauche et droit, premier plan, toit, porte, fenêtre, ruelle).
// Le tirage est pondéré : une direction qui vient de servir perd du poids, les autres en regagnent, ce qui évite les
// répétitions sans rendre l'ordre prévisible. Chaque arrivée est annoncée (bruit, repère au bord de l'écran, porte qui
// s'ouvre, ombre qui grandit au sol avant une chute). Zone nettoyée : flèche « GO », la caméra s'ouvre vers la suivante.
// Les ennemis viennent d'une réserve créée avec la scène (aucune instanciation pendant le combat).
public class ArenaDirector25D : MonoBehaviour
{
    public enum Entry { Left, Right, Front, Roof, Door, Window, Alley }

    [Serializable]
    public class Wave
    {
        public EnemyArchetype[] enemies = Array.Empty<EnemyArchetype>();
    }

    // Apparence en vue de 3/4 d'un archétype (les autres gardent leurs sprites de profil pour l'instant).
    [Serializable]
    public class Look
    {
        public EnemyArchetype archetype;
        public CharacterLook25D look;
    }

    [Serializable]
    public class Zone
    {
        public string name = "Zone";
        public float centerX;
        public SpawnPoint25D[] entrances = Array.Empty<SpawnPoint25D>();
        public Wave[] waves = Array.Empty<Wave>();
    }

    [SerializeField] private Zone[] zones = Array.Empty<Zone>();
    [SerializeField] private Enemy25D[] pool = Array.Empty<Enemy25D>();
    [SerializeField] private Body25D player;
    [SerializeField] private CameraFollow cameraFollow;
    [Tooltip("Cible suivie par la caméra : l'abscisse de Nathan, à hauteur fixe (la profondeur ne fait pas bouger l'image).")]
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private float cameraY = 1.2f;
    [Tooltip("Demi-largeur de la vue (m) : 3,4 × 16/9.")]
    [SerializeField] private float halfView = 6.04f;
    [SerializeField] private float edgeMargin = 0.45f;
    [SerializeField] private HUD25D hud;
    [SerializeField] private Look[] looks = Array.Empty<Look>();

    [Header("Arrivées")]
    [SerializeField] private int maxAlive = 8;
    [SerializeField] private Vector2 arrivalGap = new(0.45f, 1.3f);
    [SerializeField] private float edgeWarning = 0.7f;
    [SerializeField] private float dropWarning = 0.9f;
    [SerializeField] private float roofHeight = 7f;
    [Tooltip("Ombres d'annonce des chutes du toit (réserve).")]
    [SerializeField] private SpriteRenderer[] dropShadows = Array.Empty<SpriteRenderer>();
    [SerializeField] private AudioClip runSound;
    [SerializeField] private AudioClip roofSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip goSound;
    [Tooltip("Archétypes du banc d'essai (touche 8 : 8 ennemis autour de Nathan).")]
    [SerializeField] private EnemyArchetype[] benchmarkArchetypes = Array.Empty<EnemyArchetype>();

    private readonly float[] weights = { 1f, 1f, 1f, 1f, 1f, 1f, 1f };
    private int pending;
    private int zoneIndex;
    private Coroutine run;

    public int Alive => Enemy25D.Living.Count + pending;
    public bool GoVisible { get; private set; }

    // Une arrivée vient d'être annoncée (banc d'essai : capture de chaque type d'arrivée).
    public event Action<Entry> ArrivalAnnounced;
    public Zone CurrentZone => zones.Length > 0 ? zones[Mathf.Clamp(zoneIndex, 0, zones.Length - 1)] : null;

    private void Start()
    {
        foreach (Enemy25D enemy in pool)
            enemy.gameObject.SetActive(false);

        run = StartCoroutine(Run());
    }

    private void Update()
    {
        if (GameInput.RestartPressed)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (GameInput.WavePressed(7))
            SpawnAround(8 - Alive);
    }

    private void LateUpdate()
    {
        if (cameraAnchor != null && player != null)
            cameraAnchor.position = new Vector3(player.Ground.x, cameraY, 0f);
    }

    // Banc d'essai : arrête le déroulé des zones (les ennemis présents restent).
    public void StopWaves()
    {
        if (run != null)
            StopCoroutine(run);

        run = null;
        SetGo(false);
    }

    // ---------------------------------------------------------------- Zones

    private IEnumerator Run()
    {
        for (zoneIndex = 0; zoneIndex < zones.Length; zoneIndex++)
        {
            Zone zone = zones[zoneIndex];
            Lock(zone);
            hud?.Message(zone.name, 1.5f);

            foreach (Wave wave in zone.waves)
            {
                yield return SpawnWave(zone, wave);

                while (Alive > 0)
                    yield return null;

                yield return new WaitForSeconds(0.6f);
            }

            if (zoneIndex == zones.Length - 1)
            {
                hud?.Message("RUE NETTOYÉE — R pour recommencer", 999f);
                yield break;
            }

            Zone next = zones[zoneIndex + 1];
            Open(zone, next);
            GameAudio.Play(goSound);
            SetGo(true);

            while (player.Ground.x < next.centerX - halfView * 0.5f)
                yield return null;

            SetGo(false);
        }
    }

    private void SetGo(bool visible)
    {
        GoVisible = visible;
        hud?.ShowGo(visible);
    }

    private void Lock(Zone zone)
    {
        cameraFollow.SetBounds(zone.centerX, zone.centerX);
        Playfield25D.Instance.SetX(zone.centerX - halfView + edgeMargin, zone.centerX + halfView - edgeMargin);
    }

    private void Open(Zone zone, Zone next)
    {
        cameraFollow.SetBounds(zone.centerX, next.centerX);
        Playfield25D.Instance.SetX(zone.centerX - halfView + edgeMargin, next.centerX + halfView - edgeMargin);
    }

    private IEnumerator SpawnWave(Zone zone, Wave wave)
    {
        foreach (EnemyArchetype archetype in wave.enemies)
        {
            while (Alive >= maxAlive)
                yield return null;

            StartCoroutine(Arrive(zone, archetype, PickEntry(zone)));
            yield return new WaitForSeconds(UnityEngine.Random.Range(arrivalGap.x, arrivalGap.y));
        }
    }

    // Banc d'essai et touche 8 : count ennemis d'un coup, par les bords, le premier plan et le toit.
    public void SpawnAround(int count)
    {
        Zone zone = CurrentZone;

        if (zone == null || benchmarkArchetypes.Length == 0)
            return;

        for (int i = 0; i < count; i++)
        {
            var entry = (Entry)(i % 4);
            StartCoroutine(Arrive(zone, benchmarkArchetypes[i % benchmarkArchetypes.Length], entry));
        }
    }

    // ---------------------------------------------------------- Tirage

    private Entry PickEntry(Zone zone)
    {
        float total = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            if (Available(zone, (Entry)i))
                total += weights[i];
        }

        float roll = UnityEngine.Random.value * total;
        var picked = Entry.Left;

        for (int i = 0; i < weights.Length; i++)
        {
            if (!Available(zone, (Entry)i))
                continue;

            picked = (Entry)i;
            roll -= weights[i];

            if (roll <= 0f)
                break;
        }

        // La direction tirée perd du poids, les autres en regagnent.
        for (int i = 0; i < weights.Length; i++)
            weights[i] = i == (int)picked ? weights[i] * 0.3f : Mathf.Min(1f, weights[i] + 0.25f);

        return picked;
    }

    private static bool Available(Zone zone, Entry entry) =>
        entry is Entry.Left or Entry.Right or Entry.Front or Entry.Roof || FindEntrance(zone, entry) != null;

    private static SpawnPoint25D FindEntrance(Zone zone, Entry entry)
    {
        foreach (SpawnPoint25D point in zone.entrances)
        {
            if (point != null && point.Kind == entry)
                return point;
        }

        return null;
    }

    // ---------------------------------------------------------- Arrivée

    private Enemy25D TakeFromPool()
    {
        foreach (Enemy25D enemy in pool)
        {
            if (!enemy.gameObject.activeSelf)
                return enemy;
        }

        return null;
    }

    private IEnumerator Arrive(Zone zone, EnemyArchetype archetype, Entry entry)
    {
        pending++;
        Playfield25D field = Playfield25D.Instance;
        float camX = zone.centerX;
        float depth = UnityEngine.Random.Range(field.Front + 0.25f, field.Back - 0.25f);
        Vector2 start, target;
        float height = 0f, lift = 0f;
        ArrivalAnnounced?.Invoke(entry);

        switch (entry)
        {
            case Entry.Left:
            case Entry.Right:
            {
                float side = entry == Entry.Left ? -1f : 1f;
                GameAudio.Play(runSound);
                hud?.Mark(new Vector2(camX + side * (halfView - 0.5f), depth + 1f), side < 0f ? "◀ !" : "! ▶", edgeWarning + 0.4f);
                yield return new WaitForSeconds(edgeWarning);
                start = new Vector2(camX + side * (halfView + 1.2f), depth);
                target = new Vector2(camX + side * (halfView - 1.8f), depth);
                break;
            }

            case Entry.Front:
            {
                float x = camX + UnityEngine.Random.Range(-halfView + 1.5f, halfView - 1.5f);
                GameAudio.Play(runSound);
                hud?.Mark(new Vector2(x, field.Front - 0.1f), "▲ !", edgeWarning + 0.4f);
                yield return new WaitForSeconds(edgeWarning);
                start = new Vector2(x, field.Front - 1.8f);
                target = new Vector2(x, field.Front + 0.5f);
                break;
            }

            case Entry.Roof:
            {
                // Chute près de Nathan : l'ombre grandit au sol avant que l'ennemi tombe.
                float x = Mathf.Clamp(player.Ground.x + UnityEngine.Random.Range(-3f, 3f), field.MinX + 0.5f, field.MaxX - 0.5f);
                start = target = new Vector2(x, depth);
                height = roofHeight;
                GameAudio.Play(roofSound);
                yield return GrowShadow(start);
                break;
            }

            default:
            {
                SpawnPoint25D point = FindEntrance(zone, entry);
                point.Announce();
                yield return new WaitForSeconds(point.Warning);
                float x = point.X;

                if (entry == Entry.Window)
                {
                    start = new Vector2(x, field.Back - 0.05f);
                    height = point.Height;
                    lift = 3f;
                    target = new Vector2(x, field.Back - 0.8f);
                }
                else
                {
                    start = new Vector2(x, field.Back + (entry == Entry.Alley ? 0.4f : 0.15f));
                    target = new Vector2(x + UnityEngine.Random.Range(-0.6f, 0.6f), field.Back - 0.9f);
                }

                break;
            }
        }

        Enemy25D enemy = TakeFromPool();
        pending--;

        if (enemy == null)
            yield break;

        enemy.gameObject.SetActive(true);
        enemy.Setup(archetype, player, Array.Find(looks, l => l.archetype == archetype)?.look);
        enemy.BeginArrival(start, height, target, lift);

        if (height <= 0f)
            yield break;

        // Réception d'une chute : poussière, tremblement, bruit.
        while (enemy.Body.Airborne && enemy.isActiveAndEnabled)
            yield return null;

        GameAudio.Play(landSound);

        if (CombatVfx.Instance != null)
            CombatVfx.Instance.Dust(enemy.Body.Ground, 10);

        if (CameraFollow.Main != null)
            CameraFollow.Main.Shake(height > 4f ? 0.35f : 0.15f);
    }

    private IEnumerator GrowShadow(Vector2 ground)
    {
        SpriteRenderer shadow = Array.Find(dropShadows, s => !s.enabled);

        if (shadow == null)
        {
            yield return new WaitForSeconds(dropWarning);
            yield break;
        }

        shadow.enabled = true;
        shadow.transform.position = new Vector3(ground.x, ground.y, 0f);
        Color color = shadow.color;
        // Ombre finale de 1,1 m, quelle que soit la taille de l'image.
        float unit = 1.1f / Mathf.Max(0.01f, shadow.sprite.bounds.size.x);

        for (float t = 0f; t < dropWarning; t += Time.deltaTime)
        {
            float k = t / dropWarning;
            float size = (0.2f + 0.8f * k) * unit;
            shadow.transform.localScale = new Vector3(size, size * 0.32f, 1f);
            shadow.color = new Color(color.r, color.g, color.b, 0.65f * k);
            yield return null;
        }

        shadow.enabled = false;
        shadow.color = color;
    }
}
