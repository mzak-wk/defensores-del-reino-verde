using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Mini-tutorial interactivo que aparece al inicio del Nivel 1 (LevelWoods)
/// para explicar la mecánica de las zonas de erosión, la interacción con la tecla [E],
/// el cronómetro de 5 segundos y el peligro de muerte por aumento crítico de temperatura.
/// Pausa el juego en segundo plano para que el jugador pueda leer con calma.
/// </summary>
public class Level1TutorialPopup : MonoBehaviour
{
    private const string PREF_KEY = "Tutorial_Erosion_Shown";

    [Header("Configuración")]
    [Tooltip("Tiempo de espera tras cargar la escena antes de mostrar el tutorial (para permitir que la furgoneta llegue).")]
    [SerializeField] private float delayBeforeShow = 5.2f;

    [Tooltip("Si es true, sólo se muestra la primera vez que se juega el nivel. Si es false, se muestra siempre.")]
    [SerializeField] private bool onlyShowOnce = false;

    private GameObject popupRoot;
    private bool isShowing = false;

    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "LevelWoods")
        {
            return;
        }

        if (onlyShowOnce && PlayerPrefs.GetInt(PREF_KEY, 0) == 1)
        {
            return;
        }

        StartCoroutine(ShowTutorialAfterDelay());
    }

    private IEnumerator ShowTutorialAfterDelay()
    {
        // Esperar en tiempo real (unscaled) para no ser afectado por transiciones
        yield return new WaitForSecondsRealtime(delayBeforeShow);

        // Si se abrió un diálogo de entrada, esperar a que termine
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
        {
            yield return new WaitForSecondsRealtime(0.5f);
        }

        CreateAndShowPopup();
    }

    private void Update()
    {
        if (!isShowing) return;

        // Cerrar con Espacio, Enter, Escape o tecla E
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || 
            Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
        {
            DismissPopup();
        }
    }

    private void CreateAndShowPopup()
    {
        isShowing = true;
        Time.timeScale = 0f; // Pausar juego de fondo

        // 1. Crear Canvas en Overlay
        popupRoot = new GameObject("Tutorial_Erosion_Popup");
        Canvas canvas = popupRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Por encima de todo

        CanvasScaler scaler = popupRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        popupRoot.AddComponent<GraphicRaycaster>();

        // 2. Fondo oscurecido (Dimmer)
        GameObject bgDimmer = new GameObject("Dimmer");
        bgDimmer.transform.SetParent(popupRoot.transform, false);
        RectTransform dimRT = bgDimmer.AddComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero;
        dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = Vector2.zero;
        dimRT.offsetMax = Vector2.zero;
        Image dimImg = bgDimmer.AddComponent<Image>();
        dimImg.color = new Color(0.02f, 0.05f, 0.03f, 0.82f);

        // 3. Tarjeta Principal (Modal Box)
        GameObject cardGO = new GameObject("Card");
        cardGO.transform.SetParent(popupRoot.transform, false);
        RectTransform cardRT = cardGO.AddComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(720f, 480f);
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);

        Image cardBg = cardGO.AddComponent<Image>();
        cardBg.color = new Color(0.08f, 0.14f, 0.10f, 0.98f);

        // Borde brillante de la tarjeta
        GameObject borderGO = new GameObject("CardBorder");
        borderGO.transform.SetParent(cardGO.transform, false);
        RectTransform borderRT = borderGO.AddComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = new Vector2(-3f, -3f);
        borderRT.offsetMax = new Vector2(3f, 3f);
        Image borderImg = borderGO.AddComponent<Image>();
        borderImg.color = new Color(0.30f, 0.85f, 0.45f, 0.95f);
        borderGO.transform.SetAsFirstSibling();

        // 4. Header / Título
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(cardGO.transform, false);
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchoredPosition = new Vector2(0f, 185f);
        titleRT.sizeDelta = new Vector2(660f, 60f);
        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "ALERTA ECOLÓGICA: FALLAS DE EROSIÓN";
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.40f, 1.0f, 0.60f);

        // Subtítulo
        GameObject subGO = new GameObject("Subtitle");
        subGO.transform.SetParent(cardGO.transform, false);
        RectTransform subRT = subGO.AddComponent<RectTransform>();
        subRT.anchoredPosition = new Vector2(0f, 142f);
        subRT.sizeDelta = new Vector2(660f, 30f);
        TextMeshProUGUI subText = subGO.AddComponent<TextMeshProUGUI>();
        subText.text = "INSTRUCCIONES DE RESTAURACIÓN AMBIENTAL";
        subText.fontSize = 15;
        subText.fontStyle = FontStyles.Bold;
        subText.alignment = TextAlignmentOptions.Center;
        subText.color = new Color(0.75f, 0.90f, 0.80f, 0.8f);

        string tutorialBody =
            "<b>1. Grietas de Erosión Activas:</b>\n" +
            "La deforestación enemiga fracturó el bosque. Cada segundo que una grieta siga abierta calentará el ambiente <b>+0.1°C</b> (indicado con alertas rojas flotantes).\n\n" +
            "<b>2. Restauración Automática:</b>\n" +
            "¡Ya no necesitas presionar ninguna tecla! Solo <b>acércate a la grieta</b> y quédate cerca un momento para sanar el suelo y germinar un árbol.\n\n" +
            "<b>3. Recuperación de Vida y Equilibrio:</b>\n" +
            "Cada suelo restaurado <b>cura tu vida (+15 HP)</b> y enfría el termómetro, protegiendo al bosque del <b>colapso climático</b>.";

        GameObject bodyGO = new GameObject("Body");
        bodyGO.transform.SetParent(cardGO.transform, false);
        RectTransform bodyRT = bodyGO.AddComponent<RectTransform>();
        bodyRT.anchoredPosition = new Vector2(0f, 8f);
        bodyRT.sizeDelta = new Vector2(640f, 210f);
        TextMeshProUGUI bodyText = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyText.text = tutorialBody;
        bodyText.fontSize = 17;
        bodyText.lineSpacing = 12f;
        bodyText.color = new Color(0.92f, 0.98f, 0.94f);

        // 6. Botón de Aceptar / Entendido
        GameObject btnGO = new GameObject("DismissButton");
        btnGO.transform.SetParent(cardGO.transform, false);
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchoredPosition = new Vector2(0f, -175f);
        btnRT.sizeDelta = new Vector2(300f, 52f);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.18f, 0.65f, 0.35f, 1f);

        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(DismissPopup);

        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.28f, 0.85f, 0.48f, 1f);
        cb.pressedColor = new Color(0.12f, 0.50f, 0.25f, 1f);
        btn.colors = cb;

        // Texto del botón
        GameObject btnTextGO = new GameObject("BtnText");
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextRT = btnTextGO.AddComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.offsetMin = Vector2.zero;
        btnTextRT.offsetMax = Vector2.zero;
        TextMeshProUGUI btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
        btnText.text = "¡ENTENDIDO! [ESPACIO]";
        btnText.fontSize = 18;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
    }

    public void DismissPopup()
    {
        if (!isShowing) return;
        isShowing = false;

        Time.timeScale = 1f; // Reanudar juego
        PlayerPrefs.SetInt(PREF_KEY, 1);
        PlayerPrefs.Save();

        if (popupRoot != null)
        {
            Destroy(popupRoot);
        }
    }

    /// <summary>
    /// Auto-arranque al cargar la escena de LevelWoods si no existe en la escena.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateInLevelWoods()
    {
        if (SceneManager.GetActiveScene().name == "LevelWoods")
        {
            if (FindFirstObjectByType<Level1TutorialPopup>() == null)
            {
                GameObject host = new GameObject("Level1_Tutorial_Host");
                host.AddComponent<Level1TutorialPopup>();
            }
        }
    }
}
