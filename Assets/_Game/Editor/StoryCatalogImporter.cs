using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Importe les scènes et les actes de docs/SCENARIO.md dans Data/Story/Scenario.asset.
// Scènes : première colonne des tableaux de scènes (`chNN_...`). Actes : colonne « Acte » des
// tableaux « Choix et conséquences », avec leur effet (un point par signe + ou −, l'argent, les
// grandes demandes, l'irréversible, le retour au ch. 9).
public static class StoryCatalogImporter
{
    private const string AssetPath = "Assets/_Game/Data/Story/Scenario.asset";

    private static readonly Regex SceneId = new(@"`(ch\d{2}_[a-z0-9_]+)`");
    private static readonly Regex ActId = new(@"`(acte_[^`]+)`");
    private static readonly Regex Axis = new(@"(Humanité|Cruauté|Dépendance|Paranoïa)\s*([+−-]+)");
    private static readonly Regex Money = new(@"(^|;\s*)(Moins d')?[Aa]rgent\b");

    private static string ScenarioPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "docs", "SCENARIO.md");

    [MenuItem("RED MET/Importer le scénario (docs/SCENARIO.md)")]
    private static void ImportMenu()
    {
        StoryCatalog catalog = Import();

        if (catalog != null)
            Debug.Log($"[RED MET] Scénario importé : {catalog.scenes.Length} scènes, {catalog.acts.Length} actes.");
    }

    public static StoryCatalog Import()
    {
        if (!File.Exists(ScenarioPath))
        {
            Debug.LogError($"[RED MET] Scénario introuvable : {ScenarioPath}");
            return null;
        }

        var scenes = new List<StoryScene>();
        var acts = new List<StoryActDefinition>();
        var seen = new HashSet<string>();

        foreach (string line in File.ReadAllLines(ScenarioPath))
        {
            if (!line.StartsWith("|"))
                continue;

            string[] cells = line.Split('|');

            if (cells.Length < 4)
                continue;

            if (cells[1].TrimStart().StartsWith("`ch"))
            {
                foreach (Match match in SceneId.Matches(cells[1]))
                {
                    if (seen.Add(match.Groups[1].Value))
                        scenes.Add(new StoryScene { id = match.Groups[1].Value, chapter = Chapter(match.Groups[1].Value) });
                }
            }
            else if (cells.Length >= 5 && cells[2].Contains("`acte_"))
            {
                ReadActs(cells[2], cells[3], acts, seen);
            }
        }

        var catalog = AssetDatabase.LoadAssetAtPath<StoryCatalog>(AssetPath);

        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<StoryCatalog>();
            AssetDatabase.CreateAsset(catalog, AssetPath);
        }

        catalog.scenes = scenes.ToArray();
        catalog.acts = acts.ToArray();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssetIfDirty(catalog);
        return catalog;
    }

    // « `a` / `b` » avec « effet a / effet b » : un effet par acte ; sinon le même effet pour tous.
    private static void ReadActs(string idCell, string effectCell, List<StoryActDefinition> acts, HashSet<string> seen)
    {
        var ids = new List<string>();

        foreach (Match match in ActId.Matches(idCell))
        {
            // Les identifiants à compléter (`acte_ch09_allie_<nom>`) se créent avec le dialogue.
            if (!match.Groups[1].Value.Contains("<"))
                ids.Add(match.Groups[1].Value);
        }

        string[] effects = effectCell.Split(new[] { " / " }, System.StringSplitOptions.None);

        for (int i = 0; i < ids.Count; i++)
        {
            if (!seen.Add(ids[i]))
                continue;

            string effect = effects.Length == ids.Count ? effects[i] : effectCell;
            acts.Add(new StoryActDefinition
            {
                id = ids[i],
                chapter = Chapter(ids[i]),
                delta = ReadAxes(effect),
                money = Money.IsMatch(effect.Trim()) ? 1 : 0,
                chimisteDemand = effect.Contains("Grande demande acceptée"),
                irreversible = effect.Contains("rréversible"),
                returnsInChapter9 = effect.Contains("retour ch. 9"),
            });
        }
    }

    private static MoralAxes ReadAxes(string effect)
    {
        var axes = new MoralAxes();

        foreach (Match match in Axis.Matches(effect))
        {
            string signs = match.Groups[2].Value;
            int value = signs.Contains("+") ? signs.Length : -signs.Length;

            switch (match.Groups[1].Value)
            {
                case "Humanité": axes.humanity += value; break;
                case "Cruauté": axes.cruelty += value; break;
                case "Dépendance": axes.dependence += value; break;
                case "Paranoïa": axes.paranoia += value; break;
            }
        }

        return axes;
    }

    // « ch05_... » ou « acte_ch05_... » → 5.
    private static int Chapter(string id)
    {
        Match match = Regex.Match(id, @"ch(\d{2})");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }
}
