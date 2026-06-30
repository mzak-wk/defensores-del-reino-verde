using System.Collections;
using UnityEngine;

public class ScreamRingAttack : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject telegraphPrefab; // círculo de aviso

    [Header("Parámetros")]
    public float castTime = 3f;        // tiempo de casteo antes del daño
    public float ringDuration = 2f;    // cuánto tiempo dura el anillo haciendo daño
    [Tooltip("1 = tamaño original del prefab, 2 = el doble, 0.5 = la mitad, etc.")]
    public float ringRadius = 1f;      // multiplicador de tamaño
    public float damage = 50f;
    public float tickInterval = 0.5f;  // cada cuánto aplica daño

    [Header("Daño")]
    public LayerMask damageLayerMask;  // capa del Player

    // escala original del prefab (tal cual está en el editor)
    private Vector3 originalScale;
    void Awake()
    {
        originalScale = transform.localScale;
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
    {
        sr.sortingLayerName = "Characters";  // o la capa que uses
        sr.sortingOrder = 0;                 // algo menor que el player
    }
    }

    // Llamado por el BossGorgonController
    public IEnumerator Execute(Transform origin, Transform player, Vector3 scaleFromBoss)
    {
        // --- OPCIONAL: ocultar sprite propio mientras telegrapea ---
        var myRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in myRenderers) sr.enabled = false;

        // ============================
        // 1) TELEGRAPH (AVISO)
        // ============================
        GameObject tele = null;
        Vector3 teleOriginalScale = Vector3.one;

        if (telegraphPrefab != null)
        {
            tele = Instantiate(telegraphPrefab, origin.position, Quaternion.identity);
            teleOriginalScale = tele.transform.localScale;
            // 1 = tamaño base del prefab de telegraph, ringRadius = multiplicador
            tele.transform.localScale = teleOriginalScale * ringRadius;
        }

        float elapsed = 0f;
        while (elapsed < castTime)
        {
            if (tele != null) tele.transform.position = origin.position;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (tele != null) Destroy(tele);

        // ============================
        // 2) ACTIVAR EL ANILLO REAL
        // ============================
        transform.position = origin.position;
        // 1 = tamaño base del prefab screamAttack, ringRadius = multiplicador
        transform.localScale = originalScale * ringRadius;

        foreach (var sr in myRenderers) sr.enabled = true;

        // Calculamos el radio REAL en unidades de mundo usando el sprite ya escalado
        float worldRadius = ringRadius;
        var mainSr = GetComponentInChildren<SpriteRenderer>();
        if (mainSr != null)
        {
            worldRadius = mainSr.bounds.extents.x; // mitad del ancho
        }

        // ============================
        // 3) BUCLE DE DAÑO
        // ============================
        float lived = 0f;
        while (lived < ringDuration)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position,
                worldRadius,
                damageLayerMask
            );

            for (int i = 0; i < hits.Length; i++)
            {
                ApplyDamage(hits[i]);
            }

            lived += tickInterval;
            yield return new WaitForSeconds(tickInterval);
        }

        Destroy(gameObject);
    }

    void ApplyDamage(Collider2D c)
    {
        if (c == null) return;

        GameObject root = c.transform.root.gameObject;

        bool applied = false;
        var barra = root.GetComponentInChildren<BarraVida>();
        if (barra != null)
        {
            barra.RecibirDanio(damage);
            applied = true;
        }

        if (!applied)
        {
            root.SendMessage("RecibirDanio", damage, SendMessageOptions.DontRequireReceiver);
            root.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
    }

    // Gizmo para ver el radio real (puedes borrar todo este método si no lo quieres)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.25f);

        float r = 1f;
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            r = sr.bounds.extents.x;
        }

        Gizmos.DrawSphere(transform.position, r);
    }
}
