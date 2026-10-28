using UnityEngine;

// Annonce visuelle des attaques : couleur du corps pendant l'armement, ligne de visée,
// état (sonné, rechargement), renfort d'un ordre, flash blanc quand il est touché.
// Priorité : flash > mort > armement > état > renfort.
// En attendant les animations, c'est la seule lecture des attaques : rien ne frappe sans elle.
public class EnemyTelegraph : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private Color buffColor = new(1f, 0.45f, 0.1f);
    [SerializeField] private Color deadColor = new(0.25f, 0.2f, 0.2f);
    [Tooltip("Cône de danger du fusil à pompe (maillage plat, couleurs par sommet).")]
    [SerializeField] private MeshFilter cone;
    [Tooltip("Lueur rouge au canon pendant l'armement du fusil.")]
    [SerializeField] private SpriteRenderer muzzleGlow;

    private const int ConeSegments = 12;
    private Mesh coneMesh;
    private Vector3[] coneVertices;
    private Color[] coneColors;

    private Color baseColor = Color.white;
    private Color? windupColor;
    private Color? stateColor;
    private bool buffed;
    private bool dead;
    private int flashLeft;

    private void Awake()
    {
        if (body == null)
            body = GetComponentInChildren<SpriteRenderer>();

        if (cone != null)
            BuildConeMesh();

        HideAim();
    }

    // Deux éventails : bout portant (mort instantanée, plus opaque) puis jusqu'à la portée maximale.
    private void BuildConeMesh()
    {
        int rays = ConeSegments + 1;
        coneVertices = new Vector3[1 + rays * 2];
        coneColors = new Color[coneVertices.Length];
        var triangles = new int[ConeSegments * 3 + ConeSegments * 6];
        int t = 0;

        for (int i = 0; i < ConeSegments; i++)
        {
            int inner = 1 + i, innerNext = 2 + i;
            int outer = 1 + rays + i, outerNext = 2 + rays + i;
            triangles[t++] = 0; triangles[t++] = innerNext; triangles[t++] = inner;
            triangles[t++] = inner; triangles[t++] = innerNext; triangles[t++] = outerNext;
            triangles[t++] = inner; triangles[t++] = outerNext; triangles[t++] = outer;
        }

        coneMesh = new Mesh { name = "DangerCone" };
        coneMesh.MarkDynamic();
        coneMesh.vertices = coneVertices;
        coneMesh.colors = coneColors;
        coneMesh.triangles = triangles;
        cone.sharedMesh = coneMesh;
    }

    private void OnDestroy()
    {
        if (coneMesh != null)
            Destroy(coneMesh);
    }

    // Cône de danger : du canon dans la direction visée, bout portant marqué plus fort ; locked = visée figée
    // (le tir approche, le cône s'intensifie). Lueur rouge pulsée au canon.
    public void ShowCone(Vector2 muzzle, Vector2 aim, float halfAngle, float closeRange, float maxRange, Color color, bool locked)
    {
        if (cone == null)
            return;

        aim = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
        float baseAngle = Mathf.Atan2(aim.y, aim.x);
        Transform t = cone.transform;
        t.position = new Vector3(muzzle.x, muzzle.y, 0f);
        t.rotation = Quaternion.identity;
        // Le cône est calculé dans le monde : on annule le retournement de l'ennemi (échelle x négative).
        t.localScale = new Vector3(Mathf.Sign(transform.lossyScale.x), 1f, 1f);
        int rays = ConeSegments + 1;
        // Sur le décor rouge sombre, le cône doit rester lisible : rouge vif, bout portant presque opaque.
        float strength = locked ? 1f : 0.65f;
        var inner = new Color(1f, 0.25f, 0.15f, 0.75f * strength);
        var mid = new Color(1f, 0.2f, 0.1f, 0.5f * strength);
        var outer = new Color(1f, 0.15f, 0.1f, 0.05f);
        coneVertices[0] = Vector3.zero;
        coneColors[0] = inner;

        for (int i = 0; i < rays; i++)
        {
            float a = baseAngle + Mathf.Lerp(-halfAngle, halfAngle, i / (float)ConeSegments) * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            coneVertices[1 + i] = dir * closeRange;
            coneColors[1 + i] = mid;
            coneVertices[1 + rays + i] = dir * maxRange;
            coneColors[1 + rays + i] = outer;
        }

        coneMesh.vertices = coneVertices;
        coneMesh.colors = coneColors;
        coneMesh.RecalculateBounds();
        cone.gameObject.SetActive(true);

        if (muzzleGlow != null)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * (locked ? 40f : 18f));
            muzzleGlow.transform.position = new Vector3(muzzle.x, muzzle.y, 0f);
            muzzleGlow.color = new Color(1f, 0.12f, 0.08f, pulse);
            muzzleGlow.enabled = true;
        }
    }

    // Le flash compte les images même pendant l'arrêt sur image : c'est là qu'il se voit.
    private void FixedUpdate()
    {
        if (flashLeft > 0 && --flashLeft == 0)
            Refresh();
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

    public void Flash(int frames)
    {
        flashLeft = Mathf.Max(flashLeft, frames);
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

    // Ligne de visée, cône de danger et lueur du canon.
    public void HideAim()
    {
        if (aimLine != null)
            aimLine.enabled = false;

        if (cone != null)
            cone.gameObject.SetActive(false);

        if (muzzleGlow != null)
            muzzleGlow.enabled = false;
    }

    private void Refresh()
    {
        if (body == null)
            return;

        // Avec le shader de flash, les annonces sont un voile de couleur sur le sprite :
        // une teinte multiplicative assombrirait un sprite déjà coloré.
        if (SpriteFlash.Supports(body))
        {
            body.color = dead ? Color.Lerp(baseColor, deadColor, 0.6f) : baseColor;

            if (flashLeft > 0)
                SpriteFlash.Set(body, 1f, Color.white);
            else if (dead)
                SpriteFlash.Set(body, 0f, Color.white);
            else if (windupColor.HasValue)
                SpriteFlash.Set(body, 0.55f, windupColor.Value);
            else if (stateColor.HasValue)
                SpriteFlash.Set(body, 0.4f, stateColor.Value);
            else
                SpriteFlash.Set(body, buffed ? 0.3f : 0f, buffColor);

            return;
        }

        if (flashLeft > 0)
            body.color = Color.white;
        else if (dead)
            body.color = deadColor;
        else if (windupColor.HasValue)
            body.color = windupColor.Value;
        else if (stateColor.HasValue)
            body.color = stateColor.Value;
        else
            body.color = buffed ? Color.Lerp(baseColor, buffColor, 0.45f) : baseColor;
    }
}
