using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CommonDamage : MonoBehaviour
{
    [Tooltip("Daño aplicado (por golpe o por segundo si useDPS=true)")]
    public float damage = 10f;

    [Tooltip("Si true aplica damage cada frame (scaled) mientras el player está dentro")]
    public bool useDPS = false;

    [Tooltip("Layer mask para filtrar targets (usa el layer del player)")]
    public LayerMask targetMask = 1 << 8; // por defecto layer 8 (ajusta si usas otro)

    [Tooltip("auto destroy al terminar (para impactos puntuales)")]
    public float lifetime = 0f; // 0 = no auto-destruir

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!MaskContains(targetMask, other.gameObject.layer)) return;

        if (!useDPS)
        {
            ApplyDamageTo(other.gameObject, damage);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!useDPS) return;
        if (other == null) return;
        if (!MaskContains(targetMask, other.gameObject.layer)) return;

        // aplicar daño proporcional al deltaTime
        float d = damage * Time.deltaTime;
        ApplyDamageTo(other.gameObject, d);
    }

    static bool MaskContains(LayerMask mask, int layer)
    {
        return ((1 << layer) & mask) != 0;
    }

    void ApplyDamageTo(GameObject go, float dmg)
    {
        if (go == null) return;

        // 1) SendMessage common names
        go.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);
        go.SendMessage("ApplyDamage", dmg, SendMessageOptions.DontRequireReceiver);
        go.SendMessage("RecibirDanio", dmg, SendMessageOptions.DontRequireReceiver);
        go.SendMessage("ReceiveDamage", dmg, SendMessageOptions.DontRequireReceiver);

        // 2) Reflection fallback: buscar un método que tome (float)
        var monos = go.GetComponents<MonoBehaviour>();
        foreach (var m in monos)
        {
            if (m == null) continue;
            var t = m.GetType();
            var mi = t.GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     ?? t.GetMethod("ApplyDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     ?? t.GetMethod("ReceiveDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     ?? t.GetMethod("RecibirDanio", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (mi != null)
            {
                var pars = mi.GetParameters();
                if (pars.Length == 1 && (pars[0].ParameterType == typeof(float) || pars[0].ParameterType == typeof(double) || pars[0].ParameterType == typeof(int)))
                {
                    object arg = dmg;
                    if (pars[0].ParameterType == typeof(int)) arg = Mathf.RoundToInt(dmg);
                    mi.Invoke(m, new object[] { arg });
                    return;
                }
            }
        }
    }
}
