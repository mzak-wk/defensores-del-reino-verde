using System.Collections;
using UnityEngine;

public class PlayerAutoAttack : MonoBehaviour
{
    [Header("Referencias")]
    public SpriteRenderer facingSprite;      // sprite del player (para saber izquierda/derecha)

    [Header("Prefabs")]
    [SerializeField] GameObject slashPrefab;
    [SerializeField] GameObject quakePrefab;

    [Header("Nivel actual (1–15)")]
    [SerializeField] int level = 1;

    // --- Stats base del SLASH (nivel 1) ---
    [Header("Slash - stats base (nivel 1)")]
    [SerializeField] float baseDamage   = 50f;
    [SerializeField] float baseScale    = 1f;
    [SerializeField] float baseForward  = 1.0f; // distancia delante del jugador
    [SerializeField] float baseCooldown = 1.0f; // tiempo entre slashes

    // --- Runtime: stats actuales del SLASH (dependen del nivel) ---
    [Header("Slash - stats actuales (solo lectura)")]
    [SerializeField] float slashDamage;
    [SerializeField] float slashScale;
    [SerializeField] float slashForward;
    [SerializeField] float slashCooldown;
    public float slashLifetime = 0.25f;

    // --- Doble Slash (nivel 5+) ---
    [Header("Double Slash (lvl 5)")]
    [SerializeField] float standStillVelocityThreshold = 0.05f;   // por debajo de esto lo consideramos quieto

    // --- Quake: factores relativos al slash ---
    [Header("Quake - factores relativos al slash")]
    [SerializeField] float quakeScaleFactor   = 2f;   // 2x tamaño
    [SerializeField] float quakeCDMultiplier  = 1.5f; // más lento que el slash

    [Header("Quake - mejora nivel 10+")]
    [SerializeField] float quakeLifeTimeLv10    = 2f;   // más duración en lvl 10+
    [SerializeField] float quakeShakeIntensity  = 0.05f;
    [SerializeField] float quakeShakeFrequency  = 30f;

    [Header("Quake - radio")]
    public float baseQuakeRadiusMul = 1f;
    public float currentQuakeRadiusMul = 1f;


    float nextSlashTime;
    float nextQuakeTime;

    void Awake()
    {
        if (!facingSprite)
            facingSprite = GetComponentInChildren<SpriteRenderer>();

        // Primera vez: que pueda disparar de inmediato
        nextSlashTime = Time.time;
        nextQuakeTime = Time.time;

        // inicializar stats para el nivel inicial
        RecalculateLevelBonuses();
    }

    bool IsMovingHorizontally()
    {
        // Usa el mismo eje que tu movimiento / SimpleWalkXAnim
        float x = Input.GetAxisRaw("Horizontal");
        return Mathf.Abs(x) > standStillVelocityThreshold;
    }

    /// <summary>
    /// Llamado por tu ExperienceSystem / AttackLevelSync cuando subes de nivel.
    /// </summary>
    public void SetLevel(int newLevel)
    {
        level = Mathf.Clamp(newLevel, 1, 15);
        RecalculateLevelBonuses();
    }

    void Update()
    {
        // SLASH automático
        if (Time.time >= nextSlashTime)
        {
            FireSlash();
            nextSlashTime = Time.time + slashCooldown;
        }

        // QUAKE: solo desde nivel 10
        if (level >= 10 && quakePrefab != null)
        {
            float quakeCD = slashCooldown * quakeCDMultiplier;

            if (Time.time >= nextQuakeTime)
            {
                FireQuake();
                nextQuakeTime = Time.time + quakeCD;
            }
        }
    }

    void TintSlash(GameObject go, Color color)
    {
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.color = color;
    }

    // Llama a esto siempre que cambie el nivel
    void RecalculateLevelBonuses()
    {
        // Multiplicadores acumulados
        float damageMul = 1f;   // afecta al daño del slash
        float rangeMul  = 1f;   // afecta tamaño/distancia del slash
        float speedMul  = 1f;   // cooldown (1 = igual, <1 = más rápido, >1 = más lento)

        // -----------------------------
        // EARLY GAME (2–4)
        // -----------------------------
        if (level >= 2)
        {
            damageMul *= 1.6f;
        }
        if (level >= 3)
        {
            damageMul *= 1.55f;
            speedMul  *= 0.8f;   // más rápido
        }
        if (level >= 4)
        {
            damageMul *= 1.5f;
        }

        // -----------------------------
        // POWER SPIKE NIVEL 5 – DOBLE SLASH
        // -----------------------------
        if (level >= 5)
        {
            damageMul *= 2f;
            speedMul  *= 0.80f;   // aún más rápido
        }

        // -----------------------------
        // MID GAME (6–9)
        // -----------------------------
        if (level >= 6)
        {
            damageMul *= 1.5f;
        }
        if (level >= 7)
        {
            damageMul *= 1.4f;
        }
        if (level >= 8)
        {
            damageMul *= 1.4f;
        }
        if (level >= 9)
        {
            damageMul *= 1.3f;
        }

        // -----------------------------
        // NIVEL 10 – PICO “HÉROE”
        // -----------------------------
        if (level >= 10)
        {
            damageMul *= 1.5f;
        }

        // -----------------------------
        // LATE GAME (11–15) – MODO SALVAJADA
        // -----------------------------
        if (level >= 11)
        {
            damageMul *= 1.25f;
        }
        if (level >= 12)
        {
            damageMul *= 1.25f;
        }
        if (level >= 13)
        {
            damageMul *= 1.25f;
        }
        if (level >= 14)
        {
            damageMul *= 1.2f;
        }
        if (level >= 15)
        {
            damageMul *= 1.2f;
        }

        // --- RANGO (solo visual) ---
        if (level >= 3)  rangeMul *= 1.20f;
        if (level >= 6)  rangeMul *= 1.20f;
        if (level >= 9)  rangeMul *= 1.20f;
        if (level >= 12) rangeMul *= 1.20f;
        if (level >= 15) rangeMul *= 1.20f;

        // Aplicar stats finales
        slashDamage   = baseDamage   * damageMul;
        slashScale    = baseScale    * rangeMul;
        slashForward  = baseForward  * rangeMul;
        slashCooldown = baseCooldown * speedMul;
        float tQuake = Mathf.InverseLerp(10f, 15f, level);           // 0 en lvl10, 1 en lvl15 (clamp interno)
        currentQuakeRadiusMul = 1f;
        
        if (level >= 11) currentQuakeRadiusMul = 1.1f;
        if (level >= 13) currentQuakeRadiusMul = 1.2f;
        if (level >= 15) currentQuakeRadiusMul = 1.3f;
    
        Debug.Log($"[PlayerAutoAttack] Lvl {level} | DMG {slashDamage:F1} | SCALE x{slashScale:F2} | FWD {slashForward:F2} | CD {slashCooldown:F2}");
    }

    // ---------------------------------------------------------
    //  SLASH: nivel 1–4 = 1 slash
    //         nivel 5+  = 2 slash (lógica según si se mueve o no)
    // ---------------------------------------------------------
    void FireSlash()
    {
        if (!slashPrefab) return;

        // Dirección de mirada (según sprite)
        bool lookLeft;
        if (facingSprite != null)
            lookLeft = facingSprite.flipX;
        else
            lookLeft = transform.localScale.x < 0f;

        // En tu lógica actual: si flipX = true, dispara a la derecha
        Vector2 facingDir = lookLeft ? Vector2.right : Vector2.left;

        // ¿Está moviéndose o quieto? (según input A/D)
        bool isMovingHoriz = IsMovingHorizontally();

        // Niveles 1–4: un solo slash hacia la dirección de mirada
        if (level < 5)
        {
            SpawnSlash(facingDir);
            return;
        }

        // Nivel 5+ : doble slash
        if (!isMovingHoriz)
        {
            // QUIETO → un slash a cada lado
            SpawnSlash(Vector2.right);
            SpawnSlash(Vector2.left);
        }
        else
        {
            // Primer slash (normal)
            SpawnSlash(facingDir);

            // Segundo slash (delay + color distinto)
            StartCoroutine(DelayedSecondSlash(facingDir));
        }
    }

    IEnumerator DelayedSecondSlash(Vector2 dir)
    {
        yield return new WaitForSeconds(0.1f); // ← Ajustable

        GameObject go = SpawnSlash(dir);

        // Cambia el color del segundo slash para diferenciarlo
        TintSlash(go, new Color(0.4f, 0.7f, 1f, 1f)); // azul más claro
    }

    // Spawnea un slash en una dirección concreta
    GameObject SpawnSlash(Vector2 dir)
    {
        Vector3 spawnPos = transform.position + (Vector3)(dir * slashForward);

        GameObject go = Instantiate(slashPrefab, spawnPos, Quaternion.identity);

        // Configurar daño
        var slash = go.GetComponent<SlashAttack>();
        if (slash != null)
        {
            slash.damage = slashDamage;
            slash.initialScaleMultiplier *= slashScale;
            slash.finalScaleMultiplier   *= slashScale;
            slash.forwardDistance       *= slashScale;
        }

        // Sprite mira hacia su dirección
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.flipX = (dir.x > 0f);

        return go;
    }

    // ---------------------------------------------------------
    //  EARTHQUAKE: se desbloquea a NIVEL 10
    // ---------------------------------------------------------
    void FireQuake()
    {
        if (!quakePrefab) return;

        // --- 1) DAÑO DEL QUAKE EN FUNCIÓN DEL NIVEL ---
        float quakeDamageMultiplier;

        if (level < 10)
        {
            quakeDamageMultiplier = 1.0f;   // realmente no debería entrar aquí
        }
        else if (level < 13)
        {
            quakeDamageMultiplier = 1.25f;  // lvl 10–12
        }
        else
        {
            quakeDamageMultiplier = 1.5f;  // lvl 13–15
        }

        float quakeDamage = slashDamage * quakeDamageMultiplier;

        // --- 2) INSTANCIAR PREFAB ---
        GameObject q = Instantiate(quakePrefab, transform.position, Quaternion.identity);

        // escala visual relativa al slash
        float finalScale = slashScale * quakeScaleFactor;
        q.transform.localScale *= finalScale;

        // --- 3) PASAR DAÑO Y MEJORAS AL SCRIPT DEL ÁREA ---
    var area = q.GetComponent<QuakeAreaDamage>();
    if (area != null)
    {
        area.damage = quakeDamage;          // o damagePerSecond según tu script
        area.radiusMultiplier = currentQuakeRadiusMul;

        if (level >= 10)
        {
            area.lifeTime       = quakeLifeTimeLv10;
            area.enableShake    = true;
            area.shakeIntensity = quakeShakeIntensity;
            area.shakeFrequency = quakeShakeFrequency;
        }
    }


        Debug.Log($"[FireQuake] lvl {level} | quakeDamage={quakeDamage}, scale x{finalScale}");
    }
}
