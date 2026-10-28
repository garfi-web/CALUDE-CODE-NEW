using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemySpawn
{
    public EnemyArchetype archetype;
    public float x;
}

[Serializable]
public class EnemyWave
{
    public string name = "Vague";
    public EnemySpawn[] enemies = Array.Empty<EnemySpawn>();
    [Tooltip("Position x des abris de cette vague.")]
    public float[] covers = Array.Empty<float>();
    [Tooltip("La vague fait passer Nathan au stade M4 avec le fusil à pompe (test).")]
    public bool nathanM4;
}

// Arène de test : les touches 1 à 8 lancent une vague (docs/ENEMIES.md, dernière section).
// Une nouvelle vague efface la précédente, ses abris et les renforts.
public class EnemyWaveManager : MonoBehaviour
{
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private Cover coverPrefab;
    [SerializeField] private Transform target;
    [SerializeField] private float groundY = -1.5f;
    [SerializeField] private float minX = -19f;
    [SerializeField] private float maxX = 19f;
    [SerializeField] private float reinforcementOffset = 5f;
    [SerializeField] private int startWave;
    [SerializeField] private EnemyWave[] waves = Array.Empty<EnemyWave>();

    private readonly List<Enemy> enemies = new();
    private readonly List<Cover> covers = new();
    private Health targetHealth;
    private bool cleared;

    public IReadOnlyList<EnemyWave> Waves => waves;
    public EnemyWave CurrentWave { get; private set; }
    public IReadOnlyList<Enemy> Enemies => enemies;

    public event Action<string> WaveStarted;
    public event Action WaveCleared;
    // PV cumulés de la vague : actuels, maximum.
    public event Action<int, int> HealthChanged;

    private void Start()
    {
        if (target != null)
            targetHealth = target.GetComponent<Health>();

        if (startWave >= 0 && startWave < waves.Length)
            StartWave(startWave);
    }

    private void Update()
    {
        if (targetHealth != null && targetHealth.IsDead)
            return;

        for (int i = 0; i < waves.Length; i++)
        {
            if (GameInput.WavePressed(i))
            {
                StartWave(i);
                return;
            }
        }
    }

    public void StartWave(int index)
    {
        Clear();
        EnemyWave wave = waves[index];
        CurrentWave = wave;

        foreach (float x in wave.covers)
            SpawnCover(x);

        foreach (EnemySpawn spawn in wave.enemies)
        {
            if (spawn.archetype != null)
                Spawn(spawn.archetype, spawn.x);
        }

        cleared = false;
        WaveStarted?.Invoke(wave.name);
        RaiseHealth();
    }

    public Enemy Spawn(EnemyArchetype archetype, float x)
    {
        x = Mathf.Clamp(x, minX, maxX);
        var position = new Vector3(x, groundY + archetype.bodySize.y * 0.5f + 0.05f, 0f);
        Enemy enemy = Instantiate(enemyPrefab, position, Quaternion.identity, transform);
        enemy.Initialize(archetype, target);
        enemy.Face(target != null ? target.position.x - x : -1f);

        enemy.Health.HealthChanged += OnEnemyHealthChanged;
        enemy.Health.Died += OnEnemyDied;
        enemy.ReinforcementRequested += OnReinforcementRequested;
        enemies.Add(enemy);
        return enemy;
    }

    private void SpawnCover(float x)
    {
        Cover cover = Instantiate(coverPrefab, transform);
        var box = cover.GetComponent<BoxCollider2D>();
        float halfHeight = box != null ? box.size.y * cover.transform.lossyScale.y * 0.5f : 0.5f;
        cover.transform.position = new Vector3(x, groundY + halfHeight, 0f);
        covers.Add(cover);
    }

    private void Clear()
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
                continue;

            enemy.Health.HealthChanged -= OnEnemyHealthChanged;
            enemy.Health.Died -= OnEnemyDied;
            enemy.ReinforcementRequested -= OnReinforcementRequested;
            Destroy(enemy.gameObject);
        }

        foreach (Cover cover in covers)
        {
            if (cover != null)
                Destroy(cover.gameObject);
        }

        // Balles et grenades encore en vol (rendues à leur réserve).
        foreach (Projectile projectile in FindObjectsByType<Projectile>())
            Pool.Release(projectile);

        foreach (Grenade grenade in FindObjectsByType<Grenade>())
            Pool.Release(grenade);

        enemies.Clear();
        covers.Clear();
    }

    // Le renfort arrive du côté de l'appelant, un peu plus loin que lui.
    private void OnReinforcementRequested(Enemy caller, EnemyArchetype archetype)
    {
        float side = target != null ? Mathf.Sign(caller.Position.x - target.position.x) : 1f;
        Enemy reinforcement = Spawn(archetype, caller.Position.x + side * reinforcementOffset);
        reinforcement.Alert();
        RaiseHealth();
    }

    private void OnEnemyHealthChanged(int current, int max) => RaiseHealth();

    private void OnEnemyDied()
    {
        if (cleared)
            return;

        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead)
                return;
        }

        cleared = true;
        WaveCleared?.Invoke();
    }

    private void RaiseHealth()
    {
        int current = 0;
        int max = 0;

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
                continue;

            current += enemy.Health.CurrentHealth;
            max += enemy.Health.MaxHealth;
        }

        HealthChanged?.Invoke(current, max);
    }
}
