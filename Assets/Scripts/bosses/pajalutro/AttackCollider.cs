using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Componente genérico para prefabs de ataque que aplican daño vía triggers.
/// - Funciona en singleHit o tickMode (daño por ticks mientras el player esté dentro).
/// - Añade Rigidbody2D kinematic si falta.
/// - Intenta aplicar daño vía BarraVida, luego SendMessage fallback.
/// - Emite debug logs detallados para verificar contactos.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AttackCollider : MonoBehaviour
{
    [Header("Ajustes")]
    public float damage = 25f;
    public LayerMask targetMask = ~0; // por defecto todo (debug); asigna Layer del player en inspector
    public string targetTag = "Player"; // opcional: si lo usas, compara por tag además de layer
    public bool singleHit = true;      // true = aplica daño 1 sola vez al entrar; false = tick mode
    public bool tickMode = false;      // si true, aplica daño cada tickInterval mientras dentro
    public float tickInterval = 0.5f;

    [Header("Vida del prefab")]
    public float lifeTime = 2f;        // autodestruye el prefab luego de esto (0 para infinito)

    [Header("Debug")]
    public bool debugLogs = true;
    public Color gizmoColor = new Color(1f, 0.2f, 0.2f, 0.25f);

    // estado
    HashSet<GameObject> inside = new HashSet<GameObject>();
    Coroutine tickCoroutine;

    Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError($"[AttackCollider] {name} necesita Collider2D.");
            enabled = false;
            return;
        }

        // asegurar trigger
        if (!col.isTrigger)
        {
            if (debugLogs) Debug.LogWarning($"[AttackCollider] {name}: Collider no estaba marcado 'isTrigger'. Lo marco automáticamente.");
            col.isTrigger = true;
        }

        // asegurar Rigidbody2D kinemático para que Unity notifique triggers correctamente
        var rb = GetComponent<Rigidbody2D>();
        if (!rb)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
            rb.gravityScale = 0f;
            if (debugLogs) Debug.Log($"[AttackCollider] {name}: Rigidbody2D kinematic añadido automáticamente.");
        }

        // autodestruir si corresponde
        if (lifeTime > 0f)
            Destroy(gameObject, lifeTime);
    }

    void OnEnable()
    {
        inside.Clear();
        if (!tickMode && tickCoroutine != null)
        {
            StopCoroutine(tickCoroutine);
            tickCoroutine = null;
        }
        if (tickMode)
            tickCoroutine = StartCoroutine(TickLoop());
    }

    void OnDisable()
    {
        if (tickCoroutine != null) { StopCoroutine(tickCoroutine); tickCoroutine = null; }
        inside.Clear();
    }

    // Trigger enter
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidTarget(other)) return;

        GameObject root = other.transform.root.gameObject;

        if (singleHit && inside.Contains(root))
        {
            if (debugLogs) Debug.Log($"[AttackCollider] {name}: {root.name} ya recibió singleHit.");
            return;
        }

        inside.Add(root);

        if (debugLogs) Debug.Log($"[AttackCollider] {name}: OnTriggerEnter2D -> {root.name} (layer {other.gameObject.layer}, tag {other.gameObject.tag})");

        if (singleHit && !tickMode)
        {
            ApplyDamageToRoot(root, damage);
        }
        // si tickMode=true, el coroutine TickLoop se encarga de aplicar daño periódicamente
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsValidTarget(other)) return;
        GameObject root = other.transform.root.gameObject;
        if (inside.Contains(root))
        {
            inside.Remove(root);
            if (debugLogs) Debug.Log($"[AttackCollider] {name}: OnTriggerExit2D -> {root.name}");
        }
    }

    IEnumerator TickLoop()
    {
        while (true)
        {
            if (inside.Count > 0)
            {
                foreach (var go in new List<GameObject>(inside))
                {
                    if (go == null) continue;
                    if (debugLogs) Debug.Log($"[AttackCollider] {name}: Tick aplicando {damage} a {go.name}");
                    ApplyDamageToRoot(go, damage);
                }
            }
            yield return new WaitForSeconds(Mathf.Max(0.05f, tickInterval));
        }
    }

    bool IsValidTarget(Collider2D other)
    {
        if (other == null) return false;

        // tag check (si se usa)
        if (!string.IsNullOrEmpty(targetTag) && other.CompareTag(targetTag))
            return true;

        // layer mask check
        if ((targetMask.value & (1 << other.gameObject.layer)) != 0)
            return true;

        return false;
    }

    void ApplyDamageToRoot(GameObject root, float dmg)
    {
        if (root == null) return;

        // 1) Buscar BarraVida en root o hijos
        var barra = root.GetComponentInChildren<BarraVida>();
        if (barra != null)
        {
            if (debugLogs) Debug.Log($"[AttackCollider] {name}: Aplicando {dmg} a {root.name} via BarraVida.");
            barra.RecibirDanio(dmg);
            return;
        }

        // 2) Fallback: SendMessage para compatibilidad
        if (debugLogs) Debug.Log($"[AttackCollider] {name}: No BarraVida en {root.name}, enviando SendMessage RecibirDanio/TakeDamage.");
        root.SendMessage("RecibirDanio", dmg, SendMessageOptions.DontRequireReceiver);
        root.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);
    }

    void OnDrawGizmosSelected()
    {
        if (!col) col = GetComponent<Collider2D>();
        Gizmos.color = gizmoColor;
        if (col is CircleCollider2D cc)
        {
            Vector3 pos = transform.TransformPoint(cc.offset);
            float r = Mathf.Abs(cc.radius) * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            Gizmos.DrawSphere(pos, r);
        }
        else if (col is BoxCollider2D bc)
        {
            Vector3 center = transform.TransformPoint(bc.offset);
            Vector3 size = new Vector3(bc.size.x * transform.lossyScale.x, bc.size.y * transform.lossyScale.y, 1f);
            Matrix4x4 rot = Matrix4x4.TRS(center, transform.rotation, size);
            Gizmos.matrix = rot;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
        else
        {
            // fallback: draw bounds
            var b = col.bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}
