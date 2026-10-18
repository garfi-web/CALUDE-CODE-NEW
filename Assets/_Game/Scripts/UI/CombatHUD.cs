using UnityEngine;
using UnityEngine.UI;

// Barres de vie et message central. Les barres se remplissent en réglant
// l'ancre droite de l'image de remplissage, sans dépendre d'un sprite.
public class CombatHUD : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Health enemyHealth;
    [SerializeField] private RectTransform playerFill;
    [SerializeField] private RectTransform enemyFill;
    [SerializeField] private Text messageText;

    private void OnEnable()
    {
        if (playerHealth != null) playerHealth.HealthChanged += OnPlayerHealthChanged;
        if (enemyHealth != null) enemyHealth.HealthChanged += OnEnemyHealthChanged;
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.HealthChanged -= OnPlayerHealthChanged;
        if (enemyHealth != null) enemyHealth.HealthChanged -= OnEnemyHealthChanged;
    }

    private void Start()
    {
        if (playerHealth != null) OnPlayerHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        if (enemyHealth != null) OnEnemyHealthChanged(enemyHealth.CurrentHealth, enemyHealth.MaxHealth);
    }

    public void ShowMessage(string message)
    {
        if (messageText == null)
            return;

        messageText.text = message;
        messageText.enabled = !string.IsNullOrEmpty(message);
    }

    private void OnPlayerHealthChanged(int current, int max) => SetFill(playerFill, current, max);

    private void OnEnemyHealthChanged(int current, int max) => SetFill(enemyFill, current, max);

    private static void SetFill(RectTransform fill, int current, int max)
    {
        if (fill == null)
            return;

        Vector2 anchorMax = fill.anchorMax;
        anchorMax.x = max > 0 ? (float)current / max : 0f;
        fill.anchorMax = anchorMax;
    }
}
