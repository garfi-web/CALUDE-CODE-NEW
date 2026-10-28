using System;
using System.Collections.Generic;

public enum DoseChoice
{
    Refused,
    Taken,
}

// Stades de Nathan dans le monde de la Red Met et dans le réel, tenue de Mara (docs/CHARACTERS.md).
public enum NathanStage { M0, M1, M2, M3, M4 }
public enum RealStage { R0, R1, R2, R3, R4 }
public enum MaraOutfit { T0, T1, T2, T3, T4 }

// Karma caché (docs/GDD.md, « Karma caché ») : jamais affiché au joueur.
[Serializable]
public struct MoralAxes
{
    public int humanity;
    public int cruelty;
    public int dependence;
    public int paranoia;

    public MoralAxes(int humanity, int cruelty, int dependence, int paranoia)
    {
        this.humanity = humanity;
        this.cruelty = cruelty;
        this.dependence = dependence;
        this.paranoia = paranoia;
    }

    public static MoralAxes operator +(MoralAxes a, MoralAxes b) =>
        new(a.humanity + b.humanity, a.cruelty + b.cruelty, a.dependence + b.dependence, a.paranoia + b.paranoia);

    public override string ToString() =>
        $"Humanité {humanity}, Cruauté {cruelty}, Dépendance {dependence}, Paranoïa {paranoia}";
}

// Dose prise ou refusée avant un niveau du monde parallèle.
[Serializable]
public class LevelChoice
{
    public int level;
    public DoseChoice dose;
}

// Un acte commis : le jeu se souvient des faits, pas seulement d'un score.
[Serializable]
public class StoryAct
{
    public string id;
    public int level;
    public MoralAxes delta;
}

// État de la partie, enregistré tel quel en JSON (GameSession). Les stades de Nathan et de Mara
// ne sont pas stockés : ils se déduisent des doses par la ProgressionTable, pour qu'un changement
// de seuils s'applique aussi aux sauvegardes existantes.
[Serializable]
public class GameState
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    // Numéro de l'incursion en cours ou dernière jouée (0 : pas encore entré dans le monde parallèle).
    // Chaque niveau du monde parallèle est une incursion.
    public int incursion;
    public List<LevelChoice> doses = new();
    public List<StoryAct> acts = new();
    public MoralAxes axes;

    public int DosesTaken
    {
        get
        {
            int count = 0;

            foreach (LevelChoice choice in doses)
            {
                if (choice.dose == DoseChoice.Taken)
                    count++;
            }

            return count;
        }
    }

    public DoseChoice? DoseAt(int level)
    {
        foreach (LevelChoice choice in doses)
        {
            if (choice.level == level)
                return choice.dose;
        }

        return null;
    }

    // Dose prise à chacun des niveaux 1 à levelCount (condition de la fin avec Mara).
    public bool TookEveryDose(int levelCount)
    {
        for (int level = 1; level <= levelCount; level++)
        {
            if (DoseAt(level) != DoseChoice.Taken)
                return false;
        }

        return levelCount > 0;
    }

    public bool HasAct(string id) => acts.Exists(act => act.id == id);

    // Listes absentes d'un ancien fichier : JsonUtility les laisse nulles.
    public void Repair()
    {
        doses ??= new List<LevelChoice>();
        acts ??= new List<StoryAct>();
    }
}
