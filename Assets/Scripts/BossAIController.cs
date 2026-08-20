using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class BossAIController : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private BossStatsOverride statsOverride;

    [Header("Referencias (auto-detectadas)")]
    [SerializeField] private EnemyChase enemyChase;
    [SerializeField] private Animator animator;
    [SerializeField] private string idleAnimName = "running";

    private BossHealth bossHealth;
    private Rigidbody2D bossRb;
    private Transform player;
    private bool isActive;
    private bool isCasting;
    private float[] cooldownTimers;
    private Coroutine attackLoopCoroutine;

    public BossStatsOverride StatsOverride => statsOverride;

    void Awake()
    {
        if (enemyChase == null) enemyChase = GetComponent<EnemyChase>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        bossHealth = GetComponent<BossHealth>();
        bossRb = GetComponent<Rigidbody2D>();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Start()
    {
        // Congelar IA desde el frame 0 del nivel
        Deactivate();
        Debug.Log("[BossAIController] IA congelada desde el inicio del nivel.");
    }

    public void Activate()
    {
        isActive = true;
        isCasting = false;

        if (enemyChase != null)
            enemyChase.SetMovementSuspended(false);

        if (statsOverride != null)
        {
            ApplyStatsOverride();
        }

        // Configurar física para combate: detección de colisiones continua y rotación bloqueada
        if (bossRb != null)
        {
            bossRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            bossRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            bossRb.mass = 100000f;
            bossRb.simulated = true;
            bossRb.linearVelocity = Vector2.zero;
        }

        if (statsOverride != null && statsOverride.attacks != null)
        {
            cooldownTimers = new float[statsOverride.attacks.Length];
        }

        attackLoopCoroutine = StartCoroutine(AttackLoop());
    }

    /// <summary>
    /// Inicio oficial de la batalla: reactiva el GameObject (el boss se guarda
    /// inactivo como placeholder durante la cinemática) y arranca el ciclo de IA.
    /// Activar el objeto ANTES de Activate() es obligatorio, porque StartCoroutine
    /// falla en GameObjects inactivos.
    /// </summary>
    public void StartBattle()
    {
        gameObject.SetActive(true);
        Activate();
    }

    public void Deactivate()
    {
        isActive = false;

        if (attackLoopCoroutine != null)
        {
            StopCoroutine(attackLoopCoroutine);
            attackLoopCoroutine = null;
        }

        if (enemyChase != null)
            enemyChase.enabled = false;

        if (bossRb != null)
        {
            bossRb.simulated = true;
            bossRb.linearVelocity = Vector2.zero;
        }
    }

    public void SetStatsOverride(BossStatsOverride newStats)
    {
        statsOverride = newStats;
    }

    protected void ApplyStatsOverride()
    {
        if (bossHealth != null)
        {
            bossHealth.SetHP(statsOverride.maxHP);
            bossHealth.contactDamage = statsOverride.contactDamage;
        }

        if (enemyChase != null)
        {
            enemyChase.SetSpeedMultiplier(statsOverride.moveSpeed / Mathf.Max(0.1f, 1.5f));
        }
    }

    private IEnumerator AttackLoop()
    {
        while (isActive)
        {
            yield return new WaitForSeconds(0.5f);

            if (isCasting) continue;
            if (player == null) continue;
            if (statsOverride == null || statsOverride.attacks == null || statsOverride.attacks.Length == 0) continue;

            int chosenIndex = SelectWeightedAttack();
            if (chosenIndex < 0) continue;

            BossAttackData chosenAttack = statsOverride.attacks[chosenIndex];

            if (cooldownTimers[chosenIndex] > 0f) continue;

            float dist = Vector2.Distance(transform.position, player.position);
            if (dist > 10f) continue;

            yield return StartCoroutine(ExecuteAttack(chosenAttack, chosenIndex));
        }
    }

    private int SelectWeightedAttack()
    {
        var available = new List<int>();
        float totalWeight = 0f;

        for (int i = 0; i < statsOverride.attacks.Length; i++)
        {
            if (cooldownTimers[i] <= 0f)
            {
                available.Add(i);
                totalWeight += statsOverride.attacks[i].weight;
            }
        }

        if (available.Count == 0) return -1;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (int idx in available)
        {
            cumulative += statsOverride.attacks[idx].weight;
            if (roll <= cumulative) return idx;
        }

        return available[available.Count - 1];
    }

    private IEnumerator ExecuteAttack(BossAttackData attack, int attackIndex)
    {
        isCasting = true;

        FreezeMovement();

        if (attack.sfx != null)
            AudioSource.PlayClipAtPoint(attack.sfx, transform.position);

        GameObject telegraph = null;
        if (attack.telegraphPrefab != null)
        {
            telegraph = Instantiate(attack.telegraphPrefab, transform.position, Quaternion.identity);
        }

        if (animator != null && !string.IsNullOrEmpty(attack.attackName))
            animator.Play(attack.attackName);

        yield return new WaitForSeconds(attack.castTime);

        if (telegraph != null)
            Destroy(telegraph);

        if (attack.attackPrefab != null)
        {
            GameObject go = Instantiate(attack.attackPrefab, transform.position, Quaternion.identity);

            var comp = go.GetComponent<MonoBehaviour>();
            if (comp != null)
            {
                MethodInfo mi = null;

                foreach (var m in comp.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (m.Name == "Execute")
                    {
                        var pars = m.GetParameters();
                        if (pars.Length == 3 &&
                            pars[0].ParameterType == typeof(Transform) &&
                            pars[1].ParameterType == typeof(Transform) &&
                            pars[2].ParameterType == typeof(Vector3))
                        {
                            mi = m;
                            break;
                        }
                    }
                }

                if (mi != null)
                {
                    IEnumerator exec = (IEnumerator)mi.Invoke(comp, new object[] { transform, player, attack.spawnScale });
                    if (exec != null)
                        yield return StartCoroutine(exec);
                }
                else
                {
                    Debug.LogWarning($"[BossAIController] Attack prefab {attack.attackName} does not implement Execute(Transform, Transform, Vector3).");
                    Destroy(go, 5f);
                }
            }
            else
            {
                Destroy(go, 5f);
            }
        }

        cooldownTimers[attackIndex] = attack.cooldown;
        StartCoroutine(CooldownTick(attackIndex, attack.cooldown));

        RestoreMovement();

        if (animator != null && !string.IsNullOrEmpty(idleAnimName))
            animator.Play(idleAnimName);

        isCasting = false;
    }

    private IEnumerator CooldownTick(int index, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cooldownTimers[index] = Mathf.Max(0f, duration - elapsed);
            yield return null;
        }
        cooldownTimers[index] = 0f;
    }

    private void FreezeMovement()
    {
        if (enemyChase != null) enemyChase.SetMovementSuspended(true);

        if (bossRb != null)
        {
            bossRb.linearVelocity = Vector2.zero;
        }
    }

    private void RestoreMovement()
    {
        if (enemyChase != null) enemyChase.SetMovementSuspended(false);

        if (bossRb != null)
        {
            bossRb.simulated = true;
            bossRb.linearVelocity = Vector2.zero;
        }
    }

    void OnDestroy()
    {
        if (attackLoopCoroutine != null)
            StopCoroutine(attackLoopCoroutine);
    }
}
