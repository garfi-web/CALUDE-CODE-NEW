using UnityEngine;
using UnityEngine.UI;

// Interface de l'arène 2,5D : vie et cartouches de Nathan, ennemis restants, flèche « GO », message de zone,
// repères d'arrivée au bord de l'écran (gauche, droite, bas) et images par seconde (F1).
public class HUD25D : MonoBehaviour
{
    [SerializeField] private Health player;
    [SerializeField] private BrawlerCombat25D combat;
    [SerializeField] private ArenaDirector25D director;
    [SerializeField] private RectTransform healthFill;
    [SerializeField] private Text ammo;
    [SerializeField] private Text enemies;
    [SerializeField] private Text go;
    [SerializeField] private Text message;
    [SerializeField] private Text fps;
    [SerializeField] private Text[] markers = System.Array.Empty<Text>();

    private Vector2[] markerPoints;
    private float[] markerUntil;
    private float messageUntil;
    private bool showGo;
    private float smoothedDelta = 1f / 60f;

    private void Awake()
    {
        markerPoints = new Vector2[markers.Length];
        markerUntil = new float[markers.Length];

        foreach (Text marker in markers)
            marker.enabled = false;

        go.enabled = false;
        message.enabled = false;
        fps.enabled = false;

        if (combat != null)
            combat.AmmoChanged += (shells, magazine) => ammo.text = $"Fusil {shells}/{magazine}";
    }

    public void ShowGo(bool value) => showGo = value;

    public void Message(string text, float duration)
    {
        message.text = text;
        message.enabled = true;
        messageUntil = Time.time + duration;
    }

    // Repère d'arrivée : suit le point du monde, ramené dans l'écran.
    public void Mark(Vector2 world, string label, float duration)
    {
        int index = System.Array.FindIndex(markerUntil, until => until < Time.time);

        if (index < 0)
            return;

        markers[index].text = label;
        markers[index].enabled = true;
        markerPoints[index] = world;
        markerUntil[index] = Time.time + duration;
    }

    private void Update()
    {
        if (GameInput.DebugOverlayPressed)
            fps.enabled = !fps.enabled;

        smoothedDelta = Mathf.Lerp(smoothedDelta, Time.unscaledDeltaTime, 0.05f);

        if (fps.enabled)
            fps.text = $"{1f / smoothedDelta:F0} FPS — {Enemy25D.Living.Count} ennemis";

        if (player != null)
            healthFill.localScale = new Vector3(player.CurrentHealth / (float)player.MaxHealth, 1f, 1f);

        if (director != null)
            enemies.text = director.Alive > 0 ? $"Ennemis : {director.Alive}" : "";

        // Clignote : la flèche invite à avancer.
        go.enabled = showGo && Mathf.FloorToInt(Time.time * 2.5f) % 2 == 0;
        message.enabled = Time.time < messageUntil;

        Camera cam = Camera.main;

        for (int i = 0; i < markers.Length; i++)
        {
            bool visible = Time.time < markerUntil[i] && cam != null;
            markers[i].enabled = visible && Mathf.FloorToInt(Time.time * 8f) % 2 == 0;

            if (!visible)
                continue;

            Vector3 screen = cam.WorldToScreenPoint(markerPoints[i]);
            float marginX = markers[i].rectTransform.rect.width * 0.5f * markers[i].canvas.scaleFactor + 10f;
            float marginY = markers[i].rectTransform.rect.height * 0.5f * markers[i].canvas.scaleFactor + 30f;
            screen.x = Mathf.Clamp(screen.x, marginX, Screen.width - marginX);
            screen.y = Mathf.Clamp(screen.y, marginY, Screen.height - marginY);
            markers[i].rectTransform.position = screen;
        }
    }
}
