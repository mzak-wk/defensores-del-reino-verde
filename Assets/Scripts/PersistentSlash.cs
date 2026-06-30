using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class PersistentSlash : MonoBehaviour
{
    public Transform followTarget;              // asignado por PlayerAutoAttack
    public SpriteRenderer spriteRenderer;       // child or root SR
    public Collider2D hitCollider;              // trigger collider (CircleCollider2D or BoxCollider2D)
    public float lifetime = 0.5f;               // tiempo que permanece visible cada activación
    public int maxHitsPerActivation = 999;      // si quieres limitar hits por activación
    public LayerMask enemyMask;                 // layer de enemigos (opcional check)

    [HideInInspector] public float damage = 10f;
    [HideInInspector] public Vector2 facing = Vector2.right; // dirección donde "mira" (normalized)
    [HideInInspector] public float forwardOffset = 0.6f;     // distancia delante del player
    [HideInInspector] public float followDistance = 0f;      // usado si quieres un offset fijo
    int hitsThisActivation;

    bool activeFollowing = false;
    float deactivateAt = 0f;

    void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        hitCollider = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        // asegurarse de que el collider es trigger
        if (hitCollider != null) hitCollider.isTrigger = true;
    }

    void Update()
    {
        if (!activeFollowing || followTarget == null) return;

        // Mantener la posición delante del jugador en la dirección facing
        Vector3 pos = followTarget.position + (Vector3)(facing.normalized * forwardOffset);
        transform.position = pos;

        // Desactivar cuando expire el lifetime
        if (Time.time >= deactivateAt) Deactivate();
    }

    public void Activate(Transform target, Vector2 dir, Sprite sprite, float visualScale, float forward, float damageValue, int maxHits = 999)
    {
        followTarget = target;
        facing = dir.normalized;
        damage = damageValue;
        maxHitsPerActivation = maxHits;
        hitsThisActivation = 0;

        // Sprite
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer && sprite != null) spriteRenderer.sprite = sprite;

        // scale: aplicamos escala local (respetando prefab base)
        transform.localScale = Vector3.one * visualScale;

        // forward offset
        forwardOffset = forward;

        // flipX en función de dir.x (no rotar en Y)
        if (spriteRenderer) spriteRenderer.flipX = (facing.x > 0f);

        // ordenar por encima del player si el player tiene SR (lo hará PlayerAutoAttack)
        activeFollowing = true;
        deactivateAt = Time.time + lifetime;

        // aseguramos collider trigger y rest
        if (hitCollider) hitCollider.isTrigger = true;
        gameObject.SetActive(true);
    }

    void Deactivate()
    {
        activeFollowing = false;
        // opción: ocultar (mejor desactivar para que no consuma)
        gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // si está desactivado lister no hace nada
        if (!activeFollowing) return;

        // chequea por componente Enemy
        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            // aplicar daño
            enemy.TakeDamage(damage);
            hitsThisActivation++;

            // (opcional) si quieres destruir al enemigo o disparar FX, hazlo aquí.
            if (hitsThisActivation >= maxHitsPerActivation)
            {
                // si quieres que desaparezca el slash al primer golpe, descomenta:
                // Deactivate();
            }
            return;
        }

        // alternativa: usar layer mask
        if (enemyMask != 0)
        {
            if (((1 << other.gameObject.layer) & enemyMask) != 0)
            {
                var e = other.GetComponentInParent<Enemy>();
                if (e != null) e.TakeDamage(damage);
            }
        }
    }
}
