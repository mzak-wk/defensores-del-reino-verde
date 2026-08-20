using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponSlotManager weaponSlotManager;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform firePoint;

    [Header("Facing")]
    [Tooltip("SpriteRenderer del jugador para detectar orientacion FlipX.")]
    [SerializeField] private SpriteRenderer playerSprite;

    private PlayerMovement playerMovement;
    private float nextBasicTime;
    private float nextSkillXTime;
    private float nextSkillCTime;
    private float nextSkillVTime;

    // --- Lightning Ring (Arma ID "6"): último impacto y regla de no-repetición ---
    private Vector2 lastStrikePosition;
    private bool hasLastStrike;

    // --- Vórtice de Captura (Arma ID "3"): referencia al vórtice activo ---
    private GameObject activeVortex;

    // --- Candado de combate (cinemáticas, intro de boss) ---
    private bool combatLocked;

    // --- Proyector de Reforestación (Arma ID "5"): motor de calor global ---
    private GameObject activeProjector;  // referencia al proyector activo
    private float currentHeat;           // calor actual (segundos de fuego sostenido)
    private float maxHeat;               // umbral de sobrecalentamiento (= scaledStats.duration)
    private bool isOverheated;           // bloqueo por sobrecalentamiento
    private float overheatLockTimer;     // countdown de bloqueo (= weapon.BasicCooldown)

    private void Awake()
    {
        if (weaponSlotManager == null)
            weaponSlotManager = GetComponent<WeaponSlotManager>();
        if (animator == null)
            animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        // Cinemáticas (ej. intro de boss): se congelan TODOS los ataques.
        if (combatLocked) return;

        HandleBasicAttack();
        HandleSkillX();
        HandleSkillC();
        HandleSkillV();
    }

    /// <summary>
    /// Bloquea o libera TODA la acción de combate del jugador.
    /// Al bloquear libera de inmediato cualquier ataque canalizado activo
    /// (vórtice/proyector) y resetea el calor, para que durante la cinemática
    /// no quede un "arma fantasma" (Update() ya no procesa ataques).
    /// </summary>
    public void SetCombatLocked(bool locked)
    {
        combatLocked = locked;

        if (!locked) return;

        if (activeVortex != null)
        {
            CaptureVortex vortex = activeVortex.GetComponent<CaptureVortex>();
            if (vortex != null) vortex.Release();
            activeVortex = null;
        }

        if (activeProjector != null)
        {
            ReforestationProjector projector = activeProjector.GetComponent<ReforestationProjector>();
            if (projector != null) projector.Release(false);
            activeProjector = null;
        }

        currentHeat = 0f;
        isOverheated = false;
    }

    private void HandleBasicAttack()
    {
        WeaponData weapon = weaponSlotManager.CurrentWeapon;

        // BUG FIX: validación GLOBAL de los ataques canalizados (vórtice y proyector).
        // El cierre no puede vivir solo dentro de HandleVortexAttack/HandleReforestationAttack
        // (que únicamente se ejecutan con su arma): si el jugador suelta el botón O cambia
        // de arma mientras uno está activo, se cierra en este mismo frame (evita arma fantasma).
        if (activeVortex != null)
        {
            bool buttonReleased = !Input.GetMouseButton(0);
            bool weaponChanged = weapon == null || weapon.WeaponId != "3";
            if (buttonReleased || weaponChanged)
            {
                activeVortex.GetComponent<CaptureVortex>().Release();
                activeVortex = null;
            }
        }

        if (activeProjector != null)
        {
            bool buttonReleased = !Input.GetMouseButton(0);
            bool weaponChanged = weapon == null || weapon.WeaponId != "5";
            if (buttonReleased || weaponChanged)
            {
                activeProjector.GetComponent<ReforestationProjector>().Release(false);
                activeProjector = null;
            }
        }

        // Fase 0 - Motor de Calor Global: corre cada frame (también con el arma
        // cambiada o nula) para calentar, enfriar y resolver el bloqueo.
        HandleHeatState(weapon);

        if (weapon == null) return;
        if (weapon.BasicAttackPrefab == null) return;
        if (weapon.BasicCooldown <= 0f) return;

        // Vórtice de Captura (Arma ID "3"): ataque continuo por botón sostenido.
        // No aplica el cooldown de "fuego" (nextBasicTime): su BasicCooldown
        // marca el ritmo de ticks de daño dentro del propio vórtice.
        if (weapon.WeaponId == "3")
        {
            HandleVortexAttack(weapon);
            return;
        }

        // Proyector de Reforestación (Arma ID "5"): ataque canalizado por botón
        // sostenido con motor de calor (overheat) y daño progresivo por ticks.
        if (weapon.WeaponId == "5")
        {
            HandleReforestationAttack(weapon);
            return;
        }

        if (Time.time < nextBasicTime) return;

        // Lightning Ring (Arma ID "6"): auto-disparo orbital. Ignora input y firePoint.
        if (weapon.WeaponId == "6")
        {
            FireOrbitalBasicAttack(weapon);
            return;
        }

        if (!Input.GetMouseButton(0)) return;

        nextBasicTime = Time.time + weapon.BasicCooldown;

        animator.SetTrigger("BasicAttack");
        
        BaseStats scaledStats = CalculateLevelScaledStats(weapon, weapon.Stats);
        SpawnAttack(weapon.BasicAttackPrefab, scaledStats, weapon.IsRanged, weapon);
    }

    /// <summary>
    /// Lightning Ring (Arma ID "6") - Fases 0, 1 y 2.
    /// Ataque orbital: cae directamente sobre el enemigo seleccionado, sin usar firePoint.
    /// Fase 0: cancela y reinicia el cooldown si no hay enemigos activos en escena.
    /// Fase 1: selecciona un enemigo aleatorio, evitando repetir el píxel del último impacto.
    /// Fase 2: instancia el BasicAttackPrefab sobre el enemigo y delega el daño a LightningStrikeProjectile.
    /// </summary>
    private void FireOrbitalBasicAttack(WeaponData weapon)
    {
        if (weapon == null || weapon.BasicAttackPrefab == null) return;

        // Fase 0: validación global + filtro de viewport.
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        // Filtrar solo enemigos visibles en pantalla
        System.Collections.Generic.List<Enemy> visibleEnemies = new System.Collections.Generic.List<Enemy>();
        Camera cam = Camera.main;
        
        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
            
            // Verificar si el enemigo está dentro del viewport de la cámara
            if (cam != null)
            {
                Vector3 vp = cam.WorldToViewportPoint(enemy.transform.position);
                bool inScreen = vp.z > 0 && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
                if (inScreen)
                    visibleEnemies.Add(enemy);
            }
            else
            {
                visibleEnemies.Add(enemy);
            }
        }
        
        if (visibleEnemies.Count == 0)
        {
            // Sin enemigos visibles: se cancela el ataque y se reinicia el cooldown sin instanciar nada.
            nextBasicTime = Time.time + weapon.BasicCooldown;
            return;
        }

        // Fase 1: targeting aleatorio + no-repetición (solo de enemigos visibles).
        Enemy center = visibleEnemies[Random.Range(0, visibleEnemies.Count)];
        if (hasLastStrike && (Vector2)center.transform.position == lastStrikePosition)
        {
            Enemy alternative = null;
            foreach (var enemy in visibleEnemies)
            {
                if ((Vector2)enemy.transform.position != lastStrikePosition)
                {
                    alternative = enemy;
                    break;
                }
            }
            if (alternative == null)
            {
                // Todos los enemigos están en el mismo píxel: se cancela y se reinicia el cooldown.
                nextBasicTime = Time.time + weapon.BasicCooldown;
                return;
            }
            center = alternative;
        }

        lastStrikePosition = center.transform.position;
        hasLastStrike = true;

        // Fase 2: el rayo cae EXACTAMENTE sobre el enemigo seleccionado. Ignora firePoint.
        nextBasicTime = Time.time + weapon.BasicCooldown;

        animator.SetTrigger("BasicAttack");

        BaseStats scaledStats = CalculateLevelScaledStats(weapon, weapon.Stats);
        GameObject go = Instantiate(weapon.BasicAttackPrefab, center.transform.position, Quaternion.identity);
        LightningStrikeProjectile strike = go.GetComponent<LightningStrikeProjectile>();
        if (strike != null)
            strike.Init(center, scaledStats);
        else
            Destroy(go);
    }

    /// <summary>
    /// Vórtice de Captura (Arma ID "3") - Fase 0.
    /// Ataque continuo: se mantiene activo mientras el jugador mantenga pulsado
    /// Input.GetMouseButton(0). El prefab se instancia como hijo del firePoint
    /// (transform.SetParent) para que siga al jugador al caminar; la orientación
    /// se resuelve dinámicamente en CaptureVortex (UpdateFacing) pidiendo a
    /// GetFacingDirection/GetSpawnPosition, ya que el firePoint es estático y el
    /// jugador se gira por el Flip del Sprite (Bug 2 corregido). Solo un vórtice
    /// activo a la vez. El cierre (Release) se valida GLOBALMENTE en HandleBasicAttack.
    /// </summary>
    private void HandleVortexAttack(WeaponData weapon)
    {
        if (weapon == null || weapon.BasicAttackPrefab == null) return;

        // Auto-sanación: si el vórtice fue destruido externamente
        // (muerte del jugador, cierre de escena...), soltar la referencia.
        if (activeVortex != null && activeVortex.GetComponent<CaptureVortex>() == null)
            activeVortex = null;

        // Este método ya solo se ejecuta con arma "3" y botón sostenido
        // (el release se maneja al inicio de HandleBasicAttack), pero se
        // mantiene la guarda por robustez.
        if (!Input.GetMouseButton(0)) return;

        // Regla anti-duplicados: solo UN vórtice activo.
        if (activeVortex != null) return;

        Vector2 dir = GetFacingDirection();

        // Instanciar en la posición del firePoint y hacerlo hijo del firePoint
        // (conservando su posición mundo). Desde Init, CaptureVortex ancla su
        // posición/rotación al cañón real mediante GetSpawnPosition/GetFacingDirection.
        GameObject go = Instantiate(weapon.BasicAttackPrefab, GetSpawnPosition(dir), Quaternion.identity);
        Transform parent = (firePoint != null) ? firePoint : transform;
        go.transform.SetParent(parent, true);

        CaptureVortex vortex = go.GetComponent<CaptureVortex>();
        if (vortex == null)
        {
            Destroy(go);
            return;
        }

        BaseStats scaledStats = CalculateLevelScaledStats(weapon, weapon.Stats);
        vortex.Init(weapon, scaledStats, firePoint, this);
        activeVortex = go;
    }

    /// <summary>
    /// Proyector de Reforestación (Arma ID "5") - Fases 0 y 1.
    /// Ataque canalizado por botón sostenido: se mantiene activo mientras el jugador
    /// mantenga pulsado Input.GetMouseButton(0) y no esté sobrecalentado. El prefab
    /// se instancia como hijo del firePoint (transform.SetParent) para que siga al
    /// jugador; la orientación se resuelve dinámicamente en ReforestationProjector
    /// vía GetFacingDirection/GetSpawnPosition. Solo un proyector activo a la vez.
    /// El cierre (Release) se valida GLOBALMENTE en HandleBasicAttack.
    /// </summary>
    private void HandleReforestationAttack(WeaponData weapon)
    {
        if (weapon == null || weapon.BasicAttackPrefab == null) return;

        // Bloqueo por sobrecalentamiento: no se puede disparar durante el cooldown.
        if (isOverheated) return;

        // Auto-sanación: si el proyector fue destruido externamente
        // (muerte del jugador, cierre de escena...), soltar la referencia.
        if (activeProjector != null && activeProjector.GetComponent<ReforestationProjector>() == null)
            activeProjector = null;

        // Este método ya solo se ejecuta con arma "5" y botón sostenido
        // (el release se maneja al inicio de HandleBasicAttack), pero se
        // mantiene la guarda por robustez.
        if (!Input.GetMouseButton(0)) return;

        // Regla anti-duplicados: solo UN proyector activo.
        if (activeProjector != null) return;

        Vector2 dir = GetFacingDirection();

        // Instanciar en la posición del firePoint y hacerlo hijo del firePoint
        // (conservando su posición mundo), igual que el vórtice.
        GameObject go = Instantiate(weapon.BasicAttackPrefab, GetSpawnPosition(dir), Quaternion.identity);
        Transform parent = (firePoint != null) ? firePoint : transform;
        go.transform.SetParent(parent, true);

        ReforestationProjector projector = go.GetComponent<ReforestationProjector>();
        if (projector == null)
        {
            Destroy(go);
            return;
        }

        // maxHeat = scaledStats.duration: segundos de fuego sostenido hasta sobrecalentar.
        BaseStats scaledStats = CalculateLevelScaledStats(weapon, weapon.Stats);
        maxHeat = scaledStats.duration;

        projector.Init(weapon, scaledStats, firePoint, this);
        activeProjector = go;
    }

    /// <summary>
    /// Fase 0 - Motor de Calor Global. Persiste en el jugador y corre cada frame:
    ///   - Calentamiento: botón sostenido + proyector activo => currentHeat += deltaTime.
    ///     Al llegar a maxHeat fuerza el sobrecalentamiento (destrucción + bloqueo).
    ///   - Enfriamiento: botón suelto (o sin proyector) => currentHeat -= deltaTime
    ///     a la misma velocidad que calienta.
    ///   - Bloqueo: mientras isOverheated, el timer de BasicCooldown descuenta y al
    ///     terminar se restablece el estado para poder disparar de nuevo.
    /// </summary>
    private void HandleHeatState(WeaponData weapon)
    {
        if (isOverheated)
        {
            overheatLockTimer -= Time.deltaTime;
            if (overheatLockTimer <= 0f)
            {
                isOverheated = false;
                currentHeat = 0f;
            }
            return;
        }

        bool firing = Input.GetMouseButton(0) && activeProjector != null;
        if (firing)
        {
            currentHeat += Time.deltaTime;
            if (maxHeat > 0f && currentHeat >= maxHeat)
                ForceOverheat(weapon);
        }
        else
        {
            currentHeat = Mathf.Max(0f, currentHeat - Time.deltaTime);
        }
    }

    /// <summary>
    /// Fase 0 - Fuerza el sobrecalentamiento: destruye el proyector (Release(true)),
    /// marca isOverheated y arranca el timer de bloqueo con weapon.BasicCooldown.
    /// </summary>
    private void ForceOverheat(WeaponData weapon)
    {
        isOverheated = true;
        currentHeat = maxHeat;
        overheatLockTimer = (weapon != null) ? weapon.BasicCooldown : 5f;

        if (activeProjector != null)
        {
            ReforestationProjector proj = activeProjector.GetComponent<ReforestationProjector>();
            if (proj != null)
                proj.Release(true);
            activeProjector = null;
        }
    }

    private void HandleSkillX()
    {
        if (!Input.GetKeyDown(KeyCode.X)) return;

        WeaponData weapon = weaponSlotManager.CurrentWeapon;
        if (weapon == null) return;
        if (weapon.PrefabX == null) return;
        if (weapon.CooldownX <= 0f) return;
        if (Time.time < nextSkillXTime) return;

        int playerLevel = GetPlayerLevel();
        if (playerLevel < weapon.RequiredLevelX)
        {
            Debug.Log($"[PlayerCombat] Nivel {weapon.RequiredLevelX} requerido para Skill X. Actual: {playerLevel}");
            return;
        }

        nextSkillXTime = Time.time + weapon.CooldownX;

        animator.SetTrigger("AttackX");

        BaseStats skillStats = weapon.Stats;
        skillStats.damage = weapon.DamageX;
        BaseStats scaledStats = CalculateLevelScaledStats(weapon, skillStats);
        SpawnAttack(weapon.PrefabX, scaledStats, weapon.IsRanged, weapon);
    }

    private void HandleSkillC()
    {
        if (!Input.GetKeyDown(KeyCode.C)) return;

        WeaponData weapon = weaponSlotManager.CurrentWeapon;
        if (weapon == null) return;
        if (weapon.PrefabC == null) return;
        if (weapon.CooldownC <= 0f) return;
        if (Time.time < nextSkillCTime) return;

        int playerLevel = GetPlayerLevel();
        if (playerLevel < weapon.RequiredLevelC)
        {
            Debug.Log($"[PlayerCombat] Nivel {weapon.RequiredLevelC} requerido para Skill C. Actual: {playerLevel}");
            return;
        }

        nextSkillCTime = Time.time + weapon.CooldownC;

        animator.SetTrigger("AttackC");

        BaseStats skillStats = weapon.Stats;
        skillStats.damage = weapon.DamageC;
        BaseStats scaledStats = CalculateLevelScaledStats(weapon, skillStats);
        SpawnAttack(weapon.PrefabC, scaledStats, weapon.IsRanged, weapon);
    }

    private void HandleSkillV()
    {
        if (!Input.GetKeyDown(KeyCode.V)) return;

        WeaponData weapon = weaponSlotManager.CurrentWeapon;
        if (weapon == null) return;
        if (weapon.PrefabV == null) return;
        if (weapon.CooldownV <= 0f) return;
        if (Time.time < nextSkillVTime) return;

        int weaponLevel = weaponSlotManager.CurrentWeaponLevel;
        if (weaponLevel < weapon.RequiredLevelV)
        {
            Debug.Log($"[PlayerCombat] Nivel de arma {weapon.RequiredLevelV} requerido para Ultimate V. Actual: {weaponLevel}");
            return;
        }

        nextSkillVTime = Time.time + weapon.CooldownV;

        animator.SetTrigger("AttackV");

        BaseStats skillStats = weapon.Stats;
        skillStats.damage = weapon.DamageV;
        BaseStats scaledStats = CalculateLevelScaledStats(weapon, skillStats);
        SpawnAttack(weapon.PrefabV, scaledStats, weapon.IsRanged, weapon);
    }

    private BaseStats CalculateLevelScaledStats(WeaponData weapon, BaseStats baseStats)
    {
        int playerLevel = GetPlayerLevel();
        float t = Mathf.InverseLerp(1f, 15f, playerLevel);

        // Curva de daño exponencial (ajustable)
        float damageMultiplier = Mathf.Lerp(1f, 30f, t * t);
        
        // Cooldown reduce con nivel (más rápido)
        float cooldownMultiplier = Mathf.Lerp(1f, 0.25f, t);
        
        // Velocidad de proyectil aumenta
        float speedMultiplier = Mathf.Lerp(1f, 3f, t);
        
        // Rango/área aumenta
        float rangeMultiplier = Mathf.Lerp(1f, 2.5f, t);

        BaseStats scaled = baseStats;
        scaled.damage *= damageMultiplier;
        scaled.cooldown *= cooldownMultiplier;
        scaled.projectileSpeed *= speedMultiplier;
        scaled.areaSize *= rangeMultiplier;
        scaled.duration *= rangeMultiplier;
        scaled.projectileMaxLifetime *= rangeMultiplier;
        scaled.slashForwardDistance *= rangeMultiplier;
        scaled.slashInitialScale *= rangeMultiplier;
        scaled.slashFinalScale *= rangeMultiplier;
        scaled.areaDamagePerSecond *= damageMultiplier;
        scaled.areaDuration *= rangeMultiplier;
        scaled.areaScaleMultiplier *= rangeMultiplier;

        return scaled;
    }

    private void SpawnAttack(GameObject prefab, BaseStats stats, bool isRanged, WeaponData weapon)
    {
        if (prefab == null) return;

        Vector2 dir = GetFacingDirection();
        Vector3 spawnPos = GetSpawnPosition(dir);
        Quaternion spawnRot = Quaternion.identity;

        GameObject go = Instantiate(prefab, spawnPos, spawnRot);

        if (isRanged)
        {
            // Try ArrowProjectile first
            ArrowProjectile arrow = go.GetComponent<ArrowProjectile>();
            if (arrow != null)
            {
                arrow.Init(dir, stats.damage, stats.projectileSpeed, stats.projectileMaxLifetime);
                return;
            }

            // Try PrismLaserProjectile (Hitscan Laser)
            PrismLaserProjectile laser = go.GetComponent<PrismLaserProjectile>();
            if (laser != null)
            {
                laser.Init(weapon, stats, dir);
                return;
            }

            // Try FireballProjectile
            FireballProjectile fireball = go.GetComponent<FireballProjectile>();
            if (fireball != null)
            {
                // Find nearest enemy as target
                Transform target = FindNearestEnemy(spawnPos);
                if (target != null)
                {
                    fireball.Init(
                        target,
                        stats.damage,
                        stats.projectileSpeed,
                        stats.projectileMaxLifetime
                    );
                }
                else
                {
                    Destroy(go);
                }
                return;
            }

            // Fallback to generic Projectile
            Projectile proj = go.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Initialize(dir, stats.projectileSpeed, stats.damage, stats.pierce);
            }
        }
        else
        {
            // Melee attack (SlashAttack)
            go.transform.rotation = (dir.x >= 0f) 
                ? Quaternion.Euler(0f, 180f, 0f) 
                : Quaternion.identity;

            SlashAttack slash = go.GetComponent<SlashAttack>();
            if (slash != null)
            {
                slash.Init(
                    transform,
                    dir,
                    stats.damage,
                    stats.slashLifetime,
                    stats.slashMaxHits,
                    stats.slashDestroyOnFirstHit,
                    stats.slashForwardDistance
                );
            }
            else
            {
                // Fallback: just destroy after duration
                float duration = (stats.duration > 0f) ? stats.duration : 0.3f;
                Destroy(go, duration);
            }
        }
    }

    private Transform FindNearestEnemy(Vector3 fromPosition)
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Transform nearest = null;
        float minDistSq = float.MaxValue;

        foreach (var e in enemies)
        {
            if (!e) continue;
            float dSq = (e.transform.position - fromPosition).sqrMagnitude;
            if (dSq < minDistSq)
            {
                minDistSq = dSq;
                nearest = e.transform;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Dirección actual hacia la que mira el jugador.
    /// Pública para que CaptureVortex pueda re-evaluar el facing dinámico
    /// mientras el ataque se mantiene activo.
    /// </summary>
    public Vector2 GetFacingDirection()
    {
        if (playerMovement != null && playerMovement.LastFacingDirection.sqrMagnitude > 0.01f)
        {
            return playerMovement.LastFacingDirection.normalized;
        }

        bool facingLeft = playerSprite != null && playerSprite.flipX;
        return facingLeft ? Vector2.left : Vector2.right;
    }

    /// <summary>
    /// Posición mundial del cañón según la dirección de mira.
    /// Pública para que CaptureVortex re-posicione el vórtice dinámicamente:
    /// refleja el offset del firePoint sobre el eje X en función de `dir`.
    /// </summary>
    public Vector3 GetSpawnPosition(Vector2 dir)
    {
        if (firePoint != null)
        {
            float localX = Mathf.Abs(firePoint.localPosition.x) * (dir.x < 0f ? -1f : 1f);
            Vector3 adjustedLocalPos = new Vector3(localX, firePoint.localPosition.y, firePoint.localPosition.z);
            return transform.TransformPoint(adjustedLocalPos);
        }

        return transform.position + (Vector3)(dir * 1.3f);
    }

    private int GetPlayerLevel()
    {
        ExperienceSystem xp = GetComponent<ExperienceSystem>();
        return xp != null ? xp.level : 1;
    }
}
