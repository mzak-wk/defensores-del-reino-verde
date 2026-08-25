using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Permite restaurar una zona de suelo al permanecer dentro de su trigger.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ErodedSoilZone : MonoBehaviour
{
    [Header("Siembra")]
    [SerializeField, Min(0f)] private float requiredTime = 3f;
    [SerializeField, Min(0f)] private float co2ReductionAmount = 10f;
    [SerializeField] private GameObject treePrefab;

    [Header("UI de progreso (opcional)")]
    [SerializeField] private Slider progressBarUI;

    private readonly HashSet<Collider2D> playerColliders = new HashSet<Collider2D>();
    private Collider2D zoneCollider;
    private float currentProgress;
    private bool isCompleted;
    private bool isRegistered;

    /// <summary>Indica si esta zona ya genero su arbol y aplico su reduccion de CO2.</summary>
    public bool IsCompleted => isCompleted;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        UpdateProgressUI(false);
    }

    private void OnEnable()
    {
        if (isCompleted)
            return;

        if (CO2Manager.Instance != null)
        {
            CO2Manager.Instance.RegisterActiveZone();
            isRegistered = true;
        }
    }

    private void OnDisable()
    {
        UnregisterFromManager();
    }

    private void Update()
    {
        if (isCompleted || playerColliders.Count == 0)
            return;

        currentProgress = Mathf.Min(requiredTime, currentProgress + Time.deltaTime);
        UpdateProgressUI(true);

        if (currentProgress >= requiredTime)
            CompletePlanting();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCompleted || !other.CompareTag("Player"))
            return;

        playerColliders.Add(other);
        UpdateProgressUI(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerColliders.Remove(other);
        UpdateProgressUI(playerColliders.Count > 0);
    }

    private void CompletePlanting()
    {
        isCompleted = true;
        playerColliders.Clear();
        UpdateProgressUI(false);

        if (treePrefab != null)
        {
            Instantiate(treePrefab, transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogError("[ErodedSoilZone] treePrefab no está asignado en el Inspector!", this);
        }

        UnregisterFromManager();

        if (HazardMeterManager.Instance != null)
            HazardMeterManager.Instance.ReduceHazard(co2ReductionAmount);

        if (CO2Manager.Instance != null)
            CO2Manager.Instance.ReduceCO2(co2ReductionAmount);
        else if (HazardMeterManager.Instance == null)
            Debug.LogWarning($"[{nameof(ErodedSoilZone)}] No existe un gestor ambiental activo en la escena.", this);

        Destroy(gameObject);
    }

    private void UnregisterFromManager()
    {
        if (!isRegistered)
            return;

        if (CO2Manager.Instance != null)
            CO2Manager.Instance.UnregisterActiveZone();
        isRegistered = false;
    }

    private void UpdateProgressUI(bool visible)
    {
        if (progressBarUI == null)
            return;

        progressBarUI.gameObject.SetActive(visible && !isCompleted);
        progressBarUI.minValue = 0f;
        progressBarUI.maxValue = 1f;
        progressBarUI.value = requiredTime <= 0f ? 1f : Mathf.Clamp01(currentProgress / requiredTime);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        requiredTime = Mathf.Max(0f, requiredTime);
        co2ReductionAmount = Mathf.Max(0f, co2ReductionAmount);
    }
#endif
}
