using System;
using UnityEngine;

// Relais placé sur l'objet enfant qui porte le trigger d'attaque.
// Il transmet les contacts au script de combat du parent, ce qui évite de
// dépendre de la propagation des messages de collision entre enfant et parent.
[RequireComponent(typeof(Collider2D))]
public class Hitbox : MonoBehaviour
{
    [SerializeField] private SpriteRenderer debugVisual;

    private Collider2D hitCollider;

    public event Action<Collider2D> Hit;

    public bool IsActive => Collider.enabled;

    private Collider2D Collider
    {
        get
        {
            if (hitCollider == null)
            {
                hitCollider = GetComponent<Collider2D>();
                hitCollider.isTrigger = true;
            }

            return hitCollider;
        }
    }

    public void SetActive(bool active)
    {
        Collider.enabled = active;

        if (debugVisual != null)
            debugVisual.enabled = active;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Hit?.Invoke(other);
    }

    private void OnDrawGizmos()
    {
        if (!(GetComponent<Collider2D>() is BoxCollider2D box))
            return;

        Gizmos.color = box.enabled ? Color.red : new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.offset, box.size);
    }
}
