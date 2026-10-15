using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Indicador visual flotante en espacio de mundo de alta visibilidad para objetos interactuables.
/// Muestra la tecla [E] al acercarse, indicador de acción (Sembrar, Hablar, Computadora),
/// progreso circular de mantener presionado, y cronómetro de cuenta regresiva (5s) con alertas.
/// </summary>
[DisallowMultipleComponent]
public class WorldInteractionPrompt : MonoBehaviour
{
    [Header("Configuración del Prompt")]
    [SerializeField] private string keyText = "E";
    [SerializeField] private string actionText = "Interactuar";
    [SerializeField] private float heightOffset = 1.35f;
    [SerializeField] private float baseScale = 0.038f;
    [SerializeField] private float bobSpeed = 3.5f;
    [SerializeField] private float bobAmount = 0.06f;

    [Header("Comportamiento de Cuenta Regresiva")]
    [Tooltip("Si es true, el cronómetro de peligro se muestra visible sobre la zona incluso si el jugador aún no está pegado a ella.")]
    [SerializeField] private bool alwaysShowCountdown = true;

    [Header("Referencias Visuales")]
    [SerializeField] private GameObject visualContainer;
    [SerializeField] private GameObject promptBadgeRoot;
    [SerializeField] private GameObject countdownBadgeRoot;
    [SerializeField] private TMP_Text promptKeyText;
    [SerializeField] private TMP_Text actionLabelText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private Image countdownFillImage;
    [SerializeField] private Image progressRingImage;
    [SerializeField] private TMP_Text penaltyNoticeText;

    private Transform visualTransform;
    private Canvas worldCanvas;
    private Vector3 initialLocalPos;
    private bool isPromptVisible = false;
    private bool isHeld = false;
    private float currentProgress = 0f;
    private float currentCountdown = -1f;
    private float maxCountdown = 5f;
    private Coroutine penaltyCoroutine;

    private void Awake()
    {
        // Si el valor serializado en prefab/escena guardó el valor minúsculo previo, actualizarlo
        if (baseScale < 0.03f)
        {
            baseScale = 0.038f;
        }
        if (heightOffset < 1.1f)
        {
            heightOffset = 1.35f;
        }

        EnsureVisualComponents();
        SetPromptVisible(false);
    }

    private void Start()
    {
        if (visualTransform != null)
        {
            initialLocalPos = new Vector3(0f, heightOffset, 0f);
        }
    }

    private void LateUpdate()
    {
        if (visualTransform == null) return;

        // Normalizar escala contra la escala acumulada (lossyScale) del padre
        Vector3 lossy = transform.lossyScale;
        float factorX = Mathf.Abs(lossy.x) > 0.001f ? (1f / lossy.x) : 1f;
        float factorY = Mathf.Abs(lossy.y) > 0.001f ? (1f / lossy.y) : 1f;

        // Calcular posición superior en espacio de mundo evitando multiplicaciones de escala en padres gigantes
        float topWorldY = transform.position.y;
        var col = GetComponent<Collider2D>();
        var sr = GetComponent<SpriteRenderer>();

        if (col != null)
        {
            topWorldY = col.bounds.max.y;
        }
        else if (sr != null)
        {
            topWorldY = sr.bounds.max.y;
        }
        else
        {
            topWorldY = transform.position.y + heightOffset;
        }

        // Efecto flotante suave en espacio de mundo
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        float targetY = (col != null || sr != null) ? (topWorldY + 0.35f + bob) : (topWorldY + bob);
        visualTransform.position = new Vector3(transform.position.x, targetY, transform.position.z);

        // Feedback al mantener presionado
        float pulse = isHeld ? (1.0f + Mathf.Sin(Time.time * 10f) * 0.08f) : 1.0f;
        visualTransform.localScale = new Vector3(factorX * baseScale * pulse, factorY * baseScale * pulse, 1f);

        // Mantener Canvas en orden de render correcto y vincular cámara
        if (worldCanvas != null)
        {
            if (worldCanvas.worldCamera == null)
            {
                worldCanvas.worldCamera = Camera.main;
            }
            worldCanvas.sortingLayerName = "Player";
            worldCanvas.sortingOrder = 100;
        }

        // Actualizar visibilidad de componentes
        bool showPrompt = isPromptVisible;
        if (promptBadgeRoot != null)
        {
            promptBadgeRoot.SetActive(showPrompt);
        }

        bool showCountdown = currentCountdown > 0f && (alwaysShowCountdown || isPromptVisible);
        if (countdownBadgeRoot != null)
        {
            countdownBadgeRoot.SetActive(showCountdown);
        }

        if (visualContainer != null)
        {
            visualContainer.SetActive(showPrompt || showCountdown || (penaltyNoticeText != null && penaltyNoticeText.gameObject.activeSelf));
        }
    }

    // ==========================================
    // CONTROL DE VISIBILIDAD Y ESTADO
    // ==========================================

    public void SetPromptVisible(bool visible)
    {
        isPromptVisible = visible;
        if (!visible)
        {
            isHeld = false;
            currentProgress = 0f;
            if (progressRingImage != null)
            {
                progressRingImage.fillAmount = 0f;
            }
        }
    }

    public void SetActionText(string text)
    {
        actionText = text;
        if (actionLabelText != null)
        {
            actionLabelText.text = text;
        }
    }

    public void SetKeyText(string text)
    {
        keyText = text;
        if (promptKeyText != null)
        {
            promptKeyText.text = text;
        }
    }

    public void SetKeyHeld(bool held)
    {
        isHeld = held;
    }

    public void SetProgress(float progress01)
    {
        currentProgress = Mathf.Clamp01(progress01);
        if (progressRingImage != null)
        {
            progressRingImage.fillAmount = currentProgress;
        }
    }

    public void SetAlwaysShowCountdown(bool always)
    {
        alwaysShowCountdown = always;
    }

    public void SetCountdown(float remainingSeconds, float maxDuration)
    {
        currentCountdown = remainingSeconds;
        maxCountdown = Mathf.Max(0.1f, maxDuration);

        if (countdownText != null && currentCountdown > 0f)
        {
            countdownText.text = $"{currentCountdown:0.0}s";

            // Transición de colores por urgencia
            if (currentCountdown <= 1.5f)
            {
                // Parpadeo rojo urgente
                bool flash = (Mathf.FloorToInt(Time.time * 6f) % 2) == 0;
                countdownText.color = flash ? Color.red : new Color(1f, 0.35f, 0.35f);
            }
            else if (currentCountdown <= 3.0f)
            {
                countdownText.color = new Color(1f, 0.85f, 0.2f); // Ámbar / amarillo
            }
            else
            {
                countdownText.color = new Color(0.4f, 1f, 0.6f); // Verde / seguro
            }
        }

        if (countdownFillImage != null && maxCountdown > 0f)
        {
            countdownFillImage.fillAmount = Mathf.Clamp01(currentCountdown / maxCountdown);
            if (currentCountdown <= 1.5f)
                countdownFillImage.color = Color.red;
            else if (currentCountdown <= 3.0f)
                countdownFillImage.color = new Color(1f, 0.8f, 0.2f);
            else
                countdownFillImage.color = new Color(0.2f, 0.9f, 0.4f);
        }
    }

    public void FlashPenalty(string message)
    {
        if (penaltyCoroutine != null) StopCoroutine(penaltyCoroutine);
        penaltyCoroutine = StartCoroutine(ShowPenaltyNotice(message));
    }

    private IEnumerator ShowPenaltyNotice(string message)
    {
        if (penaltyNoticeText != null)
        {
            penaltyNoticeText.gameObject.SetActive(true);
            penaltyNoticeText.text = message;
            penaltyNoticeText.color = Color.red;

            float elapsed = 0f;
            float dur = 1.2f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / dur);
                penaltyNoticeText.color = new Color(1f, 0.2f, 0.2f, alpha);
                yield return null;
            }
            penaltyNoticeText.gameObject.SetActive(false);
        }
    }

    // ==========================================
    // CONSTRUCCIÓN PROCEDURAL DE ELEMENTOS VISUALES
    // ==========================================

    private void EnsureVisualComponents()
    {
        if (visualContainer != null)
        {
            visualTransform = visualContainer.transform;
            return;
        }

        Transform existing = transform.Find("World_Prompt_Canvas");
        if (existing != null)
        {
            visualContainer = existing.gameObject;
            visualTransform = existing;
            worldCanvas = visualContainer.GetComponent<Canvas>();
            promptKeyText = visualContainer.transform.Find("Badge/KeyText")?.GetComponent<TMP_Text>();
            actionLabelText = visualContainer.transform.Find("Badge/ActionLabel")?.GetComponent<TMP_Text>();
            countdownText = visualContainer.transform.Find("Countdown/TimerText")?.GetComponent<TMP_Text>();
            return;
        }

        // Crear Canvas en WorldSpace con sorting layer "Player" y sortingOrder = 100
        GameObject promptGO = new GameObject("World_Prompt_Canvas");
        promptGO.transform.SetParent(transform, false);
        promptGO.transform.localPosition = new Vector3(0f, heightOffset, 0f);

        worldCanvas = promptGO.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = Camera.main;
        worldCanvas.overrideSorting = true;
        worldCanvas.sortingLayerName = "Player";
        worldCanvas.sortingOrder = 100;

        RectTransform canvasRT = promptGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(100f, 100f);
        canvasRT.localScale = Vector3.one * 0.02f; // Escala base en unidades de mundo

        // 1. HEADER DE CUENTA REGRESIVA (CRONÓMETRO DE 5s)
        GameObject cdGO = new GameObject("Countdown");
        cdGO.transform.SetParent(promptGO.transform, false);
        RectTransform cdRT = cdGO.AddComponent<RectTransform>();
        cdRT.sizeDelta = new Vector2(74f, 24f);
        cdRT.anchoredPosition = new Vector2(0f, 26f);

        Image cdBg = cdGO.AddComponent<Image>();
        cdBg.color = new Color(0.12f, 0.05f, 0.05f, 0.90f);

        // Barra de tiempo restante dentro del header
        GameObject cdFillGO = new GameObject("Fill");
        cdFillGO.transform.SetParent(cdGO.transform, false);
        RectTransform cdFillRT = cdFillGO.AddComponent<RectTransform>();
        cdFillRT.anchorMin = Vector2.zero;
        cdFillRT.anchorMax = Vector2.one;
        cdFillRT.offsetMin = new Vector2(2f, 2f);
        cdFillRT.offsetMax = new Vector2(-2f, -2f);
        countdownFillImage = cdFillGO.AddComponent<Image>();
        countdownFillImage.type = Image.Type.Filled;
        countdownFillImage.fillMethod = Image.FillMethod.Horizontal;
        countdownFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        countdownFillImage.fillAmount = 1f;
        countdownFillImage.color = new Color(0.2f, 0.9f, 0.4f, 0.45f);

        // Texto del cronómetro (ej. "5.0s")
        GameObject cdTextGO = new GameObject("TimerText");
        cdTextGO.transform.SetParent(cdGO.transform, false);
        RectTransform cdTextRT = cdTextGO.AddComponent<RectTransform>();
        cdTextRT.anchorMin = Vector2.zero;
        cdTextRT.anchorMax = Vector2.one;
        cdTextRT.offsetMin = Vector2.zero;
        cdTextRT.offsetMax = Vector2.zero;
        countdownText = cdTextGO.AddComponent<TextMeshProUGUI>();
        countdownText.text = "5.0s";
        countdownText.fontSize = 14;
        countdownText.fontStyle = FontStyles.Bold;
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.color = new Color(0.4f, 1f, 0.6f);
        countdownBadgeRoot = cdGO;

        Sprite circle = GetCircleSprite();

        // 2. BADGE PRINCIPAL DE LA TECLA [E]
        GameObject badgeGO = new GameObject("Badge");
        badgeGO.transform.SetParent(promptGO.transform, false);
        RectTransform badgeRT = badgeGO.AddComponent<RectTransform>();
        badgeRT.sizeDelta = new Vector2(36f, 36f);
        badgeRT.anchoredPosition = new Vector2(0f, -4f);

        Image badgeImg = badgeGO.AddComponent<Image>();
        badgeImg.sprite = circle;
        badgeImg.color = new Color(0.08f, 0.12f, 0.10f, 0.92f);

        // Borde circular
        GameObject borderGO = new GameObject("Border");
        borderGO.transform.SetParent(badgeGO.transform, false);
        RectTransform borderRT = borderGO.AddComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = new Vector2(-2f, -2f);
        borderRT.offsetMax = new Vector2(2f, 2f);
        Image borderImg = borderGO.AddComponent<Image>();
        borderImg.sprite = circle;
        borderImg.color = new Color(0.35f, 0.85f, 0.45f, 0.95f);
        borderGO.transform.SetAsFirstSibling();

        // Anillo de progreso al mantener 'E'
        GameObject ringGO = new GameObject("ProgressRing");
        ringGO.transform.SetParent(badgeGO.transform, false);
        RectTransform ringRT = ringGO.AddComponent<RectTransform>();
        ringRT.anchorMin = Vector2.zero;
        ringRT.anchorMax = Vector2.one;
        ringRT.offsetMin = new Vector2(-4f, -4f);
        ringRT.offsetMax = new Vector2(4f, 4f);
        progressRingImage = ringGO.AddComponent<Image>();
        progressRingImage.sprite = circle;
        progressRingImage.type = Image.Type.Filled;
        progressRingImage.fillMethod = Image.FillMethod.Radial360;
        progressRingImage.fillOrigin = (int)Image.Origin360.Top;
        progressRingImage.fillClockwise = true;
        progressRingImage.fillAmount = 0f;
        progressRingImage.color = new Color(1f, 0.85f, 0.2f, 0.95f);

        // Texto de la tecla: "E"
        GameObject textGO = new GameObject("KeyText");
        textGO.transform.SetParent(badgeGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        promptKeyText = textGO.AddComponent<TextMeshProUGUI>();
        promptKeyText.text = keyText;
        promptKeyText.fontSize = 20;
        promptKeyText.fontStyle = FontStyles.Bold;
        promptKeyText.alignment = TextAlignmentOptions.Center;
        promptKeyText.color = Color.white;

        // Subtítulo de acción: "Sembrar" / "Hablar"
        GameObject actionGO = new GameObject("ActionLabel");
        actionGO.transform.SetParent(badgeGO.transform, false);
        RectTransform actionRT = actionGO.AddComponent<RectTransform>();
        actionRT.sizeDelta = new Vector2(140f, 22f);
        actionRT.anchoredPosition = new Vector2(0f, -27f);
        actionLabelText = actionGO.AddComponent<TextMeshProUGUI>();
        actionLabelText.text = actionText;
        actionLabelText.fontSize = 13;
        actionLabelText.fontStyle = FontStyles.Bold;
        actionLabelText.alignment = TextAlignmentOptions.Center;
        actionLabelText.textWrappingMode = TextWrappingModes.NoWrap;
        actionLabelText.color = new Color(0.92f, 0.98f, 0.94f);

        promptBadgeRoot = badgeGO;

        // 3. AVISO DE PENALIZACIÓN (ej. "¡+5°C!")
        GameObject penaltyGO = new GameObject("PenaltyNotice");
        penaltyGO.transform.SetParent(promptGO.transform, false);
        RectTransform penaltyRT = penaltyGO.AddComponent<RectTransform>();
        penaltyRT.sizeDelta = new Vector2(80f, 20f);
        penaltyRT.anchoredPosition = new Vector2(0f, 44f);
        penaltyNoticeText = penaltyGO.AddComponent<TextMeshProUGUI>();
        penaltyNoticeText.text = "¡+5°C!";
        penaltyNoticeText.fontSize = 16;
        penaltyNoticeText.fontStyle = FontStyles.Bold;
        penaltyNoticeText.alignment = TextAlignmentOptions.Center;
        penaltyNoticeText.color = Color.red;
        penaltyGO.SetActive(false);

        visualContainer = promptGO;
        visualTransform = promptGO.transform;
    }

    private static Sprite circleSprite;
    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float radius = size * 0.48f;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01((radius - dist) + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return circleSprite;
    }
}

