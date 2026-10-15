using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tipo de ecosistema o perfil mecánico del nivel actual.
/// </summary>
public enum EcosystemType
{
    TropicalForest, // Nivel 1: Tropical / Bosque (Base: ~24°C, Peligro: 50°C, Erosión de suelo)
    PolarIce,       // Nivel 2: Frío / Polo Norte (Base: -15°C, Peligro: 15°C, Hollín y derretimiento)
    SunnyBeach,     // Nivel 3: Soleado / Playa (Base: 32°C, Peligro: 60°C, Residuos y calor extremo)
    FinalBossUlti   // Nivel Final: Medidor de Definitiva del Jefe Final (0% a 100%)
}

/// <summary>
/// Gestor genérico y extensible de peligro ambiental por nivel (Termómetro / Boss Ulti).
/// Mantiene el valor actual, los límites térmicos/porcentaje, la tasa acelerada de incremento
/// si hay zonas de daño ecológico sin atender y dispara el Game Over / Ulti al alcanzar el límite.
/// </summary>
public class HazardMeterManager : MonoBehaviour
{
    public static HazardMeterManager Instance { get; private set; }

    /// <summary>Disparado cuando el peligro alcanza el límite configurado.</summary>
    public static event Action<DeathReason> OnHazardGameOver;

    /// <summary>Disparado específicamente cuando la ulti del boss final se carga al 100%.</summary>
    public static event Action OnBossUltiTriggered;

    [Header("Perfil de Ecosistema")]
    [Tooltip("Ecosistema o comportamiento del medidor según el nivel actual.")]
    [SerializeField] private EcosystemType ecosystemType = EcosystemType.TropicalForest;

    [Header("Configuración de Rango y Temperatura")]
    [Tooltip("Valor mínimo visible del termómetro (ej. -20°C en hielo polar, 15°C en bosque, 0% en boss).")]
    [SerializeField] private float minDisplayValue = 15f;

    [Tooltip("Valor inicial al comenzar el nivel.")]
    [SerializeField] private float initialValue = 24f;

    [Tooltip("Límite máximo o temperatura crítica que provoca Game Over / activación de ulti.")]
    [SerializeField, Min(1f)] private float hazardLimit = 50f;

    [Header("Incremento y Presión Ambiental")]
    [Tooltip("Tasa pasiva continua de incremento por segundo durante combate.")]
    [SerializeField, Min(0f)] private float baseRiseRatePerSecond = 0.25f;

    [Tooltip("Incremento adicional por segundo POR CADA zona de daño/erosión activa no atendida.")]
    [SerializeField, Min(0f)] private float riseRatePerZone = 0.85f;

    [Tooltip("Reducción de peligro por acción de limpieza, siembra de árbol o enfriamiento.")]
    [SerializeField, Min(0f)] private float defaultHazardReduction = 8.0f;

    [Header("Tipo de Peligro")]
    [SerializeField] private DeathReason deathReason = DeathReason.TemperatureLimit;

    [Header("Estado Runtime")]
    [SerializeField] private float currentValue = 24f;
    [SerializeField] private bool isCombatActive = false;
    [SerializeField] private bool isGameOver = false;

    // Colección de zonas de daño ecológico activas en la escena
    private readonly HashSet<Component> activeDamageZones = new HashSet<Component>();

    // Eventos
    public event Action<float, float> OnHazardValueChanged;
    public event Action<float, float, EcosystemType> OnEcosystemHazardChanged;
    public static event Action<float, float> OnGlobalHazardValueChanged;

    public EcosystemType CurrentEcosystem => ecosystemType;
    public float CurrentValue => currentValue;
    public float MinDisplayValue => minDisplayValue;
    public float HazardLimit => hazardLimit;
    public float DefaultHazardReduction => defaultHazardReduction;
    public bool IsCombatActive => isCombatActive;
    public DeathReason HazardDeathReason => deathReason;
    public int ActiveDamageZonesCount => activeDamageZones.Count;

    public float EffectiveRiseRate
    {
        get
        {
            if (isGameOver) return 0f;
            if (ecosystemType == EcosystemType.FinalBossUlti)
                return isCombatActive ? baseRiseRatePerSecond : (baseRiseRatePerSecond * 0.5f);

            if (!isCombatActive && activeDamageZones.Count == 0) return 0f;
            return baseRiseRatePerSecond + (activeDamageZones.Count * riseRatePerZone);
        }
    }

    public float NormalizedProgress
    {
        get
        {
            float range = hazardLimit - minDisplayValue;
            if (range <= 0.001f) return 0f;
            return Mathf.Clamp01((currentValue - minDisplayValue) / range);
        }
    }

    private void Awake()
    {
        string sceneName = gameObject.scene.name;
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        // El HUB es base segura; MenuScene y BootScene no son niveles de combate ecológico
        if (sceneName == "LevelHUB" || sceneName == "MenuScene" || sceneName == "BootScene")
        {
            var existingUI = FindObjectsByType<ThermometerUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ui in existingUI)
            {
                Destroy(ui.gameObject);
            }
            enabled = false;
            Destroy(gameObject);
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentValue = Mathf.Clamp(initialValue, minDisplayValue, hazardLimit);
        isGameOver = false;
    }

    private void Start()
    {
        EnsureRuntimeHUD();
        SubscribeToSpawnerEvents();
        NotifyValueChanged();
    }

    private void EnsureRuntimeHUD()
    {
        string sceneName = gameObject.scene.name;
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
        if (sceneName == "LevelHUB" || sceneName == "MenuScene" || sceneName == "BootScene")
        {
            return;
        }

        if (FindFirstObjectByType<ThermometerUI>() != null)
            return;

        Canvas canvas = null;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c.gameObject.name == "Canvas" || c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas = c;
                break;
            }
        }
        if (canvas == null && canvases.Length > 0) canvas = canvases[0];
        if (canvas == null) return;

        Transform hudRoot = canvas.transform.Find("HUD_Gameplay");
        if (hudRoot == null)
        {
            GameObject hudGO = new GameObject("HUD_Gameplay");
            hudGO.transform.SetParent(canvas.transform, false);
            RectTransform hudRT = hudGO.AddComponent<RectTransform>();
            hudRT.anchorMin = Vector2.zero;
            hudRT.anchorMax = Vector2.one;
            hudRT.offsetMin = Vector2.zero;
            hudRT.offsetMax = Vector2.zero;
            hudRoot = hudGO.transform;
            hudRoot.SetAsFirstSibling();
        }

        GameObject prefab = Resources.Load<GameObject>("UI_Thermometer");
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, hudRoot);
            instance.name = "UI_Thermometer";
            ThermometerUI ui = instance.GetComponent<ThermometerUI>();
            if (ui != null)
            {
                ui.SetEcosystem(ecosystemType);
                ui.TryBindManager();
            }
            Debug.Log($"[HazardMeterManager] ThermometerUI auto-creado en tiempo de ejecución en {gameObject.scene.name}");
        }
    }

    private void Update()
    {
        if (isGameOver) return;

        float rate = EffectiveRiseRate;
        if (rate > 0f)
        {
            float newVal = Mathf.Min(hazardLimit, currentValue + rate * Time.deltaTime);

            if (!Mathf.Approximately(newVal, currentValue))
            {
                currentValue = newVal;
                NotifyValueChanged();
            }

            if (currentValue >= hazardLimit && !isGameOver)
            {
                TriggerHazardGameOver();
            }
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromSpawnerEvents();
        if (Instance == this) Instance = null;
    }

    private void TriggerHazardGameOver()
    {
        currentValue = hazardLimit;
        isGameOver = true;
        NotifyValueChanged();

        if (ecosystemType == EcosystemType.FinalBossUlti)
        {
            Debug.LogWarning("[HazardMeterManager] ¡EL JEFE FINAL CARGÓ EL 100% DE SU ULTI!");
            OnBossUltiTriggered?.Invoke();
        }

        OnHazardGameOver?.Invoke(deathReason);

        if (DeathMenuController.Instance != null)
        {
            DeathMenuController.Instance.TriggerDeathSequence(deathReason);
        }
    }

    // ==========================================
    // MÉTODOS DE ZONAS DE DAÑO ECOLÓGICO (EROSIÓN / HOLLÍN / BASURA)
    // ==========================================

    /// <summary>
    /// Registra una nueva zona de daño ecológico no atendida en el nivel.
    /// Aumenta la velocidad a la que sube el termómetro.
    /// </summary>
    public void RegisterDamageZone(Component zone)
    {
        if (zone == null) return;
        activeDamageZones.Add(zone);
        PruneNullZones();
        NotifyValueChanged();
    }

    /// <summary>
    /// Desregistra una zona de daño tras ser restaurada o plantada.
    /// Reduce la velocidad de incremento.
    /// </summary>
    public void UnregisterDamageZone(Component zone)
    {
        if (zone == null) return;
        activeDamageZones.Remove(zone);
        PruneNullZones();
        NotifyValueChanged();
    }

    private void PruneNullZones()
    {
        activeDamageZones.RemoveWhere(z => z == null);
    }

    // ==========================================
    // MÉTODOS PÚBLICOS DE MANIPULACIÓN
    // ==========================================

    public void IncreaseHazard(float amount)
    {
        if (isGameOver || amount <= 0f) return;

        currentValue = Mathf.Min(hazardLimit, currentValue + amount);
        NotifyValueChanged();

        if (currentValue >= hazardLimit && !isGameOver)
        {
            TriggerHazardGameOver();
        }
    }

    public void ReduceHazard(float amount)
    {
        if (isGameOver || amount <= 0f) return;

        currentValue = Mathf.Max(minDisplayValue, currentValue - amount);
        NotifyValueChanged();
    }

    public void SetHazardActive(bool active)
    {
        isCombatActive = active;
    }

    public void SetHazardLimit(float newLimit)
    {
        hazardLimit = Mathf.Max(minDisplayValue + 1f, newLimit);
        currentValue = Mathf.Clamp(currentValue, minDisplayValue, hazardLimit);
        NotifyValueChanged();
    }

    /// <summary>Permite cargar la definitiva del boss final directamente (0-100%).</summary>
    public void SetBossUltiPercent(float percent)
    {
        if (ecosystemType != EcosystemType.FinalBossUlti) return;
        currentValue = Mathf.Clamp(percent, 0f, 100f);
        NotifyValueChanged();

        if (currentValue >= 100f && !isGameOver)
        {
            TriggerHazardGameOver();
        }
    }

    public void AddBossUltiCharge(float amount)
    {
        if (ecosystemType != EcosystemType.FinalBossUlti) return;
        IncreaseHazard(amount);
    }

    public void NotifyValueChanged()
    {
        OnHazardValueChanged?.Invoke(currentValue, hazardLimit);
        OnEcosystemHazardChanged?.Invoke(currentValue, hazardLimit, ecosystemType);
        OnGlobalHazardValueChanged?.Invoke(currentValue, hazardLimit);
    }

    // ==========================================
    // PRESETS DE CALIBRACIÓN POR ECOSISTEMA
    // ==========================================

    public void ApplyEcosystemPreset(EcosystemType type)
    {
        ecosystemType = type;
        switch (type)
        {
            case EcosystemType.TropicalForest:
                minDisplayValue = 15f;
                initialValue = 24f;
                hazardLimit = 50f;
                baseRiseRatePerSecond = 0.25f;
                riseRatePerZone = 0.90f;
                defaultHazardReduction = 8.0f;
                deathReason = DeathReason.TemperatureLimit;
                break;

            case EcosystemType.PolarIce:
                minDisplayValue = -20f;
                initialValue = -15f;
                hazardLimit = 15f;
                baseRiseRatePerSecond = 0.20f;
                riseRatePerZone = 0.75f;
                defaultHazardReduction = 6.0f;
                deathReason = DeathReason.TemperatureLimit;
                break;

            case EcosystemType.SunnyBeach:
                minDisplayValue = 25f;
                initialValue = 32f;
                hazardLimit = 60f;
                baseRiseRatePerSecond = 0.30f;
                riseRatePerZone = 1.10f;
                defaultHazardReduction = 10.0f;
                deathReason = DeathReason.TemperatureLimit;
                break;

            case EcosystemType.FinalBossUlti:
                minDisplayValue = 0f;
                initialValue = 0f;
                hazardLimit = 100f;
                baseRiseRatePerSecond = 1.25f;
                riseRatePerZone = 0f;
                defaultHazardReduction = 15.0f;
                deathReason = DeathReason.HazardLimit;
                break;
        }

        currentValue = initialValue;
        NotifyValueChanged();
    }

    // ==========================================
    // INTEGRACIÓN CON OLEADAS DE ZONA
    // ==========================================
    private void SubscribeToSpawnerEvents()
    {
        if (ZoneSpawner.Instance != null)
        {
            ZoneSpawner.Instance.OnWaveStarted += HandleWaveStarted;
            ZoneSpawner.Instance.OnWaveEnded += HandleWaveEnded;
        }
    }

    private void UnsubscribeFromSpawnerEvents()
    {
        if (ZoneSpawner.Instance != null)
        {
            ZoneSpawner.Instance.OnWaveStarted -= HandleWaveStarted;
            ZoneSpawner.Instance.OnWaveEnded -= HandleWaveEnded;
        }
    }

    private void HandleWaveStarted(SpawnZone zone, int waveNumber)
    {
        isCombatActive = true;
    }

    private void HandleWaveEnded(SpawnZone zone, int waveNumber)
    {
        isCombatActive = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (hazardLimit <= minDisplayValue)
            hazardLimit = minDisplayValue + 1f;

        initialValue = Mathf.Clamp(initialValue, minDisplayValue, hazardLimit);
        baseRiseRatePerSecond = Mathf.Max(0f, baseRiseRatePerSecond);
        riseRatePerZone = Mathf.Max(0f, riseRatePerZone);
        defaultHazardReduction = Mathf.Max(0f, defaultHazardReduction);
    }
#endif
}
