using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance;

    // -------- Estado del jugador --------
    [Header("Jugador: Vida / Oro")]
    public float playerMaxHP = 100f;
    public float playerCurrentHP = 100f;
    public int gold = 0;

    [Header("Jugador: Regeneración")]
    public float baseRegenPerSecond = 0.1f;
    public float regenPerSecond = 0.1f;

    [Header("Tiempo total (run actual)")]
    public float totalMinutesPlayed = 0f;

    [Header("XP / Nivel (run actual)")]
    public int   playerLevel    = 1;
    public float playerXP       = 0f;
    public float playerXpToNext = 50f;

    // -------- Tutorial movimiento --------
    [Header("Tutorial movimiento (W/A/S/D)")]
    public bool movedW = false;
    public bool movedA = false;
    public bool movedS = false;
    public bool movedD = false;

    // -------- Tienda / meta progreso --------
    [Header("Tienda - niveles comprados")]
    public int hpLevel     = 0; // Vida
    public int speedLevel  = 0; // Velocidad
    public int resistLevel = 0; // lo usamos como NIVEL DEL IMÁN

    [Header("Tienda - límite de nivel")]
    public int maxHpLevel     = 30;
    public int maxSpeedLevel  = 10;
    public int maxResistLevel = 20;   // imán

    [Header("Tienda - precios base")]
    public int basePriceHP     = 2;
    public int basePriceSpeed  = 20;
    public int basePriceResist = 10; // imán

    [Header("Tienda - escalado de precio")]
    public float priceFactor = 1.35f;

    [Header("Tienda - límite de precio")]
    public int maxUpgradePrice = 400;

    [Header("Tienda - efectos derivados")]
    public float moveSpeedMul       = 1f; // 1 + 0.05 * speedLevel, por ejemplo
    public float damageTakenMul     = 1f; // legacy, lo dejamos por compatibilidad
    public float magnetRadiusBonus  = 0f; // +1 por nivel de resist/imán
    public float magnetPullBonus    = 0f; // +5 por nivel de resist/imán

    // -------- Snapshot inicial (para ResetAll) --------
    float _initial_playerMaxHP;
    float _initial_playerCurrentHP;
    int   _initial_gold;
    int   _initial_hpLevel;
    int   _initial_speedLevel;
    int   _initial_resistLevel;
    int   _initial_basePriceHP, _initial_basePriceSpeed, _initial_basePriceResist;
    float _initial_priceFactor;
    float _initial_totalMinutesPlayed;
    int   _initial_playerLevel;
    float _initial_playerXP, _initial_playerXpToNext;
    float _initial_baseRegen, _initial_regen;

    // ==========================
    // Precios tienda
    // ==========================
    public int CurrentPriceHP()
    {
        int rawPrice = Mathf.CeilToInt(basePriceHP * Mathf.Pow(priceFactor, hpLevel));
        return Mathf.Min(maxUpgradePrice, rawPrice);
    }

    public int CurrentPriceSpeed()
    {
        int rawPrice = Mathf.CeilToInt(basePriceSpeed * Mathf.Pow(priceFactor, speedLevel));
        return Mathf.Min(maxUpgradePrice, rawPrice);
    }

    public int CurrentPriceResist() // ahora es el IMÁN
    {
        int rawPrice = Mathf.CeilToInt(basePriceResist * Mathf.Pow(priceFactor, resistLevel));
        return Mathf.Min(maxUpgradePrice, rawPrice);
    }

    // ==========================
    // Multiplicadores tienda
    // ==========================
    public void RecomputeShopMultipliers()
    {
        // Velocidad de movimiento: +5% por nivel
        moveSpeedMul = 1f + speedLevel * 0.15f;

        // Dureza ya no se usa, lo dejamos a 1
        damageTakenMul = 1f;

        // Imán: +1 de radio y +5 de velocidad por nivel
        magnetRadiusBonus = resistLevel * 1f;
        magnetPullBonus   = resistLevel * 5f;

        // Regeneración: base +0.1 cada 5 niveles de vida
        int regenSteps = hpLevel / 5; // enteros
        regenPerSecond = baseRegenPerSecond + regenSteps * 0.1f;
    }

    public float GetMoveSpeedMultiplier()   => moveSpeedMul;
    public float GetDamageTakenMultiplier() => damageTakenMul;
    public float GetMagnetRadiusBonus()     => magnetRadiusBonus;
    public float GetMagnetPullBonus()       => magnetPullBonus;

    // ==========================
    // Gasto / Upgrades
    // ==========================
    public bool TrySpend(int amount)
    {
        if (gold < amount) return false;
        gold -= amount;
        return true;
    }

    public bool ApplyHpUpgrade()
    {
        if (hpLevel >= maxHpLevel) return false;

        int cost = CurrentPriceHP();
        if (!TrySpend(cost)) return false;

        hpLevel++;

        // +20 de vida máx por nivel
        playerMaxHP += 30f;
        playerCurrentHP = Mathf.Min(playerCurrentHP + 20f, playerMaxHP);

        RecomputeShopMultipliers();
        return true;
    }

    public bool ApplySpeedUpgrade()
    {
        if (speedLevel >= maxSpeedLevel) return false;

        int cost = CurrentPriceSpeed();
        if (!TrySpend(cost)) return false;

        speedLevel++;
        RecomputeShopMultipliers();
        return true;
    }

    // OJO: Resist ahora es el IMÁN (+rango y +pull speed)
    public bool ApplyResistUpgrade()
    {
        if (resistLevel >= maxResistLevel) return false;

        int cost = CurrentPriceResist();
        if (!TrySpend(cost)) return false;

        resistLevel++;
        RecomputeShopMultipliers();
        return true;
    }

    // Sobrecargas legacy
    public bool ApplyHpUpgrade(int _ignoredCost)       => ApplyHpUpgrade();
    public bool ApplySpeedUpgrade(int _ignoredCost)    => ApplySpeedUpgrade();
    public bool ApplyResistUpgrade(int _ignoredCost)   => ApplyResistUpgrade();
    public bool ApplyHpUpgrade(float _ignoredCost)     => ApplyHpUpgrade();
    public bool ApplySpeedUpgrade(float _ignoredCost)  => ApplySpeedUpgrade();
    public bool ApplyResistUpgrade(float _ignoredCost) => ApplyResistUpgrade();

    // ==========================
    // Ciclo de vida
    // ==========================
    void Awake()
    {
        // Singleton
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Snapshot inicial
        _initial_playerMaxHP       = playerMaxHP;
        _initial_playerCurrentHP   = playerCurrentHP;
        _initial_gold              = gold;
        _initial_hpLevel           = hpLevel;
        _initial_speedLevel        = speedLevel;
        _initial_resistLevel       = resistLevel;
        _initial_basePriceHP       = basePriceHP;
        _initial_basePriceSpeed    = basePriceSpeed;
        _initial_basePriceResist   = basePriceResist;
        _initial_priceFactor       = priceFactor;
        _initial_totalMinutesPlayed= totalMinutesPlayed;
        _initial_playerLevel       = playerLevel;
        _initial_playerXP          = playerXP;
        _initial_playerXpToNext    = playerXpToNext;
        _initial_baseRegen         = baseRegenPerSecond;
        _initial_regen             = regenPerSecond;

        RecomputeShopMultipliers();

        Debug.Log($"[GameSession] Awake snapshot saved.");

        // Registrar listener para detectar cambios de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // ==========================
    // Reset TOTAL (nueva partida)
    // ==========================
    public void ResetAll()
    {
        playerMaxHP      = _initial_playerMaxHP;
        playerCurrentHP  = _initial_playerMaxHP; // vida llena

        gold             = _initial_gold;

        hpLevel          = _initial_hpLevel;
        speedLevel       = _initial_speedLevel;
        resistLevel      = _initial_resistLevel;

        basePriceHP      = _initial_basePriceHP;
        basePriceSpeed   = _initial_basePriceSpeed;
        basePriceResist  = _initial_basePriceResist;
        priceFactor      = _initial_priceFactor;

        totalMinutesPlayed = _initial_totalMinutesPlayed;

        playerLevel      = _initial_playerLevel;
        playerXP         = _initial_playerXP;
        playerXpToNext   = _initial_playerXpToNext;

        movedW = movedA = movedS = movedD = false;

        baseRegenPerSecond = _initial_baseRegen;
        regenPerSecond     = _initial_regen;

        RecomputeShopMultipliers();

        Debug.Log("[GameSession] ResetAll() - sesión reiniciada a valores base.");
    }

    public void ResetToDefaults() => ResetAll();

    // ==========================
    // Detección de cambio de escena
    // ==========================
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Si se carga la escena MenuScene, reiniciar todos los valores
        if (scene.name == "MenuScene")
        {
            ResetAll();
            Debug.Log("[GameSession] MenuScene cargada - valores reiniciados a base.");
        }
    }

    void OnDestroy()
    {
        // Desregistrar el listener cuando se destruya
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
