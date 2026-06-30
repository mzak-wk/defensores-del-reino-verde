using System.Collections;
using System.Collections.Generic; // <--- Necesario para List<>
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BossWhiteMage2 : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public EnemyChase enemyChase;

    [Header("Timing")]
    public float attackInterval = 2f;
    public bool startAttackingOnStart = true;

    [Header("Rain (lluvia)")]
    public GameObject rainTelegraphPrefab;
    public GameObject rainPrefab;
    public float rainDelay = 1.0f;
    public float rainDuration = 4.0f;
    public float rainDamage = 30f;
    // --- Variables Nuevas para Lluvia Múltiple ---
    public int rainCount = 5;
    public float rainSpawnRadius = 3.0f;
    public float rainMinSeparation = 1.0f;
    // ---------------------------------------------

    [Header("Ray (rayo)")]
    public GameObject rayTelegraphPrefab;
    public GameObject rayPrefab;
    public float rayDelay = 1.0f;
    public float rayDuration = 2.0f;
    public float rayDamage = 80f;
    public float rayHeight = 2.0f;

    [Header("Aura (aura)")]
    public GameObject auraTelegraphPrefab;
    public GameObject auraPrefab;
    public float auraDelay = 0.7f;
    public float auraDuration = 2.0f;
    public float auraRadius = 3.0f;
    public float auraTelegraphRadius = 2.0f;
    public float auraDamage = 50f;

    [Header("Meteor (meteorito)")]
    public GameObject meteorTelegraphPrefab;
    public GameObject meteorPrefab;
    public int meteorCount = 2;
    public float meteorInterval = 2.0f;
    public float meteorDelay = 1.0f;
    public float meteorDamage = 70f;
    public float meteorSpawnHeight = 10f;

    [Header("Visual / Animador")]
    [Tooltip("Si asignas un Animator, se usará para las animaciones de casteo.")]
    public Animator animator;
    public string animatorCastingParam = "isCasting";
    [Tooltip("Sprite fallback si no hay Animator")]
    public Sprite castingSprite;

    SpriteRenderer spriteRenderer;
    Sprite originalSprite;

    bool isCasting = false;

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer) originalSprite = spriteRenderer.sprite;

        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!enemyChase) enemyChase = GetComponent<EnemyChase>();

#if UNITY_EDITOR
        if (animator != null)
        {
            bool hasParam = false;
            foreach (var p in animator.parameters)
            {
                if (p.name == animatorCastingParam) { hasParam = true; break; }
            }
            if (!hasParam)
                Debug.LogWarning($"[BossWhiteMage2] Animator asignado pero no tiene el parámetro '{animatorCastingParam}'.");
        }
#endif
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

            int roll = Random.Range(0, 100);
            if (roll < 30) yield return StartCoroutine(DoRain());
            else if (roll < 50) yield return StartCoroutine(DoRay());
            else if (roll < 70) yield return StartCoroutine(DoAura());
            else yield return StartCoroutine(DoMeteorSequence());
        }
    }

    // --------------------------------------------------------------------------
    // --- DoRain MODIFICADO (Lanza múltiples ataques dispersos) ---
    // --------------------------------------------------------------------------
    IEnumerator DoRain()
    {
        if (rainTelegraphPrefab == null || rainPrefab == null || player == null) yield break;

        isCasting = true;
        SetCastingState(true, true);

        Vector3 center = player.position;

        // 1. Calcular posiciones dispersas
        List<Vector3> rainTargets = new List<Vector3>();

        for (int i = 0; i < rainCount; i++)
        {
            // Intentar encontrar una posición separada (max 50 intentos)
            for (int j = 0; j < 50; j++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * rainSpawnRadius;
                Vector3 target = center + new Vector3(randomCircle.x, randomCircle.y, 0f);

                bool isSeparated = true;
                foreach (Vector3 existingTarget in rainTargets)
                {
                    if (Vector3.Distance(target, existingTarget) < rainMinSeparation)
                    {
                        isSeparated = false;
                        break;
                    }
                }

                if (isSeparated)
                {
                    rainTargets.Add(target);
                    break;
                }
            }
        }

        // 2. Instanciar Telegrafos en esas posiciones
        List<GameObject> telegraphs = new List<GameObject>();
        foreach (Vector3 target in rainTargets)
        {
            var t = Instantiate(rainTelegraphPrefab, target, Quaternion.identity);
            SyncTelegraph(t, rainPrefab);
            telegraphs.Add(t);
        }

        // Esperar tiempo de aviso
        yield return new WaitForSeconds(rainDelay);

        // 3. Instanciar Ataques reales y borrar telegrafos
        foreach (GameObject t in telegraphs)
        {
            if (t)
            {
                Vector3 attackPos = t.transform.position;
                Destroy(t);

                var r = Instantiate(rainPrefab, attackPos, Quaternion.identity);
                EnsureAreaDamage(r, rainDamage);

                // Si el prefab de lluvia tiene duración en ImpactArea, se destruye solo.
                // Si no, asegúrate de destruirlo como hacías antes:
                if (rainDuration > 0) Destroy(r, rainDuration);
            }
        }

        // Esperamos la duración del ataque para que el boss no haga nada más mientras llueve
        yield return new WaitForSeconds(rainDuration);

        SetCastingState(false, false);
        isCasting = false;
    }

    // --------------------------------------------------------------------------
    // --- DoRay ORIGINAL (Sin cambios) ---
    // --------------------------------------------------------------------------
    IEnumerator DoRay()
    {
        if (rayTelegraphPrefab == null || rayPrefab == null || player == null) { yield break; }
        isCasting = true;
        SetCastingState(true, true);

        Vector3 dir = (player.position - transform.position).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = transform.right;

        Vector3 off;
        if (Camera.main)
        {
            float h = Camera.main.orthographicSize * 2f;
            float w = h * Camera.main.aspect;
            off = transform.position + dir * (Mathf.Max(w, h) * 1.2f);
        }
        else off = transform.position + dir * 30f;

        Vector3 center = (transform.position + off) * 0.5f;
        float dist = Vector3.Distance(transform.position, off);

        GameObject t = Instantiate(rayTelegraphPrefab, center, Quaternion.identity);
        t.transform.right = dir;
        t.transform.localScale = new Vector3(dist, rayHeight, 1f);
        SyncTelegraph(t, rayPrefab);

        yield return new WaitForSeconds(rayDelay);

        if (t) Destroy(t);
        GameObject r = Instantiate(rayPrefab, center, Quaternion.identity);
        r.transform.right = dir;
        r.transform.localScale = new Vector3(dist, rayHeight, 1f);
        EnsureAreaDamage(r, rayDamage);

        yield return new WaitForSeconds(rayDuration);
        if (r) Destroy(r);

        SetCastingState(false, false);
        isCasting = false;
    }

    // --------------------------------------------------------------------------
    // --- DoAura ORIGINAL (Sin cambios) ---
    // --------------------------------------------------------------------------
    IEnumerator DoAura()
    {
        if (auraTelegraphPrefab == null || auraPrefab == null) yield break;
        isCasting = true;
        SetCastingState(true, true);

        Vector3 center = transform.position;
        GameObject t = Instantiate(auraTelegraphPrefab, center, Quaternion.identity);
        float telegraphScale = auraTelegraphRadius * 2f;
        t.transform.localScale = new Vector3(telegraphScale, telegraphScale, 1f);
        SyncTelegraph(t, auraPrefab);

        yield return new WaitForSeconds(auraDelay);

        if (t) Destroy(t);
        GameObject a = Instantiate(auraPrefab, center, Quaternion.identity);
        float auraScale = auraRadius * 2f;
        a.transform.localScale = new Vector3(auraScale, auraScale, 1f);
        EnsureAreaDamage(a, auraDamage);

        yield return new WaitForSeconds(auraDuration);
        if (a) Destroy(a);

        SetCastingState(false, false);
        isCasting = false;
    }

    // --------------------------------------------------------------------------
    // --- DoMeteorSequence ORIGINAL (Sin cambios) ---
    // --------------------------------------------------------------------------
    IEnumerator DoMeteorSequence()
    {
        if (meteorTelegraphPrefab == null || meteorPrefab == null || player == null) yield break;
        isCasting = true;

        SetCastingState(true, true);
        yield return new WaitForSeconds(1f);
        SetCastingState(false, false);

        for (int i = 0; i < meteorCount; i++)
        {
            Vector3 target = player.position;
            GameObject t = Instantiate(meteorTelegraphPrefab, target, Quaternion.identity);
            SyncTelegraph(t, meteorPrefab);

            yield return new WaitForSeconds(meteorDelay);

            if (t) Destroy(t);
            Vector3 spawn = target + Vector3.up * meteorSpawnHeight;
            GameObject m = Instantiate(meteorPrefab, spawn, Quaternion.identity);
            EnsureAreaDamage(m, meteorDamage);

            StartCoroutine(FallAndDestroy(m, target, 0.9f));
            yield return new WaitForSeconds(meteorInterval);
        }

        isCasting = false;
    }

    // --------------------------------------------------------------------------
    // --- HELPER METHODS ORIGINALES ---
    // --------------------------------------------------------------------------

    IEnumerator FallAndDestroy(GameObject m, Vector3 target, float timeToFall)
    {
        if (m == null) yield break;
        Vector3 start = m.transform.position;
        float elapsed = 0f;
        while (elapsed < timeToFall && m != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / timeToFall);
            m.transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
        yield return new WaitForSeconds(0.25f);
        if (m) Destroy(m);
    }

    void EnsureAreaDamage(GameObject go, float dmg)
    {
        if (go == null) return;
        var ad = go.GetComponent<AreaDamage>();
        if (ad == null) ad = go.AddComponent<AreaDamage>();
        ad.damage = dmg;
        ad.destroyOnHit = false;
    }

    void SyncTelegraph(GameObject tele, GameObject attackPrefab)
    {
        if (tele == null || attackPrefab == null) return;
        var attackSR = attackPrefab.GetComponentInChildren<SpriteRenderer>();
        var tSync = tele.GetComponent<TelegraphSync>();
        if (tSync != null) tSync.SyncToPrefab(attackSR);
    }

    void SetCastingState(bool on, bool freezeMovement)
    {
        if (animator != null)
        {
            animator.SetBool(animatorCastingParam, on);
        }
        else
        {
            if (spriteRenderer)
            {
                spriteRenderer.sprite = on && castingSprite != null ? castingSprite : originalSprite;
            }
        }

        if (enemyChase) enemyChase.enabled = !freezeMovement;
    }

    public void OnCastStart() { }
    public void OnCastEnd() { }
}