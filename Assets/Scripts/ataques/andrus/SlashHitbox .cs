using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SlashHitbox : MonoBehaviour
{
    public float damage = 50f;
    public float lifeTime = 0.25f;
    public LayerMask enemyMask; // pon aquí la layer de enemigos

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        if (lifeTime > 0f)
            Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Filtro por layer (si está configurado)
        if (enemyMask != 0 && ((1 << other.gameObject.layer) & enemyMask) == 0)
            return;

        Enemy e = other.GetComponentInParent<Enemy>();
        if (e != null)
        {
            e.TakeDamage(damage);
        }
    }
}
