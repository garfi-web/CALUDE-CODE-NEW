using System;
using UnityEngine;

// Puissance de Nathan à un stade du monde de la Red Met.
[Serializable]
public class NathanStageData
{
    public NathanStage stage;
    [Tooltip("Doses prises à partir desquelles ce stade s'applique.")]
    public int minDoses;
    public int maxHealth = 100;
    [Tooltip("Multiplicateur des dégâts de tous ses coups (DamageScale).")]
    public float damageMultiplier = 1f;
    [Tooltip("Fusil à pompe disponible.")]
    public bool shotgun;
    [Tooltip("Sprites du stade. Vide : ceux du stade inférieur le plus proche qui en a.")]
    public RuntimeAnimatorController animator;
    public GibSet gibs;
    public float height = 1.7f;
}

// Ce que les doses prises changent (docs/GDD.md, « Difficulté » ; seuils dans docs/CHARACTERS.md).
// Une seule table pour Nathan (monde parallèle et réel) et pour la tenue de Mara.
[CreateAssetMenu(menuName = "RED MET/Table de progression", fileName = "Progression")]
public class ProgressionTable : ScriptableObject
{
    [Tooltip("Niveaux du monde parallèle : la fin avec Mara demande une dose à chacun.")]
    public int levelCount = 10;
    [Tooltip("Dépendance ajoutée par dose prise.")]
    public int dependencePerDose = 1;

    [Tooltip("Stades M0 à M4, dans l'ordre.")]
    public NathanStageData[] nathan = Array.Empty<NathanStageData>();

    [Tooltip("Doses minimales pour les tenues M-T0 à M-T4.")]
    public int[] maraOutfitMinDoses = { 0, 1, 2, 4, 5 };

    [Tooltip("Doses minimales pour les stades réels R0 à R4.")]
    public int[] realStageMinDoses = { 0, 1, 2, 4, 5 };

    public NathanStageData Nathan(int doses)
    {
        NathanStageData result = nathan.Length > 0 ? nathan[0] : null;

        foreach (NathanStageData data in nathan)
        {
            if (doses >= data.minDoses)
                result = data;
        }

        return result;
    }

    public NathanStageData Get(NathanStage stage) => Array.Find(nathan, data => data.stage == stage);

    public RuntimeAnimatorController AnimatorFor(NathanStageData data)
    {
        for (int i = Array.IndexOf(nathan, data); i >= 0; i--)
        {
            if (nathan[i].animator != null)
                return nathan[i].animator;
        }

        return null;
    }

    public GibSet GibsFor(NathanStageData data)
    {
        for (int i = Array.IndexOf(nathan, data); i >= 0; i--)
        {
            if (nathan[i].gibs != null)
                return nathan[i].gibs;
        }

        return null;
    }

    public MaraOutfit Mara(int doses) => (MaraOutfit)Threshold(maraOutfitMinDoses, doses);

    public RealStage Real(int doses) => (RealStage)Threshold(realStageMinDoses, doses);

    private static int Threshold(int[] minDoses, int doses)
    {
        int index = 0;

        for (int i = 0; i < minDoses.Length; i++)
        {
            if (doses >= minDoses[i])
                index = i;
        }

        return index;
    }
}
