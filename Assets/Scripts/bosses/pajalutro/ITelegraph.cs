// ITelegraph.cs
using UnityEngine;

public interface ITelegraph
{
    // Para ray: dir normalizado, length, width, duration
    void ConfigureForRay(Vector2 origin, Vector2 dir, float length, float width, float duration, Color color);

    // Para aura/meteor: origin y radius/duration
    void ConfigureForAura(Vector2 origin, float radius, float duration, Color color);

    // Para meteor telegraph point
    void ConfigureForPoint(Vector2 origin, float radius, float duration, Color color);
}
