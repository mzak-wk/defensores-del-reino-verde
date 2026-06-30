using System.Collections;
using UnityEngine;

/// <summary>
/// Area que hace daño por ticks (usar con CircleCollider2D isTrigger).
/// Si el prefab no tiene, el BossHowlController lo configura en runtime.
/// </summary>
public class HowlArea : MonoBehaviour
{
    [Header("Ajustes")]
    public float duration = 4f;
    public float tickInterval = 0.5f;
    public float damage = 20f;

    [Header("Targets")]
    public LayerMask playerLayerMask; // asignar LayerMask del Player
    public bool destroyProjectiles = true;
    public string projectileTag = "Projectile";

    //bool running = false;

    void OnEnable()
    {
        StartCoroutine(RunTicks());
    }

    IEnumerator RunTicks()
    {
        //running = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // daño por tick: solapamos por círculo
            ApplyTickDamage();

            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }

        //running = false;
    }

    void ApplyTickDamage()
    {
        float r = GetRadius();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, r, playerLayerMask);
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit == null) continue;

            // intentar BarraVida en el root o en padres
            var vida = hit.transform.root.GetComponentInChildren<BarraVida>();
            if (vida != null)
            {
                vida.RecibirDanio(damage);
            }
            else
            {
                // fallback por SendMessage
                hit.transform.root.SendMessage("RecibirDanio", damage, SendMessageOptions.DontRequireReceiver);
                hit.transform.root.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            }
        }

        // destruir proyectiles dentro del radio
        if (destroyProjectiles)
        {
            Collider2D[] proj = Physics2D.OverlapCircleAll(transform.position, r);
            for (int j = 0; j < proj.Length; j++)
            {
                var p = proj[j];
                if (p == null) continue;
                if (p.CompareTag(projectileTag))
                {
                    Destroy(p.gameObject);
                }
            }
        }
    }

    float GetRadius()
    {
        var cc = GetComponent<CircleCollider2D>();
        if (cc != null) return Mathf.Abs(cc.radius) * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        // si no hay collider, intentamos leer escala (no ideal)
        return 1f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawSphere(transform.position, GetRadius());
    }
}
