using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interfaz visual del Termómetro Ambiental en el HUD.
/// Utiliza los assets de 'termomeñemaconassetsdefuegiothieloyvaina+rellenodeldiablasasaso.png':
/// - Marco del termómetro (Sprite _0)
/// - Relleno vertical del termómetro (Sprite _1)
/// - 3 Símbolos dinámicos de temperatura: Normal (_2), Calor/Peligro (_3), Frío (_4)
/// - Texto TMP de lectura térmica.
/// </summary>
public class ThermometerUI : MonoBehaviour
{
    [Header("Referencias de UI")]
    [SerializeField] private Image frameImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image symbolImage;
    [SerializeField] private TMP_Text valueText;

    [Header("Sprites del Termómetro")]
    [Tooltip("Marco o carcasa del termómetro. Sprite _0")]
    [SerializeField] private Sprite frameSprite;
    [Tooltip("Relleno líquido vertical. Sprite _1")]
    [SerializeField] private Sprite fillSprite;

    [Header("3 Símbolos de Estado de Temperatura")]
    [Tooltip("Símbolo de temperatura normal / templada (30%-70%). Sprite _2")]
    [SerializeField] private Sprite symbolNormal;
    [Tooltip("Símbolo de calor / peligro crítico (>70%). Sprite _3")]
    [SerializeField] private Sprite symbolHot;
    [Tooltip("Símbolo de frío / inicio (<30%). Sprite _4")]
    [SerializeField] private Sprite symbolCold;

    [Header("Configuración de Umbrales")]
    [Range(0f, 1f)] [SerializeField] private float coldThreshold = 0.30f;
    [Range(0f, 1f)] [SerializeField] private float hotThreshold = 0.70f;

    [Header("Formato de Texto")]
    [SerializeField] private bool showText = true;
    [SerializeField] private string textFormat = "{0:0.0} °C";

    private HazardMeterManager boundManager;

    private void Awake()
    {
        EnsureComponentReferences();
    }

    private void OnEnable()
    {
        HazardMeterManager.OnGlobalHazardValueChanged += HandleHazardChanged;
        TryBindManager();
    }

    private void OnDisable()
    {
        HazardMeterManager.OnGlobalHazardValueChanged -= HandleHazardChanged;
        if (boundManager != null)
        {
            boundManager.OnHazardValueChanged -= HandleHazardChanged;
            boundManager = null;
        }
    }

    private void Start()
    {
        TryBindManager();
    }

    private void EnsureComponentReferences()
    {
        if (frameImage == null)
        {
            frameImage = GetComponent<Image>();
            if (frameImage == null)
            {
                Transform t = transform.Find("Frame");
                if (t != null) frameImage = t.GetComponent<Image>();
            }
        }

        if (fillImage == null)
        {
            Transform t = transform.Find("Fill");
            if (t != null) fillImage = t.GetComponent<Image>();
        }

        if (symbolImage == null)
        {
            Transform t = transform.Find("Symbol");
            if (t != null) symbolImage = t.GetComponent<Image>();
        }

        if (valueText == null)
        {
            valueText = GetComponentInChildren<TMP_Text>();
        }

        // Configurar tipo de llenado vertical en fillImage si existe
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Vertical;
            fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        }
    }

    public void TryBindManager()
    {
        if (boundManager == null)
        {
            boundManager = HazardMeterManager.Instance ?? Object.FindFirstObjectByType<HazardMeterManager>();
            if (boundManager != null)
            {
                boundManager.OnHazardValueChanged += HandleHazardChanged;
                HandleHazardChanged(boundManager.CurrentValue, boundManager.HazardLimit);
            }
        }
    }

    public void HandleHazardChanged(float currentValue, float limit)
    {
        if (limit <= 0f) limit = 50f;
        float progress = Mathf.Clamp01(currentValue / limit);

        // Actualizar barra de llenado vertical
        if (fillImage != null)
        {
            fillImage.fillAmount = progress;
        }

        // Actualizar símbolo según el rango térmico
        UpdateSymbolState(progress);

        // Actualizar texto
        if (showText && valueText != null)
        {
            valueText.text = string.Format(textFormat, currentValue, limit);
        }
    }

    private void UpdateSymbolState(float progress)
    {
        if (symbolImage == null) return;

        if (progress > hotThreshold)
        {
            // Estado Calor / Peligro
            if (symbolHot != null)
            {
                symbolImage.sprite = symbolHot;
                symbolImage.gameObject.SetActive(true);
            }
        }
        else if (progress < coldThreshold)
        {
            // Estado Frío / Base
            if (symbolCold != null)
            {
                symbolImage.sprite = symbolCold;
                symbolImage.gameObject.SetActive(true);
            }
        }
        else
        {
            // Estado Normal
            if (symbolNormal != null)
            {
                symbolImage.sprite = symbolNormal;
                symbolImage.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Asigna rápidamente los sprites extraídos de 'termomeñemaconassetsdefuegiothieloyvaina+rellenodeldiablasasaso.png'.
    /// </summary>
    public void SetupSprites(Sprite frame, Sprite fill, Sprite normal, Sprite hot, Sprite cold)
    {
        frameSprite = frame;
        fillSprite = fill;
        symbolNormal = normal;
        symbolHot = hot;
        symbolCold = cold;

        if (frameImage != null && frameSprite != null) frameImage.sprite = frameSprite;
        if (fillImage != null && fillSprite != null) fillImage.sprite = fillSprite;

        if (boundManager != null)
            HandleHazardChanged(boundManager.CurrentValue, boundManager.HazardLimit);
        else
            HandleHazardChanged(0f, 50f);
    }
}
