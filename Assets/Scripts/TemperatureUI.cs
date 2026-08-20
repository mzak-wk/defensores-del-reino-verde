using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presenta la temperatura actual y el Albedo en un Slider y textos TMP.
/// Debe colocarse como hijo de HUD_Root para respetar ocultación automática de overlays.
/// </summary>
public class TemperatureUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TemperatureManager temperatureManager;
    [SerializeField] private Slider temperatureSlider;
    [SerializeField] private TMP_Text temperatureText;
    [SerializeField] private TMP_Text albedoText;
    [SerializeField] private Image sliderFillImage;

    [Header("Rango y Colores del Slider")]
    [SerializeField, Min(0f)] private float minimumTemperature = 0f;
    [SerializeField, Min(0f)] private float maximumTemperature = 100f;

    [Tooltip("Color del slider a temperaturas bajas (<50°C).")]
    [SerializeField] private Color coolColor = new Color(0.2f, 0.8f, 1f, 1f); // Cyan / Celeste

    [Tooltip("Color del slider a temperaturas medias (50°C - 75°C).")]
    [SerializeField] private Color warmColor = new Color(1f, 0.65f, 0f, 1f); // Naranja

    [Tooltip("Color del slider a temperaturas críticas (>75°C).")]
    [SerializeField] private Color hotColor = new Color(1f, 0.2f, 0.2f, 1f); // Rojo

    private TemperatureManager subscribedManager;

    private void OnEnable()
    {
        SubscribeToManager();
        RefreshUI();
    }

    private void Start()
    {
        SubscribeToManager();
        RefreshUI();
    }

    private void OnDisable()
    {
        UnsubscribeFromManager();
    }

    private void SubscribeToManager()
    {
        TemperatureManager manager = temperatureManager != null ? temperatureManager : TemperatureManager.Instance;
        if (manager == subscribedManager)
            return;

        UnsubscribeFromManager();
        subscribedManager = manager;

        if (subscribedManager != null)
        {
            subscribedManager.OnTemperatureChanged += UpdateTemperatureDisplay;
        }
    }

    private void UnsubscribeFromManager()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OnTemperatureChanged -= UpdateTemperatureDisplay;
        }

        subscribedManager = null;
    }

    private void RefreshUI()
    {
        TemperatureManager manager = temperatureManager != null ? temperatureManager : TemperatureManager.Instance;
        if (manager != null)
        {
            UpdateTemperatureDisplay(manager.CurrentTemperature, manager.AlbedoPercent);
        }
    }

    private void UpdateTemperatureDisplay(float currentTemp, float currentAlbedo)
    {
        float max = Mathf.Max(minimumTemperature, maximumTemperature);

        if (temperatureSlider != null)
        {
            temperatureSlider.minValue = minimumTemperature;
            temperatureSlider.maxValue = max;
            temperatureSlider.value = Mathf.Clamp(currentTemp, minimumTemperature, max);
        }

        if (temperatureText != null)
        {
            temperatureText.text = $"{currentTemp:0.0} °C";
        }

        if (albedoText != null)
        {
            albedoText.text = $"Albedo: {currentAlbedo:0}%";
        }

        if (sliderFillImage != null)
        {
            float normalized = Mathf.InverseLerp(minimumTemperature, max, currentTemp);
            if (normalized < 0.5f)
            {
                // De 0% a 50%: Transición de cool a warm
                sliderFillImage.color = Color.Lerp(coolColor, warmColor, normalized / 0.5f);
            }
            else
            {
                // De 50% a 100%: Transición de warm a hot
                sliderFillImage.color = Color.Lerp(warmColor, hotColor, (normalized - 0.5f) / 0.5f);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minimumTemperature = Mathf.Max(0f, minimumTemperature);
        maximumTemperature = Mathf.Max(minimumTemperature, maximumTemperature);
    }
#endif
}
