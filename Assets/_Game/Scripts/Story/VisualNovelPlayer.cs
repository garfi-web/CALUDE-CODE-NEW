using System;
using System.Text.RegularExpressions;
using Ink.Runtime;
using Ink.UnityIntegration;
using UnityEngine;
using UnityEngine.UI;

// Lecteur de visual novel minimal : joue un nœud Ink (identifiant de scène de docs/SCENARIO.md),
// une réplique par appui (GameInput.DialogueNextPressed), avec le portrait de Mara choisi par sa
// tenue (GameSession.MaraOutfit). Le jeu est en pause pendant la scène (GameTime).
// Étiquettes lues sur chaque réplique : #voix:<id> (voix ElevenLabs) et #statut:<marque>
// (« À RÉÉCRIRE », « À ÉCRIRE », « PROVISOIRE »), affichée en rouge tant que l'auteur n'a pas écrit la scène.
public class VisualNovelPlayer : MonoBehaviour
{
    [SerializeField] private InkFile inkFile;
    [Tooltip("Portraits de face, M-T0 à M-T4 (scènes d'ouverture).")]
    [SerializeField] private Texture2D[] frontPortraits = new Texture2D[5];
    [Tooltip("Portraits de dos, M-T0 à M-T4 (scènes de fin). Vide : le portrait de face, signalé comme provisoire.")]
    [SerializeField] private Texture2D[] backPortraits = new Texture2D[5];

    // « MARA: réplique » : le nom du locuteur est en majuscules au début de la ligne.
    private static readonly Regex SpeakerPrefix = new(@"^([A-ZÀ-Ý' ]+):\s*(.*)$");

    private Story story;
    private GameObject root;
    private RawImage portrait;
    private Text speakerText;
    private Text lineText;
    private Text statusText;
    private Text sceneText;
    private bool wasPaused;

    public bool IsPlaying { get; private set; }
    public string Scene { get; private set; }
    public string Speaker { get; private set; }
    public string Line { get; private set; }
    public string VoiceId { get; private set; }
    public string Status { get; private set; }
    public event Action Finished;

    private void Awake() => BuildUI();

    private void Update()
    {
        if (IsPlaying && GameInput.DialogueNextPressed)
            Next();
    }

    public bool Play(string sceneId)
    {
        if (inkFile == null || !inkFile.isCompiled)
        {
            Debug.LogError("[RED MET] Visual novel : fichier Ink absent ou non compilé.");
            return false;
        }

        story ??= new Story(inkFile.storyJson);
        InkStoryState.Sync(story, ChapterOf(sceneId));

        try
        {
            story.ChoosePathString(sceneId);
        }
        catch (StoryException exception)
        {
            Debug.LogError($"[RED MET] Scène absente du fichier Ink : {sceneId} ({exception.Message})");
            return false;
        }

        GameSession.EnterScene(sceneId);
        Scene = sceneId;
        sceneText.text = sceneId;
        ShowPortrait(sceneId.Contains("_mara_fin"));

        wasPaused = GameTime.IsPaused;
        GameTime.SetPaused(true);
        IsPlaying = true;
        root.SetActive(true);
        Next();
        return true;
    }

    // Réplique suivante ; la scène se ferme quand le nœud est terminé.
    public void Next()
    {
        while (story.canContinue)
        {
            string text = story.Continue().Trim();

            if (text.Length == 0)
                continue;

            ShowLine(text, story.currentTags);
            return;
        }

        if (story.currentChoices.Count > 0)
            Debug.LogWarning($"[RED MET] {Scene} propose des choix : le lecteur ne les gère pas encore.");

        Close();
    }

    private void ShowLine(string text, System.Collections.Generic.List<string> tags)
    {
        VoiceId = null;
        Status = null;

        foreach (string tag in tags)
        {
            string value = tag.Trim();

            if (value.StartsWith("voix:"))
                VoiceId = value.Substring(5).Trim();
            else if (value.StartsWith("statut:"))
                Status = value.Substring(7).Trim();
        }

        Match match = SpeakerPrefix.Match(text);
        Speaker = match.Success ? match.Groups[1].Value.Trim() : "";
        Line = match.Success ? match.Groups[2].Value : text;

        speakerText.text = Speaker;
        lineText.text = Line;
        statusText.text = Status ?? "";
        portrait.color = Speaker == "MARA" ? Color.white : new Color(0.55f, 0.55f, 0.55f);
    }

    private void Close()
    {
        IsPlaying = false;
        root.SetActive(false);
        GameTime.SetPaused(wasPaused);
        Finished?.Invoke();
    }

    private void ShowPortrait(bool back)
    {
        int outfit = Mathf.Clamp((int)GameSession.MaraOutfit, 0, frontPortraits.Length - 1);
        Texture2D texture = back ? backPortraits[outfit] : null;
        bool provisional = back && texture == null;

        portrait.texture = texture != null ? texture : frontPortraits[outfit];
        portrait.enabled = portrait.texture != null;

        if (portrait.texture != null)
        {
            // Hauteur fixe, entre le haut de la boîte de texte et le haut de l'écran ; largeur selon l'image.
            float height = 720f;
            portrait.rectTransform.sizeDelta = new Vector2(height * portrait.texture.width / portrait.texture.height, height);
        }

        sceneText.text += provisional ? "  (portrait de dos à venir : portrait de face provisoire)" : "";
    }

    private static int ChapterOf(string sceneId)
    {
        Match match = Regex.Match(sceneId, @"^ch(\d{2})_");
        return match.Success ? int.Parse(match.Groups[1].Value) : GameSession.Incursion;
    }

    // ------------------------------------------------------------ Interface

    private void BuildUI()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        root = new GameObject("VisualNovel", typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        Image dim = NewImage("Voile", root.transform, new Color(0.02f, 0f, 0f, 0.82f));
        Stretch(dim.rectTransform, Vector2.zero, Vector2.one);

        portrait = new GameObject("Portrait", typeof(RawImage)).GetComponent<RawImage>();
        portrait.transform.SetParent(root.transform, false);
        RectTransform portraitRect = portrait.rectTransform;
        portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0.5f, 0f);
        portraitRect.pivot = new Vector2(0.5f, 0f);
        // Le bas du portrait repose sur le haut de la boîte de texte (30 % de la hauteur, soit 324 px).
        portraitRect.anchoredPosition = new Vector2(0f, 334f);

        Image box = NewImage("Boite", root.transform, new Color(0.06f, 0.01f, 0.02f, 0.92f));
        Stretch(box.rectTransform, new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.3f));

        speakerText = NewText("Nom", box.transform, font, 44, FontStyle.Bold, new Color(0.95f, 0.25f, 0.25f), TextAnchor.UpperLeft);
        Stretch(speakerText.rectTransform, new Vector2(0.03f, 0.72f), new Vector2(0.6f, 0.95f));

        statusText = NewText("Statut", box.transform, font, 34, FontStyle.Bold, new Color(1f, 0.35f, 0.1f), TextAnchor.UpperRight);
        Stretch(statusText.rectTransform, new Vector2(0.6f, 0.72f), new Vector2(0.97f, 0.95f));

        lineText = NewText("Replique", box.transform, font, 40, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
        Stretch(lineText.rectTransform, new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.72f));

        sceneText = NewText("Scene", box.transform, font, 22, FontStyle.Italic, new Color(0.6f, 0.6f, 0.6f), TextAnchor.LowerLeft);
        Stretch(sceneText.rectTransform, new Vector2(0.03f, 0.02f), new Vector2(0.8f, 0.14f));

        Text hint = NewText("Suite", box.transform, font, 26, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f), TextAnchor.LowerRight);
        Stretch(hint.rectTransform, new Vector2(0.8f, 0.02f), new Vector2(0.97f, 0.14f));
        hint.text = "Entrée ▸";

        root.SetActive(false);
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        var image = new GameObject(name, typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        return image;
    }

    private static Text NewText(string name, Transform parent, Font font, int size, FontStyle style, Color color, TextAnchor anchor)
    {
        var text = new GameObject(name, typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false);
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
