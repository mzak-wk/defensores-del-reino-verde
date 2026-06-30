using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HowlAttack : MonoBehaviour
{
    [Header("Ajustes del aura")]
    public float radius = 4f;
    public float duration = 6f;
    public float tickInterval = 0.5f;
    public float damagePerTick = 10f;
    public bool singleHit = false; // si true -> solo aplica 1 vez al entrar; si false -> ticks

    [Header("Target / Layers")]
    public LayerMask damageLayerMask = 0; // asigna aquí la layer del Player
    [Tooltip("Si asignas la layer 'PlayerProjectile' aquí, se destruirán proyectiles de esa layer.")]
    public LayerMask projectileLayerMask = 0;
    [Tooltip("Tags alternativos para proyectiles (ej: 'PlayerAttack', 'Projectile')")]
    public string[] projectileTags = new string[] { "PlayerAttack", "Projectile", "PlayerProjectile" };

    [Header("Deflect / Destroy proyectiles")]
    public bool deflectProjectiles = true;

    [Header("VFX / Debug")]
    public GameObject impactVfxPrefab;
    public bool debugLogs = false;

    Collider2D myCollider;

    // Llamar si quieres reinicializar desde el spawner
    public void Initialize()
    {
        // placeholder: cualquier setup adicional si hace falta
    }

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        if (myCollider == null)
        {
            Debug.LogError("[HowlAttack] Falta Collider2D en el prefab.");
            gameObject.AddComponent<CircleCollider2D>();
            myCollider = GetComponent<Collider2D>();
        }

        // aseguramos trigger y que sea CircleCollider2D si no hay una
        var cc = myCollider as CircleCollider2D;
        if (cc == null)
        {
            // intenta añadir un CircleCollider2D y quitar el otro para consistencia
            DestroyImmediate(myCollider);
            cc = gameObject.AddComponent<CircleCollider2D>();
            myCollider = cc;
        }
        myCollider.isTrigger = true;

        // Ajustamos el radio del CircleCollider2D según 'radius' (ten en cuenta escala)
        cc.radius = Mathf.Max(0.01f, radius); // radius en unidades de mundo

        // Rigidbody kinemático para triggers
        if (!GetComponent<Rigidbody2D>())
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }

        // Destruir al terminar duration
        if (duration > 0f) Destroy(gameObject, duration);
    }

    void OnEnable()
    {
        // si usamos ticks
        if (!singleHit)
        {
            StartCoroutine(TickCoroutine());
        }
    }

    // si singleHit, OnTriggerEnter aplica; si ticks, usamos OverlapCircleAll periódico
    void OnTriggerEnter2D(Collider2D other)
    {
        // 1) comprueba si es proyectil y debemos deflectarlo
        if (deflectProjectiles && IsProjectileCollider(other))
        {
            HandleProjectileHit(other);
            return;
        }

        // 2) si es singleHit y pertenece a la capa objetivo, aplicamos daño
        if (singleHit && LayerMaskIncludes(damageLayerMask, other.gameObject.layer))
        {
            ApplyDamageToCollider(other);
        }
    }

    IEnumerator TickCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // usamos OverlapCircleAll en la posición actual
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, damageLayerMask);
            for (int i = 0; i < hits.Length; i++)
            {
                ApplyDamageToCollider(hits[i]);
            }
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }
    }

    // Aplica daño a un collider (intenta BarraVida o SendMessage fallback)
    void ApplyDamageToCollider(Collider2D c)
    {
        if (c == null) return;
        GameObject root = c.transform.root.gameObject;

        bool applied = false;
        var barra = root.GetComponentInChildren<BarraVida>();
        if (barra != null)
        {
            barra.RecibirDanio(damagePerTick);
            applied = true;
            if (debugLogs) Debug.Log($"[HowlAttack] Applied {damagePerTick} to {root.name}");
        }

        if (!applied)
        {
            root.SendMessage("RecibirDanio", damagePerTick, SendMessageOptions.DontRequireReceiver);
            root.SendMessage("TakeDamage", damagePerTick, SendMessageOptions.DontRequireReceiver);
            if (debugLogs) Debug.Log($"[HowlAttack] SendMessage damage to {root.name}");
        }

        if (impactVfxPrefab != null && applied)
        {
            Instantiate(impactVfxPrefab, c.transform.position, Quaternion.identity);
        }
    }

bool IsProjectileCollider(Collider2D other)
    {
        if (other == null) return false;

        // 1) por layer (esta parte está bien)
        if (projectileLayerMask != 0 && LayerMaskIncludes(projectileLayerMask, other.gameObject.layer))
            return true;

        // 2) por tag (¡AQUÍ ESTÁ EL CAMBIO!)
        for (int i = 0; i < projectileTags.Length; i++)
        {
            if (!string.IsNullOrEmpty(projectileTags[i]))
            {
                // CAMBIO: Usar .tag == string en lugar de .CompareTag(string)
                if (other.tag == projectileTags[i]) return true;
            }
        }        // 3) intento heurístico: si el nombre del gameObject contiene keywords comunes
        string name = other.gameObject.name.ToLower();
        if (name.Contains("proj") || name.Contains("arrow") || name.Contains("fire") || name.Contains("bullet") || name.Contains("slash"))
            return true;

        // 4) reflection: intenta detectar componentes con nombres comunes (no es obligatorio que existan)
        string[] commonNames = new string[] { "Projectile", "Fireball", "Arrow", "SlashAttack", "PersistentSlash", "AreaDamage" };
        foreach (var nm in commonNames)
        {
            var t = System.Type.GetType(nm);
            if (t != null)
            {
                var comp = other.GetComponent(t);
                if (comp != null) return true;
            }
        }

        return false;
    }

    void HandleProjectileHit(Collider2D other)
    {
        if (other == null) return;
        GameObject go = other.gameObject;

        // Primero intenta mandar un mensaje tipo "OnDeflected" por si el proyectil tiene lógica
        go.SendMessage("OnDeflected", SendMessageOptions.DontRequireReceiver);

        // Luego, si no quieres efectos especiales, destruye el proyectil
        Destroy(go);

        if (debugLogs) Debug.Log($"[HowlAttack] Deflected/destroyed projectile {go.name}");
    }

    static bool LayerMaskIncludes(LayerMask mask, int layer)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    // Gizmo para ver el radio en editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.25f);
        Gizmos.DrawSphere(transform.position, radius);
    }
}
