using UnityEngine;

// Morceaux d'un personnage pulvérisé (tools/blender/redmet_props.py, split_gibs) : tête, torse,
// deux bras, deux jambes, rendus de face avec les plaies, à l'échelle des sprites.
[CreateAssetMenu(menuName = "RED MET/Morceaux de corps", fileName = "Gibs")]
public class GibSet : ScriptableObject
{
    public Sprite head;
    public Sprite torso;
    public Sprite armLeft;
    public Sprite armRight;
    public Sprite legLeft;
    public Sprite legRight;

    // Hauteur de chaque morceau au départ, en part de la hauteur du corps (depuis les pieds).
    public static readonly float[] Heights = { 0.9f, 0.62f, 0.7f, 0.7f, 0.25f, 0.25f };

    public Sprite this[int index] => index switch
    {
        0 => head,
        1 => torso,
        2 => armLeft,
        3 => armRight,
        4 => legLeft,
        _ => legRight,
    };

    public const int Count = 6;
}
