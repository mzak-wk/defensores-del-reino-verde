using UnityEngine;

public class ArcherAutoAttack : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject arrowPrefab;
    public GameObject tornadoPrefab;   // prefab del tornado (nivel 10)

    [Header("Stats base (nivel 1)")]
    public float baseDamage = 150f;
    public float baseCooldown = 2f;
    public float baseProjectileSpeed = 8f;
    public float baseRange = 30f;

    [Header("Stats actuales (calculados por nivel)")]
    public float currentDamage;
    public float currentCooldown;
    public float currentProjectileSpeed;
    public float currentRange;

    [Header("Nivel (1–15)")]
    [SerializeField] int level = 1;

    [Header("Dirección de mira")]
    public Vector2 currentAimDir = Vector2.right; // por defecto derecha

    [Header("Flecha perforante (nivel 5+)")]
    public int piercingEveryNShots = 2;   // cada 2 disparos una perforante
    int shotsCounter = 0;

    [Header("Escalado global a nivel 15")]
    public float maxDamageMultiplierAt15 = 22.5f;
    public float minCooldownMultiplierAt15 = 0.24f;
    public float maxProjectileSpeedMultiplierAt15 = 2.5f;
    public float maxRangeMultiplierAt15 = 2f;

    [Header("Tornado (nivel 10)")]
    public float tornadoInterval = 2.5f;
    public float tornadoLifeTime = 5f;
    public float tornadoDamageFactor = 2f;  // 2x daño de la flecha

    float nextShootTime = 0f;
    float nextTornadoTime = 0f;

    void Awake()
    {
        RecalculateLevelBonuses();
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Clamp(newLevel, 1, 15);
        RecalculateLevelBonuses();
    }

    void RecalculateLevelBonuses()
    {
        float t = Mathf.InverseLerp(1, 15, level);

        float dmgMul = Mathf.Lerp(1f, maxDamageMultiplierAt15, t);
        float cdMul  = Mathf.Lerp(1f, minCooldownMultiplierAt15, t);
        float spdMul = Mathf.Lerp(1f, maxProjectileSpeedMultiplierAt15, t);
        float rngMul = Mathf.Lerp(1f, maxRangeMultiplierAt15, t);

        currentDamage          = baseDamage * dmgMul;
        currentCooldown        = baseCooldown * cdMul;
        currentProjectileSpeed = baseProjectileSpeed * spdMul;
        currentRange           = baseRange * rngMul;

        Debug.Log($"[Archer] Lvl {level} | DMG {currentDamage:F1} | SPD {currentProjectileSpeed:F1} | CD {currentCooldown:F2} | RNG {currentRange:F1}");
    }

    void Update()
    {
        ActualizarDireccionDeMira();
        TryShoot();
        TrySpawnTornado();
    }

    void ActualizarDireccionDeMira()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 input = new Vector2(x, y);

        if (input.sqrMagnitude > 0.01f)
        {
            input = input.normalized;
            currentAimDir = Quantizar8Direcciones(input);
        }
    }

    Vector2 Quantizar8Direcciones(Vector2 dir)
    {
        Vector2[] dirs =
        {
            new Vector2( 1,  0),
            new Vector2(-1,  0),
            new Vector2( 0,  1),
            new Vector2( 0, -1),
            new Vector2( 1,  1).normalized,
            new Vector2( 1, -1).normalized,
            new Vector2(-1,  1).normalized,
            new Vector2(-1, -1).normalized
        };

        float bestDot = -Mathf.Infinity;
        Vector2 best = Vector2.right;

        foreach (var d in dirs)
        {
            float dot = Vector2.Dot(dir, d);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = d;
            }
        }
        return best;
    }

    void TryShoot()
    {
        if (!arrowPrefab) return;
        if (Time.time < nextShootTime) return;
        if (currentAimDir.sqrMagnitude < 0.01f) return;

        nextShootTime = Time.time + currentCooldown;
        FireArrow(currentAimDir);
    }

    void FireArrow(Vector2 dir)
    {
        Vector3 spawnPos = transform.position + (Vector3)(dir * 0.3f);
        GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);

        bool isPiercing = false;

        // Nivel 10+: siempre perforante (100%)
        if (level >= 10)
        {
            isPiercing = true;
        }
        // Nivel 5-9: cada N disparos es perforante
        else if (level >= 5)
        {
            shotsCounter++;
            if (shotsCounter >= Mathf.Max(1, piercingEveryNShots))
            {
                isPiercing = true;
                shotsCounter = 0;
            }
        }

        var proj = arrow.GetComponent<ArrowProjectile>();
        if (proj != null)
        {
            proj.enemyMask = LayerMask.GetMask("Enemy");  // ajusta si usas otra capa
            proj.Init(
                dir,
                currentDamage,
                currentProjectileSpeed,
                isPiercing      // windTrailEnabled -> perforante
            );
        }

        // Rotación visual de la flecha
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.AngleAxis(ang, Vector3.forward);
    }

    void TrySpawnTornado()
    {
        if (level < 10) return;
        if (!tornadoPrefab) return;
        if (Time.time < nextTornadoTime) return;

        Enemy[] enemies = UnityEngine.Object.FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        if (enemies.Length == 0) return;

        nextTornadoTime = Time.time + tornadoInterval;

        Enemy randomEnemy = enemies[Random.Range(0, enemies.Length)];
        if (!randomEnemy) return;

        GameObject t = Instantiate(tornadoPrefab, randomEnemy.transform.position, Quaternion.identity);

        var area = t.GetComponent<TornadoAreaDamage>();
        if (area != null)
        {
            area.damagePerSecond = currentDamage * tornadoDamageFactor;
            area.lifeTime = tornadoLifeTime;
        }

        Debug.Log($"[Archer] Tornado spawn en {randomEnemy.name}, DPS={currentDamage * tornadoDamageFactor}");
    }
}
