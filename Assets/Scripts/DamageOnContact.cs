using UnityEngine;

public class DamageOnContact : MonoBehaviour
{
    [Header("Daño periódico")]
    [Tooltip("Tiempo entre golpes mientras el jugador está dentro del hitbox")]
    public float damageRate = 0.5f;

    [Tooltip("Capa del jugador. Déjalo en Nothing para ignorar capas.")]
    public LayerMask playerMask;

    private Enemy enemy;
    private Collider2D myCollider;
    private float timer;

    void Awake()
    {
        // Enemy puede estar en el padre (muy típico en bosses)
        enemy = GetComponentInParent<Enemy>();
        if (enemy == null)
            Debug.LogWarning("[DamageOnContact] No encontré Enemy en los padres.", this);

        myCollider = GetComponent<Collider2D>();
        if (myCollider == null)
            Debug.LogWarning("[DamageOnContact] Necesita un Collider2D en el mismo objeto.", this);
        else
            myCollider.isTrigger = true;   // importante: que sea trigger
    }

    void Update()
    {
        if (timer > 0f)
            timer -= Time.deltaTime;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (timer > 0f) return;
        if (enemy == null) return;

        // Filtrar por capa si se configuró playerMask
        if (playerMask.value != 0 &&
            (playerMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        // Buscar la barra de vida en el root del jugador
        Transform root = other.attachedRigidbody
            ? other.attachedRigidbody.transform
            : other.transform.root;

        BarraVida vida = root.GetComponent<BarraVida>() ??
                         root.GetComponentInChildren<BarraVida>();

        if (vida == null) return;

        vida.RecibirDanio(enemy.contactDamage);
        timer = damageRate;
        // Debug.Log($"[DamageOnContact] {name} dañó a {root.name}");
    }

    // Solo para ver el hitbox en el Scene view
    void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = Color.cyan;

        if (col is BoxCollider2D box)
        {
            Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
        }
        else if (col is CircleCollider2D circle)
        {
            Gizmos.DrawWireSphere(circle.bounds.center, circle.bounds.extents.x);
        }
        else if (col is CapsuleCollider2D capsule)
        {
            Gizmos.DrawWireCube(capsule.bounds.center, capsule.bounds.size);
        }
    }
}
