using UnityEngine;

// Voile de couleur sur un sprite (shader « RED MET/Sprite Flash ») : flash blanc d'impact,
// annonce d'armement, états. Passe par un MaterialPropertyBlock : aucun matériau n'est dupliqué.
// Sans ce shader, on retombe sur la couleur du SpriteRenderer.
public static class SpriteFlash
{
    private static readonly int AmountId = Shader.PropertyToID("_Flash");
    private static readonly int ColorId = Shader.PropertyToID("_FlashColor");
    private static MaterialPropertyBlock block;

    public static bool Supports(SpriteRenderer renderer) =>
        renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(AmountId);

    public static void Set(SpriteRenderer renderer, float amount, Color color)
    {
        block ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetFloat(AmountId, amount);
        block.SetColor(ColorId, color);
        renderer.SetPropertyBlock(block);
    }
}
