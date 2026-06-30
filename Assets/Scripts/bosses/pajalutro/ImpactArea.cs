using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Necesario para la lista de objetivos

[RequireComponent(typeof(Collider2D))]
public class ImpactArea : MonoBehaviour
{
    [Header("Ajustes básicos (compatibles)")]
    public float duration = 3f;
    public float damage = 10f;
    public bool singleHit = true;
    public float tickInterval = 0.5f;

    // [REINTRODUCIDO] Propiedad Radius para COMPATIBILIDAD (BossWhiteMage)
    // Ya NO se usa internamente para la detección de colisión.
    [Header("Compatibilidad con otros scripts")]
    public float radius = 1.5f;
    public LayerMask damageLayerMask = 0;

    [Header("Opcional / VFX")]
    public GameObject impactVfxPrefab;

    [Header("Debug / Gizmos")]
    public Color gizmoColor = new Color(1f, 0.6f, 0f, 0.25f);

    public bool debugLogs = false;

    Collider2D myCollider;

    // Para daño por ticks (singleHit = false)
    private Dictionary<Collider2D, float> targetsInside = new Dictionary<Collider2D, float>();

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        if (!myCollider)
        {
            Debug.LogError("[ImpactArea] Falta Collider2D en el prefab.");
        }
        else if (!myCollider.isTrigger)
        {
            myCollider.isTrigger = true;
            if (debugLogs) Debug.LogWarning("[ImpactArea] Collider2D forzado a isTrigger.");
        }

        // Aseguramos un Rigidbody2D kinemático
        if (!GetComponent<Rigidbody2D>())
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }

        if (duration > 0f) Destroy(gameObject, duration);
    }

    void Start()
    {
        if (!singleHit)
            StartCoroutine(TickDamageLoop());
    }

    // --- Detección de Colisiones (Usa el Collider2D asignado, PolygonCollider2D) ---

    // 1. Detección de entrada
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!LayerMaskIncludes(damageLayerMask, other.gameObject.layer)) return;

        if (singleHit)
        {
            // Daño instantáneo (si singleHit es TRUE)
            ApplyToCollider(other);
            return;
        }

        // Para daño por ticks (si singleHit es FALSE): Añadimos el objetivo
        if (!targetsInside.ContainsKey(other))
        {
            targetsInside.Add(other, Time.time);
            if (debugLogs) Debug.Log($"[ImpactArea] {other.name} entró en el área.");
        }
    }

    // 2. Detección de salida
    void OnTriggerExit2D(Collider2D other)
    {
        // Para daño por ticks: Removemos el objetivo
        if (!singleHit && targetsInside.ContainsKey(other))
        {
            targetsInside.Remove(other);
            if (debugLogs) Debug.Log($"[ImpactArea] {other.name} salió del área.");
        }
    }

    // 3. Bucle de daño por ticks (usa la lista de objetivos 'targetsInside')
    IEnumerator TickDamageLoop()
    {
        while (true)
        {
            // Clonamos las llaves para iterar sin problemas de modificación
            List<Collider2D> keys = new List<Collider2D>(targetsInside.Keys);
            foreach (var c in keys)
            {
                // Solo aplicar daño si ha pasado el tickInterval desde el último golpe
                // y si el Collider aún es válido
                if (c != null && targetsInside.ContainsKey(c) && Time.time >= targetsInside[c] + tickInterval)
                {
                    ApplyToCollider(c);
                    targetsInside[c] = Time.time; // Actualizamos el tiempo del último golpe
                }
            }

            yield return null; // Esperamos al siguiente frame para revisar tiempos
        }
    }

    void ApplyToCollider(Collider2D c)
    {
        if (c == null) return;

        GameObject root = c.transform.root.gameObject;

        bool applied = false;

        var barra = root.GetComponentInChildren<BarraVida>();
        if (barra != null)
        {
            barra.RecibirDanio(damage);
            applied = true;
            if (debugLogs) Debug.Log($"[ImpactArea] Aplicado daño {damage} a {root.name} via BarraVida.");
        }

        if (!applied)
        {
            root.SendMessage("RecibirDanio", damage, SendMessageOptions.DontRequireReceiver);
            root.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

            if (debugLogs) Debug.Log($"[ImpactArea] SendMessage aplicado (posible) a {root.name}.");
        }

        if (impactVfxPrefab && applied)
        {
            Instantiate(impactVfxPrefab, c.transform.position, Quaternion.identity);
        }
    }

static bool LayerMaskIncludes(LayerMask mask, int layer)
{
    // Si nadie configuró capas, aceptamos todas
    if (mask.value == 0) return true;
    return (mask.value & (1 << layer)) != 0;
}


    void OnDrawGizmosSelected()
    {
        // NO DIBUJAMOS NINGÚN CÍRCULO AQUÍ. El Polygon Collider 2D lo hace por nosotros.
        // Mantenemos solo la definición de color si deseas usarla en un futuro.
        Gizmos.color = gizmoColor;
    }
}