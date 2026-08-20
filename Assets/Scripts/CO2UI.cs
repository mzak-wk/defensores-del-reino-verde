using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presenta el CO2 actual en un Slider y/o un texto de TextMeshPro.
/// </summary>
public class CO2UI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CO2Manager co2Manager;
    [SerializeField] private Slider co2Slider;
    [SerializeField] private TMP_Text co2Text;

    [Header("Rango del medidor")]
    [SerializeField, Min(0f)] private float minimumCO2 = 0f;
    [SerializeField, Min(0f)] private float maximumCO2 = 400f;

    private CO2Manager subscribedManager;

    private void OnEnable()
    {
        SubscribeToManager();
        RefreshUI();
    }

    private void Start()
    {
        // Da oportunidad al singleton de terminar Awake si la UI se habilita antes.
        SubscribeToManager();
        RefreshUI();
    }

    private void OnDisable()
    {
        UnsubscribeFromManager();
    }

    private void SubscribeToManager()
    {
        CO2Manager manager = co2Manager != null ? co2Manager : CO2Manager.Instance;
        if (manager == subscribedManager)
            return;

        UnsubscribeFromManager();
        subscribedManager = manager;

        if (subscribedManager != null)
            subscribedManager.OnCO2Changed += UpdateCO2Display;
    }

    private void UnsubscribeFromManager()
    {
        if (subscribedManager != null)
            subscribedManager.OnCO2Changed -= UpdateCO2Display;

        subscribedManager = null;
    }

    private void RefreshUI()
    {
        CO2Manager manager = co2Manager != null ? co2Manager : CO2Manager.Instance;
        if (manager != null)
            UpdateCO2Display(manager.CurrentCO2);
    }

    private void UpdateCO2Display(float currentCO2)
    {
        if (co2Slider != null)
        {
            float max = Mathf.Max(minimumCO2, maximumCO2);
            co2Slider.minValue = minimumCO2;
            co2Slider.maxValue = max;
            co2Slider.value = Mathf.Clamp(currentCO2, minimumCO2, max);
        }

        if (co2Text != null)
            co2Text.text = $"{currentCO2:0.0} PPM";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minimumCO2 = Mathf.Max(0f, minimumCO2);
        maximumCO2 = Mathf.Max(minimumCO2, maximumCO2);
    }
#endif
}
