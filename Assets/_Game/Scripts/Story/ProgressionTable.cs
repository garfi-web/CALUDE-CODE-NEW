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

public enum DosePriceKind
{
    Offered,
    Credit,
    Money,
    Demand,
}

// Prix de la dose d'un niveau (docs/SCENARIO.md, colonne « Prix de la dose »).
[Serializable]
public class DosePrice
{
    public int level;
    public DosePriceKind kind;
    [Tooltip("Crédit : dette ajoutée. Argent : somme à payer, en plus de la dette en cours.")]
    public int cost;
    [Tooltip("Demande : l'acte qui paie la dose (la grande demande acceptée).")]
    public string demandAct;
    [Tooltip("Acte qui rend la dose impossible (le rituel du ch. 5 pour la dose du ch. 6).")]
    public string blockedByAct;
}

// Ce que les doses prises changent (docs/GDD.md, « Difficulté » ; seuils dans docs/CHARACTERS.md).
// Une seule table pour Nathan (monde parallèle et réel) et pour la tenue de Mara.
[CreateAssetMenu(menuName = "RED MET/Table de progression", fileName = "Progression")]
public class ProgressionTable : ScriptableObject
{
    [Tooltip("Niveaux du monde parallèle, une dose possible par niveau.")]
    public int levelCount = 10;
    [Tooltip("Dépendance ajoutée par dose prise, quand le catalogue n'a pas d'acte pour cette dose.")]
    public int dependencePerDose = 1;

    [Tooltip("Prix de la dose de chaque niveau.")]
    public DosePrice[] dosePrices =
    {
        new() { level = 1, kind = DosePriceKind.Offered },
        new() { level = 2, kind = DosePriceKind.Credit, cost = 1 },
        new() { level = 3, kind = DosePriceKind.Money, cost = 2 },
        new() { level = 4, kind = DosePriceKind.Demand, demandAct = "acte_ch04_humiliation_acceptee" },
        new() { level = 5, kind = DosePriceKind.Demand, demandAct = "acte_ch05_coeur_marc" },
        new() { level = 6, kind = DosePriceKind.Money, cost = 2, blockedByAct = "acte_ch05_rituel" },
        new() { level = 7, kind = DosePriceKind.Demand, demandAct = "acte_ch07_enfant_livre" },
        new() { level = 8, kind = DosePriceKind.Money, cost = 2 },
        new() { level = 9, kind = DosePriceKind.Money, cost = 2 },
        new() { level = 10, kind = DosePriceKind.Offered },
    };

    [Tooltip("Stades M0 à M4, dans l'ordre.")]
    public NathanStageData[] nathan = Array.Empty<NathanStageData>();

    [Tooltip("Doses minimales pour les tenues M-T0 à M-T4.")]
    public int[] maraOutfitMinDoses = { 0, 1, 3, 5, 7 };

    [Tooltip("Doses minimales pour les stades réels R0 à R4.")]
    public int[] realStageMinDoses = { 0, 1, 3, 5, 7 };

    [Header("Fin de la voie immorale")]
    [Tooltip("Doses prises au minimum.")]
    public int immoralEndingMinDoses = 6;
    [Tooltip("Grandes demandes du Chimiste acceptées au minimum.")]
    public int immoralEndingMinDemands = 2;
    [Tooltip("Actes qui marquent l'acceptation des grandes demandes (identifiants de docs/SCENARIO.md).")]
    public string[] chimisteDemandActs =
    {
        "acte_ch04_humiliation_acceptee",
        "acte_ch05_coeur_marc",
        "acte_ch07_enfant_livre",
    };

    public bool IsImmoralEnding(GameState state) =>
        state.DosesTaken >= immoralEndingMinDoses &&
        state.CountActs(chimisteDemandActs) >= immoralEndingMinDemands;

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

    // Sans prix renseigné, la dose est offerte.
    public DosePrice PriceFor(int level) =>
        Array.Find(dosePrices, price => price.level == level) ?? new DosePrice { level = level, kind = DosePriceKind.Offered };

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
