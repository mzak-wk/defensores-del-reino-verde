using System;
using UnityEngine;

/// <summary>
/// Mantiene el valor global de CO2 para la escena actual.
/// </summary>
public class CO2Manager : MonoBehaviour
{
    public static CO2Manager Instance { get; private set; }
    public static event Action OnGameOver;

    [Header("Estado ambiental")]
    [Tooltip("Concentracion inicial de CO2 en partes por millon.")]
    [SerializeField, Min(0f)] private float initialCO2 = 350f;

    [Tooltip("Concentracion maxima (peligro) de CO2 en partes por millon.")]
    [SerializeField, Min(0f)] private float maximumCO2 = 450f;

    [Tooltip("PPM de CO2 que aumentan por segundo por cada zona erosionada activa.")]
    [SerializeField, Min(0f)] private float co2RiseRatePerZone = 0.5f;

    [SerializeField, Min(0f)] private float currentCO2 = 350f;

    private bool isGameOver;

    /// <summary>Se invoca despues de cada cambio de CO2.</summary>
    public event Action<float> OnCO2Changed;

    public float CurrentCO2 => currentCO2;

    private int activeSoilZonesCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Prevenir que tasas legacy descalibradas (ej. 10 PPM/s) causen instakill en 5 segundos
        if (co2RiseRatePerZone > 2f)
        {
            co2RiseRatePerZone = 0.5f;
        }
        currentCO2 = Mathf.Clamp(initialCO2, 0f, maximumCO2);
    }

    private void Start()
    {
        // Permite que la UI se sincronice aunque se habilite despues del manager.
        NotifyCO2Changed();
    }

    private void Update()
    {
        // Si el gestor global HazardMeterManager está presente, él gobierna el peligro y Game Over
        if (HazardMeterManager.Instance != null)
            return;

        if (activeSoilZonesCount <= 0 || co2RiseRatePerZone <= 0f)
            return;

        float newCO2 = Mathf.Min(maximumCO2, currentCO2 + co2RiseRatePerZone * activeSoilZonesCount * Time.deltaTime);
        if (Mathf.Approximately(newCO2, currentCO2))
            return;

        currentCO2 = newCO2;
        NotifyCO2Changed();

        if (currentCO2 >= maximumCO2 && !isGameOver)
        {
            currentCO2 = maximumCO2;
            isGameOver = true;
            OnGameOver?.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Notifica al gestor que una zona erosionada sin sembrar aparecio.</summary>
    public void RegisterActiveZone()
    {
        activeSoilZonesCount++;
    }

    /// <summary>Notifica al gestor que una zona erosionada dejo de estar activa.</summary>
    public void UnregisterActiveZone()
    {
        activeSoilZonesCount = Mathf.Max(0, activeSoilZonesCount - 1);
    }

    /// <summary>Reduce el CO2 sin permitir valores negativos.</summary>
    public void ReduceCO2(float amount)
    {
        if (amount <= 0f)
            return;

        float newCO2 = Mathf.Max(0f, currentCO2 - amount);
        if (Mathf.Approximately(newCO2, currentCO2))
            return;

        currentCO2 = newCO2;
        NotifyCO2Changed();
    }

    /// <summary>Aumenta el CO2 sin permitir exceder el máximo (penalización térmica).</summary>
    public void IncreaseCO2(float amount)
    {
        if (amount <= 0f)
            return;

        float newCO2 = Mathf.Min(maximumCO2, currentCO2 + amount);
        if (Mathf.Approximately(newCO2, currentCO2))
            return;

        currentCO2 = newCO2;
        NotifyCO2Changed();

        if (currentCO2 >= maximumCO2 && !isGameOver && HazardMeterManager.Instance == null)
        {
            isGameOver = true;
            OnGameOver?.Invoke();
        }
    }

    private void NotifyCO2Changed()
    {
        OnCO2Changed?.Invoke(currentCO2);
    }
}
