using UnityEngine;

// Sol de l'arène 2,5D (beat'em up) : un plan X (gauche/droite) × profondeur (haut/bas de l'écran).
// La profondeur est la coordonnée y du monde au niveau des pieds ; la hauteur (saut, projection, chute)
// est un axe séparé, porté par Body25D. Plus bas à l'écran = plus près = dessiné devant.
// Les bornes en X suivent la zone de combat que bloque ArenaDirector25D.
public class Playfield25D : MonoBehaviour
{
    public static Playfield25D Instance { get; private set; }

    // Ordre d'affichage : 100 crans par mètre de profondeur, autour de 1000 (décor en dessous, premier plan au-dessus).
    private const float SortingPerMeter = 100f;
    private const int SortingBase = 1000;

    [Tooltip("Bord avant du sol (bas de l'écran).")]
    [SerializeField] private float front = -2.1f;
    [Tooltip("Bord arrière du sol (pied des façades).")]
    [SerializeField] private float back = 0.1f;
    [SerializeField] private float minX = -5.6f;
    [SerializeField] private float maxX = 5.6f;

    public float Front => front;
    public float Back => back;
    public float MinX => minX;
    public float MaxX => maxX;

    private void Awake()
    {
        Instance = this;
        // Le sang et les morceaux se posent à la profondeur de leur victime (CombatVfx).
        CombatVfx.GroundOf = health => health != null && health.TryGetComponent(out Body25D body) ? (float?)body.Depth : null;
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        CombatVfx.GroundOf = null;
    }

    public void SetX(float min, float max)
    {
        minX = min;
        maxX = Mathf.Max(min, max);
    }

    public static int SortingOrder(float depth) => SortingBase + Mathf.RoundToInt(-depth * SortingPerMeter);

    // Ramène un point du sol dans l'aire de jeu. wall : -1 ou +1 si le bord gauche ou droit a arrêté le point.
    public Vector2 Clamp(Vector2 ground, out int wall)
    {
        wall = ground.x < minX ? -1 : ground.x > maxX ? 1 : 0;
        return new Vector2(Mathf.Clamp(ground.x, minX, maxX), Mathf.Clamp(ground.y, front, back));
    }

    public bool Contains(Vector2 ground, float margin = 0f) =>
        ground.x >= minX - margin && ground.x <= maxX + margin && ground.y >= front - margin && ground.y <= back + margin;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(new Vector3((minX + maxX) * 0.5f, (front + back) * 0.5f), new Vector3(maxX - minX, back - front));
    }
}
