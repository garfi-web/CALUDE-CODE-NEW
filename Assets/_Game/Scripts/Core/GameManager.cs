using UnityEngine;
using UnityEngine.SceneManagement;

// Victoire quand tous les ennemis sont morts, défaite à la mort du joueur.
// R (ou Start à la manette) relance l'arène une fois la partie terminée.
public class GameManager : MonoBehaviour
{
    [SerializeField] private Health player;
    [SerializeField] private Health[] enemies;
    [SerializeField] private CombatHUD hud;

    private int enemiesRemaining;
    private bool gameOver;

    private void Start()
    {
        if (player != null)
            player.Died += OnPlayerDied;

        foreach (Health enemy in enemies)
        {
            if (enemy == null)
                continue;

            enemy.Died += OnEnemyDied;
            enemiesRemaining++;
        }

        if (hud != null)
            hud.ShowMessage(string.Empty);
    }

    private void OnDestroy()
    {
        if (player != null)
            player.Died -= OnPlayerDied;

        foreach (Health enemy in enemies)
        {
            if (enemy != null)
                enemy.Died -= OnEnemyDied;
        }
    }

    private void Update()
    {
        if (gameOver && GameInput.RestartPressed)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnPlayerDied()
    {
        if (gameOver)
            return;

        gameOver = true;

        if (hud != null)
            hud.ShowMessage("DÉFAITE\nAppuie sur R pour recommencer");
    }

    private void OnEnemyDied()
    {
        enemiesRemaining--;

        if (gameOver || enemiesRemaining > 0)
            return;

        gameOver = true;

        if (hud != null)
            hud.ShowMessage("VICTOIRE\nAppuie sur R pour recommencer");
    }
}
