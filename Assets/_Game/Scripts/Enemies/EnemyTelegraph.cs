using UnityEngine;

// Annonce visuelle des attaques : couleur du corps pendant l'armement, ligne de visée,
// état (sonné, rechargement), renfort d'un ordre. Priorité : mort > armement > état > renfort.
// En attendant les animations, c'est la seule lecture des attaques : rien ne frappe sans elle.
public class EnemyTelegraph : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private Color buffColor = new(1f, 0.45f, 0.1f);
    [SerializeField] private Color deadColor = new(0.25f, 0.2f, 0.2f);

    private Color baseColor = Color.white;
    private Color? windupColor;
    private Color? stateColor;
    private bool buffed;
    private bool dead;

    private void Awake()
    {
        if (body == null)
            body = GetComponentInChildren<SpriteRenderer>();

        HideAim();
    }

    public void SetBaseColor(Color color)
    {
        baseColor = color;
        Refresh();
    }

    public void ShowWindup(Color color)
    {
        windupColor = color;
        Refresh();
    }

    public void HideWindup()
    {
        windupColor = null;
        Refresh();
    }

    public void SetState(Color? color)
    {
        stateColor = color;
        Refresh();
    }

    public void SetBuffed(bool value)
    {
        buffed = value;
        Refresh();
    }

    public void SetDead()
    {
        dead = true;
        HideAim();
        Refresh();
    }

    public void ShowAim(Vector2 from, Vector2 to, Color color, float width)
    {
        if (aimLine == null)
            return;

        aimLine.enabled = true;
        aimLine.positionCount = 2;
        aimLine.SetPosition(0, from);
        aimLine.SetPosition(1, to);
        aimLine.startColor = aimLine.endColor = color;
        aimLine.startWidth = aimLine.endWidth = width;
    }

    public void HideAim()
    {
        if (aimLine != null)
            aimLine.enabled = false;
    }

    private void Refresh()
    {
        if (body == null)
            return;

        if (dead)
            body.color = deadColor;
        else if (windupColor.HasValue)
            body.color = windupColor.Value;
        else if (stateColor.HasValue)
            body.color = stateColor.Value;
        else
            body.color = buffed ? Color.Lerp(baseColor, buffColor, 0.45f) : baseColor;
    }
}
