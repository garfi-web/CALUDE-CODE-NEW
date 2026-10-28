using Ink.Runtime;

// Copie l'état de la partie dans les variables d'une histoire Ink, juste avant une scène.
// Ink ne calcule aucun seuil : stades, tenue de Mara et faits viennent de GameSession
// (docs/source/dialogues/mara_v4/ANALYSE.md, section 2). Une variable absente du fichier est ignorée.
public static class InkStoryState
{
    public static void Sync(Story story, int chapter)
    {
        GameState state = GameSession.State;

        Set(story, "redmet_doses", GameSession.DosesTaken);
        Set(story, "redmet_taken_this_level", state.DoseAt(chapter) == DoseChoice.Taken);
        Set(story, "nathan_stage", (int)GameSession.NathanStage);
        Set(story, "mara_stage", (int)GameSession.MaraOutfit);
        Set(story, "corruption_score", GameSession.CorruptionScore);
        Set(story, "father_alive", GameSession.MarcAlive);
        Set(story, "child_mission_choice", state.HasAct("acte_ch07_enfant_livre") ? 1 : 0);
        Set(story, "mara_role", (int)GameSession.MaraRole);
    }

    private static void Set(Story story, string name, object value)
    {
        if (story.variablesState.GlobalVariableExistsWithName(name))
            story.variablesState[name] = value;
    }
}
