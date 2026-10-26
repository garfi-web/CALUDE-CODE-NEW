using System.Collections.Generic;
using UnityEngine;

// Abri : bloc solide qu'on peut enjamber d'un saut.
// Libre, il arrête tous les tirs (Nathan s'en sert pour progresser).
// Occupé par un ennemi, il laisse passer les tirs, et l'occupant ne reçoit que 25 % des
// dégâts à distance venus d'en face (Enemy.Filter). De flanc ou au corps à corps : 100 %.
[RequireComponent(typeof(Collider2D))]
public class Cover : MonoBehaviour
{
    public static readonly List<Cover> All = new();

    public Enemy Occupant { get; private set; }
    public float X => transform.position.x;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => All.Clear();

    private void OnEnable() => All.Add(this);

    private void OnDisable()
    {
        All.Remove(this);

        if (Occupant != null)
            Occupant.LeaveCover();
    }

    public bool TryOccupy(Enemy enemy)
    {
        if (Occupant != null && Occupant != enemy)
            return false;

        Occupant = enemy;
        return true;
    }

    public void Release(Enemy enemy)
    {
        if (Occupant == enemy)
            Occupant = null;
    }

    // L'abri protège position contre un tir parti de origin s'ils sont de part et d'autre.
    public bool Shields(Vector2 position, Vector2 origin) =>
        Mathf.Sign(origin.x - X) != Mathf.Sign(position.x - X);

    public bool LetsShotsThrough => Occupant != null;
}
