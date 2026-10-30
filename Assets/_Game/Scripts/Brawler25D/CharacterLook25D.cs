using System;
using System.Collections.Generic;
using UnityEngine;

// Apparence d'un personnage dans l'arène 2,5D : sprites en vue de 3/4 (contrôleur d'animation), calque de visage anime
// (une image par expression) et piste du visage, image par image, relevée dans Blender (tools/blender/redmet_quality.py,
// face.json). Les valeurs de jeu restent dans l'archétype ; ceci n'est que le rendu.
[CreateAssetMenu(menuName = "RED MET/Apparence 2,5D", fileName = "Look25D")]
public class CharacterLook25D : ScriptableObject
{
    // Une image d'animation : où est le visage (fraction de l'image, depuis le bas à gauche), son inclinaison (degrés),
    // sa largeur apparente, s'il se voit et s'il regarde vers l'arrière de l'image.
    [Serializable]
    public struct FaceFrame
    {
        public Sprite sprite;
        public Vector2 position;
        public float angle;
        public float scale;
        public bool visible;
        public bool flipped;
    }

    public RuntimeAnimatorController animator;
    public Sprite idle;
    [Tooltip("Neutre, effort, douleur, rage (FaceLayer25D.Expression).")]
    public Sprite[] expressions = new Sprite[4];
    [Tooltip("Taille du dessin du visage sur la tête (Voyou : plus grand, sous la capuche).")]
    public float faceScale = 1f;
    public FaceFrame[] faceFrames = Array.Empty<FaceFrame>();

    [NonSerialized] private Dictionary<Sprite, int> index;

    public bool TryGetFace(Sprite sprite, out FaceFrame frame)
    {
        if (index == null)
        {
            index = new Dictionary<Sprite, int>(faceFrames.Length);

            for (int i = 0; i < faceFrames.Length; i++)
            {
                if (faceFrames[i].sprite != null)
                    index[faceFrames[i].sprite] = i;
            }
        }

        if (sprite != null && index.TryGetValue(sprite, out int found))
        {
            frame = faceFrames[found];
            return true;
        }

        frame = default;
        return false;
    }
}
