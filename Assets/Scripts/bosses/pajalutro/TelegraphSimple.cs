// TelegraphSimple.cs
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TelegraphSimple : MonoBehaviour, ITelegraph
{
    SpriteRenderer sr;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    public void ConfigureForRay(Vector2 origin, Vector2 dir, float length, float width, float duration, Color color)
    {
        transform.position = origin;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = new Vector3(length, width, 1f);
        if (sr) { sr.color = color; sr.enabled = true; }
        Destroy(gameObject, duration);
    }

    public void ConfigureForAura(Vector2 origin, float radius, float duration, Color color)
    {
        transform.position = origin;
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        if (sr) { sr.color = color; sr.enabled = true; }
        Destroy(gameObject, duration);
    }

    public void ConfigureForPoint(Vector2 origin, float radius, float duration, Color color)
    {
        ConfigureForAura(origin, radius, duration, color);
    }
}
