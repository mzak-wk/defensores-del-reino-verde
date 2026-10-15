using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida dinámica en el HUD para Andrius con 3 estados visuales según el porcentaje de salud:
/// - Estado Saludable (> 70%): marco y relleno verdes
/// - Estado Alerta (30% - 70%): marco y relleno amarillos
/// - Estado Crítico (< 30%): marco y relleno rojos
/// Incluye barra de retardo (chip damage) animada en tiempo no escalado y texto numérico con TMP.
/// </summary>
public class PlayerHUDHealthBar : MonoBehaviour
{
    [Header("Referencias de UI")]
    [SerializeField] private Image frameImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image fillDelayImage;
    [SerializeField] private TMP_Text healthText;

    [Header("Sprites de los 3 Estados (barra de vida UI + relleno (s))")]
    [Tooltip("Marco para vida alta (>70%). Sprite _0")]
    [SerializeField] private Sprite frameHigh;
    [Tooltip("Relleno para vida alta (>70%). Sprite _1")]
    [SerializeField] private Sprite fillHigh;

    [Tooltip("Marco para vida media (30%-70%). Sprite _2")]
    [SerializeField] private Sprite frameMid;
    [Tooltip("Relleno para vida media (30%-70%). Sprite _3")]
    [SerializeField] private Sprite fillMid;

    [Tooltip("Marco para vida baja (<30%). Sprite _4")]
    [SerializeField] private Sprite frameLow;
    [Tooltip("Relleno para vida baja (<30%). Sprite _5")]
    [SerializeField] private Sprite fillLow;

    [Header("Parámetros de Animación")]
    [Tooltip("Velocidad de seguimiento de la barra de retardo.")]
    [SerializeField] private float chipSpeed = 1.8f;
    [Tooltip("Umbral superior de salud (por defecto 0.7 = 70%).")]
    [Range(0f, 1f)] [SerializeField] private float highThreshold = 0.70f;
    [Tooltip("Umbral inferior de salud (por defecto 0.3 = 30%).")]
    [Range(0f, 1f)] [SerializeField] private float lowThreshold = 0.30f;

    [Header("Formato de Texto")]
    [SerializeField] private bool showText = true;
    [SerializeField] private string textFormat = "{0:0} / {1:0}";

    private BarraVida boundVida;
    private float targetNormalizedHealth = 1f;
    public float CurrentNormalizedHealth => fillImage != null ? fillImage.fillAmount : targetNormalizedHealth;

    private void Awake()
    {
        EnsureComponentReferences();
    }

    private void OnEnable()
    {
        BarraVida.OnGlobalHealthChanged += HandleHealthChanged;
        TryBindPlayerVida();
    }

    private void OnDisable()
    {
        BarraVida.OnGlobalHealthChanged -= HandleHealthChanged;
        if (boundVida != null)
        {
            boundVida.OnHealthChanged -= HandleHealthChanged;
            boundVida = null;
        }
    }

    private void Start()
    {
        TryBindPlayerVida();
    }

    private void Update()
    {
        if (fillDelayImage != null)
        {
            fillDelayImage.fillAmount = Mathf.MoveTowards(
                fillDelayImage.fillAmount,
                targetNormalizedHealth,
                chipSpeed * Time.unscaledDeltaTime
            );
        }
    }

    private void EnsureComponentReferences()
    {
        if (fillImage == null)
        {
            Transform fillT = transform.Find("Fill");
            if (fillT != null) fillImage = fillT.GetComponent<Image>();
        }

        if (fillDelayImage == null)
        {
            Transform delayT = transform.Find("FillDelay");
            if (delayT != null) fillDelayImage = delayT.GetComponent<Image>();
        }

        if (frameImage == null)
        {
            frameImage = GetComponent<Image>();
            if (frameImage == null)
            {
                Transform frameT = transform.Find("Frame");
                if (frameT != null) frameImage = frameT.GetComponent<Image>();
            }
        }

        if (healthText == null)
        {
            healthText = GetComponentInChildren<TMP_Text>();
        }
    }

    public void TryBindPlayerVida()
    {
        if (boundVida == null)
        {
            boundVida = Object.FindFirstObjectByType<BarraVida>();
            if (boundVida != null)
            {
                boundVida.OnHealthChanged += HandleHealthChanged;
                HandleHealthChanged(boundVida.currentHP, boundVida.maxHP);
            }
        }
    }

    public void HandleHealthChanged(float currentHP, float maxHP)
    {
        if (maxHP <= 0f) maxHP = 100f;
        targetNormalizedHealth = Mathf.Clamp01(currentHP / maxHP);

        // Actualizar barra instantánea
        if (fillImage != null)
        {
            fillImage.fillAmount = targetNormalizedHealth;
        }

        // Actualizar texto
        if (showText && healthText != null)
        {
            healthText.text = string.Format(textFormat, Mathf.Max(0f, currentHP), maxHP);
        }

        // Actualizar sprites según los 3 estados
        UpdateVisualState(targetNormalizedHealth);
    }

    private void UpdateVisualState(float percent)
    {
        if (percent > highThreshold)
        {
            // Estado 1: Verde / Saludable
            if (frameImage != null && frameHigh != null) frameImage.sprite = frameHigh;
            if (fillImage != null && fillHigh != null) fillImage.sprite = fillHigh;
        }
        else if (percent > lowThreshold)
        {
            // Estado 2: Amarillo / Alerta
            if (frameImage != null && frameMid != null) frameImage.sprite = frameMid;
            if (fillImage != null && fillMid != null) fillImage.sprite = fillMid;
        }
        else
        {
            // Estado 3: Rojo / Crítico
            if (frameImage != null && frameLow != null) frameImage.sprite = frameLow;
            if (fillImage != null && fillLow != null) fillImage.sprite = fillLow;
        }
    }

    /// <summary>
    /// Configuración rápida de sprites de la hoja 'barra de vida UI + relleno (s).png'.
    /// </summary>
    public void SetupSprites(Sprite fHigh, Sprite fillH, Sprite fMid, Sprite fillM, Sprite fLow, Sprite fillL)
    {
        frameHigh = fHigh;
        fillHigh = fillH;
        frameMid = fMid;
        fillMid = fillM;
        frameLow = fLow;
        fillLow = fillL;

        if (boundVida != null)
            HandleHealthChanged(boundVida.currentHP, boundVida.maxHP);
        else
            HandleHealthChanged(100f, 100f);
    }
}
