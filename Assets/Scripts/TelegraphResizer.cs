using UnityEngine;

/// <summary>
/// Ajusta la escala del telegraph para que coincida con un radius dado.
/// Funciona tomando el ancho del sprite (world units) y escalándolo.
/// </summary>
[ExecuteAlways]
public class TelegraphResizer : MonoBehaviour
{
    public float baseSpriteSize = 1f;
    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            baseSpriteSize = sr.bounds.size.x;
        }
    }

    /// <summary>
    /// Ajusta el telegraph para que cubra un círculo de radio 'radius' (unidades de mundo).
    /// </summary>
    public void SetRadius(float radius)
    {
        if (sr != null && sr.sprite != null)
        {
            float desiredDiameter = radius * 2f;
            float currentSize = sr.bounds.size.x;

            if (currentSize <= 0.0001f)
                currentSize = baseSpriteSize;

            float factor = desiredDiameter / currentSize;
            transform.localScale = new Vector3(factor, factor, 1f);
        }
        else
        {
            float factor = radius * 2f;
            transform.localScale = new Vector3(factor, factor, 1f);
        }
    }
}
