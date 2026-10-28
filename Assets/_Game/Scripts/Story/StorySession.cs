using UnityEngine;

// Branche la partie (GameSession) sur ses tables de données, avant le Start des autres composants.
// Touches de test (arène) : F5 sauvegarder, F9 charger, F6 prendre la dose et entrer dans l'incursion
// suivante (son prix est ignoré, et le message dit ce qui manquait), F7 la refuser et entrer,
// F8 nouvelle partie, F10 et F11 les scènes de Mara (ouverture, fin) du chapitre en cours.
[DefaultExecutionOrder(-100)]
public class StorySession : MonoBehaviour
{
    [SerializeField] private ProgressionTable progression;
    [SerializeField] private IncursionTable incursions;
    [SerializeField] private StoryCatalog catalog;
    [SerializeField] private CombatHUD hud;
    [SerializeField] private VisualNovelPlayer visualNovel;

    private void Awake() => GameSession.Configure(progression, incursions, catalog);

    // Scène de Mara du chapitre en cours ; au ch. 10, la fin dépend de la voie (section 11.1 ou 11.2).
    public static string MaraScene(bool opening)
    {
        int chapter = Mathf.Max(1, GameSession.Incursion);

        if (opening)
            return $"ch{chapter:00}_mara_ouverture";

        if (chapter == 10)
            return GameSession.ImmoralEnding ? "ch10_mara_fin_immorale" : "ch10_mara_fin_morale";

        return $"ch{chapter:00}_mara_fin";
    }

    private void Update()
    {
        if (visualNovel != null && visualNovel.IsPlaying)
            return;

        if (visualNovel != null && GameInput.DebugMaraOpeningPressed)
            visualNovel.Play(MaraScene(true));
        else if (visualNovel != null && GameInput.DebugMaraEndingPressed)
            visualNovel.Play(MaraScene(false));
        else if (GameInput.DebugSavePressed)
            Report(GameSession.Save() ? "PARTIE SAUVEGARDÉE" : "SAUVEGARDE IMPOSSIBLE");
        else if (GameInput.DebugLoadPressed)
            Report(GameSession.Load() ? "PARTIE CHARGÉE" : "AUCUNE SAUVEGARDE");
        else if (GameInput.DebugTakeDosePressed)
            EnterNextIncursion(true);
        else if (GameInput.DebugRefuseDosePressed)
            EnterNextIncursion(false);
        else if (GameInput.DebugNewGamePressed)
        {
            GameSession.NewGame();
            Report("NOUVELLE PARTIE");
        }
    }

    private void EnterNextIncursion(bool takeDose)
    {
        DoseCheck price = takeDose ? GameSession.CanTakeDose() : DoseCheck.Ok;
        GameSession.ChooseDose(takeDose, ignorePrice: true);
        GameSession.BeginIncursion();

        string note = price is DoseCheck.Ok or DoseCheck.AlreadyChosen ? "" : $" (test : {price} ignoré)";
        Report((takeDose ? "DOSE PRISE" : "DOSE REFUSÉE") + note + $" — INCURSION {GameSession.Incursion}");
    }

    private void Report(string title)
    {
        if (hud != null)
            hud.ShowMessage($"{title}\n{Describe()}");
    }

    // Résumé lisible de l'état (message du HUD, debug F1).
    public static string Describe()
    {
        GameState state = GameSession.State;
        return $"Incursion {state.incursion} · doses {state.DosesTaken} · Nathan {GameSession.NathanStage}/{GameSession.RealStage} · " +
               $"Mara M-{GameSession.MaraOutfit} · {state.axes} · actes {state.acts.Count} · " +
               $"argent {state.money}" + (string.IsNullOrEmpty(state.scene) ? "" : $" · scène {state.scene}");
    }
}
