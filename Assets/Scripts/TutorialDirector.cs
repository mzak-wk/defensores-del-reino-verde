using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Events;

public class TutorialDirector : MonoBehaviour
{
    [Header("Duraciones")]
    public float tutorialSeconds = 180f; // Tiempo de supervivencia normal
    public float windowSeconds = 30f;    // Tiempo de "Muerte Súbita" (Escape/Boss)

    [Header("Referencias")]
    public Transform player;
    public PortalTeleporter portal;
    public ArrowEdgePointer uiArrow;

    [Header("UI")]
    public TextMeshProUGUI mainTimerText;
    public TextMeshProUGUI phaseLabel;
    public Color blinkA = Color.white;
    public Color blinkB = Color.red;
    public float blinkSpeed = 6f;

    // ...
    [Header("Escenas")]
    public string tutorialSceneName = "Level0Scene";   // Portal abre directo

    // --- NUEVAS VARIABLES PARA EL FINAL ---
    public string finalLevelSceneName = "Level4Scene"; // El nivel donde termina el juego
    public string cinematicSceneName = "CinematicScene"; // La escena del video final
    public string easterEggSceneName = "easter_egg";     // <--- ¡NUEVA VARIABLE AÑADIDA!                                                     // -----------------------------------
    public string gameOverSceneName = "GameOverScene";
    public GameOverController gameOverInScene;

    [Header("Eventos")]
    public UnityEvent onWindowOpen; // Spawnea al Boss

    // --- estado interno ---
    float t;
    bool windowOpen;    // true = estamos en tiempo de descuento (30s finales)
    bool finished;      // true = juego terminado
    bool portalUnlocked; // true = el portal está físicamente abierto
    public bool bossFightActive;
    public string defaultPhaseText = "SOBREVIVE";

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (!portal) portal = FindFirstObjectByType<PortalTeleporter>();
        if (!mainTimerText) mainTimerText = GameObject.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
        //if (!phaseLabel) phaseLabel.text = "Sobrevive...";
    }

    void Start()
    {
        finished = false;
        windowOpen = false;
        portalUnlocked = false;
        t = tutorialSeconds;

        // Aseguramos que el portal inicie cerrado y la flecha apagada
        if (portal) portal.SetOpen(false);
        if (uiArrow) uiArrow.gameObject.SetActive(false);

        if (phaseLabel) phaseLabel.text = defaultPhaseText;

        UpdateTimerUI(t, false);
    }

    void Update()
    {
        if (finished) return;

#if UNITY_EDITOR
    if (Input.GetKeyDown(KeyCode.P)) TriggerPhaseChange();
    if (Input.GetKeyDown(KeyCode.K)) OnBossDefeated(); // Simular kill
#endif

        // Solo decrementamos el tiempo si NO estamos en la fase del jefe
        if (!bossFightActive)
        {
            t -= Time.deltaTime;
        }

        if (!windowOpen)
        {
            // --- FASE 1: SUPERVIVENCIA ---
            UpdateTimerUI(Mathf.Max(0f, t), false);

            if (t <= 0f)
            {
                TriggerPhaseChange(); // Se acabó el tiempo normal
            }
        }
        else
        {
            // --- FASE 2: MUERTE SÚBITA (BOSS / ESCAPE) ---

            // Si la pelea del jefe está activa, NO mostramos el timer de cuenta regresiva
            if (bossFightActive)
            {
                UpdateTimerUI(-1f, false); // <--- Usamos -1f como bandera para NO mostrar tiempo
                                           // No hay que hacer nada más, ya que 't' no se decrementa si bossFightActive es true.
            }
            else // Lógica original (cuenta regresiva si es tutorial o si el boss ya murió)
            {
                // El tiempo corre y parpadea.
                UpdateTimerUI(Mathf.Max(0f, t), true);

                if (t <= 0f)
                {
                    // Se acabó el tiempo extra -> Game Over
                    finished = true;
                    FailAndGameOver();
                }
            }
        }
    }
    // Se llama cuando el tiempo principal llega a 0
    void TriggerPhaseChange()
    {
        if (windowOpen) return; // Evitar repetir

        Debug.Log("[TutorialDirector] ¡Tiempo agotado! Inicia fase final.");

        // 1. Cambiamos estado a "Ventana Abierta" y reiniciamos timer a 30s
        windowOpen = true;
        t = windowSeconds;

        // 2. Lógica de Portal según la escena
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == tutorialSceneName)
        {
            // ES TUTORIAL: Abrir portal inmediatamente. No hay Boss Fight de tiempo fijo.
            OpenPortalNow();
        }
        else
        {
            // ES NIVEL NORMAL (incluido el 4): INICIAMOS LA BOSS FIGHT.
            bossFightActive = true; // <--- Activamos el estado de Boss Fight!
            portalUnlocked = false;
            if (portal) portal.SetOpen(false);

            if (phaseLabel) phaseLabel.text = "¡DERROTA AL JEFE!";
        }

        // 3. Invocamos al Boss (siempre se invoca después de decidir el estado)
        onWindowOpen?.Invoke();
        TryAutoStartBoss();
    }

    // ----------------------------------------------------------------------
    // ESTA FUNCIÓN LA LLAMAS DESDE EL SCRIPT DE MUERTE DEL BOSS
    // ----------------------------------------------------------------------
    // ----------------------------------------------------------------------
    // ESTA FUNCIÓN LA LLAMAS DESDE EL SCRIPT DE MUERTE DEL BOSS (BossHealth.cs)
    // ----------------------------------------------------------------------
    public void OnBossDefeated()
    {
        bossFightActive = false; // <--- ¡Importante! El Boss murió, el tiempo de 30s inicia AHORA.
        Debug.Log("[TutorialDirector] Boss derrotado. El timer de 30s (windowSeconds) comenzará a correr.");
        // 1) Averiguamos la escena actual
        string currentScene = SceneManager.GetActiveScene().name;

        // ------------------------------------------------------------------
        // A) EASTER EGG: boss del TUTORIAL
        //    ➜ Se ejecuta SIEMPRE que mates al boss en la escena de tutorial,
        //       aunque windowOpen sea false.
        // ------------------------------------------------------------------
        if (currentScene == tutorialSceneName)
        {
            Debug.Log("¡EASTER EGG! Boss de tutorial derrotado. Cargando escena secreta.");
            finished = true;
            LoadScene(easterEggSceneName);   // asegúrate de que el nombre coincide
            return;
        }

        // ------------------------------------------------------------------
        // B) Para el resto de escenas SÍ exigimos que sea la fase del jefe
        // ------------------------------------------------------------------
        if (!windowOpen || finished)
        {
            Debug.LogWarning("[TutorialDirector] Boss derrotado, pero las condiciones iniciales (windowOpen o finished) fallaron. No hay acción.");
            return;
        }

        // --- 2. LÓGICA DE ESCENAS ESPECIALES ---

        // B1) FINAL (Level 4)
        if (currentScene == finalLevelSceneName)
        {
            Debug.Log("¡Juego completado! Cargando cinemática...");
            finished = true;
            LoadScene(cinematicSceneName);
            return;
        }

        // 3. NIVELES INTERMEDIOS (1, 2, 3)

        if (portalUnlocked)
        {
            Debug.Log("Portal ya estaba abierto/desbloqueado. Ignorando OnBossDefeated.");
            return;
        }

        Debug.Log("Nivel intermedio completado. Abriendo portal.");
        OpenPortalNow();
    }

    // Función auxiliar interna para abrir visualmente el portal
    void OpenPortalNow()
    {
        portalUnlocked = true;
        if (portal) portal.SetOpen(true);

        if (uiArrow && portal)
        {
            if (!uiArrow.cam) uiArrow.cam = Camera.main;
            uiArrow.target = portal.transform;
            uiArrow.gameObject.SetActive(true);
        }

        if (phaseLabel) phaseLabel.text = "¡Entra al portal!";
    }

    // ----------------------------------------------------------------------

    void TryAutoStartBoss()
    {
        var sp = FindFirstObjectByType<EnemySpawner>();
        if (sp != null)
        {
            MethodInfo mi = sp.GetType().GetMethod("StartBossPhase", BindingFlags.Public | BindingFlags.Instance);
            if (mi != null) mi.Invoke(sp, null);
        }
    }

    public void OnPlayerEnteredPortal()
    {
        // Solo dejamos pasar si el portal está desbloqueado
        if (!windowOpen || !portalUnlocked || finished) return;

        finished = true;
        if (uiArrow) uiArrow.gameObject.SetActive(false);
        LoadNextSceneInBuild();
    }

    void FailAndGameOver()
    {
        if (portal) portal.SetOpen(false);
        if (uiArrow) uiArrow.gameObject.SetActive(false);

        if (gameOverInScene != null)
            gameOverInScene.OpenGameOver();
        else
            LoadScene(gameOverSceneName);
    }

    void LoadNextSceneInBuild()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int total = SceneManager.sceneCountInBuildSettings;
        if (total <= 0) return;

        int nextIndex = current + 1;
        if (nextIndex >= total) nextIndex = 0;

        string path = SceneUtility.GetScenePathByBuildIndex(nextIndex);
        string sceneName = Path.GetFileNameWithoutExtension(path);
        SceneManager.LoadScene(sceneName);
    }

    void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("[TutorialDirector] Se intentó cargar una escena con nombre vacío. Revisa el Inspector.");
        }
    }

    void UpdateTimerUI(float seconds, bool blinking)
    {
        if (!mainTimerText) return;

        if (seconds < 0f) // <--- Nueva lógica para ocultar/detener el tiempo
        {
            mainTimerText.text = "--:--";
            mainTimerText.color = blinkA; // Color normal
        }
        else
        {
            int s = Mathf.CeilToInt(seconds);
            int mm = s / 60;
            int ss = s % 60;
            mainTimerText.text = $"{mm:00}:{ss:00}";

            if (blinking)
            {
                // Parpadeo rojo intenso
                float tBlink = 0.5f * (1f + Mathf.Sin(Time.unscaledTime * blinkSpeed));
                mainTimerText.color = Color.Lerp(blinkA, blinkB, tBlink);
            }
            else
            {
                mainTimerText.color = blinkA;
            }
        }
    }
}