using System;
using UnityEngine;

// Arrêt sur image d'un combattant (hitstop) : il se fige quelques images sur sa propre horloge,
// sans toucher à Time.timeScale. Les scripts de combat ne font pas avancer leurs compteurs
// d'images tant que IsFrozen est vrai, et CharacterAnimator met l'Animator à l'arrêt.
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody2D))]
public class HitStop : MonoBehaviour
{
    private Rigidbody2D rb;
    private int framesLeft;
    private Vector2 resumeVelocity;
    private float savedGravity;

    public bool IsFrozen => framesLeft > 0;

    public event Action<bool> FrozenChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnDisable()
    {
        if (IsFrozen)
        {
            framesLeft = 0;
            Release();
        }
    }

    // resumeVelocity : vitesse rendue à la fin de l'arrêt (le recul part après le figement).
    public void Freeze(int frames, Vector2? velocityAfter = null)
    {
        if (frames <= 0)
        {
            if (velocityAfter.HasValue && !IsFrozen)
                rb.linearVelocity = velocityAfter.Value;
            else if (velocityAfter.HasValue)
                resumeVelocity = velocityAfter.Value;

            return;
        }

        if (!IsFrozen)
        {
            resumeVelocity = rb.linearVelocity;
            savedGravity = rb.gravityScale;
            rb.gravityScale = 0f;
            FrozenChanged?.Invoke(true);
        }

        if (velocityAfter.HasValue)
            resumeVelocity = velocityAfter.Value;

        framesLeft = Mathf.Max(framesLeft, frames);
        rb.linearVelocity = Vector2.zero;
    }

    // Exécuté avant les autres FixedUpdate : la dernière image figée est consommée ici.
    private void FixedUpdate()
    {
        if (!IsFrozen)
            return;

        rb.linearVelocity = Vector2.zero;
        framesLeft--;

        if (framesLeft == 0)
            Release();
    }

    private void Release()
    {
        rb.gravityScale = savedGravity;
        rb.linearVelocity = resumeVelocity;
        FrozenChanged?.Invoke(false);
    }
}
