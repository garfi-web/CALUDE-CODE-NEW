using UnityEngine;

// Ce qu'il faut à CombatVfx pour pulvériser un personnage : ses morceaux, son sprite (caché quand
// il explose) et sa hauteur. Nathan : réglé par NathanForm ; ennemis : par Enemy, depuis l'archétype.
public class GibSource : MonoBehaviour
{
    [SerializeField] private GibSet set;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private bool gibbable = true;
    [SerializeField] private float height = 1.7f;

    public GibSet Set => set;
    public SpriteRenderer Body => body;
    public bool Gibbable => gibbable;
    public float Height => height;
    public bool Exploded { get; private set; }

    public void Configure(GibSet newSet, bool canBeGibbed, float bodyHeight)
    {
        set = newSet;
        gibbable = canBeGibbed;
        height = bodyHeight;
        Restore();
    }

    public void SetGibs(GibSet newSet) => set = newSet;

    // Le corps a volé en morceaux : son sprite disparaît (le gameplay continue de voir un mort).
    public void MarkExploded()
    {
        Exploded = true;

        if (body != null)
            body.enabled = false;
    }

    public void Restore()
    {
        Exploded = false;

        if (body != null)
            body.enabled = true;
    }
}
