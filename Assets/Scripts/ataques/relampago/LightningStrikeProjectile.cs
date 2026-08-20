using System.Collections;
using UnityEngine;

/// <summary>
/// Lightning Ring (Anillo de Rayos).
/// Se instancia en la coordenada del enemigo-centro seleccionada por el controlador.
/// Todo el daño se aplica en el Frame 0 (100% directo al centro + 50% a todos en el área);
/// la animación es puramente visual y su duración se escala con scaledStats.duration.
/// </summary>
public class LightningStrikeProjectile : MonoBehaviour
{
    [Header("Datageo de área")]
    [Tooltip("Radio base del círculo de impacto. Se multiplica por scaledStats.areaSize.")]
    [SerializeField, Min(0f)] private float baseRadius = 1.5f;

    [Tooltip("Multiplicador del daño secundario aplicado a todos los enemigos dentro del área.")]
    [SerializeField, Range(0f, 1f)] private float secondaryMultiplier = 0.5f;

    // Llenadas en Init() / Awake().
    private LayerMask enemyLayerMask;
    private float primaryDamage;
    private float impactRadius;

    private void Awake()
    {
        enemyLayerMask = LayerMask.GetMask("Enemy");
        if (enemyLayerMask.value == 0)
            enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    /// <summary>
    /// Llamado justo después de instanciar el prefab en la posición del enemigo-centro.
    /// Ejecuta Fase 2 (daño directo), Fase 3 (daño de área) y Fase 4 (animación + Destroy).
    /// </summary>
    public void Init(Enemy center, BaseStats scaledStats)
    {
        primaryDamage = scaledStats.damage;
        impactRadius = baseRadius * Mathf.Max(0f, scaledStats.areaSize);

        ApplyAreaDamage();       // Fase 3: todos los enemigos en el círculo, incluido el centro, reciben 50%.
        ApplyDirectDamage(center); // Fase 2: el centro recibe además el 100%.
        StartCoroutine(AnimateAndDestroy(scaledStats.duration));
    }

    // Fase 3 - Daño de área (Pierce infinito, aplicado por código en Frame 0).
    private void ApplyAreaDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, impactRadius, enemyLayerMask);
        float secondaryDamage = primaryDamage * secondaryMultiplier;

        foreach (var col in hits)
        {
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue; // ignora enemigos muertos/apiscados
            enemy.TakeDamage(secondaryDamage);
        }
    }

    // Fase 2 - Daño directo al enemigo-centro (Pierce 1, por referencia).
    private void ApplyDirectDamage(Enemy center)
    {
        if (center == null || !center.gameObject.activeInHierarchy) return;
        center.TakeDamage(primaryDamage);
    }

    // Fase 4 - Alarga/encoge la animación según scaledStats.duration y destruye el objeto al terminar.
    private IEnumerator AnimateAndDestroy(float duration)
    {
        duration = Mathf.Max(0.001f, duration);

        Animator anim = GetComponent<Animator>();
        float clipLength = anim != null ? anim.GetCurrentAnimatorStateInfo(0).length : 1f;

        // duration == 1 -> velocidad normal. duration == 2 -> animación el doble de lenta.
        if (anim != null)
            anim.speed = 1f / duration;

        yield return new WaitForSeconds(clipLength * duration);
        Destroy(gameObject);
    }

    // Solo para depurar el círculo en el Editor.
    private void OnDrawGizmosSelected()
    {
        float r = impactRadius > 0f ? impactRadius : baseRadius;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, r);
    }
}