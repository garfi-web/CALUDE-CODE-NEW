using UnityEngine;
using UnityEngine.UI;

// Barres de vie et message central. Les barres se remplissent en réglant
// l'ancre droite de l'image de remplissage, sans dépendre d'un sprite.
// La barre ennemie montre les PV cumulés de la vague (GameManager).
public class CombatHUD : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private RectTransform playerFill;
    [SerializeField] private RectTransform enemyFill;
    [SerializeField] private Text enemyLabel;
    [SerializeField] private Text messageText;

    private void OnEnable()
    {
        if (playerHealth != null) playerHealth.HealthChanged += OnPlayerHealthChanged;
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.HealthChanged -= OnPlayerHealthChanged;
    }

    private void Start()
    {
        if (playerHealth != null) OnPlayerHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    public void ShowMessage(string message)
    {
        if (messageText == null)
            return;

        messageText.text = message;
        messageText.enabled = !string.IsNullOrEmpty(message);
    }

    public void SetEnemyLabel(string label)
    {
        if (enemyLabel != null)
            enemyLabel.text = label;
    }

    public void SetEnemyHealth(int current, int max) => SetFill(enemyFill, current, max);

    private void OnPlayerHealthChanged(int current, int max) => SetFill(playerFill, current, max);

    private static void SetFill(RectTransform fill, int current, int max)
    {
        if (fill == null)
            return;

        Vector2 anchorMax = fill.anchorMax;
        anchorMax.x = max > 0 ? (float)current / max : 0f;
        fill.anchorMax = anchorMax;
    }
}
