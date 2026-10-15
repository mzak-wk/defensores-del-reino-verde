using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gestiona la pausa del juego desde cualquier nivel y el menú de opciones en el HUB.
/// Congela el juego en el fondo (Time.timeScale = 0f), permite abrir el menú
/// de opciones para controlar el volumen global y ajustes gráficos en tiempo real,
/// y regresar al Hub o Menú Principal de forma segura.
/// </summary>
public class PausarJuego : MonoBehaviour
{
    public static PausarJuego Instance { get; private set; }

    [Header("Paneles")]
    [Tooltip("Panel principal del menú de pausa")]
    [SerializeField] private GameObject panelPausa;

    [Tooltip("Panel de opciones/configuración")]
    [SerializeField] private GameObject panelOpciones;

    [Header("Escenas de Salida")]
    [SerializeField] private string sceneHubName = "LevelHUB";
    [SerializeField] private string sceneMenuName = "MenuScene";

    public bool juegoPausado { get; private set; } = false;

    /// <summary>
    /// Auto-garantiza que en escenas como LevelHUB o niveles de juego siempre
    /// exista un PauseManager escuchando la tecla ESC.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitSceneLoadedListener()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsurePauseManagerInScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsurePauseManagerInScene();
    }

    private static void EnsurePauseManagerInScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "MenuScene" || currentScene == "IntroCinematic" || currentScene == "CreditsScene")
            return;

        if (Instance == null)
        {
            var existing = FindFirstObjectByType<PausarJuego>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            GameObject pauseGO = new GameObject("PauseManager_Auto");
            var canvas = pauseGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var scaler = pauseGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            pauseGO.AddComponent<GraphicRaycaster>();
            pauseGO.AddComponent<PausarJuego>();
            Debug.Log($"[PausarJuego] PauseManager inicializado automáticamente en escena: {currentScene}");
        }
    }

    private void Awake()
    {
        Instance = this;

        EnsurePausePanel();
        EnsureOptionsPanel();
        EnsurePauseMenuListeners();

        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        juegoPausado = false;
    }

    public void EnsurePausePanel()
    {
        if (panelPausa != null) return;

        var p = transform.Find("Panel_Pausa");
        if (p != null)
        {
            panelPausa = p.gameObject;
            return;
        }

        var inScene = GameObject.Find("Panel_Pausa");
        if (inScene != null)
        {
            panelPausa = inScene;
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("Panel_Pausa");
        if (prefab != null)
        {
            Transform parentTransform = transform;
            panelPausa = Instantiate(prefab, parentTransform);
            panelPausa.name = "Panel_Pausa";
            panelPausa.SetActive(false);
        }
    }

    public void EnsureOptionsPanel()
    {
        if (panelOpciones != null) return;

        var existing = transform.Find("OptionsPanel");
        if (existing != null)
        {
            panelOpciones = existing.gameObject;
            return;
        }

        var optInScene = FindFirstObjectByType<OptionsMenu>(FindObjectsInactive.Include);
        if (optInScene != null)
        {
            panelOpciones = optInScene.gameObject;
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("OptionsPanel");
        if (prefab != null)
        {
            Transform parentTransform = transform;
            panelOpciones = Instantiate(prefab, parentTransform);
            panelOpciones.name = "OptionsPanel";
            panelOpciones.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[PausarJuego] No se encontró prefab OptionsPanel en Resources.");
        }
    }

    public void EnsurePauseMenuListeners()
    {
        if (panelPausa == null) return;

        var buttons = panelPausa.GetComponentsInChildren<Button>(true);
        bool isHub = SceneManager.GetActiveScene().name == sceneHubName;

        foreach (var b in buttons)
        {
            string bName = b.name.ToLower();
            var txt = b.GetComponentInChildren<TMP_Text>();
            string tText = txt != null ? txt.text.ToLower() : "";

            if (bName.Contains("resume") || tText.Contains("reanudar"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(Btn_Reanudar);
                if (txt != null && txt.text == "Button") txt.text = "Reanudar";
            }
            else if (bName.Contains("option") || bName.Contains("opcion") || tText.Contains("opcion"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(Btn_Opciones);
                if (txt != null && txt.text == "Button") txt.text = "Opciones";
            }
            else if (bName.Contains("back_hub") || bName.Contains("hub") || tText.Contains("hub"))
            {
                if (isHub)
                {
                    b.gameObject.SetActive(false);
                }
                else
                {
                    b.gameObject.SetActive(true);
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(Btn_SalirHUB);
                    if (txt != null && txt.text == "Button") txt.text = "Volver al Hub";
                }
            }
            else if (bName.Contains("salir") || bName.Contains("menu") || bName.Contains("quit") || tText.Contains("salir"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(Btn_SalirMenu);
                if (txt != null && txt.text == "Button") txt.text = "Menú Principal";
            }
        }
    }

    private void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsGameOver)
            return;

        // Si no está pausado y no se puede pausar (diálogo, tienda u otro overlay activo), ignoramos
        if (!juegoPausado && UIManager.Instance != null && !UIManager.Instance.CanPause)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (panelOpciones != null && panelOpciones.activeSelf)
            {
                Btn_CerrarOpciones();
            }
            else if (juegoPausado)
            {
                Btn_Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    // ==========================================
    // MÉTODOS PÚBLICOS PARA EVENTOS DE UI (OnClick)
    // ==========================================

    public void Btn_Reanudar()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterOverlay("Pause");
            UIManager.Instance.UnregisterOverlay("Options");
        }

        if (panelPausa != null)
            panelPausa.SetActive(false);

        if (panelOpciones != null)
            panelOpciones.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;
        juegoPausado = false;
    }

    public void Pausar()
    {
        EnsurePausePanel();
        EnsureOptionsPanel();
        EnsurePauseMenuListeners();

        if (panelPausa != null)
            panelPausa.SetActive(true);

        if (panelOpciones != null)
            panelOpciones.SetActive(false);

        Time.timeScale = 0f;
        AudioListener.pause = true;
        juegoPausado = true;

        if (UIManager.Instance != null)
            UIManager.Instance.RegisterOverlay("Pause", () => Btn_Reanudar());
    }

    public void Btn_Opciones()
    {
        EnsureOptionsPanel();

        if (panelPausa != null)
            panelPausa.SetActive(false);

        if (panelOpciones != null)
            panelOpciones.SetActive(true);

        AudioListener.pause = false;
        Time.timeScale = 0f;
    }

    public void Btn_CerrarOpciones()
    {
        if (panelOpciones != null)
            panelOpciones.SetActive(false);

        if (panelPausa != null)
            panelPausa.SetActive(true);

        AudioListener.pause = true;
        Time.timeScale = 0f;
    }

    public void Btn_SalirHUB()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        juegoPausado = false;

        if (UIManager.Instance != null)
            UIManager.Instance.ForceCloseAllOverlays();

        SceneManager.LoadScene(sceneHubName);
    }

    public void Btn_SalirMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        juegoPausado = false;

        if (UIManager.Instance != null)
            UIManager.Instance.ForceCloseAllOverlays();

        SceneManager.LoadScene(sceneMenuName);
    }
}
