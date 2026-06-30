using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase : MonoBehaviour
{
    Transform target;
    Enemy enemy;
    Rigidbody2D rb;

    [Header("Multiplicadores de velocidad")]
    [Tooltip("Multiplicador que se aplica automáticamente a jefes (p. ej. 3 => x3).")]
    public float bossSpeedMultiplier = 3f;

    [Tooltip("Si está activo intentará detectar componentes con 'Boss' en su nombre y aplicar el multiplicador.")]
    public bool applyToBossesAutomatically = true;

    // multiplicador activo (1 = normal)
    float currentMultiplier = 1f;

    [HideInInspector] public SpawnZone originZone;
    [HideInInspector] public float leashDistance = 15f;
    bool isReturning = false;
    Vector2 returnTarget;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();

        if (enemy == null) Debug.LogError($"[EnemyChase] Falta componente Enemy en {name}.");
        if (rb == null) Debug.LogError($"[EnemyChase] Falta Rigidbody2D en {name}.");

        // Si queremos detectar jefes automáticamente, comprobar componentes cuyo nombre contenga "Boss"
        if (applyToBossesAutomatically && IsLikelyBoss())
        {
            currentMultiplier = Mathf.Max(0f, bossSpeedMultiplier);
            if (currentMultiplier <= 1f)
                currentMultiplier = 1f; // asegurar mínimo 1
        }
    }

    // Setea la target desde otros scripts
    public void SetTarget(Transform t) => target = t;

    void FixedUpdate()
    {
        if (!target || enemy == null || rb == null) return;

        if (originZone != null && leashDistance > 0)
        {
            Collider2D col = originZone.GetComponent<Collider2D>();
            if (col != null)
            {
                Vector2 closestPoint = col.ClosestPoint(rb.position);
                float distToZone = Vector2.Distance(rb.position, closestPoint);

                if (distToZone > leashDistance)
                {
                    if (!isReturning)
                    {
                        isReturning = true;
                        returnTarget = originZone.GetRandomPointInZone();
                    }
                }
            }
        }

        Vector2 moveTarget = isReturning ? returnTarget : (Vector2)target.position;
        Vector2 dir = (moveTarget - rb.position).normalized;
        float speed = enemy.moveSpeed * currentMultiplier;
        Vector2 next = rb.position + dir * speed * Time.fixedDeltaTime;
        rb.MovePosition(next);

        if (isReturning && Vector2.Distance(rb.position, returnTarget) < 0.5f)
        {
            isReturning = false;
        }
    }

    // Devuelve true si el GameObject tiene algún componente cuyo tipo contiene "Boss" (case-insensitive)
    bool IsLikelyBoss()
    {
        var mbs = GetComponents<MonoBehaviour>();
        foreach (var mb in mbs)
        {
            if (mb == null) continue;
            string tname = mb.GetType().Name.ToLowerInvariant();
            if (tname.Contains("boss")) return true;
        }
        return false;
    }

    // API pública: setear multiplicador permanente
    public void SetSpeedMultiplier(float multiplier)
    {
        currentMultiplier = Mathf.Max(0f, multiplier);
        if (currentMultiplier == 0f) currentMultiplier = 1f;
    }

    // API pública: setear multiplicador temporal (vuelve al anterior)
    public void SetTemporarySpeedMultiplier(float multiplier, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(TemporaryMultiplierCoroutine(multiplier, duration));
    }

    IEnumerator TemporaryMultiplierCoroutine(float multiplier, float duration)
    {
        float previous = currentMultiplier;
        currentMultiplier = Mathf.Max(0f, multiplier);
        if (currentMultiplier == 0f) currentMultiplier = 1f;
        yield return new WaitForSeconds(duration);
        currentMultiplier = previous;
    }

    public void ResetChaseState()
    {
        isReturning = false;
        originZone = null;
        leashDistance = 15f;
    }
}
