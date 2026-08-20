using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private Transform target;

    [Header("Multiplicadores de velocidad")]
    public float bossSpeedMultiplier = 3f;
    public bool applyToBossesAutomatically = true;

    [Header("Pausa táctica del jefe")]
    [SerializeField] private float bossContactRange = 2f;
    [SerializeField] private float bossPauseDuration = 1.75f;
    [SerializeField] private float bossPauseRearmDistance = 2.5f;

    Enemy enemy;
    Rigidbody2D rb;
    float currentMultiplier = 1f;
    bool isBoss;
    bool movementSuspended;
    bool bossPauseArmed = true;
    float bossPauseUntil;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();

        if (enemy == null) Debug.LogError($"[EnemyChase] Falta Enemy en {name}.");
        if (rb == null) Debug.LogError($"[EnemyChase] Falta Rigidbody2D en {name}.");

        isBoss = IsLikelyBoss();
        if (isBoss)
        {
            if (applyToBossesAutomatically)
                currentMultiplier = Mathf.Max(1f, bossSpeedMultiplier);

            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            enabled = false;
        }
    }

    public void SetTarget(Transform t) => target = t;

    void FixedUpdate()
    {
        if (rb == null)
            return;

        if (!enabled || movementSuspended || rb.bodyType == RigidbodyType2D.Kinematic)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (enemy == null) return;

        // IA suspendida por el Vórtice de Captura: no perseguir al jugador.
        // Doble seguro (además del Kinematic en SetTrapped) para que la IA
        // nunca vuelva a escribir velocidad en el Rigidbody mientras esté atrapado.
        if (enemy.IsTrapped)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distanceToTarget = Vector2.Distance(rb.position, target.position);
        if (isBoss)
        {
            if (!bossPauseArmed && distanceToTarget >= bossPauseRearmDistance)
                bossPauseArmed = true;

            if (Time.time < bossPauseUntil)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            if (bossPauseArmed && distanceToTarget <= bossContactRange)
            {
                bossPauseArmed = false;
                bossPauseUntil = Time.time + bossPauseDuration;
                rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        Vector2 direction = ((Vector2)target.position - rb.position).normalized;
        float speed = enemy.moveSpeed * currentMultiplier;
        rb.linearVelocity = direction * speed;
    }

    bool IsLikelyBoss()
    {
        if (GetComponent<BossAIController>() != null || GetComponent<BossHealth>() != null
            || GetComponentInParent<BossAIController>() != null || GetComponentInParent<BossHealth>() != null
            || GetComponentInChildren<BossAIController>() != null || GetComponentInChildren<BossHealth>() != null)
            return true;

        foreach (var mb in GetComponents<MonoBehaviour>())
        {
            if (mb != null && mb.GetType().Name.ToLowerInvariant().Contains("boss"))
                return true;
        }
        return false;
    }

    void OnDisable()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    public void SetMovementSuspended(bool suspended)
    {
        movementSuspended = suspended;
        if (suspended && rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        currentMultiplier = Mathf.Max(1f, multiplier);
    }

    public void SetTemporarySpeedMultiplier(float multiplier, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(TemporaryMultiplierCoroutine(multiplier, duration));
    }

    IEnumerator TemporaryMultiplierCoroutine(float multiplier, float duration)
    {
        float previous = currentMultiplier;
        currentMultiplier = Mathf.Max(1f, multiplier);
        yield return new WaitForSeconds(duration);
        currentMultiplier = previous;
    }
}
