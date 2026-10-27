using UnityEngine;

// Affichage de debug (F1) : images par seconde, attaque de Nathan image par image
// (démarrage en gris, actif en rouge, récupération en bleu, fenêtre d'annulation encadrée),
// zone de frappe, et phase de chaque ennemi au-dessus de sa tête.
// F2 donne à Nathan une arme de test tranchante (sang, démembrement). F3 lance la démo de combo.
public class CombatDebugOverlay : MonoBehaviour
{
    // Modifiable aussi par les scripts de test et de capture (zones de frappe affichées sans F1).
    public static bool Visible { get; set; }

    [SerializeField] private PlayerCombat player;
    [SerializeField] private ComboDemo demo;

    private static readonly Color StartupColor = new(0.7f, 0.7f, 0.7f);
    private static readonly Color ActiveColor = new(1f, 0.25f, 0.25f);
    private static readonly Color RecoveryColor = new(0.3f, 0.5f, 1f);

    private Texture2D pixel;
    private GUIStyle style;
    private float fps;
    private string lastAttack = "—";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Visible = false;

    private void Awake()
    {
        pixel = new Texture2D(1, 1);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();

        if (player != null)
            player.HitLanded += OnHitLanded;
    }

    private void OnDestroy()
    {
        if (player != null)
            player.HitLanded -= OnHitLanded;

        Destroy(pixel);
    }

    private void OnHitLanded(AttackData attack, Health target) =>
        lastAttack = $"{attack.displayName} : {attack.damage} dégâts, hitstop {attack.hitstop} images, recul {attack.knockback}";

    private void Update()
    {
        if (GameInput.DebugOverlayPressed)
            Visible = !Visible;

        if (GameInput.DebugWeaponPressed && player != null)
            player.TestBlade = !player.TestBlade;

        if (GameInput.DemoPressed && demo != null)
            demo.Play();

        if (Time.unscaledDeltaTime > 0f)
            fps = Mathf.Lerp(fps, 1f / Time.unscaledDeltaTime, 0.05f);
    }

    private void OnGUI()
    {
        if (!Visible)
            return;

        style ??= new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true };

        float x = 20f;
        float y = 110f;
        Label(x, ref y, $"<b>DEBUG (F1)</b>   {fps:F0} images/s   ·   arme de test (F2) : {(player != null && player.TestBlade ? "lame" : "mains nues")}   ·   démo (F3)");

        if (player != null)
            DrawPlayer(x, ref y);

        Label(x, ref y, $"Dernier coup : {lastAttack}");
        DrawEnemies();
    }

    private void DrawPlayer(float x, ref float y)
    {
        AttackData attack = player.CurrentAttack;

        if (attack == null)
        {
            Label(x, ref y, "Nathan : —");
            return;
        }

        Label(x, ref y,
            $"Nathan : <b>{attack.displayName}</b>   {player.Phase} {player.CurrentFrame}/{attack.TotalFrames}   " +
            $"(démarrage {attack.startup}, actif {attack.active}, récupération {attack.recovery}, annulation {attack.cancelWindow.x}-{attack.cancelWindow.y})" +
            (player.HitConnected ? "   <color=#7f7>touché</color>" : string.Empty));

        // Frise des images : une case par image.
        const float cell = 9f;

        for (int i = 1; i <= attack.TotalFrames; i++)
        {
            Color color = i <= attack.startup ? StartupColor : i <= attack.startup + attack.active ? ActiveColor : RecoveryColor;

            if (i > player.CurrentFrame)
                color *= 0.45f;

            Rect r = new(x + (i - 1) * cell, y, cell - 1f, 14f);
            DrawRect(r, color);

            if (i >= attack.cancelWindow.x && i <= attack.cancelWindow.y)
                DrawRect(new Rect(r.x, r.yMax + 1f, cell - 1f, 3f), Color.yellow);
        }

        y += 24f;
    }

    private void DrawEnemies()
    {
        Camera cam = Camera.main;

        if (cam == null)
            return;

        foreach (Enemy enemy in Enemy.Living)
        {
            Vector3 screen = cam.WorldToScreenPoint(enemy.Position + Vector2.up * (enemy.Archetype.bodySize.y * 0.5f + 0.5f));

            if (screen.z < 0f)
                continue;

            string phase = enemy.CurrentPhase switch
            {
                Enemy.Phase.Windup => "<color=#ffd84d>armement</color>",
                Enemy.Phase.Active => "<color=#ff5050>actif</color>",
                Enemy.Phase.Recovery => "<color=#6f9bff>récupération</color>",
                _ => enemy.IsStunned ? "<color=#bbb>étourdi</color>" : "libre",
            };

            string text = enemy.CurrentAttack != null
                ? $"{enemy.CurrentAttack.name}\n{phase} {enemy.PhaseFrame}/{enemy.PhaseLength}"
                : phase;

            GUI.Label(new Rect(screen.x - 120f, Screen.height - screen.y - 50f, 240f, 50f), text, style);
        }
    }

    private void Label(float x, ref float y, string text)
    {
        GUI.Label(new Rect(x, y, 1600f, 26f), text, style);
        y += 26f;
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, pixel);
        GUI.color = previous;
    }
}
