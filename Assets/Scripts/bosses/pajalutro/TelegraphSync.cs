using UnityEngine;

/// <summary>
/// Utility para telegraph prefabs: permite sincronizar tamaño y sprite
/// con el prefab del ataque o con un radio (SetRadius).
/// </summary>
[DisallowMultipleComponent]
public class TelegraphSync : MonoBehaviour
{
    [Tooltip("SpriteRenderer del telegraph (opcional). Si no hay se intenta buscar uno en hijos).")]
    public SpriteRenderer teleSprite;

    void Awake()
    {
        if (teleSprite == null)
            teleSprite = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// Ajusta el telegraph para que tenga un radio (en unidades world) dado.
    /// Asume que el sprite de telegraph cubre 1x1 unidades en su escala = 1 por defecto.
    /// </summary>
    public void SetRadius(float radius)
    {
        if (teleSprite == null || teleSprite.sprite == null) return;

        // diámetro en unidades world
        float diameter = Mathf.Max(0.001f, radius * 2f);

        // tamaño (bounds) del sprite en unidades (en escala local 1)
        Vector2 spriteSize = teleSprite.sprite.bounds.size;
        if (spriteSize.x <= 0.0001f || spriteSize.y <= 0.0001f) return;

        // calculamos escala necesaria para que la sprite cubra diameter en ambos ejes
        float scaleX = diameter / spriteSize.x;
        float scaleY = diameter / spriteSize.y;

        // aplicar escala (manteniendo z=1)
        transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    /// <summary>
    /// Sincroniza el telegraph con el SpriteRenderer del prefab de ataque (match visual).
    /// Ajusta la escala del telegraph para que tenga el mismo tamaño en world-space
    /// que el sprite del prefab de ataque.
    /// </summary>
    public void SyncToPrefab(SpriteRenderer attackSpriteRenderer)
    {
        if (teleSprite == null || teleSprite.sprite == null || attackSpriteRenderer == null || attackSpriteRenderer.sprite == null)
            return;

        // tamaño world del sprite de ataque = sprite.bounds * prefab.localScale
        Vector2 attackSpriteSize = attackSpriteRenderer.sprite.bounds.size;
        Vector3 attackScale = attackSpriteRenderer.transform.localScale;
        Vector2 attackWorldSize = new Vector2(attackSpriteSize.x * Mathf.Abs(attackScale.x), attackSpriteSize.y * Mathf.Abs(attackScale.y));

        // tamaño base del teleSprite (sprite bounds)
        Vector2 teleBaseSize = teleSprite.sprite.bounds.size;
        if (teleBaseSize.x <= 0.0001f || teleBaseSize.y <= 0.0001f) return;

        // escala que hace al telegraph tener el mismo world size que el attack sprite
        Vector3 newScale = new Vector3(attackWorldSize.x / teleBaseSize.x, attackWorldSize.y / teleBaseSize.y, 1f);
        transform.localScale = newScale;
    }

    // helper opcional para depuración
    public void SetColor(Color c)
    {
        if (teleSprite) teleSprite.color = c;
    }
}
