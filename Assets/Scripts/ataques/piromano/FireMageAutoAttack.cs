using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireMageAutoAttack : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject fireballPrefab;
    public GameObject fireAreaPrefab;    // prefab del área de fuego (nivel 10+)

    [Header("Stats base (nivel 1)")]
    public float baseDamage = 75f;
    public float baseCooldown = 0.9f;
    public float baseProjectileSpeed = 10f;
    public float baseRange = 20f;

    [Header("Stats actuales (se recalculan por nivel)")]
    public float currentDamage;
    public float currentCooldown;
    public float currentProjectileSpeed;
    public float currentRange;

    [Header("Nivel (1–15)")]
    [SerializeField] int level = 1;

    [Header("Colores de proyectiles")]
    public Color mainFireColor = new Color(1f, 0.9f, 0.3f, 1f);  // amarillo
    public Color extraFireColor1 = new Color(1f, 0.3f, 0.2f, 1f); // rojo
    public Color extraFireColor2 = new Color(0.3f, 0.6f, 1f, 1f); // azul

    [Header("Fire Area (lvl 10+)")]
    [Tooltip("Duración base del área de fuego en segundos (antes de escalado por nivel).")]
    public float baseAreaDuration = 5f;
    [Tooltip("Escala base del área de fuego.")]
    public float baseAreaScale = 1.5f;

    float nextFireTime;

    void Awake()
    {
        RecalculateLevelBonuses();
    }

    // Llamado por tu sistema de experiencia / AttackLevelSync
    public void SetLevel(int newLevel)
    {
        level = Mathf.Clamp(newLevel, 1, 15);
        RecalculateLevelBonuses();
    }

    // -------- BALANCEO DE NIVELES --------
    void RecalculateLevelBonuses()
    {
        float dmgMul = 1f;
        float rangeMul = 1f;
        float projMul = 1f;
        float cdMul = 1f;

        // EARLY GAME 2–4 (sube daño y algo de rango/velocidad)
        if (level >= 2) { dmgMul *= 1.6f; cdMul *= 0.9f; rangeMul *= 1.1f; }
        if (level >= 3) { dmgMul *= 1.3f; projMul *= 1.1f; }
        if (level >= 4) { dmgMul *= 1.3f; cdMul *= 0.85f; }

        // POWER SPIKE LVL 5 (doble + 1 bola → 3 en total)
        if (level >= 5) { dmgMul *= 1.4f; cdMul *= 0.9f; rangeMul *= 1.1f; projMul *= 1.1f;}

        // MID GAME 6–9
        if (level >= 6) { dmgMul *= 1.3f; }
        if (level >= 7) { dmgMul *= 1.25f; projMul *= 1.1f; }
        if (level >= 8) { dmgMul *= 1.2f; cdMul *= 0.9f; }
        if (level >= 9) { dmgMul *= 1.2f; rangeMul *= 1.1f; }

        // LVL 10: desbloquea áreas y pega bastante fuerte
        if (level >= 10) { dmgMul *= 1.2f; cdMul *= 0.9f; rangeMul *= 1.1f; }

        // LATE GAME 11–15 (locura progresiva)
        if (level >= 11) { dmgMul *= 1.2f; projMul *= 1.2f;}
        if (level >= 12) { dmgMul *= 1.25f; }
        if (level >= 13) { dmgMul *= 1.25f; }
        if (level >= 14) { dmgMul *= 1.15f; }
        if (level >= 15) { dmgMul *= 1.1f; }

        currentDamage            = baseDamage * dmgMul;
        currentCooldown          = baseCooldown * cdMul;
        currentProjectileSpeed   = baseProjectileSpeed * projMul;
        currentRange             = baseRange * rangeMul;

        Debug.Log($"[FireMage] Lvl {level} | DMG {currentDamage:F1} | RNG {currentRange:F1} | SPD {currentProjectileSpeed:F1} | CD {currentCooldown:F2}");
    }

    void Update()
    {
        TryShoot();
    }

    // -------- DISPARO AUTOMÁTICO --------
    void TryShoot()
    {
        if (Time.time < nextFireTime) return;

        List<Transform> targets = FindVisibleEnemiesInRange();
        if (targets.Count == 0) return;

        nextFireTime = Time.time + currentCooldown;

        // a partir de lvl 5: 3 bolas (amarilla, roja, azul)
        int fireballCount = (level >= 5) ? 3 : 1;

        for (int i = 0; i < fireballCount; i++)
        {
            // objetivo: los 3 más cercanos, o repetir el primero si no hay tantos
            Transform target = targets[Mathf.Min(i, targets.Count - 1)];

            Color projColor = mainFireColor;
            if (i == 1) projColor = extraFireColor1;
            else if (i == 2) projColor = extraFireColor2;

            // pequeño delay para que se noten las 3
            float delay = (level >= 5) ? i * 0.07f : 0f;
            StartCoroutine(SpawnFireballDelayed(target, projColor, delay));
        }
    }

    IEnumerator SpawnFireballDelayed(Transform target, Color projColor, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (!target) yield break;
        SpawnConfiguredFireball(target, projColor);
    }

    // Configura TODO para la fireball (daño, área, colores…)
    void SpawnConfiguredFireball(Transform target, Color projColor)
    {
        if (!fireballPrefab || target == null) return;

        GameObject go = Instantiate(fireballPrefab, transform.position, Quaternion.identity);

        bool spawnArea = (level >= 10);

        // -------- parámetros del área de daño --------
        float areaDamagePerSecond = 0f;
        float areaDuration = baseAreaDuration;
        float areaScale = baseAreaScale;

        if (spawnArea)
        {
            // El área aporta ~30–50% del DPS según nivel
            float aoeFactor;
            if (level < 13) aoeFactor = 0.4f; // lvl 10–12
            else            aoeFactor = 0.6f;  // lvl 13–15

            areaDamagePerSecond = currentDamage * aoeFactor;
            areaDuration = baseAreaDuration + Mathf.Max(0, level - 10) * 0.4f; // un poco más larga en niveles altos
            areaScale = baseAreaScale + Mathf.Max(0, level - 10) * 0.3f;       // área más grande
        }

        var fb = go.GetComponent<FireballProjectile>();
        if (fb != null)
        {
            fb.Init(
                target,
                currentDamage,
                currentProjectileSpeed,
                spawnArea,
                fireAreaPrefab,
                areaDamagePerSecond,
                areaDuration,
                areaScale,
                projColor,          // color del proyectil
                projColor           // color del área (mismo tono que la bola)
            );
        }
    }

    // -------- BÚSQUEDA DE ENEMIGOS --------
    List<Transform> FindVisibleEnemiesInRange()
    {
        Camera cam = Camera.main;
        List<Transform> result = new List<Transform>();
        if (!cam) return result;

        Enemy[] enemies = UnityEngine.Object.FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        Vector3 origin = transform.position;
        float maxDistSq = currentRange * currentRange;

        foreach (var e in enemies)
        {
            if (!e) continue;
            Vector3 pos = e.transform.position;

            // ¿está en pantalla?
            Vector3 vp = cam.WorldToViewportPoint(pos);
            if (vp.z < 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
                continue;

            float dSq = (pos - origin).sqrMagnitude;
            if (dSq <= maxDistSq)
                result.Add(e.transform);
        }

        // ordenar por distancia (más cercanos primero)
        result.Sort((a, b) =>
        {
            float da = (a.position - origin).sqrMagnitude;
            float db = (b.position - origin).sqrMagnitude;
            return da.CompareTo(db);
        });

        return result;
    }
}
