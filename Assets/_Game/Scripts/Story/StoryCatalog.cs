using System;
using UnityEngine;

// Une scène de docs/SCENARIO.md : son identifiant sert de nœud Ink et de préfixe aux voix.
[Serializable]
public class StoryScene
{
    public string id;
    public int chapter;
}

// Un acte possible de docs/SCENARIO.md (tableaux « Choix et conséquences ») et ce qu'il change.
[Serializable]
public class StoryActDefinition
{
    public string id;
    public int chapter;
    [Tooltip("Effet sur les quatre axes : un point par signe + ou − du scénario.")]
    public MoralAxes delta;
    [Tooltip("Argent gagné (unités abstraites ; le prix des doses est dans la ProgressionTable).")]
    public int money;
    [Tooltip("Grande demande du Chimiste acceptée (condition de la fin immorale).")]
    public bool chimisteDemand;
    public bool irreversible;
    [Tooltip("Fait revenir un personnage dans la guerre finale (ch. 9).")]
    public bool returnsInChapter9;
}

// Scènes et actes du scénario, importés de docs/SCENARIO.md (menu RED MET → Importer le scénario).
// Le document reste la référence : on le modifie, puis on réimporte.
[CreateAssetMenu(menuName = "RED MET/Catalogue du scénario", fileName = "Scenario")]
public class StoryCatalog : ScriptableObject
{
    public StoryScene[] scenes = Array.Empty<StoryScene>();
    public StoryActDefinition[] acts = Array.Empty<StoryActDefinition>();

    public StoryActDefinition FindAct(string id) => Array.Find(acts, act => act.id == id);

    public bool HasScene(string id) => Array.Exists(scenes, scene => scene.id == id);
}
