using System.Collections;
using UnityEngine;

// Démo de combo (F3) : joue un combo complet sur l'ennemi le plus proche en injectant les
// commandes dans GameInput, comme un joueur. Sert à vérifier les annulations, les rebonds
// et la tenue des 60 FPS, et à faire des captures.
// Jab → direct → lanceur (bas + K) → saut → J ×3 en l'air → smash → relance au sol → crochet → coup lourd.
public class ComboDemo : MonoBehaviour
{
    [SerializeField] private PlayerCombat player;
    [SerializeField] private int maxWaitFrames = 90;

    private Coroutine routine;

    public bool IsPlaying => routine != null;

    public void Play()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        Enemy target = Nearest();

        if (target == null || player == null)
        {
            routine = null;
            yield break;
        }

        // Se placer au contact.
        float timeout = Time.time + 3f;

        while (target != null && Mathf.Abs(target.Position.x - player.transform.position.x) > 1.1f && Time.time < timeout)
        {
            GameInput.HorizontalOverride = Mathf.Sign(target.Position.x - player.transform.position.x);
            yield return null;
        }

        GameInput.HorizontalOverride = null;
        yield return Frames(4);

        yield return Do(GameAction.Light);               // jab
        yield return Do(GameAction.Light);               // direct
        GameInput.VerticalOverride = -1f;
        yield return Do(GameAction.Heavy);               // lanceur
        GameInput.VerticalOverride = null;
        yield return Press(GameAction.Jump);             // saut d'annulation
        yield return Frames(12);
        yield return Do(GameAction.Light);               // J ×3 en l'air
        yield return Do(GameAction.Light);
        yield return Do(GameAction.Light);
        yield return Do(GameAction.Heavy);               // smash : rebond au sol
        yield return WaitUntilIdleAndGrounded();
        yield return Press(GameAction.Dash);             // dash vers l'ennemi qui rebondit
        yield return Frames(4);
        yield return Do(GameAction.Light);               // dash annulé en attaque
        yield return Do(GameAction.Light);
        yield return Do(GameAction.Light);               // crochet
        yield return Do(GameAction.Heavy);               // coup lourd : rebond au mur

        routine = null;
    }

    private static IEnumerator Press(GameAction action)
    {
        GameInput.Inject(action);
        yield return null;
    }

    private static IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++)
            yield return new WaitForFixedUpdate();
    }

    // Appuie, attend que la nouvelle attaque démarre, puis qu'elle touche et ouvre sa fenêtre d'annulation.
    private IEnumerator Do(GameAction action)
    {
        AttackData previous = player.CurrentAttack;
        GameInput.Inject(action);

        for (int i = 0; i < maxWaitFrames; i++)
        {
            yield return new WaitForFixedUpdate();

            if (player.CurrentAttack != null && player.CurrentAttack != previous)
                break;
        }

        for (int i = 0; i < maxWaitFrames; i++)
        {
            yield return new WaitForFixedUpdate();

            if (!player.IsAttacking || (player.InCancelWindow && player.HitConnected))
                yield break;
        }
    }

    private IEnumerator WaitUntilIdleAndGrounded()
    {
        var controller = player.GetComponent<PlayerController>();

        for (int i = 0; i < maxWaitFrames; i++)
        {
            yield return new WaitForFixedUpdate();

            if (!player.IsAttacking && controller.IsGrounded)
                yield break;
        }
    }

    private Enemy Nearest()
    {
        Enemy best = null;
        float bestDistance = float.MaxValue;

        foreach (Enemy enemy in Enemy.Living)
        {
            float distance = Mathf.Abs(enemy.Position.x - player.transform.position.x);

            if (distance < bestDistance)
            {
                best = enemy;
                bestDistance = distance;
            }
        }

        return best;
    }

    private void OnDisable()
    {
        GameInput.HorizontalOverride = null;
        GameInput.VerticalOverride = null;
    }
}
