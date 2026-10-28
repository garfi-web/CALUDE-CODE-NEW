using System;
using System.IO;
using UnityEngine;

// Peut-on prendre la dose du prochain niveau, et sinon pourquoi.
public enum DoseCheck
{
    Ok,
    AlreadyChosen,
    // Un acte l'interdit (le rituel du ch. 5 pour la dose du ch. 6).
    Blocked,
    // L'acte immoral qui paie la dose n'a pas été commis.
    ActNotDone,
}

// Rôle de Mara à partir du ch. 7 (docs/SCENARIO.md).
public enum MaraRole
{
    Undecided,
    Savior,
    Enemy,
    Ambitious,
}

// Partie en cours : l'état (GameState) et les tables qui le traduisent en puissance de Nathan,
// tenue de Mara et force des ennemis. Tout ce qui dépend des choix du joueur lit ici, jamais
// une valeur en dur. Scènes et actes : identifiants de docs/SCENARIO.md (StoryCatalog).
// Sauvegarde en JSON dans Application.persistentDataPath/saves.
public static class GameSession
{
    private const string SaveFolder = "saves";

    public static GameState State { get; private set; } = new();
    public static ProgressionTable Progression { get; private set; }
    public static IncursionTable Incursions { get; private set; }
    public static StoryCatalog Catalog { get; private set; }

    // Tout changement de l'état (dose, acte, incursion, chargement).
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        State = new GameState();
        Progression = null;
        Incursions = null;
        Catalog = null;
        Changed = null;
    }

    public static void Configure(ProgressionTable progression, IncursionTable incursions, StoryCatalog catalog)
    {
        Progression = progression;
        Incursions = incursions;
        Catalog = catalog;
        Changed?.Invoke();
    }

    public static int DosesTaken => State.DosesTaken;
    public static int Incursion => State.incursion;
    // Niveau dont la dose reste à choisir.
    public static int NextLevel => State.incursion + 1;

    public static NathanStageData NathanData => Progression != null ? Progression.Nathan(DosesTaken) : null;
    public static NathanStage NathanStage => NathanData?.stage ?? NathanStage.M0;
    public static RealStage RealStage => Progression != null ? Progression.Real(DosesTaken) : RealStage.R0;
    public static MaraOutfit MaraOutfit => Progression != null ? Progression.Mara(DosesTaken) : MaraOutfit.T0;

    public static IncursionScaling EnemyScaling =>
        Incursions != null ? Incursions.For(Mathf.Max(1, State.incursion)) : IncursionScaling.None;

    // Fin de la voie immorale (section 11.1 du GDD v0.1) : assez de doses et assez de grandes
    // demandes du Chimiste acceptées (docs/SCENARIO.md, « La fin »).
    public static bool ImmoralEnding => Progression != null && Progression.IsImmoralEnding(State);

    // Faits qui se déduisent des actes (aucun drapeau en double dans l'état).
    public static bool MarcAlive => !State.HasAct("acte_ch05_coeur_marc");

    // Score de corruption lu par les dialogues (module de Mara) : Cruauté − Humanité, au minimum 0
    // (décision de l'auteur du 2026-10-28).
    public static int CorruptionScore => Mathf.Max(0, State.axes.cruelty - State.axes.humanity);

    public static MaraRole MaraRole =>
        State.HasAct("acte_ch07_mara_salut") ? MaraRole.Savior :
        State.HasAct("acte_ch07_mara_ennemie") ? MaraRole.Enemy :
        State.HasAct("acte_ch07_mara_ambition") ? MaraRole.Ambitious :
        MaraRole.Undecided;

    public static void NewGame()
    {
        State = new GameState();
        Changed?.Invoke();
    }

    // ------------------------------------------------------------ Doses

    public static DosePrice NextDosePrice =>
        Progression != null ? Progression.PriceFor(NextLevel) : new DosePrice { level = NextLevel };

    public static DoseCheck CanTakeDose()
    {
        int level = NextLevel;

        if (State.DoseAt(level).HasValue)
            return DoseCheck.AlreadyChosen;

        DosePrice price = NextDosePrice;

        if (!string.IsNullOrEmpty(price.blockedByAct) && State.HasAct(price.blockedByAct))
            return DoseCheck.Blocked;

        if (price.kind == DosePriceKind.Act && Array.TrueForAll(price.payingActs, act => !State.HasAct(act)))
            return DoseCheck.ActNotDone;

        return DoseCheck.Ok;
    }

    // Dose du prochain niveau, définitive. La prendre suppose que l'acte qui la paie a été commis ;
    // ignorePrice ne sert qu'aux touches de test. Refuser est toujours possible.
    // Les actes acte_chNN_dose et acte_chNN_refus du catalogue sont inscrits s'ils existent.
    public static DoseCheck ChooseDose(bool take, bool ignorePrice = false)
    {
        int level = NextLevel;

        if (State.DoseAt(level).HasValue)
            return DoseCheck.AlreadyChosen;

        if (take)
        {
            DoseCheck check = CanTakeDose();

            if (check != DoseCheck.Ok && !ignorePrice)
                return check;
        }

        State.doses.Add(new LevelChoice { level = level, dose = take ? DoseChoice.Taken : DoseChoice.Refused });

        string actId = $"acte_ch{level:00}_{(take ? "dose" : "refus")}";

        if (Catalog != null && Catalog.FindAct(actId) != null)
            RecordAct(actId);
        else if (take && Progression != null)
            State.axes.dependence += Progression.dependencePerDose;

        Changed?.Invoke();
        return DoseCheck.Ok;
    }

    // Argent d'une activité du réel qui n'est pas un acte du catalogue. Il ne paie jamais une dose.
    public static void EarnMoney(int amount)
    {
        State.money += amount;
        Changed?.Invoke();
    }

    // Entrée dans le niveau suivant du monde parallèle.
    public static void BeginIncursion()
    {
        State.incursion++;
        Changed?.Invoke();
    }

    // Un acte du catalogue : ses effets (axes, argent) viennent de docs/SCENARIO.md. Faux si
    // l'identifiant est inconnu ou si l'acte est déjà inscrit (chaque acte n'a lieu qu'une fois).
    public static bool RecordAct(string id)
    {
        StoryActDefinition definition = Catalog != null ? Catalog.FindAct(id) : null;

        if (definition == null)
        {
            Debug.LogWarning($"[RED MET] Acte inconnu du catalogue : {id} (docs/SCENARIO.md, puis RED MET → Importer le scénario).");
            return false;
        }

        if (State.HasAct(id))
            return false;

        State.money += definition.money;
        RecordAct(id, definition.delta);
        return true;
    }

    // Acte hors catalogue (identifiant à compléter, comme acte_ch09_allie_<nom>) : effets donnés par l'appelant.
    public static void RecordAct(string id, MoralAxes delta)
    {
        State.acts.Add(new StoryAct { id = id, level = State.incursion, delta = delta });
        State.axes += delta;
        Changed?.Invoke();
    }

    // ------------------------------------------------------------ Scènes

    // Entrée dans une scène : point de reprise de la sauvegarde.
    public static void EnterScene(string id)
    {
        if (Catalog != null && !Catalog.HasScene(id))
            Debug.LogWarning($"[RED MET] Scène inconnue du catalogue : {id}");

        State.scene = id;

        if (!State.scenesSeen.Contains(id))
            State.scenesSeen.Add(id);

        Changed?.Invoke();
    }

    public static bool SceneSeen(string id) => State.scenesSeen.Contains(id);

    // ------------------------------------------------------------ Sauvegarde

    public static string SavePath(int slot) =>
        Path.Combine(Application.persistentDataPath, SaveFolder, $"partie_{slot}.json");

    public static bool HasSave(int slot = 0) => File.Exists(SavePath(slot));

    public static bool Save(int slot = 0)
    {
        string path = SavePath(slot);
        string temp = path + ".tmp";

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            State.version = GameState.CurrentVersion;
            File.WriteAllText(temp, JsonUtility.ToJson(State, true));

            // Écriture dans un fichier temporaire puis remplacement : une coupure ne laisse jamais
            // une sauvegarde à moitié écrite.
            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[RED MET] Sauvegarde impossible ({path}) : {exception.Message}");
            return false;
        }
    }

    public static bool Load(int slot = 0)
    {
        string path = SavePath(slot);

        if (!File.Exists(path))
            return false;

        try
        {
            var state = JsonUtility.FromJson<GameState>(File.ReadAllText(path));

            if (state == null || state.version > GameState.CurrentVersion)
            {
                Debug.LogError($"[RED MET] Sauvegarde illisible ou d'une version plus récente : {path}");
                return false;
            }

            state.Repair();
            State = state;
            Changed?.Invoke();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[RED MET] Chargement impossible ({path}) : {exception.Message}");
            return false;
        }
    }
}
