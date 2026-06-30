// Assets/Scripts/Gameplay/AreaDamage.cs
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AreaDamage : MonoBehaviour
{
    [Tooltip("Daño aplicado al player cuando el player entra en el área")]
    public float damage = 10f;

    [Tooltip("Si true, el área se destruye después de golpear (útil para impactos puntuales)")]
    public bool destroyOnHit = false;

    [Tooltip("LayerMask para buscar sólo jugadores u objetivos relevantes (opcional)")]
    public LayerMask targetMask = ~0;

    void Reset()
    {
        // por defecto, dejar como trigger
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        // sólo procesar si el collider está dentro del mask
        if (((1 << other.gameObject.layer) & targetMask) == 0) return;

        // 1) Mensajes comunes (funciona si tu BarraVida tiene estos métodos)
        other.gameObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        other.gameObject.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
        other.gameObject.SendMessage("RecibirDanio", damage, SendMessageOptions.DontRequireReceiver);
        other.gameObject.SendMessage("ReceiveDamage", damage, SendMessageOptions.DontRequireReceiver);

        // 2) Intento por reflexión (por si tu script tiene un método con otro nombre)
        var monos = other.GetComponents<MonoBehaviour>();
        foreach (var m in monos)
        {
            if (m == null) continue;
            var t = m.GetType();
            // intentos comunes
            var mi = t.GetMethod("TakeDamage") ?? t.GetMethod("ApplyDamage") ?? t.GetMethod("ReceiveDamage") ?? t.GetMethod("RecibirDanio");
            if (mi != null)
            {
                var pars = mi.GetParameters();
                if (pars.Length == 1 && pars[0].ParameterType == typeof(float))
                {
                    mi.Invoke(m, new object[] { damage });
                    break;
                }
            }
        }

        if (destroyOnHit)
            Destroy(gameObject);
    }
}
