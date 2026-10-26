using UnityEngine;
using UnityEngine.UI;

// Menu Options (Échap, ou Select à la manette) : niveau de gore, intensité du tremblement de
// caméra, images d'impact (risque épileptique). Le jeu est en pause tant qu'il est ouvert.
// Haut/bas pour choisir, gauche/droite pour régler.
public class OptionsMenu : MonoBehaviour
{
    private const int ItemCount = 4;

    [SerializeField] private GameObject panel;
    [SerializeField] private Text body;

    private int selected;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Start()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        if (GameInput.OptionsPressed)
        {
            SetOpen(!IsOpen);
            return;
        }

        if (!IsOpen)
            return;

        if (GameInput.MenuUp)
            selected = (selected + ItemCount - 1) % ItemCount;
        else if (GameInput.MenuDown)
            selected = (selected + 1) % ItemCount;
        else if (GameInput.MenuLeft)
            Change(-1);
        else if (GameInput.MenuRight)
            Change(1);
        else if (selected == ItemCount - 1 && (GameInput.LightPressed || GameInput.JumpPressed))
            SetOpen(false);

        Refresh();
    }

    private void SetOpen(bool open)
    {
        if (panel == null)
            return;

        panel.SetActive(open);
        GameTime.SetPaused(open);
        Refresh();
    }

    private void Change(int step)
    {
        switch (selected)
        {
            case 0:
                GameOptions.Gore = (GoreLevel)Mathf.Clamp((int)GameOptions.Gore + step, 0, 2);
                break;
            case 1:
                GameOptions.Shake = Mathf.Round((GameOptions.Shake + step * 0.1f) * 10f) / 10f;
                break;
            case 2:
                GameOptions.ImpactFlashes = !GameOptions.ImpactFlashes;
                break;
        }
    }

    private void Refresh()
    {
        if (body == null || !IsOpen)
            return;

        string gore = GameOptions.Gore switch
        {
            GoreLevel.Off => "désactivé",
            GoreLevel.Reduced => "réduit",
            _ => "complet",
        };

        body.text =
            "OPTIONS\n\n" +
            Line(0, $"Gore : {gore}") +
            Line(1, $"Tremblement de caméra : {Mathf.RoundToInt(GameOptions.Shake * 100f)} %") +
            Line(2, $"Images d'impact (flashs) : {(GameOptions.ImpactFlashes ? "activées" : "désactivées")}") +
            Line(3, "Reprendre") +
            "\n<size=24>Haut / bas : choisir   ·   Gauche / droite : régler   ·   Échap : fermer</size>";
    }

    private string Line(int index, string text) =>
        (index == selected ? "<color=#ff5050>▶ " : "   ") + text + (index == selected ? "</color>" : string.Empty) + "\n";
}
