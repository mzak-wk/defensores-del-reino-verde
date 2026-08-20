using System;
using UnityEngine;

/// <summary>
/// Gestor ambiental exclusivo del Nivel 2 (Hielo/Nieve).
/// Controla la temperatura global y el nivel de Albedo según las manchas de hollín activas.
/// La temperatura solo sube durante oleadas de combate activas.
/// </summary>
public class TemperatureManager : MonoBehaviour
{
    public static TemperatureManager Instance { get; private set; }

    /// <summary>Disparado cuando la temperatura alcanza el tope máximo (100°C).</summary>
    public static event Action OnTemperatureGameOver;

    [Header("Configuración Térmica")]
    [Tooltip("Temperatura inicial en °C.")]
    [SerializeField, Min(0f)] private float initialTemperature = 0f;

    [Tooltip("Temperatura máxima (°C) que causa Game Over.")]
    [SerializeField, Min(0f)] private float maximumTemperature = 100f;

    [Tooltip("Incremento pasivo continuo de temperatura por segundo.")]
    [SerializeField, Min(0f)] private float baseTempRiseRate = 1.0f;

    [Tooltip("Reducción de temperatura fija al limpiar una mancha de hollín.")]
    [SerializeField, Min(0f)] private float tempReductionPerClean = 5.0f;

    [Header("Configuración de Albedo")]
    [Tooltip("Cantidad de manchas de referencia para saturar el 0% de Albedo.")]
    [SerializeField, Min(1)] private int maxSootCapacity = 10;

    [Tooltip("Porcentaje de Albedo considerado crítico (umbral de peligro).")]
    [SerializeField, Range(0f, 100f)] private float criticalAlbedoThreshold = 30f;

    [Tooltip("Multiplicador de aumento térmico cuando el Albedo está por debajo del umbral crítico.")]
    [SerializeField, Min(1f)] private float criticalTempMultiplier = 1.15f;

    [Header("Estado Runtime")]
    [SerializeField, Min(0f)] private float currentTemperature = 0f;

    private int activeSootCount = 0;
    private bool isGameOver = false;

    /// <summary>
    /// Flag que indica si hay una oleada de combate activa.
    /// La temperatura solo sube cuando isCombatActive == true.
    /// </summary>
    private bool isCombatActive = false;

    /// <summary>Se invoca cuando cambia la temperatura o el albedo. Parámetros: (currentTemperature, albedoPercent).</summary>
    public event Action<float, float> OnTemperatureChanged;

    public float CurrentTemperature => currentTemperature;
    public float MaximumTemperature => maximumTemperature;
    public int ActiveSootCount => activeSootCount;
    public float TempReductionPerClean => tempReductionPerClean;
    public bool IsCombatActive => isCombatActive;

    /// <summary>Calcula el porcentaje actual de Albedo (0% a 100%).</summary>
    public float AlbedoPercent
    {
        get
        {
            if (maxSootCapacity <= 0) return 100f;
            return Mathf.Clamp(100f - ((float)activeSootCount / maxSootCapacity * 100f), 0f, 100f);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentTemperature = Mathf.Clamp(initialTemperature, 0f, maximumTemperature);
        isGameOver = false;
        isCombatActive = false;
    }

    private void Start()
    {
        SubscribeToWaveEvents();
        NotifyTemperatureChanged();
    }

    private void Update()
    {
        if (isGameOver) return;

        // La temperatura solo sube durante oleadas de combate activas
        if (!isCombatActive) return;

        // Multiplicador térmico según el Albedo
        float currentMultiplier = (AlbedoPercent < criticalAlbedoThreshold) ? criticalTempMultiplier : 1.0f;
        float effectiveRate = baseTempRiseRate * currentMultiplier;

        if (effectiveRate <= 0f) return;

        float newTemp = Mathf.Min(maximumTemperature, currentTemperature + effectiveRate * Time.deltaTime);

        if (!Mathf.Approximately(newTemp, currentTemperature))
        {
            currentTemperature = newTemp;
            NotifyTemperatureChanged();
        }

        if (currentTemperature >= maximumTemperature && !isGameOver)
        {
            currentTemperature = maximumTemperature;
            isGameOver = true;
            OnTemperatureGameOver?.Invoke();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromWaveEvents();

        if (Instance == this)
            Instance = null;
    }

    // ==========================================
    // SUSCRIPCIÓN A EVENTOS DE OLEADAS
    // ==========================================

    private void SubscribeToWaveEvents()
    {
        if (ZoneSpawner.Instance == null)
        {
            Debug.Log("[TemperatureManager] ZoneSpawner.Instance no encontrado; la temperatura no se activará por oleadas.");
            return;
        }

        ZoneSpawner.Instance.OnWaveStarted += HandleWaveStarted;
        ZoneSpawner.Instance.OnWaveEnded += HandleWaveEnded;
    }

    private void UnsubscribeFromWaveEvents()
    {
        if (ZoneSpawner.Instance == null) return;

        ZoneSpawner.Instance.OnWaveStarted -= HandleWaveStarted;
        ZoneSpawner.Instance.OnWaveEnded -= HandleWaveEnded;
    }

    private void HandleWaveStarted(SpawnZone zone, int waveNumber)
    {
        isCombatActive = true;
        Debug.Log($"[TemperatureManager] Oleada {waveNumber} iniciada en {zone.zoneName}. Temperatura ACTIVA.");
    }

    private void HandleWaveEnded(SpawnZone zone, int waveNumber)
    {
        isCombatActive = false;
        Debug.Log($"[TemperatureManager] Oleada {waveNumber} finalizada en {zone.zoneName}. Temperatura PAUSADA.");
    }

    // ==========================================
    // API PÚBLICA: CONTROL MANUAL DE COMBATE
    // ==========================================

    /// <summary>Activa manualmente el incremento de temperatura (para casos fuera del flujo de ZoneSpawner).</summary>
    public void SetCombatActive(bool active)
    {
        isCombatActive = active;
    }

    // ==========================================
    // API PÚBLICA: HOLLÍN Y TEMPERATURA
    // ==========================================

    /// <summary>Registra una nueva mancha de hollín en el escenario.</summary>
    public void RegisterSoot()
    {
        activeSootCount++;
        NotifyTemperatureChanged();
    }

    /// <summary>Desregistra una mancha de hollín (por limpieza o destrucción).</summary>
    public void UnregisterSoot()
    {
        activeSootCount = Mathf.Max(0, activeSootCount - 1);
        NotifyTemperatureChanged();
    }

    /// <summary>Reduce la temperatura actual sin permitir valores bajo 0°C.</summary>
    public void ReduceTemperature(float amount)
    {
        if (amount <= 0f || isGameOver) return;

        float newTemp = Mathf.Max(0f, currentTemperature - amount);
        if (!Mathf.Approximately(newTemp, currentTemperature))
        {
            currentTemperature = newTemp;
            NotifyTemperatureChanged();
        }
    }

    private void NotifyTemperatureChanged()
    {
        OnTemperatureChanged?.Invoke(currentTemperature, AlbedoPercent);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        initialTemperature = Mathf.Max(0f, initialTemperature);
        maximumTemperature = Mathf.Max(initialTemperature, maximumTemperature);
        baseTempRiseRate = Mathf.Max(0f, baseTempRiseRate);
        tempReductionPerClean = Mathf.Max(0f, tempReductionPerClean);
        maxSootCapacity = Mathf.Max(1, maxSootCapacity);
        criticalTempMultiplier = Mathf.Max(1f, criticalTempMultiplier);
    }
#endif
}
