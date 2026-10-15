using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// OptionsMenu — Gestiona el panel de opciones y configuraciones del juego (QoL).
/// Funciona tanto desde el Menú Principal como en el Menú de Pausa de cualquier nivel.
///
/// Funcionalidades QoL:
///   • Volumen Maestro Global: controla AudioListener.volume en tiempo real en TODAS las escenas
///     y sincroniza con el AudioMixer (curva logarítmica dB).
///   • Texto de porcentaje de volumen en tiempo real (ej. "80%").
///   • Calidad Gráfica: selector de presets (Baja, Media, Alta, Ultra).
///   • V-Sync: alternar sincronización vertical para evitar tearing y regular FPS.
///   • Selector de Resoluciones: ordenadas de mayor a menor sin duplicados.
///   • Modo Pantalla Completa (Fullscreen / Ventana).
///   • Botón Volver inteligente: regresa al menú de pausa si está en nivel o al panel principal si está en menú.
///   • Persistencia total con PlayerPrefs entre escenas y reinicios del juego.
/// </summary>
public class OptionsMenu : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Claves para PlayerPrefs
    // ─────────────────────────────────────────────

    private const string PREF_VOLUME     = "MasterVolume";
    private const string PREF_RES_INDEX  = "ResolutionIndex";
    private const string PREF_FULLSCREEN = "Fullscreen";
    private const string PREF_QUALITY    = "QualityLevel";
    private const string PREF_VSYNC      = "VSync";

    // ─────────────────────────────────────────────
    // Referencias de Audio
    // ─────────────────────────────────────────────

    [Header("Audio")]
    [Tooltip("El AudioMixer del proyecto con parámetro 'MasterVolume'.")]
    public AudioMixer audioMixer;

    [Tooltip("Slider que controla el volumen maestro (0.0001 a 1).")]
    public Slider volumeSlider;

    [Tooltip("Texto opcional que muestra el porcentaje numérico del volumen (ej. '80%').")]
    public TMP_Text volumePercentText;

    // ─────────────────────────────────────────────
    // Referencias de Video & Pantalla
    // ─────────────────────────────────────────────

    [Header("Pantalla y Resolución")]
    [Tooltip("Dropdown de TMP con las resoluciones del monitor.")]
    public TMP_Dropdown resolutionDropdown;

    [Tooltip("Toggle para alternar pantalla completa y ventana.")]
    public Toggle fullscreenToggle;

    [Tooltip("Botón estilizado para pantalla completa.")]
    public Button fullscreenButton;

    [Header("Calidad y Rendimiento (QoL)")]
    [Tooltip("Dropdown opcional de calidad gráfica (Baja, Media, Alta, Ultra).")]
    public TMP_Dropdown qualityDropdown;

    [Tooltip("Toggle opcional de sincronización vertical (V-Sync).")]
    public Toggle vsyncToggle;

    [Tooltip("Botón estilizado para sincronización vertical (V-Sync).")]
    public Button vsyncButton;

    [Header("Navegación")]
    [Tooltip("Botón para volver atrás. Si está vacío, se busca en hijos.")]
    public Button backButton;

    // ─────────────────────────────────────────────
    // Estado interno
    // ─────────────────────────────────────────────

    private TMP_Text _fullscreenButtonText;
    private TMP_Text _vsyncButtonText;

    private List<Resolution> _availableResolutions = new List<Resolution>();
    private int _currentResolutionIndex = 0;

    /// <summary>
    /// Inicializador estático que garantiza que el volumen global guardado
    /// se aplique en TODAS las escenas antes de cargar cualquier nivel.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BootstrapGlobalAudio()
    {
        float savedVolume = PlayerPrefs.GetFloat(PREF_VOLUME, 0.8f);
        AudioListener.volume = savedVolume;
    }

    private bool _hasBeautified = false;

    private void Awake()
    {
        AutoFindReferences();
        BeautifyLayout();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && gameObject.activeInHierarchy)
        {
            // Si PausarJuego existe, él gestiona la pila de pausa; de lo contrario, cerramos directamente
            if (PausarJuego.Instance == null)
            {
                Btn_Volver();
            }
        }
    }

    private void OnEnable()
    {
        AutoFindReferences();
        BeautifyLayout();
        EnsureFullscreenControl();
        EnsureVSyncControl();
        InitializeResolutionDropdown();
        InitializeVolumeSlider();
        InitializeFullscreenToggle();
        InitializeQualityDropdown();
        InitializeVSyncToggle();
        SetupBackButton();
    }

    private void AutoFindReferences()
    {
        if (volumeSlider == null)
            volumeSlider = GetComponentInChildren<Slider>(true);

        if (volumePercentText == null)
        {
            var texts = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                if (t.name.ToLower().Contains("percent") || t.name.ToLower().Contains("vol_txt"))
                {
                    volumePercentText = t;
                    break;
                }
            }
        }

        if (resolutionDropdown == null)
            resolutionDropdown = GetComponentInChildren<TMP_Dropdown>(true);

        if (fullscreenToggle == null)
            fullscreenToggle = GetComponentInChildren<Toggle>(true);

        if (qualityDropdown == null)
        {
            var drops = GetComponentsInChildren<TMP_Dropdown>(true);
            foreach (var d in drops)
            {
                if (d != resolutionDropdown)
                {
                    qualityDropdown = d;
                    break;
                }
            }
        }

        if (vsyncToggle == null)
        {
            var toggles = GetComponentsInChildren<Toggle>(true);
            foreach (var tog in toggles)
            {
                if (tog != fullscreenToggle)
                {
                    vsyncToggle = tog;
                    break;
                }
            }
        }

        if (backButton == null)
        {
            var btns = GetComponentsInChildren<Button>(true);
            foreach (var b in btns)
            {
                string bName = b.gameObject.name.ToLower();
                if (bName.Contains("back") || bName.Contains("volver") || bName.Contains("cerrar") || bName == "button")
                {
                    backButton = b;
                    break;
                }
            }
        }
    }

    private static GameObject CreateUIGameObject(string name, Transform parent = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }
        return go;
    }

    /// <summary>
    /// Transforma y embellece la ventana de opciones proceduralmente:
    /// panel oscuro futurista con contorno cian, títulos de sección, etiquetas y botón Volver estilizado.
    /// </summary>
    private void BeautifyLayout()
    {
        if (_hasBeautified) return;
        _hasBeautified = true;

        RectTransform panelRT = GetComponent<RectTransform>();
        if (panelRT != null)
        {
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(680f, 500f);
        }

        Image bgImg = GetComponent<Image>();
        if (bgImg == null) bgImg = gameObject.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.09f, 0.13f, 0.96f);

        // Eliminar marco antiguo que causaba el bloque cian sólido
        Transform oldBorder = transform.Find("Modal_Border");
        if (oldBorder != null)
        {
            DestroyImmediate(oldBorder.gameObject);
        }

        // Borde elegante mediante Outline en el propio panel
        Outline panelOutline = GetComponent<Outline>();
        if (panelOutline == null) panelOutline = gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0f, 0.85f, 1f, 0.65f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        Transform titleTrans = transform.Find("Modal_Title");
        if (titleTrans == null)
        {
            GameObject titleGO = CreateUIGameObject("Modal_Title", transform);
            RectTransform titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -20f);
            titleRT.sizeDelta = new Vector2(400f, 40f);

            TMP_Text titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "CONFIGURACIÓN";
            titleTxt.fontSize = 24;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = new Color(0f, 0.95f, 1f);

            GameObject divGO = CreateUIGameObject("Title_Divider", titleGO.transform);
            RectTransform divRT = divGO.GetComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0.1f, 0f);
            divRT.anchorMax = new Vector2(0.9f, 0f);
            divRT.anchoredPosition = new Vector2(0f, -6f);
            divRT.sizeDelta = new Vector2(0f, 2f);
            Image divImg = divGO.AddComponent<Image>();
            divImg.color = new Color(0f, 0.85f, 1f, 0.45f);
        }

        // 1. SECCIÓN AUDIO
        Transform audioSecTrans = transform.Find("Section_Audio");
        if (audioSecTrans == null)
        {
            GameObject secGO = CreateUIGameObject("Section_Audio", transform);
            RectTransform secRT = secGO.GetComponent<RectTransform>();
            secRT.anchoredPosition = new Vector2(-150f, 140f);
            secRT.sizeDelta = new Vector2(300f, 24f);
            TMP_Text secTxt = secGO.AddComponent<TextMeshProUGUI>();
            secTxt.text = "VOLUMEN GENERAL";
            secTxt.fontSize = 15;
            secTxt.fontStyle = FontStyles.Bold;
            secTxt.color = new Color(0.35f, 0.95f, 0.65f);
        }

        if (volumeSlider != null)
        {
            RectTransform sRT = volumeSlider.GetComponent<RectTransform>();
            sRT.anchoredPosition = new Vector2(-30f, 105f);
            sRT.sizeDelta = new Vector2(340f, 22f);

            Transform fillArea = volumeSlider.transform.Find("Fill Area/Fill");
            if (fillArea != null)
            {
                Image fillImg = fillArea.GetComponent<Image>();
                if (fillImg != null) fillImg.color = new Color(0f, 0.95f, 0.55f);
            }

            if (volumePercentText == null)
            {
                GameObject pctGO = CreateUIGameObject("Vol_PercentText", transform);
                RectTransform pctRT = pctGO.GetComponent<RectTransform>();
                pctRT.anchoredPosition = new Vector2(185f, 105f);
                pctRT.sizeDelta = new Vector2(70f, 25f);
                volumePercentText = pctGO.AddComponent<TextMeshProUGUI>();
                volumePercentText.fontSize = 16;
                volumePercentText.fontStyle = FontStyles.Bold;
                volumePercentText.alignment = TextAlignmentOptions.Left;
                volumePercentText.color = Color.white;
            }
            else
            {
                RectTransform pctRT = volumePercentText.GetComponent<RectTransform>();
                if (pctRT != null)
                {
                    pctRT.anchoredPosition = new Vector2(185f, 105f);
                    pctRT.sizeDelta = new Vector2(70f, 25f);
                }
            }
        }

        // 2. SECCIÓN PANTALLA & GRÁFICOS
        Transform gfxSecTrans = transform.Find("Section_Graphics");
        if (gfxSecTrans == null)
        {
            GameObject secGO = CreateUIGameObject("Section_Graphics", transform);
            RectTransform secRT = secGO.GetComponent<RectTransform>();
            secRT.anchoredPosition = new Vector2(-150f, 50f);
            secRT.sizeDelta = new Vector2(300f, 24f);
            TMP_Text secTxt = secGO.AddComponent<TextMeshProUGUI>();
            secTxt.text = "PANTALLA & GRÁFICOS";
            secTxt.fontSize = 15;
            secTxt.fontStyle = FontStyles.Bold;
            secTxt.color = new Color(0.35f, 0.95f, 0.65f);
        }

        CreateOrUpdateLabel("Label_Resolution", "Resolución:", new Vector2(-150f, 10f));
        if (resolutionDropdown != null)
        {
            RectTransform resRT = resolutionDropdown.GetComponent<RectTransform>();
            resRT.anchoredPosition = new Vector2(65f, 10f);
            resRT.sizeDelta = new Vector2(245f, 34f);
            BeautifyDropdown(resolutionDropdown);
        }

        EnsureFullscreenControl();

        CreateOrUpdateLabel("Label_Quality", "Calidad Gráfica:", new Vector2(-150f, -80f));
        if (qualityDropdown == null && resolutionDropdown != null)
        {
            GameObject qGO = Instantiate(resolutionDropdown.gameObject, transform);
            qGO.name = "Dropdown_Quality";
            RectTransform qRT = qGO.GetComponent<RectTransform>();
            qRT.anchoredPosition = new Vector2(65f, -80f);
            qRT.sizeDelta = new Vector2(245f, 34f);
            qualityDropdown = qGO.GetComponent<TMP_Dropdown>();
            qualityDropdown.onValueChanged.RemoveAllListeners();
        }
        else if (qualityDropdown != null)
        {
            RectTransform qRT = qualityDropdown.GetComponent<RectTransform>();
            qRT.anchoredPosition = new Vector2(65f, -80f);
            qRT.sizeDelta = new Vector2(245f, 34f);
        }
        if (qualityDropdown != null)
        {
            BeautifyDropdown(qualityDropdown);
        }

        EnsureVSyncControl();

        // Botón Volver estilizado
        if (backButton != null)
        {
            RectTransform bRT = backButton.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0.5f, 0.5f);
            bRT.anchorMax = new Vector2(0.5f, 0.5f);
            bRT.pivot = new Vector2(0.5f, 0.5f);
            bRT.anchoredPosition = new Vector2(0f, -200f);
            bRT.sizeDelta = new Vector2(200f, 40f);

            var bTxt = backButton.GetComponentInChildren<TMP_Text>();
            if (bTxt != null)
            {
                bTxt.text = "◀ VOLVER";
                bTxt.fontSize = 16;
                bTxt.fontStyle = FontStyles.Bold;
                bTxt.alignment = TextAlignmentOptions.Center;
                bTxt.color = Color.white;
            }
            else
            {
                var legacyTxt = backButton.GetComponentInChildren<Text>();
                if (legacyTxt != null)
                {
                    legacyTxt.text = "◀ VOLVER";
                    legacyTxt.fontSize = 16;
                    legacyTxt.fontStyle = UnityEngine.FontStyle.Bold;
                    legacyTxt.color = Color.white;
                    legacyTxt.alignment = TextAnchor.MiddleCenter;
                }
            }

            Image bImg = backButton.GetComponent<Image>();
            if (bImg != null)
            {
                bImg.color = new Color(0.08f, 0.32f, 0.42f, 0.95f);
            }

            Outline bOutline = backButton.GetComponent<Outline>() ?? backButton.gameObject.AddComponent<Outline>();
            bOutline.effectColor = new Color(0f, 0.85f, 1f, 0.7f);
            bOutline.effectDistance = new Vector2(1.5f, -1.5f);

            if (backButton.GetComponent<UIButtonJuice>() == null)
            {
                backButton.gameObject.AddComponent<UIButtonJuice>();
            }
        }
    }

    private void CreateOrUpdateLabel(string objName, string text, Vector2 pos)
    {
        Transform trans = transform.Find(objName);
        TMP_Text tmp;
        if (trans == null)
        {
            GameObject go = CreateUIGameObject(objName, transform);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(220f, 26f);
            tmp = go.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            RectTransform rt = trans.GetComponent<RectTransform>() ?? trans.gameObject.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(220f, 26f);
            tmp = trans.GetComponent<TextMeshProUGUI>() ?? trans.gameObject.AddComponent<TextMeshProUGUI>();
        }

        tmp.text = text;
        tmp.fontSize = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = new Color(0.85f, 0.95f, 0.90f);
    }

    /// <summary>
    /// Aplica estilo cyberpunk de alta fidelidad al Dropdown y ajusta los márgenes del Template/Item Label
    /// para evitar que las resoluciones altas o medianas queden recortadas en su primer dígito.
    /// </summary>
    private void BeautifyDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return;

        // 1. Estilo del botón cerrado principal
        Image mainImg = dropdown.GetComponent<Image>();
        if (mainImg != null)
        {
            mainImg.color = new Color(0.08f, 0.13f, 0.19f, 0.95f);
        }

        // Borde fino en el botón principal
        Transform mainBorder = dropdown.transform.Find("Drop_Border");
        if (mainBorder == null)
        {
            GameObject bGO = CreateUIGameObject("Drop_Border", dropdown.transform);
            bGO.transform.SetAsFirstSibling();
            RectTransform bRT = bGO.GetComponent<RectTransform>();
            bRT.anchorMin = Vector2.zero;
            bRT.anchorMax = Vector2.one;
            bRT.offsetMin = new Vector2(-1.5f, -1.5f);
            bRT.offsetMax = new Vector2(1.5f, 1.5f);
            Image bImg = bGO.AddComponent<Image>();
            bImg.color = new Color(0f, 0.85f, 1f, 0.65f);
            bImg.raycastTarget = false;
        }

        // Caption Text (texto visible cuando está cerrado)
        if (dropdown.captionText != null)
        {
            dropdown.captionText.fontSize = 15;
            dropdown.captionText.fontStyle = FontStyles.Bold;
            dropdown.captionText.color = new Color(0.92f, 0.98f, 1f, 1f);
            dropdown.captionText.alignment = TextAlignmentOptions.Left;
            RectTransform capRT = dropdown.captionText.rectTransform;
            capRT.offsetMin = new Vector2(16f, 0f);
            capRT.offsetMax = new Vector2(-32f, 0f);
        }

        // Flecha derecha
        Transform arrow = dropdown.transform.Find("Arrow");
        if (arrow != null)
        {
            Image arrowImg = arrow.GetComponent<Image>();
            if (arrowImg != null) arrowImg.color = new Color(0f, 0.9f, 1f, 0.9f);
        }

        // 2. Estilo y alineación del menú desplegable (Template)
        RectTransform templateRT = dropdown.template;
        if (templateRT != null)
        {
            templateRT.sizeDelta = new Vector2(0f, 175f);

            // Fondo del desplegable
            Image tmplImg = templateRT.GetComponent<Image>();
            if (tmplImg != null)
            {
                tmplImg.color = new Color(0.05f, 0.08f, 0.13f, 0.98f);
            }

            // Borde del desplegable
            Transform tmplBorder = templateRT.Find("Tmpl_Border");
            if (tmplBorder == null)
            {
                GameObject tbGO = CreateUIGameObject("Tmpl_Border", templateRT);
                tbGO.transform.SetAsFirstSibling();
                RectTransform tbRT = tbGO.GetComponent<RectTransform>();
                tbRT.anchorMin = Vector2.zero;
                tbRT.anchorMax = Vector2.one;
                tbRT.offsetMin = new Vector2(-2f, -2f);
                tbRT.offsetMax = new Vector2(2f, 2f);
                Image tbImg = tbGO.AddComponent<Image>();
                tbImg.color = new Color(0f, 0.85f, 1f, 0.75f);
                tbImg.raycastTarget = false;
            }

            // Viewport
            Transform vp = templateRT.Find("Viewport");
            if (vp != null)
            {
                RectTransform vpRT = vp as RectTransform;
                if (vpRT != null)
                {
                    vpRT.offsetMin = new Vector2(2f, 2f);
                    vpRT.offsetMax = new Vector2(-18f, -2f); // Deja espacio para el scrollbar
                }

                // Content
                Transform content = vp.Find("Content");
                if (content != null)
                {
                    // Item
                    Transform item = content.Find("Item");
                    if (item != null)
                    {
                        RectTransform itemRT = item as RectTransform;
                        if (itemRT != null)
                        {
                            itemRT.sizeDelta = new Vector2(0f, 28f);
                        }

                        // Checkmark del item
                        Transform check = item.Find("Item Checkmark");
                        if (check != null)
                        {
                            RectTransform checkRT = check as RectTransform;
                            if (checkRT != null)
                            {
                                checkRT.anchorMin = new Vector2(0f, 0.5f);
                                checkRT.anchorMax = new Vector2(0f, 0.5f);
                                checkRT.anchoredPosition = new Vector2(16f, 0f);
                                checkRT.sizeDelta = new Vector2(16f, 16f);
                            }
                            Image checkImg = check.GetComponent<Image>();
                            if (checkImg != null)
                            {
                                checkImg.color = new Color(0.2f, 1f, 0.7f, 1f);
                            }
                        }

                        // Label del item (CORRECCIÓN CRÍTICA DE CORTE DE TEXTO)
                        Transform itemLabel = item.Find("Item Label");
                        if (itemLabel != null)
                        {
                            RectTransform labelRT = itemLabel as RectTransform;
                            if (labelRT != null)
                            {
                                labelRT.anchorMin = Vector2.zero;
                                labelRT.anchorMax = Vector2.one;
                                // 38px a la izquierda da espacio de sobra para el checkmark y evita que el texto se corte
                                labelRT.offsetMin = new Vector2(38f, 0f);
                                labelRT.offsetMax = new Vector2(-10f, 0f);
                            }
                            TMP_Text itemTxt = itemLabel.GetComponent<TMP_Text>();
                            if (itemTxt != null)
                            {
                                itemTxt.fontSize = 15;
                                itemTxt.fontStyle = FontStyles.Bold;
                                itemTxt.color = new Color(0.92f, 0.98f, 1f, 1f);
                                itemTxt.alignment = TextAlignmentOptions.Left;
                                itemTxt.margin = Vector4.zero;
                            }
                        }

                        // Fondo hover del item
                        Transform itemBg = item.Find("Item Background");
                        if (itemBg != null)
                        {
                            Image itemBgImg = itemBg.GetComponent<Image>();
                            if (itemBgImg != null)
                            {
                                itemBgImg.color = new Color(0.08f, 0.30f, 0.40f, 0.85f);
                            }
                        }
                    }
                }
            }

            // Scrollbar
            Scrollbar sb = templateRT.GetComponentInChildren<Scrollbar>(true);
            if (sb != null)
            {
                Image sbBg = sb.GetComponent<Image>();
                if (sbBg != null) sbBg.color = new Color(0.04f, 0.07f, 0.11f, 0.9f);
                if (sb.targetGraphic != null)
                {
                    sb.targetGraphic.color = new Color(0f, 0.85f, 1f, 0.8f);
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    // Inicialización de Controles
    // ─────────────────────────────────────────────

    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        Resolution[] allResolutions = Screen.resolutions;
        _availableResolutions.Clear();
        var seen = new HashSet<string>();

        for (int i = allResolutions.Length - 1; i >= 0; i--)
        {
            Resolution res = allResolutions[i];
            string key = $"{res.width}x{res.height}";
            if (seen.Contains(key)) continue;

            seen.Add(key);
            _availableResolutions.Add(res);
        }

        resolutionDropdown.ClearOptions();
        var options = new List<string>();
        int savedIndex = PlayerPrefs.GetInt(PREF_RES_INDEX, -1);
        int matchIndex = 0;

        for (int i = 0; i < _availableResolutions.Count; i++)
        {
            Resolution res = _availableResolutions[i];
            options.Add($"{res.width} x {res.height}");

            if (res.width == Screen.currentResolution.width && res.height == Screen.currentResolution.height)
            {
                matchIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        _currentResolutionIndex = (savedIndex >= 0 && savedIndex < _availableResolutions.Count)
            ? savedIndex
            : matchIndex;

        resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        resolutionDropdown.value = _currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void InitializeVolumeSlider()
    {
        if (volumeSlider == null) return;

        volumeSlider.minValue = 0.0001f;
        volumeSlider.maxValue = 1f;

        float savedVolume = PlayerPrefs.GetFloat(PREF_VOLUME, 0.8f);

        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        volumeSlider.value = savedVolume;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        ApplyVolumeGlobal(savedVolume);
    }

    private void InitializeFullscreenToggle()
    {
        bool savedFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, 1) == 1;

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggled);
            fullscreenToggle.isOn = savedFullscreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }

        Screen.fullScreen = savedFullscreen;
        UpdateFullscreenVisuals();
    }

    private void InitializeQualityDropdown()
    {
        if (qualityDropdown == null) return;

        qualityDropdown.ClearOptions();
        string[] names = QualitySettings.names;
        var options = new List<string>(names);
        qualityDropdown.AddOptions(options);

        int savedQuality = PlayerPrefs.GetInt(PREF_QUALITY, QualitySettings.GetQualityLevel());
        qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
        qualityDropdown.value = Mathf.Clamp(savedQuality, 0, names.Length - 1);
        qualityDropdown.RefreshShownValue();
        qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    private void InitializeVSyncToggle()
    {
        bool savedVSync = PlayerPrefs.GetInt(PREF_VSYNC, 1) == 1;

        if (vsyncToggle != null)
        {
            vsyncToggle.onValueChanged.RemoveListener(OnVSyncToggled);
            vsyncToggle.isOn = savedVSync;
            vsyncToggle.onValueChanged.AddListener(OnVSyncToggled);
        }

        QualitySettings.vSyncCount = savedVSync ? 1 : 0;
        UpdateVSyncVisuals();
    }

    /// <summary>
    /// Construye y asegura el control prominente para V-Sync.
    /// Ubicado alineado en x: 65, y: -125 con tamaño 245x34.
    /// </summary>
    public void EnsureVSyncControl()
    {
        CreateOrUpdateLabel("Label_VSync", "Sincronización (V-Sync):", new Vector2(-150f, -125f));

        Transform existingBtn = transform.Find("Btn_VSync");
        if (existingBtn != null && existingBtn.GetComponent<RectTransform>() == null)
        {
            DestroyImmediate(existingBtn.gameObject);
            existingBtn = null;
        }

        GameObject vBtnGO;
        if (existingBtn != null)
        {
            vBtnGO = existingBtn.gameObject;
        }
        else
        {
            vBtnGO = CreateUIGameObject("Btn_VSync", transform);
        }

        vBtnGO.SetActive(true);
        RectTransform rt = vBtnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(65f, -125f);
        rt.sizeDelta = new Vector2(245f, 34f);

        Image img = vBtnGO.GetComponent<Image>() ?? vBtnGO.AddComponent<Image>();
        img.color = new Color(0.08f, 0.13f, 0.19f, 0.95f);

        Transform borderTrans = vBtnGO.transform.Find("Border");
        if (borderTrans == null)
        {
            GameObject bGO = CreateUIGameObject("Border", vBtnGO.transform);
            bGO.transform.SetAsFirstSibling();
            RectTransform bRT = bGO.GetComponent<RectTransform>();
            bRT.anchorMin = Vector2.zero;
            bRT.anchorMax = Vector2.one;
            bRT.offsetMin = new Vector2(-1.5f, -1.5f);
            bRT.offsetMax = new Vector2(1.5f, 1.5f);
            Image bImg = bGO.AddComponent<Image>();
            bImg.color = new Color(0f, 0.85f, 1f, 0.65f);
            bImg.raycastTarget = false;
        }

        vsyncButton = vBtnGO.GetComponent<Button>() ?? vBtnGO.AddComponent<Button>();
        var colors = vsyncButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.2f, 0.9f, 1f, 1f);
        colors.pressedColor = new Color(0.1f, 0.6f, 0.8f, 1f);
        vsyncButton.colors = colors;

        Transform textTrans = vBtnGO.transform.Find("Text");
        GameObject tGO = (textTrans != null) ? textTrans.gameObject : CreateUIGameObject("Text", vBtnGO.transform);
        RectTransform tRT = tGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;

        _vsyncButtonText = tGO.GetComponent<TextMeshProUGUI>() ?? tGO.AddComponent<TextMeshProUGUI>();
        _vsyncButtonText.fontSize = 15;
        _vsyncButtonText.fontStyle = FontStyles.Bold;
        _vsyncButtonText.alignment = TextAlignmentOptions.Center;

        vsyncButton.onClick.RemoveAllListeners();
        vsyncButton.onClick.AddListener(ToggleVSyncButtonClicked);

        UpdateVSyncVisuals();

        Transform oldToggle = transform.Find("Toggle_VSync");
        if (oldToggle != null && oldToggle != vBtnGO.transform)
        {
            oldToggle.gameObject.SetActive(false);
        }
    }

    public void ToggleVSyncButtonClicked()
    {
        bool current = PlayerPrefs.GetInt(PREF_VSYNC, 1) == 1;
        bool next = !current;
        OnVSyncToggled(next);
        UpdateVSyncVisuals();
    }

    public void UpdateVSyncVisuals()
    {
        bool isEnabled = PlayerPrefs.GetInt(PREF_VSYNC, 1) == 1;
        if (_vsyncButtonText != null)
        {
            _vsyncButtonText.text = isEnabled ? "[ V-SYNC: ACTIVADO ]" : "[ V-SYNC: DESACTIVADO ]";
            _vsyncButtonText.color = isEnabled ? new Color(0.2f, 1f, 0.7f, 1f) : new Color(0.75f, 0.82f, 0.88f, 0.85f);
        }
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = isEnabled;
        }
    }

    /// <summary>
    /// Construye y asegura el control prominente para Pantalla Completa.
    /// Ubicado alineado en x: 65, y: -35 con tamaño 245x34.
    /// </summary>
    public void EnsureFullscreenControl()
    {
        CreateOrUpdateLabel("Label_Fullscreen", "Pantalla Completa:", new Vector2(-150f, -35f));

        Transform existingBtn = transform.Find("Btn_Fullscreen");
        if (existingBtn != null && existingBtn.GetComponent<RectTransform>() == null)
        {
            DestroyImmediate(existingBtn.gameObject);
            existingBtn = null;
        }

        GameObject fsBtnGO;
        if (existingBtn != null)
        {
            fsBtnGO = existingBtn.gameObject;
        }
        else
        {
            fsBtnGO = CreateUIGameObject("Btn_Fullscreen", transform);
        }

        fsBtnGO.SetActive(true);
        RectTransform rt = fsBtnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(65f, -35f);
        rt.sizeDelta = new Vector2(245f, 34f);

        Image img = fsBtnGO.GetComponent<Image>() ?? fsBtnGO.AddComponent<Image>();
        img.color = new Color(0.08f, 0.13f, 0.19f, 0.95f);

        Transform borderTrans = fsBtnGO.transform.Find("Border");
        if (borderTrans == null)
        {
            GameObject bGO = CreateUIGameObject("Border", fsBtnGO.transform);
            bGO.transform.SetAsFirstSibling();
            RectTransform bRT = bGO.GetComponent<RectTransform>();
            bRT.anchorMin = Vector2.zero;
            bRT.anchorMax = Vector2.one;
            bRT.offsetMin = new Vector2(-1.5f, -1.5f);
            bRT.offsetMax = new Vector2(1.5f, 1.5f);
            Image bImg = bGO.AddComponent<Image>();
            bImg.color = new Color(0f, 0.85f, 1f, 0.65f);
            bImg.raycastTarget = false;
        }

        fullscreenButton = fsBtnGO.GetComponent<Button>() ?? fsBtnGO.AddComponent<Button>();
        var colors = fullscreenButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.2f, 0.9f, 1f, 1f);
        colors.pressedColor = new Color(0.1f, 0.6f, 0.8f, 1f);
        fullscreenButton.colors = colors;

        Transform textTrans = fsBtnGO.transform.Find("Text");
        GameObject tGO = (textTrans != null) ? textTrans.gameObject : CreateUIGameObject("Text", fsBtnGO.transform);
        RectTransform tRT = tGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;

        _fullscreenButtonText = tGO.GetComponent<TextMeshProUGUI>() ?? tGO.AddComponent<TextMeshProUGUI>();
        _fullscreenButtonText.fontSize = 15;
        _fullscreenButtonText.fontStyle = FontStyles.Bold;
        _fullscreenButtonText.alignment = TextAlignmentOptions.Center;

        fullscreenButton.onClick.RemoveAllListeners();
        fullscreenButton.onClick.AddListener(ToggleFullscreenButtonClicked);

        UpdateFullscreenVisuals();

        if (fullscreenToggle != null && fullscreenToggle.gameObject != fsBtnGO)
        {
            fullscreenToggle.gameObject.SetActive(false);
        }
    }

    public void ToggleFullscreenButtonClicked()
    {
        bool current = Screen.fullScreen;
        bool next = !current;
        OnFullscreenToggled(next);
        UpdateFullscreenVisuals();
    }

    public void UpdateFullscreenVisuals()
    {
        bool isFS = Screen.fullScreen;
        if (_fullscreenButtonText != null)
        {
            _fullscreenButtonText.text = isFS ? "[ PANTALLA COMPLETA ]" : "[ MODO VENTANA ]";
            _fullscreenButtonText.color = isFS ? new Color(0.2f, 1f, 0.7f, 1f) : new Color(0.75f, 0.82f, 0.88f, 0.85f);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = isFS;
        }
    }

    private void SetupBackButton()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(Btn_Volver);
            backButton.onClick.AddListener(Btn_Volver);
        }
    }

    // ─────────────────────────────────────────────
    // Callbacks de UI (en tiempo real)
    // ─────────────────────────────────────────────

    public void OnVolumeChanged(float linearValue)
    {
        ApplyVolumeGlobal(linearValue);
        PlayerPrefs.SetFloat(PREF_VOLUME, linearValue);
        PlayerPrefs.Save();
    }

    public void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= _availableResolutions.Count) return;

        _currentResolutionIndex = index;
        Resolution res = _availableResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);

        PlayerPrefs.SetInt(PREF_RES_INDEX, index);
        PlayerPrefs.Save();
        Debug.Log($"[OptionsMenu] Resolución aplicada: {res.width}x{res.height}");
    }

    public void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(PREF_FULLSCREEN, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index, true);
        PlayerPrefs.SetInt(PREF_QUALITY, index);
        PlayerPrefs.Save();
        Debug.Log($"[OptionsMenu] Calidad gráfica cambiada a: {QualitySettings.names[index]}");
    }

    public void OnVSyncToggled(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        PlayerPrefs.SetInt(PREF_VSYNC, enabled ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[OptionsMenu] V-Sync: {(enabled ? "Activado" : "Desactivado")}");
    }

    /// <summary>
    /// Cierra el menú de opciones y regresa al menú correspondiente (Pausa o Menú Principal).
    /// </summary>
    public void Btn_Volver()
    {
        // 1. Si estamos en un nivel o HUB con menú de pausa/opciones activo
        if (PausarJuego.Instance != null && PausarJuego.Instance.juegoPausado)
        {
            PausarJuego.Instance.Btn_CerrarOpciones();
            return;
        }

        // 2. Si estamos en el menú principal
        var mainMgr = FindFirstObjectByType<MainMenuManager>();
        if (mainMgr != null)
        {
            mainMgr.ShowMainPanel();
            return;
        }

        // 3. Fallback: cerrar este panel
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // Aplicación del Volumen
    // ─────────────────────────────────────────────

    private void ApplyVolumeGlobal(float linearValue)
    {
        // 1. Control global del listener (afecta todos los sonidos de Unity en todas las escenas)
        AudioListener.volume = linearValue;

        // 2. Feedback de texto
        if (volumePercentText != null)
        {
            volumePercentText.text = $"{Mathf.RoundToInt(linearValue * 100f)}%";
        }

        // 3. AudioMixer si está asignado o existe en recursos
        if (audioMixer == null)
        {
            audioMixer = Resources.Load<AudioMixer>("MasterVolume");
        }

        if (audioMixer != null)
        {
            float safeValue = Mathf.Max(linearValue, 0.0001f);
            float dB = 20f * Mathf.Log10(safeValue);
            audioMixer.SetFloat("MasterVolume", dB);
        }
    }
}
