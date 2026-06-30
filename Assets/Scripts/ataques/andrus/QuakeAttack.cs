using UnityEngine;

public class QuakeAttack : MonoBehaviour
{
    // Estos se rellenan desde PlayerAutoAttack
    float damage;
    float radius;
    LayerMask enemyMask;
    float lifeTime;

    bool hasHit = false;

    /// <summary>
    /// Llamado justo después de instanciar el prefab.
    /// </summary>
    public void Init(float damage, float radius, LayerMask mask, float lifeTime)
    {
        this.damage    = damage;
        this.radius    = radius;
        this.enemyMask = mask;
        this.lifeTime  = lifeTime;

        DoHit();                       // hace daño 1 vez al crearse
        Destroy(gameObject, lifeTime); // destruye el sprite pasado un tiempo
    }

    void DoHit()
    {
        if (hasHit) return; // por si acaso
        hasHit = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyMask);

        foreach (var col in hits)
        {
            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                // Usa tu función actual de daño
                enemy.TakeDamage(damage);
                // Debug opcional:
                // Debug.Log($"[Quake] Golpeó a {enemy.name} por {damage}");
            }
        }
    }

    // Solo para ver el círculo en escena
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}