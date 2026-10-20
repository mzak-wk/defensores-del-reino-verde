using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controlador visual del contador de LutroCoins en el HUD del jugador.
/// Se ubica de forma elegante en la esquina superior derecha de la pantalla (Top-Right).
/// Persiste entre escenas (DontDestroyOnLoad) y reacciona en tiempo real a transacciones de GoldManager.
/// Oculta automáticamente el HUD durante cinemáticas, diálogos y pantallas de menú.
/// </summary>
public class CoinHUDController : MonoBehaviour
{
    private static CoinHUDController _instance;
    public static CoinHUDController Instance
    {
        get
        {
            if (_instance == null)
            {
                EnsureInstance();
            }
            return _instance;
        }
    }

    [Header("Referencias de UI")]
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform badgeContainer;
    [SerializeField] private Image coinIconImage;
    [SerializeField] private TMP_Text coinCountText;

    [Header("Estilo Visual")]
    [SerializeField] private Color normalTextColor = new Color(0.98f, 0.80f, 0.08f, 1f); // #FACC15
    [SerializeField] private Color gainFlashColor = new Color(1f, 0.96f, 0.65f, 1f);   // #FEF08A
    [SerializeField] private Color spendFlashColor = new Color(0.97f, 0.44f, 0.44f, 1f); // #F87171

    private Coroutine punchAnimationCoroutine;
    private Coroutine deltaFeedbackCoroutine;
    private bool isExplicitlyHidden = false;

    [Header("Feedback de Incremento")]
    [SerializeField] private TMP_Text deltaFeedbackText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AutoInitialize()
    {
        EnsureInstance();
    }

    public static void EnsureInstance()
    {
        if (_instance != null) return;

        _instance = FindFirstObjectByType<CoinHUDController>();
        if (_instance != null)
        {
            _instance.EnsureReferences();
            _instance.EvaluateSceneVisibility(SceneManager.GetActiveScene().name);
            _instance.RefreshDisplay();
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("CoinHUD") ?? Resources.Load<GameObject>("UI/CoinHUD");
        GameObject rootGo;
        if (prefab != null)
        {
            rootGo = Instantiate(prefab);
            rootGo.name = "[CoinHUD]";
            _instance = rootGo.GetComponent<CoinHUDController>();
            if (_instance == null) _instance = rootGo.AddComponent<CoinHUDController>();
        }
        else
        {
            rootGo = new GameObject("[CoinHUD]");
            _instance = rootGo.AddComponent<CoinHUDController>();
            _instance.BuildProceduralUI();
        }

        _instance.EnsureReferences();
        DontDestroyOnLoad(rootGo);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureReferences();
        EvaluateSceneVisibility(SceneManager.GetActiveScene().name);
        RefreshDisplay();
    }

    private void OnEnable()
    {
        GoldManager.OnGoldChanged -= HandleGoldChanged;
        GoldManager.OnGoldChanged += HandleGoldChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureReferences();
        EvaluateSceneVisibility(SceneManager.GetActiveScene().name);
        RefreshDisplay();
    }

    private void OnDisable()
    {
        GoldManager.OnGoldChanged -= HandleGoldChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        EnsureReferences();
        EvaluateSceneVisibility(SceneManager.GetActiveScene().name);
        RefreshDisplay();
    }

    private void Update()
    {
        bool shouldHide = isExplicitlyHidden || IsCutsceneOrCinematicActive();
        bool isCurrentlyVisible = canvasGroup != null ? canvasGroup.alpha > 0.01f : (hudCanvas != null && hudCanvas.enabled);

        if (shouldHide && isCurrentlyVisible)
        {
            SetHUDVisible(false);
        }
        else if (!shouldHide && !isCurrentlyVisible)
        {
            SetHUDVisible(true);
            RefreshDisplay();
        }
    }

    /// <summary>
    /// Comprueba si la escena actual es un menú o cinemática introductoria de pantalla completa.
    /// En todas las escenas de gameplay (LevelWoods, LevelBeach, LevelIce, LevelHUB, etc.)
    /// el HUD de monedas debe permanecer siempre visible de inmediato.
    /// </summary>
    private bool IsCutsceneOrCinematicActive()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(currentScene))
        {
            if (currentScene.Equals("MenuScene", System.StringComparison.OrdinalIgnoreCase) ||
                currentScene.Equals("BootScene", System.StringComparison.OrdinalIgnoreCase) ||
                currentScene.Equals("CinematicScene", System.StringComparison.OrdinalIgnoreCase) ||
                currentScene.IndexOf("cinematic", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        // Si el juego está en Game Over modal completo
        if (UIManager.Instance != null && UIManager.Instance.IsGameOver)
        {
            return true;
        }

        return false;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureReferences();
        EvaluateSceneVisibility(scene.name);
        RefreshDisplay();
    }

    /// <summary>
    /// Garantiza que todas las referencias, fuentes, estilos y posicionamiento
    /// del texto estén 100% correctos y visibles tanto en prefabs como en procedural.
    /// </summary>
    public void EnsureReferences()
    {
        if (hudCanvas == null) hudCanvas = GetComponent<Canvas>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        if (badgeContainer == null)
        {
            var badgeTr = transform.Find("CoinBadge");
            if (badgeTr != null) badgeContainer = badgeTr.GetComponent<RectTransform>();
        }

        if (badgeContainer == null)
        {
            BuildProceduralUI();
            return;
        }

        if (coinIconImage == null)
        {
            var iconTr = badgeContainer.Find("Icon");
            if (iconTr != null) coinIconImage = iconTr.GetComponent<Image>();
        }

        if (coinCountText == null)
        {
            var textTr = badgeContainer.Find("CountText");
            if (textTr != null) coinCountText = textTr.GetComponent<TMP_Text>();
        }

        if (coinCountText == null)
        {
            coinCountText = GetComponentInChildren<TMP_Text>();
        }

        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/Nunito SDF");

        // Configuración y posicionamiento robusto de coinCountText
        if (coinCountText != null)
        {
            if (fontAsset != null && coinCountText.font != fontAsset)
            {
                coinCountText.font = fontAsset;
            }

            coinCountText.color = normalTextColor;
            coinCountText.fontSize = 22f;
            coinCountText.fontStyle = FontStyles.Bold;
            coinCountText.alignment = TextAlignmentOptions.MidlineLeft;
            coinCountText.textWrappingMode = TextWrappingModes.NoWrap;
            coinCountText.overflowMode = TextOverflowModes.Overflow;
            coinCountText.raycastTarget = false;

            RectTransform textRt = coinCountText.rectTransform;
            if (textRt != null)
            {
                textRt.anchorMin = new Vector2(0f, 0f);
                textRt.anchorMax = new Vector2(1f, 1f);
                textRt.pivot = new Vector2(0f, 0.5f);
                textRt.offsetMin = new Vector2(48f, 0f);
                textRt.offsetMax = new Vector2(-8f, 0f);
            }
        }

        // Configuración del feedback visual del incremento (+1, -5)
        if (deltaFeedbackText == null && badgeContainer != null)
        {
            var deltaTr = badgeContainer.Find("DeltaText");
            if (deltaTr != null)
            {
                deltaFeedbackText = deltaTr.GetComponent<TMP_Text>();
            }
            else
            {
                GameObject deltaGO = new GameObject("DeltaText");
                deltaGO.transform.SetParent(badgeContainer, false);
                RectTransform dRt = deltaGO.AddComponent<RectTransform>();
                dRt.anchorMin = new Vector2(1f, 0f);
                dRt.anchorMax = new Vector2(1f, 0f);
                dRt.pivot = new Vector2(1f, 1f);
                dRt.anchoredPosition = new Vector2(-10f, -4f);
                dRt.sizeDelta = new Vector2(90f, 24f);

                deltaFeedbackText = deltaGO.AddComponent<TextMeshProUGUI>();
                if (fontAsset != null) deltaFeedbackText.font = fontAsset;
                deltaFeedbackText.fontSize = 17f;
                deltaFeedbackText.fontStyle = FontStyles.Bold;
                deltaFeedbackText.alignment = TextAlignmentOptions.Right;
                deltaFeedbackText.text = "";
                deltaFeedbackText.raycastTarget = false;
            }
        }
        else if (deltaFeedbackText != null && fontAsset != null && deltaFeedbackText.font != fontAsset)
        {
            deltaFeedbackText.font = fontAsset;
        }
    }

    /// <summary>
    /// Construye la jerarquía visual del badge de LutroCoins si no fue configurada desde un Prefab.
    /// </summary>
    private void BuildProceduralUI()
    {
        // 1. Canvas y CanvasScaler
        if (hudCanvas == null)
        {
            hudCanvas = gameObject.GetComponent<Canvas>();
            if (hudCanvas == null) hudCanvas = gameObject.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 45; // Por encima del juego, debajo de menús modales
        }

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var raycaster = gameObject.GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 2. Contenedor Pill (Pastilla superior derecha)
        Transform existingBadge = transform.Find("CoinBadge");
        GameObject badgeGO;
        if (existingBadge != null)
        {
            badgeGO = existingBadge.gameObject;
        }
        else
        {
            badgeGO = new GameObject("CoinBadge");
            badgeGO.transform.SetParent(transform, false);
        }

        badgeContainer = badgeGO.GetComponent<RectTransform>();
        if (badgeContainer == null) badgeContainer = badgeGO.AddComponent<RectTransform>();

        // Anclar a la esquina superior derecha (Top-Right)
        badgeContainer.anchorMin = new Vector2(1f, 1f);
        badgeContainer.anchorMax = new Vector2(1f, 1f);
        badgeContainer.pivot = new Vector2(1f, 1f);
        badgeContainer.anchoredPosition = new Vector2(-28f, -28f);
        badgeContainer.sizeDelta = new Vector2(150f, 44f);

        // Fondo oscuro grafito con borde dorado
        Image bgImage = badgeGO.GetComponent<Image>();
        if (bgImage == null) bgImage = badgeGO.AddComponent<Image>();
        bgImage.color = new Color(0.06f, 0.08f, 0.11f, 0.90f); // #0F141C

        var outline = badgeGO.GetComponent<Outline>();
        if (outline == null) outline = badgeGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.92f, 0.70f, 0.12f, 0.55f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // 3. Icono de LutroCoin
        Transform existingIcon = badgeContainer.Find("Icon");
        GameObject iconGO;
        if (existingIcon != null)
        {
            iconGO = existingIcon.gameObject;
        }
        else
        {
            iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(badgeContainer, false);
        }

        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        if (iconRect == null) iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(24f, 0f);
        iconRect.sizeDelta = new Vector2(34f, 34f);

        coinIconImage = iconGO.GetComponent<Image>();
        if (coinIconImage == null) coinIconImage = iconGO.AddComponent<Image>();
        coinIconImage.preserveAspect = true;

        // Cargar sprite de LutroCoin (Resources o moneda)
        Sprite coinSprite = Resources.Load<Sprite>("lutro_coin");
        if (coinSprite == null) coinSprite = Resources.Load<Sprite>("moneda");
        if (coinSprite != null) coinIconImage.sprite = coinSprite;

        // 4. Texto Contador TMP
        Transform existingText = badgeContainer.Find("CountText");
        GameObject textGO;
        if (existingText != null)
        {
            textGO = existingText.gameObject;
        }
        else
        {
            textGO = new GameObject("CountText");
            textGO.transform.SetParent(badgeContainer, false);
        }

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        if (textRect == null) textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0f, 0.5f);
        textRect.offsetMin = new Vector2(48f, 0f);
        textRect.offsetMax = new Vector2(-8f, 0f);

        coinCountText = textGO.GetComponent<TextMeshProUGUI>();
        if (coinCountText == null) coinCountText = textGO.AddComponent<TextMeshProUGUI>();

        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/Nunito SDF");
        if (fontAsset != null) coinCountText.font = fontAsset;

        coinCountText.fontSize = 22f;
        coinCountText.fontStyle = FontStyles.Bold;
        coinCountText.alignment = TextAlignmentOptions.MidlineLeft;
        coinCountText.color = normalTextColor;
        coinCountText.textWrappingMode = TextWrappingModes.NoWrap;
        coinCountText.overflowMode = TextOverflowModes.Overflow;
        coinCountText.raycastTarget = false;
    }

    /// <summary>
    /// Actualiza la visibilidad según si la escena es de Gameplay/HUB o Menú Principal.
    /// </summary>
    public void EvaluateSceneVisibility(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        bool isMenuOrCinematic = sceneName.Equals("MenuScene", System.StringComparison.OrdinalIgnoreCase) ||
                                 sceneName.Equals("BootScene", System.StringComparison.OrdinalIgnoreCase) ||
                                 sceneName.Equals("CinematicScene", System.StringComparison.OrdinalIgnoreCase) ||
                                 sceneName.IndexOf("cinematic", System.StringComparison.OrdinalIgnoreCase) >= 0;

        SetHUDVisible(!isMenuOrCinematic && !isExplicitlyHidden);
    }

    public void SetHUDVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        if (hudCanvas != null)
        {
            hudCanvas.enabled = visible;
        }
    }

    public void SetExplicitlyHidden(bool hidden)
    {
        isExplicitlyHidden = hidden;
        EvaluateSceneVisibility(SceneManager.GetActiveScene().name);
    }

    private void HandleGoldChanged(int totalGold, int delta)
    {
        EnsureReferences();
        RefreshDisplay();

        if (delta != 0 && gameObject.activeInHierarchy)
        {
            if (punchAnimationCoroutine != null)
                StopCoroutine(punchAnimationCoroutine);
            punchAnimationCoroutine = StartCoroutine(PunchScaleCoroutine(delta > 0));

            if (deltaFeedbackCoroutine != null)
                StopCoroutine(deltaFeedbackCoroutine);
            deltaFeedbackCoroutine = StartCoroutine(DeltaFeedbackCoroutine(delta));
        }
    }

    public void RefreshDisplay()
    {
        EnsureReferences();
        if (coinCountText == null) return;

        int currentGold = 0;
        if (GoldManager.Instance != null)
        {
            currentGold = GoldManager.Instance.gold;
        }
        else if (GameSession.Instance != null)
        {
            currentGold = GameSession.Instance.gold;
        }

        coinCountText.text = currentGold.ToString("N0");
        coinCountText.ForceMeshUpdate();
    }

    private IEnumerator PunchScaleCoroutine(bool isGain)
    {
        if (badgeContainer == null || coinCountText == null) yield break;

        Color targetFlashColor = isGain ? gainFlashColor : spendFlashColor;
        float punchScale = isGain ? 1.25f : 0.90f;
        Vector3 baseScale = Vector3.one;
        Vector3 targetScale = Vector3.one * punchScale;

        float duration = 0.22f;
        float elapsed = 0f;

        // Fase 1: Expansión / Contracción rápida con cambio de color
        coinCountText.color = targetFlashColor;
        while (elapsed < duration * 0.45f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (duration * 0.45f);
            badgeContainer.localScale = Vector3.Lerp(baseScale, targetScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        // Fase 2: Retorno suave al tamaño base y color normal
        elapsed = 0f;
        float returnDuration = duration * 0.55f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / returnDuration;
            badgeContainer.localScale = Vector3.Lerp(targetScale, baseScale, Mathf.SmoothStep(0f, 1f, t));
            coinCountText.color = Color.Lerp(targetFlashColor, normalTextColor, t);
            yield return null;
        }

        badgeContainer.localScale = baseScale;
        coinCountText.color = normalTextColor;
        punchAnimationCoroutine = null;
    }

    private IEnumerator DeltaFeedbackCoroutine(int delta)
    {
        if (deltaFeedbackText == null) yield break;

        bool isGain = delta > 0;
        deltaFeedbackText.text = isGain ? $"+{delta}" : $"{delta}";
        Color baseColor = isGain ? new Color(0.29f, 0.87f, 0.50f, 1f) : new Color(0.97f, 0.44f, 0.44f, 1f);
        deltaFeedbackText.color = baseColor;

        RectTransform dRt = deltaFeedbackText.rectTransform;
        Vector2 startPos = new Vector2(-10f, -4f);
        Vector2 endPos = new Vector2(-10f, isGain ? -18f : -2f);

        float duration = 0.75f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            dRt.anchoredPosition = Vector2.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));

            // Fade out en la segunda mitad
            float alpha = t < 0.4f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);
            deltaFeedbackText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }

        deltaFeedbackText.text = "";
        dRt.anchoredPosition = startPos;
        deltaFeedbackCoroutine = null;
    }
}
