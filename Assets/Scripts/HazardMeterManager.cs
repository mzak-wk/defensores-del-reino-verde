using System;
using UnityEngine;

/// <summary>
/// Gestor genérico y extensible de peligro ambiental por nivel (Termómetro, CO2, Radiación, etc.).
/// Mantiene el valor actual, el límite configurable (hazardLimit), el ritmo de incremento durante combate
/// y dispara el Game Over unificado hacia DeathMenuController cuando se alcanza dicho límite.
/// </summary>
public class HazardMeterManager : MonoBehaviour
{
    public static HazardMeterManager Instance { get; private set; }

    /// <summary>Disparado cuando el peligro alcanza el límite configurado.</summary>
    public static event Action<DeathReason> OnHazardGameOver;

    [Header("Configuración del Medidor de Peligro / Termómetro")]
    [Tooltip("Límite máximo o temperatura crítica que provoca Game Over.")]
    [SerializeField, Min(1f)] private float hazardLimit = 50f;

    [Tooltip("Valor inicial al comenzar el nivel (por defecto 0).")]
    [SerializeField, Min(0f)] private float initialValue = 0f;

    [Tooltip("Tasa pasiva de incremento por segundo durante oleadas activas.")]
    [SerializeField, Min(0f)] private float riseRatePerSecond = 1.0f;

    [Tooltip("Reducción de peligro por acción de limpieza o árbol plantado.")]
    [SerializeField, Min(0f)] private float defaultHazardReduction = 5.0f;

    [Header("Tipo de Peligro")]
    [SerializeField] private DeathReason deathReason = DeathReason.TemperatureLimit;

    [Header("Estado Runtime")]
    [SerializeField, Min(0f)] private float currentValue = 0f;
    [SerializeField] private bool isCombatActive = false;
    [SerializeField] private bool isGameOver = false;

    // Eventos
    public event Action<float, float> OnHazardValueChanged;
    public static event Action<float, float> OnGlobalHazardValueChanged;

    public float CurrentValue => currentValue;
    public float HazardLimit => hazardLimit;
    public float DefaultHazardReduction => defaultHazardReduction;
    public bool IsCombatActive => isCombatActive;
    public DeathReason HazardDeathReason => deathReason;

    public float NormalizedProgress => (hazardLimit <= 0f) ? 0f : Mathf.Clamp01(currentValue / hazardLimit);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentValue = Mathf.Clamp(initialValue, 0f, hazardLimit);
        isGameOver = false;
    }

    private void Start()
    {
        SubscribeToSpawnerEvents();
        NotifyValueChanged();
    }

    private void Update()
    {
        if (isGameOver) return;

        // El medidor incrementa cuando el combate está activo (o si riseRate > 0)
        if (isCombatActive && riseRatePerSecond > 0f)
        {
            float newVal = Mathf.Min(hazardLimit, currentValue + riseRatePerSecond * Time.deltaTime);

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

        OnHazardGameOver?.Invoke(deathReason);

        if (DeathMenuController.Instance != null)
        {
            DeathMenuController.Instance.TriggerDeathSequence(deathReason);
        }
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

        currentValue = Mathf.Max(0f, currentValue - amount);
        NotifyValueChanged();
    }

    public void SetHazardActive(bool active)
    {
        isCombatActive = active;
    }

    public void SetHazardLimit(float newLimit)
    {
        hazardLimit = Mathf.Max(1f, newLimit);
        currentValue = Mathf.Clamp(currentValue, 0f, hazardLimit);
        NotifyValueChanged();
    }

    public void NotifyValueChanged()
    {
        OnHazardValueChanged?.Invoke(currentValue, hazardLimit);
        OnGlobalHazardValueChanged?.Invoke(currentValue, hazardLimit);
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
}
