using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BossSimpleMage: usa la misma lluvia que BossWhiteMage + rayo extra.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BossSimpleMage : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public EnemyChase enemyChase;

    [Header("Animación / Visual")]
    public Animator animator;
    public string animatorCastingBool = "isCasting";
    public Sprite castingSprite;
    SpriteRenderer spriteRenderer;
    Sprite originalSprite;

    [Header("Loop")]
    public float attackInterval = 2f;
    public bool startAttackingOnStart = true;
    [Range(0f, 1f)] public float rainProbability = 0.65f;

    [Header("Lluvia (lightAttack con ImpactArea)")]
    public GameObject rainTelegraphPrefab;   // TelegraRain
    public GameObject rainPrefab;            // ligthAttack
    public int rainSpawnCount = 6;
    public float rainSpawnRadius = 4f;
    public float rainDelay = 2f;             // tiempo de casteo
    public float rainOverrideDamage = -1f;   // si >0, usar esto
    public float rainOverrideDuration = -1f; // si >0, usar esto
    public LayerMask rainDamageLayerMask = ~0;

    [Header("Rayo (Beam)")]
    public GameObject rayTelegraphPrefab;
    public GameObject rayPrefab;
    public float rayDelay = 1.2f;
    public float rayHeight = 2f;
    public float rayDamage = 40f;
    public float rayDuration = 2f;
    public LayerMask rayDamageLayerMask = ~0;

    [Header("Fallback valores lluvia")]
    public float rainDamage = 30f;
    public float rainDuration = 4f;

    // Estado interno
    bool isCasting = false;
    Rigidbody2D bossRb;
    bool hadMovementEnabled = false;
    bool rbSimulatedWasTrue = true;

    void OnValidate()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (enemyChase == null) enemyChase = GetComponent<EnemyChase>();
    }

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer) originalSprite = spriteRenderer.sprite;

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        bossRb = GetComponent<Rigidbody2D>();

        if (!player)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (!enemyChase)
            enemyChase = GetComponent<EnemyChase>();
    }

    void Start()
    {
        if (startAttackingOnStart)
            StartCoroutine(AttackLoop());
    }

    IEnumerator AttackLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackInterval);
            if (isCasting) continue;

            float roll = Random.value;
            if (roll < rainProbability)
            {
                Debug.Log("[BossSimpleMage] Lanzando LLUVIA");
                yield return StartCoroutine(DoRain());
            }
            else
            {
                Debug.Log("[BossSimpleMage] Lanzando RAYO");
                yield return StartCoroutine(DoRay());
            }
        }
    }

    // =========================================================
    // LLUVIA - COPIA DEL PATRÓN DE BossWhiteMage
    // =========================================================
    IEnumerator DoRain()
    {
        if (rainTelegraphPrefab == null || rainPrefab == null)
        {
            Debug.LogWarning("[BossSimpleMage] Lluvia sin prefabs asignados.");
            yield break;
        }

        isCasting = true;
        FreezeMovement(true);
        SetVisualState(true);

        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        Vector3 center = player ? player.position : transform.position;

        // 1) Generar posiciones alrededor del player
        List<Vector2> positions = new List<Vector2>(rainSpawnCount);
        for (int i = 0; i < rainSpawnCount; i++)
        {
            float r = Random.Range(0f, rainSpawnRadius);
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector2 offset = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            Vector2 pos = (Vector2)center + offset;
            positions.Add(pos);

            // Instanciar telegraph
            GameObject mk = Instantiate(rainTelegraphPrefab, pos, Quaternion.identity);
            var tm = mk.GetComponent<TelegraphMarker>();
            if (tm != null)
                tm.SetLifetime(rainDelay + 0.2f);
        }

        Debug.Log($"[BossSimpleMage] Lluvia: creados {positions.Count} telegraphs.");

        // 2) Esperar tiempo de casteo
        yield return new WaitForSeconds(rainDelay);

        // 3) Crear impactos
        foreach (var pos in positions)
        {
            GameObject go = Instantiate(rainPrefab, pos, Quaternion.identity);
            Debug.Log($"[BossSimpleMage] Spawn impact prefab en {pos}");

            var ia = go.GetComponent<ImpactArea>();
            if (ia != null)
            {
                float dmg = (rainOverrideDamage > 0f) ? rainOverrideDamage : rainDamage;
                float dur = (rainOverrideDuration > 0f) ? rainOverrideDuration : rainDuration;

                ia.damage = dmg;
                ia.duration = dur;
                ia.damageLayerMask = rainDamageLayerMask;
            }
        }

        // 4) Esperar la duración “global” de la lluvia
        float waitDur = (rainOverrideDuration > 0f) ? rainOverrideDuration : rainDuration;
        yield return new WaitForSeconds(waitDur);

        SetVisualState(false);
        FreezeMovement(false);
        isCasting = false;
    }

    // =========================================================
    // RAYO - dejamos como estaba, solo ajustando layer mask
    // =========================================================
    IEnumerator DoRay()
    {
        if (rayTelegraphPrefab == null || rayPrefab == null || player == null)
            yield break;

        isCasting = true;
        FreezeMovement(true);
        SetVisualState(true);

        Vector3 dir = (player.position - transform.position).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = transform.right;

        Vector3 off;
        if (Camera.main)
        {
            float camHalfHeight = Camera.main.orthographicSize;
            float camHeight = camHalfHeight * 2f;
            float camWidth = camHeight * Camera.main.aspect;
            off = transform.position + dir * (Mathf.Max(camWidth, camHeight) * 1.2f);
        }
        else
        {
            off = transform.position + dir * 30f;
        }

        Vector3 center = (transform.position + off) * 0.5f;
        float dist = Vector3.Distance(transform.position, off);

        // telegraph
        GameObject t = Instantiate(rayTelegraphPrefab, center, Quaternion.identity);
        t.transform.right = dir;
        t.transform.localScale = new Vector3(dist, rayHeight, 1f);

        yield return new WaitForSeconds(rayDelay);
        if (t) Destroy(t);

        // beam real
        GameObject r = Instantiate(rayPrefab, center, Quaternion.identity);
        r.transform.right = dir;
        r.transform.localScale = new Vector3(dist, rayHeight, 1f);

        var ia = r.GetComponent<ImpactArea>();
        if (ia != null)
        {
            ia.damage = rayDamage;
            ia.duration = rayDuration;
            ia.damageLayerMask = rayDamageLayerMask;
        }

        yield return new WaitForSeconds(rayDuration);
        if (r) Destroy(r);

        SetVisualState(false);
        FreezeMovement(false);
        isCasting = false;
    }

    // =========================================================
    // Helpers de estado
    // =========================================================
    void FreezeMovement(bool freeze)
    {
        if (freeze)
        {
            hadMovementEnabled = (enemyChase != null) ? enemyChase.enabled : false;
            if (enemyChase != null) enemyChase.enabled = false;
        }
        else
        {
            if (enemyChase != null) enemyChase.enabled = hadMovementEnabled;
        }

        if (bossRb != null)
        {
            if (freeze)
            {
                rbSimulatedWasTrue = bossRb.simulated;
                bossRb.linearVelocity = Vector2.zero;
                bossRb.angularVelocity = 0f;
                bossRb.simulated = false;
            }
            else
            {
                bossRb.simulated = rbSimulatedWasTrue;
            }
        }
    }

    void SetVisualState(bool casting)
    {
        if (animator != null)
        {
            animator.SetBool(animatorCastingBool, casting);
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.sprite = (casting && castingSprite != null) ? castingSprite : originalSprite;
        }
    }
}
