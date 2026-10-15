using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interfaz visual del Termómetro Ambiental / Medidor de Ulti en el HUD.
/// Utiliza los assets de 'termomeñemaconassetsdefuegiothieloyvaina+rellenodeldiablasasaso.png':
/// - Marco del termómetro (Sprite _0)
/// - Relleno vertical del termómetro (Sprite _1)
/// - 3 Símbolos dinámicos de temperatura: Normal/Sol (_2), Calor/Fuego (_3), Frío/Hielo (_4)
/// - Texto TMP de lectura térmica (°C) o porcentaje de ulti (%).
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
    [Tooltip("Símbolo de temperatura normal / soleada / templada. Sprite _2")]
    [SerializeField] private Sprite symbolNormal;
    [Tooltip("Símbolo de calor / fuego / peligro crítico. Sprite _3")]
    [SerializeField] private Sprite symbolHot;
    [Tooltip("Símbolo de frío / hielo / inicio polar. Sprite _4")]
    [SerializeField] private Sprite symbolCold;

    [Header("Ecosistema del Nivel")]
    [Tooltip("Tipo de ecosistema activo. Si hay un HazardMeterManager en escena, se sincroniza automáticamente.")]
    [SerializeField] private EcosystemType currentEcosystem = EcosystemType.TropicalForest;

    [Header("Formato de Lectura")]
    [SerializeField] private bool showText = true;
    [SerializeField] private string temperatureFormat = "{0:0.0} °C";
    [SerializeField] private string bossUltiFormat = "{0:0}%";

    private HazardMeterManager boundManager;

    private void Awake()
    {
        EnsureComponentReferences();
    }

    private void OnEnable()
    {
        HazardMeterManager.OnGlobalHazardValueChanged += HandleGlobalHazardChanged;
        TryBindManager();
    }

    private void OnDisable()
    {
        HazardMeterManager.OnGlobalHazardValueChanged -= HandleGlobalHazardChanged;
        if (boundManager != null)
        {
            boundManager.OnHazardValueChanged -= HandleHazardChanged;
            boundManager.OnEcosystemHazardChanged -= HandleEcosystemHazardChanged;
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
            if (boundManager == null)
            {
                // Auto-instanciar HazardMeterManager según la escena para garantizar sincronización perfecta
                GameObject mgrGO = new GameObject("HazardMeterManager");
                boundManager = mgrGO.AddComponent<HazardMeterManager>();
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                EcosystemType eco = EcosystemType.TropicalForest;
                if (sceneName.Contains("Ice")) eco = EcosystemType.PolarIce;
                else if (sceneName.Contains("Beach")) eco = EcosystemType.SunnyBeach;
                else if (sceneName.Contains("Final")) eco = EcosystemType.FinalBossUlti;
                boundManager.ApplyEcosystemPreset(eco);
            }

            if (boundManager != null)
            {
                boundManager.OnHazardValueChanged += HandleHazardChanged;
                boundManager.OnEcosystemHazardChanged += HandleEcosystemHazardChanged;
                currentEcosystem = boundManager.CurrentEcosystem;
                HandleHazardChanged(boundManager.CurrentValue, boundManager.HazardLimit);
            }
        }
    }

    private void HandleGlobalHazardChanged(float currentValue, float limit)
    {
        if (boundManager == null)
        {
            TryBindManager();
        }
        else
        {
            HandleHazardChanged(currentValue, limit);
        }
    }

    private void HandleEcosystemHazardChanged(float currentValue, float limit, EcosystemType ecosystem)
    {
        currentEcosystem = ecosystem;
        HandleHazardChanged(currentValue, limit);
    }

    public void HandleHazardChanged(float currentValue, float limit)
    {
        float minVal = boundManager != null ? boundManager.MinDisplayValue : 0f;
        float range = limit - minVal;
        if (range <= 0.001f) range = 50f;

        float progress = Mathf.Clamp01((currentValue - minVal) / range);

        // Actualizar barra de llenado vertical
        if (fillImage != null)
        {
            fillImage.fillAmount = progress;
        }

        // Actualizar símbolo dinámicamente según el ecosistema del nivel
        UpdateSymbolForEcosystem(currentValue, progress, currentEcosystem);

        // Actualizar texto
        if (showText && valueText != null)
        {
            if (currentEcosystem == EcosystemType.FinalBossUlti)
            {
                valueText.text = string.Format(bossUltiFormat, currentValue);
            }
            else
            {
                valueText.text = string.Format(temperatureFormat, currentValue);
            }
        }
    }

    /// <summary>
    /// Cambia el ícono dinámicamente según la temperatura actual y el ecosistema del nivel:
    /// - Nivel 1 (Bosque Tropical): Inicia en Sol (_2), calor extremo pasa a Fuego (_3).
    /// - Nivel 2 (Polo Norte): Inicia en Hielo (_4), deshielo pasa a Sol (_2), derretimiento crítico a Fuego (_3).
    /// - Nivel 3 (Playa Soleada): Inicia en Sol (_2), calor extremo pasa a Fuego (_3).
    /// - Nivel Final (Boss Ulti): 0-40% Hielo (_4), 40-80% Sol (_2), >80% Fuego (_3).
    /// </summary>
    private void UpdateSymbolForEcosystem(float currentVal, float progress, EcosystemType ecosystem)
    {
        if (symbolImage == null) return;

        Sprite selectedSprite = symbolNormal;

        switch (ecosystem)
        {
            case EcosystemType.TropicalForest:
                // Base: Bosque templado/tropical (~24°C = Sol).
                // Si la erosión calienta a más de 38°C o >65% -> Fuego.
                // Si baja mucho (< 20°C o <15%) -> Hielo.
                if (progress >= 0.65f || currentVal >= 38f)
                    selectedSprite = symbolHot != null ? symbolHot : symbolNormal;
                else if (progress <= 0.15f || currentVal <= 20f)
                    selectedSprite = symbolCold != null ? symbolCold : symbolNormal;
                else
                    selectedSprite = symbolNormal;
                break;

            case EcosystemType.PolarIce:
                // Base: Polo Norte congelado (<0°C = Hielo).
                // Descongelamiento (0°C a 10°C, 40%-80%) -> Sol.
                // Derretimiento crítico (>10°C o >80%) -> Fuego.
                if (progress >= 0.80f || currentVal >= 10f)
                    selectedSprite = symbolHot != null ? symbolHot : symbolNormal;
                else if (progress >= 0.40f || currentVal >= 0f)
                    selectedSprite = symbolNormal != null ? symbolNormal : symbolCold;
                else
                    selectedSprite = symbolCold;
                break;

            case EcosystemType.SunnyBeach:
                // Base: Playa cálida soleada (32°C = Sol).
                // Residuos y calor extremo (>48°C o >65%) -> Fuego.
                // Si baja de 28°C (<15%) -> Hielo.
                if (progress >= 0.65f || currentVal >= 48f)
                    selectedSprite = symbolHot != null ? symbolHot : symbolNormal;
                else if (progress <= 0.15f || currentVal <= 28f)
                    selectedSprite = symbolCold != null ? symbolCold : symbolNormal;
                else
                    selectedSprite = symbolNormal;
                break;

            case EcosystemType.FinalBossUlti:
                // Medidor de Definitiva del Jefe Final:
                // 0% a 40%: Hielo (carga baja/fría).
                // 40% a 80%: Sol (acumulando energía).
                // >80%: Fuego (¡Peligro inminente de Ulti!).
                if (currentVal >= 80f || progress >= 0.80f)
                    selectedSprite = symbolHot != null ? symbolHot : symbolNormal;
                else if (currentVal >= 40f || progress >= 0.40f)
                    selectedSprite = symbolNormal != null ? symbolNormal : symbolCold;
                else
                    selectedSprite = symbolCold;
                break;
        }

        if (selectedSprite != null)
        {
            symbolImage.sprite = selectedSprite;
            symbolImage.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Configura manualmente el ecosistema y actualiza la vista.
    /// </summary>
    public void SetEcosystem(EcosystemType ecosystem)
    {
        currentEcosystem = ecosystem;
        if (boundManager != null)
            HandleHazardChanged(boundManager.CurrentValue, boundManager.HazardLimit);
        else
            HandleHazardChanged(0f, 50f);
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
