using UnityEngine;

/// <summary>
/// Aplica daño por contacto periódico a Andrius cuando este entra en colisión o trigger con el enemigo.
/// Soporta tanto Colliders físicos sólidos (OnCollision) como Triggers (OnTrigger) y respeta una cadencia de daño (damageRate).
/// </summary>
public class DamageOnContact : MonoBehaviour
{
    [Header("Daño periódico")]
    [Tooltip("Tiempo mínimo en segundos entre golpes sucesivos mientras el jugador permanece en contacto.")]
    public float damageRate = 0.5f;

    [Tooltip("Capa del jugador. Déjalo en Nothing para ignorar capas.")]
    public LayerMask playerMask;

    private Enemy enemy;
    private float timer = 0f;
    private BarraVida cachedVida;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
        if (enemy == null)
            Debug.LogWarning("[DamageOnContact] No se encontró componente Enemy en los padres.", this);
    }

    private void Update()
    {
        if (timer > 0f)
            timer -= Time.deltaTime;
    }

    private void TryApplyContactDamage(GameObject targetGO)
    {
        if (timer > 0f || enemy == null) return;

        if (playerMask.value != 0 && ((playerMask.value & (1 << targetGO.layer)) == 0))
            return;

        if (cachedVida == null || !cachedVida.gameObject.activeInHierarchy)
        {
            cachedVida = targetGO.GetComponentInParent<BarraVida>() ??
                         targetGO.GetComponentInChildren<BarraVida>() ??
                         targetGO.GetComponent<BarraVida>();

            if (cachedVida == null && targetGO.CompareTag("Player"))
            {
                cachedVida = Object.FindFirstObjectByType<BarraVida>();
            }
        }

        if (cachedVida != null && cachedVida.currentHP > 0f)
        {
            float damage = enemy.contactDamage;
            if (damage <= 0f) damage = 5f; // Fallback razonable si contactDamage es 0

            cachedVida.RecibirDanio(damage);
            timer = damageRate;
        }
    }

    // ====================================================
    // DETECCIÓN TRIGGER (Hitboxes y zonas de daño)
    // ====================================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryApplyContactDamage(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryApplyContactDamage(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (cachedVida != null && other.transform.IsChildOf(cachedVida.transform))
        {
            cachedVida = null;
        }
    }

    // ====================================================
    // DETECCIÓN FÍSICA SÓLIDA (Colisiones directas)
    // ====================================================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryApplyContactDamage(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryApplyContactDamage(collision.gameObject);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (cachedVida != null && collision.transform.IsChildOf(cachedVida.transform))
        {
            cachedVida = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = Color.red;
        if (col is BoxCollider2D box)
            Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
        else if (col is CircleCollider2D circle)
            Gizmos.DrawWireSphere(circle.bounds.center, circle.bounds.extents.x);
        else if (col is CapsuleCollider2D capsule)
            Gizmos.DrawWireCube(capsule.bounds.center, capsule.bounds.size);
    }
}
