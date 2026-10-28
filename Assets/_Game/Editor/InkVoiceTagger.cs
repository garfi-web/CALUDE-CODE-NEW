using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Donne à chaque réplique des fichiers Ink de Story/Ink un identifiant de voix pour ElevenLabs :
// étiquette #voix:<nœud>_NNN, où le nœud est l'identifiant de scène de docs/SCENARIO.md.
// Les identifiants déjà attribués ne changent jamais : une nouvelle réplique prend le numéro suivant.
// Les lignes marquées #statut: (À RÉÉCRIRE, À ÉCRIRE, PROVISOIRE) n'en reçoivent pas tant que
// l'auteur ne les a pas écrites.
public static class InkVoiceTagger
{
    private const string InkFolder = "Assets/_Game/Story/Ink";

    private static readonly Regex Knot = new(@"^\s*===\s*(\w+)\s*===");
    private static readonly Regex Spoken = new(@"^\s*[A-ZÀ-Ý' ]+:");
    private static readonly Regex Voice = new(@"#voix:(\w+)_(\d{3})");

    [MenuItem("RED MET/Numéroter les voix Ink")]
    private static void TagMenu() => Debug.Log($"[RED MET] Voix Ink : {TagAll()} identifiants ajoutés.");

    // Bilan de compilation d'un fichier Ink (le compilateur du paquet Ink tourne à l'import).
    public static string CompileReport(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var ink = AssetDatabase.LoadAssetAtPath<Ink.UnityIntegration.InkFile>(path);

        if (ink == null)
            return $"{path} : introuvable";

        var report = new StringBuilder($"{path} : compilé={ink.isCompiled}, erreurs={ink.errors.Count}, " +
                                       $"avertissements={ink.warnings.Count}, exceptions={ink.unhandledCompileErrors.Count}");

        foreach (var log in ink.errors) report.Append($"\nERREUR ligne {log.lineNumber} : {log.content}");
        foreach (var log in ink.warnings) report.Append($"\nAVERTISSEMENT ligne {log.lineNumber} : {log.content}");
        foreach (string error in ink.unhandledCompileErrors) report.Append($"\nEXCEPTION : {error}");

        return report.ToString();
    }

    public static int TagAll()
    {
        int added = 0;

        foreach (string path in Directory.GetFiles(InkFolder, "*.ink", SearchOption.AllDirectories))
        {
            added += Tag(path);
            AssetDatabase.ImportAsset(path.Replace('\\', '/'));
        }

        return added;
    }

    private static int Tag(string path)
    {
        string[] lines = File.ReadAllText(path, Encoding.UTF8).Split('\n');

        // Premier passage : plus grand numéro déjà attribué dans chaque nœud.
        var last = new Dictionary<string, int>();

        foreach (string line in lines)
        {
            foreach (Match match in Voice.Matches(line))
            {
                int number = int.Parse(match.Groups[2].Value);
                last[match.Groups[1].Value] = Mathf.Max(number, last.GetValueOrDefault(match.Groups[1].Value));
            }
        }

        int added = 0;
        string knot = null;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd('\r');
            Match knotMatch = Knot.Match(line);

            if (knotMatch.Success)
            {
                knot = knotMatch.Groups[1].Value;
                continue;
            }

            if (knot == null || !Spoken.IsMatch(line) || line.Contains("#voix:") || line.Contains("#statut:"))
                continue;

            int number = last.GetValueOrDefault(knot) + 1;
            last[knot] = number;
            bool carriageReturn = lines[i].EndsWith("\r");
            lines[i] = $"{line} #voix:{knot}_{number:000}" + (carriageReturn ? "\r" : "");
            added++;
        }

        if (added > 0)
            File.WriteAllText(path, string.Join("\n", lines), new UTF8Encoding(false));

        return added;
    }
}
