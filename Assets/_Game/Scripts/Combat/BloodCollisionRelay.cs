using UnityEngine;

// Relais : Unity envoie OnParticleCollision à l'objet du système de particules du sang.
public class BloodCollisionRelay : MonoBehaviour
{
    public CombatVfx Owner { get; set; }

    private void OnParticleCollision(GameObject other)
    {
        if (Owner != null)
            Owner.OnBloodCollision(other);
    }
}
