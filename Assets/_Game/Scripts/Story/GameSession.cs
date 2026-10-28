using System;
using System.IO;
using UnityEngine;

// Partie en cours : l'état (GameState) et les tables qui le traduisent en puissance de Nathan,
// tenue de Mara et force des ennemis. Tout ce qui dépend des choix du joueur lit ici, jamais
// une valeur en dur. Sauvegarde en JSON dans Application.persistentDataPath/saves.
public static class GameSession
{
    private const string SaveFolder = "saves";

    public static GameState State { get; private set; } = new();
    public static ProgressionTable Progression { get; private set; }
    public static IncursionTable Incursions { get; private set; }

    // Tout changement de l'état (dose, acte, incursion, chargement).
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        State = new GameState();
        Progression = null;
        Incursions = null;
        Changed = null;
    }

    public static void Configure(ProgressionTable progression, IncursionTable incursions)
    {
        Progression = progression;
        Incursions = incursions;
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

    // Fin avec Mara : une dose à chacun des niveaux du monde parallèle.
    public static bool TookEveryDose => Progression != null && State.TookEveryDose(Progression.levelCount);

    public static void NewGame()
    {
        State = new GameState();
        Changed?.Invoke();
    }

    // Dose du prochain niveau. Un choix est définitif : faux s'il a déjà été fait.
    public static bool ChooseDose(bool take)
    {
        int level = NextLevel;

        if (State.DoseAt(level).HasValue)
            return false;

        State.doses.Add(new LevelChoice { level = level, dose = take ? DoseChoice.Taken : DoseChoice.Refused });

        if (take && Progression != null)
            State.axes.dependence += Progression.dependencePerDose;

        Changed?.Invoke();
        return true;
    }

    // Entrée dans le niveau suivant du monde parallèle.
    public static void BeginIncursion()
    {
        State.incursion++;
        Changed?.Invoke();
    }

    public static void RecordAct(string id, MoralAxes delta)
    {
        State.acts.Add(new StoryAct { id = id, level = State.incursion, delta = delta });
        State.axes += delta;
        Changed?.Invoke();
    }

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
