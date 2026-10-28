using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Arène de test : chaque vague lancée remet Nathan à pleine vie (et au stade M4 si la vague l'impose).
// Message et ralenti à la fin d'une vague, défaite à la mort du joueur, affichée après la courte
// mise en scène de sa mort (ralenti du corps pulvérisé).
// R (ou Start à la manette) relance l'arène après une défaite.
public class GameManager : MonoBehaviour
{
    [SerializeField] private Health player;
    [SerializeField] private EnemyWaveManager waves;
    [SerializeField] private CombatHUD hud;
    [SerializeField] private CombatVfx vfx;
    [Tooltip("Secondes réelles entre la mort de Nathan et l'écran de défaite.")]
    [SerializeField] private float defeatDelay = 1.2f;

    private bool gameOver;
    private NathanForm form;

    private void Awake()
    {
        // Abonnement avant le Start du gestionnaire de vagues, qui lance la première vague.
        if (player != null)
        {
            player.Died += OnPlayerDied;
            form = player.GetComponent<NathanForm>();
        }

        if (waves != null)
        {
            waves.WaveStarted += OnWaveStarted;
            waves.WaveCleared += OnWaveCleared;
            waves.HealthChanged += OnEnemiesHealthChanged;
        }
    }

    private void OnDestroy()
    {
        if (player != null)
            player.Died -= OnPlayerDied;

        if (waves != null)
        {
            waves.WaveStarted -= OnWaveStarted;
            waves.WaveCleared -= OnWaveCleared;
            waves.HealthChanged -= OnEnemiesHealthChanged;
        }
    }

    private void Update()
    {
        if (gameOver && GameInput.RestartPressed)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnWaveStarted(string waveName)
    {
        if (form != null && waves.CurrentWave != null && waves.CurrentWave.nathanM4)
            form.SetM4(true);

        if (player != null)
            player.Heal(player.MaxHealth);

        if (player != null && player.TryGetComponent(out HitReaction reaction))
            reaction.ResetState();

        if (vfx != null)
            vfx.ClearGore();

        if (hud != null)
        {
            hud.SetEnemyLabel(waveName.ToUpperInvariant());
            hud.ShowMessage(string.Empty);
        }
    }

    private void OnWaveCleared()
    {
        // Le coup qui achève le dernier ennemi passe au ralenti.
        if (vfx != null)
            vfx.FinalBlow();

        if (!gameOver && hud != null)
            hud.ShowMessage("VAGUE TERMINÉE\n1 à 8 : lancer une vague");
    }

    private void OnEnemiesHealthChanged(int current, int max)
    {
        if (hud != null)
            hud.SetEnemyHealth(current, max);
    }

    private void OnPlayerDied()
    {
        if (gameOver)
            return;

        gameOver = true;
        StartCoroutine(ShowDefeat());
    }

    private IEnumerator ShowDefeat()
    {
        yield return new WaitForSecondsRealtime(defeatDelay);

        if (hud != null)
            hud.ShowMessage("DÉFAITE\nAppuie sur R pour recommencer");
    }
}
