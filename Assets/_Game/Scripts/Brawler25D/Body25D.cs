using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Corps d'un combattant ou d'un objet de l'arène 2,5D. Le Rigidbody2D (cinématique, sans gravité) porte la position
// au sol : x et profondeur, déplacés par linearVelocity. La hauteur est un axe à part, avec sa propre gravité :
// le visuel monte et descend, l'ombre ovale reste au sol. Pendant un HitStop, la hauteur est figée comme le reste.
// L'ordre d'affichage suit la profondeur (SortingGroup) : plus bas à l'écran = devant.
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(Rigidbody2D), typeof(HitStop))]
public class Body25D : MonoBehaviour
{
    public enum Team { Player, Enemy, Neutral }

    // Corps actifs de la scène (frappes, encerclement, ramassage).
    public static readonly List<Body25D> All = new();

    // Hauteur du centre du corps au-dessus des pieds : repère des hitboxOffset.y (0,80 m dans l'arène latérale).
    public const float CenterHeight = 0.8f;

    [SerializeField] private Team team = Team.Enemy;
    [Tooltip("Visuel (sprites, pivot aux pieds) : il monte avec la hauteur.")]
    [SerializeField] private Transform visual;
    [Tooltip("Ombre ovale, hors du personnage : elle reste au sol, sous tous les personnages.")]
    [SerializeField] private SpriteRenderer shadow;
    [SerializeField] private SortingGroup sortingGroup;
    [SerializeField] private float gravity = 45f;
    [SerializeField] private float bodyHeight = 1.7f;
    [SerializeField] private float bodyWidth = 0.7f;
    [SerializeField] private float shadowWidth = 0.95f;

    private Rigidbody2D rb;
    private HitStop hitStop;
    private Color shadowColor;
    private float shadowUnit = 1f;

    public Team Side => team;
    public Rigidbody2D Rigidbody => rb;
    public HitStop HitStop => hitStop;
    public Health Health { get; private set; }
    public Transform Visual => visual;
    public SpriteRenderer Shadow => shadow;

    public float Height { get; private set; }
    public float HeightVelocity { get; set; }
    public bool Airborne => Height > 0.001f || HeightVelocity > 0f;
    public bool IsFrozen => hitStop.IsFrozen;
    public Vector2 Ground => rb.position;
    public float Depth => rb.position.y;
    public float BodyHeight => bodyHeight;
    public float BodyWidth => bodyWidth;
    // Centre du corps à l'écran (effets d'impact, sang).
    public Vector2 Center => new(rb.position.x, rb.position.y + Height + bodyHeight * 0.5f);
    public float Facing => transform.localScale.x >= 0f ? 1f : -1f;

    // Pendant une entrée en scène (porte, bas de l'écran), le corps peut être hors de l'aire de jeu.
    public bool ClampToPlayfield { get; set; } = true;

    // Vitesse de chute à l'impact.
    public event Action<float> Landed;
    // Bord de l'aire de jeu atteint : côté (-1 gauche, +1 droite) et vitesse en X à l'impact.
    public event Action<int, float> HitWall;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitStop = GetComponent<HitStop>();
        Health = GetComponent<Health>();
        // Les objets (caisses) restent où le décor les pose, hors de la zone de combat en cours.
        ClampToPlayfield = team != Team.Neutral;

        if (shadow != null)
        {
            shadowColor = shadow.color;
            // Taille du sprite de l'ombre : shadowWidth est en mètres, quelle que soit l'image.
            shadowUnit = shadow.sprite != null ? Mathf.Max(0.01f, shadow.sprite.bounds.size.x) : 1f;
        }
    }

    private void OnEnable()
    {
        All.Add(this);

        if (shadow != null)
            shadow.enabled = true;
    }

    private void OnDisable()
    {
        All.Remove(this);

        if (shadow != null)
            shadow.enabled = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => All.Clear();

    public void SetFacing(float direction)
    {
        if (Mathf.Abs(direction) < 0.01f)
            return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
        transform.localScale = scale;
    }

    // Place le corps (entrée en scène, mise en réserve) : au sol en ground, à la hauteur donnée, immobile.
    public void Place(Vector2 ground, float height = 0f)
    {
        rb.position = ground;
        transform.position = new Vector3(ground.x, ground.y, transform.position.z);
        rb.linearVelocity = Vector2.zero;
        Height = Mathf.Max(0f, height);
        HeightVelocity = 0f;
        UpdatePresentation();
    }

    private void FixedUpdate()
    {
        if (hitStop.IsFrozen)
            return;

        if (Height > 0f || HeightVelocity > 0f)
        {
            HeightVelocity -= gravity * Time.fixedDeltaTime;
            Height += HeightVelocity * Time.fixedDeltaTime;

            if (Height <= 0f)
            {
                float impact = -HeightVelocity;
                Height = 0f;
                HeightVelocity = 0f;
                Landed?.Invoke(impact);
            }
        }

        if (!ClampToPlayfield || Playfield25D.Instance == null)
            return;

        // La vitesse de l'étape suivante est déjà prise en compte : le corps s'arrête au bord, sans le traverser.
        Vector2 next = rb.position + rb.linearVelocity * Time.fixedDeltaTime;
        Vector2 clamped = Playfield25D.Instance.Clamp(next, out int wall);

        if (clamped == next)
            return;

        Vector2 velocity = rb.linearVelocity;

        if (wall != 0)
        {
            HitWall?.Invoke(wall, Mathf.Abs(velocity.x));
            velocity = rb.linearVelocity;

            if (Mathf.Sign(velocity.x) == wall)
                velocity.x = 0f;
        }

        if (!Mathf.Approximately(clamped.y, next.y))
            velocity.y = 0f;

        rb.linearVelocity = velocity;
        rb.position = Playfield25D.Instance.Clamp(rb.position, out _);
    }

    private void LateUpdate() => UpdatePresentation();

    private void UpdatePresentation()
    {
        if (visual != null)
            visual.localPosition = new Vector3(0f, Height, 0f);

        if (sortingGroup != null)
            sortingGroup.sortingOrder = Playfield25D.SortingOrder(transform.position.y);

        if (shadow == null)
            return;

        // L'ombre rétrécit et pâlit avec la hauteur.
        float t = Mathf.Clamp01(Height / 4f);
        Transform s = shadow.transform;
        s.position = new Vector3(transform.position.x, transform.position.y, 0f);
        float width = shadowWidth * (1f - 0.45f * t) / shadowUnit;
        s.localScale = new Vector3(width, width * 0.32f, 1f);
        Color c = shadowColor;
        c.a *= 1f - 0.5f * t;
        shadow.color = c;
    }
}
