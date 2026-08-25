using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controlador principal del menú y secuencia cinemática de muerte (Game Over).
/// Flujo: Bloqueo de Gameplay -> Oscurecimiento de Pantalla -> Animación Personaje Muerto -> 
/// Animación Texto de Muerte -> Aparición Escalonada de Botones (Reintentar, Menú Principal, Salir).
/// Todo funciona en tiempo no escalado (Time.unscaledDeltaTime) para evitar congelamientos.
/// </summary>
public class DeathMenuController : MonoBehaviour
{
    public enum DeathState
    {
        Alive,
        Dying,
        DeadMenuReady
    }

    [Header("Estado")]
    [SerializeField] private DeathState currentState = DeathState.Alive;
    public DeathState CurrentState => currentState;

    [Header("Referencias del Jugador")]
    [Tooltip("Referencia a la BarraVida del jugador (se auto-detecta si no se asigna).")]
    [SerializeField] private BarraVida playerVida;
    [Tooltip("Scripts del jugador a desactivar al morir (PlayerMovement, PlayerCombat, etc.).")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnDeath;

    [Header("1. Oscurecimiento de Pantalla (Dark Overlay)")]
    [SerializeField] private Image darkOverlay;
    [Tooltip("Color del overlay oscuro.")]
    [SerializeField] private Color overlayColor = new Color(0.035f, 0.055f, 0.045f, 0.90f);
    [Tooltip("Duración del fade de oscurecimiento en segundos (tiempo real).")]
    [Range(0.1f, 3f)] [SerializeField] private float overlayFadeDuration = 0.8f;

    [Header("2. Animación del Personaje Muerto")]
    [SerializeField] private Image characterAnimImage;
    [Tooltip("Sprites del spritesheet 'Andrius muerte' ordenados.")]
    [SerializeField] private Sprite[] characterDeathSprites;
    [Tooltip("Velocidad de reproducción de la animación del personaje en FPS.")]
    [Range(1, 60)] [SerializeField] private int characterFps = 24;
    [Tooltip("Pausa tras finalizar la animación del personaje antes del texto.")]
    [Range(0f, 2f)] [SerializeField] private float delayAfterCharacterAnim = 0.25f;

    [Header("3. Animación del Texto de Muerte")]
    [SerializeField] private Image deathTextAnimImage;
    [Tooltip("Sprites del spritesheet 'muerte texto' ordenados.")]
    [SerializeField] private Sprite[] deathTextSprites;
    [Tooltip("Velocidad de reproducción del texto de muerte en FPS.")]
    [Range(1, 60)] [SerializeField] private int deathTextFps = 20;
    [Tooltip("Pausa tras finalizar la animación del texto antes de mostrar botones.")]
    [Range(0f, 2f)] [SerializeField] private float delayBeforeButtons = 0.35f;

    [Header("4. Botones del Menú de Muerte")]
    [SerializeField] private GameObject buttonsContainer;
    [SerializeField] private Button btnRestart;
    [SerializeField] private Button btnMainMenu;
    [SerializeField] private Button btnQuit;
    [Tooltip("CanvasGroup de cada botón para controlar fade y escala.")]
    [SerializeField] private CanvasGroup cgRestart;
    [SerializeField] private CanvasGroup cgMainMenu;
    [SerializeField] private CanvasGroup cgQuit;
    [Tooltip("Tiempo de aparición de cada botón en segundos.")]
    [Range(0.05f, 1f)] [SerializeField] private float buttonFadeDuration = 0.25f;
    [Tooltip("Pausa entre la aparición de cada botón.")]
    [Range(0.05f, 1f)] [SerializeField] private float delayBetweenButtons = 0.18f;

    [Header("Opciones de Animación")]
    [Tooltip("Si es true y el spritesheet contiene ciclo duplicado, reproduce solo una explosión completa.")]
    [SerializeField] private bool singleCycleDeath = true;
    [Tooltip("Si es true, la animación del texto de glitch se reproduce en loop constante.")]
    [SerializeField] private bool loopDeathTextAnimation = true;

    [Header("Opciones de Escena y Gameplay")]
    [Tooltip("Nombre de la escena del menú principal.")]
    [SerializeField] private string mainMenuSceneName = "MenuScene";
    [Tooltip("Si es true, destruye los enemigos en escena al morir.")]
    [SerializeField] private bool clearEnemiesOnDeath = false;
    [Tooltip("Si es true, congela las físicas del juego tras iniciar la secuencia.")]
    [SerializeField] private bool pausePhysicsOnDeath = true;

    public static DeathMenuController Instance { get; private set; }

    // Control interno de corrutinas
    private Coroutine activeDeathSequenceCoroutine;
    private Coroutine loopTextCoroutine;
    private bool isBound = false;
    private DeathReason lastDeathReason = DeathReason.Generic;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Si ya hay una instancia previa en la misma escena, mantener la actual
        }
        Instance = this;

        SceneManager.sceneLoaded += OnSceneLoaded;
        CO2Manager.OnGameOver += TriggerDeathSequence;
        TemperatureManager.OnTemperatureGameOver += TriggerDeathSequence;
        HazardMeterManager.OnHazardGameOver += TriggerDeathSequenceWithReason;
        BarraVida.OnPlayerDeathWithReason += TriggerDeathSequenceWithReason;
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (currentState != DeathState.Dying && currentState != DeathState.DeadMenuReady)
        {
            InitializeUIElements();
        }
        TryBindPlayerVidaOrWait();
    }

    private void OnDestroy()
    {
        UnbindPlayerVida();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CO2Manager.OnGameOver -= TriggerDeathSequence;
        TemperatureManager.OnTemperatureGameOver -= TriggerDeathSequence;
        HazardMeterManager.OnHazardGameOver -= TriggerDeathSequenceWithReason;
        BarraVida.OnPlayerDeathWithReason -= TriggerDeathSequenceWithReason;

        if (Instance == this)
            Instance = null;

        if (activeDeathSequenceCoroutine != null)
        {
            StopCoroutine(activeDeathSequenceCoroutine);
            activeDeathSequenceCoroutine = null;
        }

        if (loopTextCoroutine != null)
        {
            StopCoroutine(loopTextCoroutine);
            loopTextCoroutine = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeUIElements();
        TryBindPlayerVidaOrWait();
    }

    /// <summary>
    /// Oculta y resetea todos los elementos visuales al estado inicial (invisibles y no interactuables).
    /// </summary>
    public void InitializeUIElements()
    {
        currentState = DeathState.Alive;

        if (darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(false);
            Color c = overlayColor;
            c.a = 0f;
            darkOverlay.color = c;
        }

        if (characterAnimImage != null)
        {
            characterAnimImage.gameObject.SetActive(false);
            if (characterDeathSprites != null && characterDeathSprites.Length > 0)
                characterAnimImage.sprite = characterDeathSprites[0];
        }

        if (deathTextAnimImage != null)
        {
            deathTextAnimImage.gameObject.SetActive(false);
            if (deathTextSprites != null && deathTextSprites.Length > 0)
                deathTextAnimImage.sprite = deathTextSprites[0];
        }

        if (buttonsContainer != null)
        {
            buttonsContainer.SetActive(false);
        }

        SetupButtonInitialState(btnRestart, cgRestart);
        SetupButtonInitialState(btnMainMenu, cgMainMenu);
        SetupButtonInitialState(btnQuit, cgQuit);

        // Conectar listeners de botones
        if (btnRestart != null)
        {
            btnRestart.onClick.RemoveAllListeners();
            btnRestart.onClick.AddListener(Btn_Reintentar);
        }
        if (btnMainMenu != null)
        {
            btnMainMenu.onClick.RemoveAllListeners();
            btnMainMenu.onClick.AddListener(Btn_MenuPrincipal);
        }
        if (btnQuit != null)
        {
            btnQuit.onClick.RemoveAllListeners();
            btnQuit.onClick.AddListener(Btn_Salir);
        }
    }

    private void SetupButtonInitialState(Button btn, CanvasGroup cg)
    {
        if (cg != null)
        {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            cg.transform.localScale = Vector3.one * 0.85f;
        }
        if (btn != null)
        {
            btn.interactable = false;
        }
    }

    // ==========================================
    // VINCULACIÓN A BARRAVIDA DEL JUGADOR
    // ==========================================
    private void TryBindPlayerVidaOrWait()
    {
        if (playerVida != null)
        {
            BindPlayerVida(playerVida);
            return;
        }

        BarraVida found = FindFirstObjectByType<BarraVida>();
        if (found != null)
        {
            BindPlayerVida(found);
        }
    }

    public void BindPlayerVida(BarraVida bv)
    {
        if (bv == null || (isBound && playerVida == bv)) return;

        UnbindPlayerVida();

        playerVida = bv;
        playerVida.OnDeath += TriggerDeathSequence;
        isBound = true;

        // Auto-detectar scripts a desactivar si la lista está vacía
        if (scriptsToDisableOnDeath == null || scriptsToDisableOnDeath.Length == 0)
        {
            List<MonoBehaviour> list = new List<MonoBehaviour>();
            var move = bv.GetComponent<PlayerMovement>();
            if (move) list.Add(move);
            var combat = bv.GetComponent<PlayerCombat>();
            if (combat) list.Add(combat);
            scriptsToDisableOnDeath = list.ToArray();
        }
    }

    public void UnbindPlayerVida()
    {
        if (!isBound) return;
        if (playerVida != null)
            playerVida.OnDeath -= TriggerDeathSequence;

        playerVida = null;
        isBound = false;
    }

    // ==========================================
    // DISPARADOR Y SECUENCIA DE MUERTE
    // ==========================================
    public void TriggerDeathSequenceWithReason(DeathReason reason)
    {
        TriggerDeathSequence(reason);
    }

    /// <summary>
    /// Dispara la secuencia de muerte de manera estrictamente idempotente.
    /// Si varios enemigos dañan al jugador a la vez o se alcanza el límite térmico, solo se ejecuta UNA vez.
    /// </summary>
    public void TriggerDeathSequence()
    {
        TriggerDeathSequence(DeathReason.Generic);
    }

    public void TriggerDeathSequence(DeathReason reason)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (currentState != DeathState.Alive)
        {
            return; // Protección contra disparos múltiples simultáneos
        }

        lastDeathReason = reason;
        currentState = DeathState.Dying;
        Debug.Log($"[DeathMenuController] === INICIANDO SECUENCIA CINEMÁTICA DE MUERTE (Causa: {reason}) ===");

        // 1. Bloqueo de Controles y Acciones de Gameplay
        LockGameplay();

        // 2. Notificar a UIManager y suspender HUD
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ForceCloseAllOverlays();
            UIManager.Instance.NotifyGameOver();
            UIManager.Instance.RegisterOverlay("DeathMenu", () => { });
        }

        // 3. Pausar físicas si está configurado
        if (pausePhysicsOnDeath)
        {
            Time.timeScale = 0f;
        }

        // 4. Limpieza opcional de enemigos
        if (clearEnemiesOnDeath)
        {
            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                if (e != null) Destroy(e.gameObject);
            }
        }

        // 5. Iniciar la corrutina cinemática en tiempo real no escalado
        if (activeDeathSequenceCoroutine != null)
        {
            StopCoroutine(activeDeathSequenceCoroutine);
        }

        // Asegurar que este GameObject y todos sus ancestros estén activos
        Transform curr = transform;
        while (curr != null)
        {
            if (!curr.gameObject.activeSelf) curr.gameObject.SetActive(true);
            curr = curr.parent;
        }

        activeDeathSequenceCoroutine = StartCoroutine(DeathCinematicSequenceCR());
    }

    private void LockGameplay()
    {
        if (scriptsToDisableOnDeath != null)
        {
            foreach (var script in scriptsToDisableOnDeath)
            {
                if (script != null)
                {
                    if (script is IPlayerMovementLock movementLock)
                    {
                        movementLock.SetMovementLocked(true);
                    }
                    script.enabled = false;
                }
            }
        }

        // Detener velocidad residual del Rigidbody2D del jugador si existe
        if (playerVida != null)
        {
            var rb = playerVida.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            var anim = playerVida.GetComponent<Animator>();
            if (anim != null)
            {
                anim.speed = 0f; // Congelar animación en el mundo
            }
        }
    }

    /// <summary>
    /// Corrutina cinemática paso a paso:
    /// 1. Fade progresivo a oscuro.
    /// 2. Animación de muerte del personaje ('Andrius muerte') en zona superior-media.
    /// 3. Animación de texto ('muerte texto') debajo del personaje.
    /// 4. Aparición progresiva de botones uno por uno con escala y fade.
    /// </summary>
    private IEnumerator DeathCinematicSequenceCR()
    {
        // ----------------------------------------------------
        // FASE 1: Oscurecimiento Progresivo de la Pantalla
        // ----------------------------------------------------
        if (darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(true);
            Color startCol = overlayColor;
            startCol.a = 0f;
            darkOverlay.color = startCol;

            float elapsed = 0f;
            while (elapsed < overlayFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / overlayFadeDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                Color current = overlayColor;
                current.a = Mathf.Lerp(0f, overlayColor.a, smoothT);
                darkOverlay.color = current;
                yield return null;
            }
            darkOverlay.color = overlayColor;
        }

        // ----------------------------------------------------
        // FASE 2: Animación del Personaje Muerto (Andrius muerte - 1 solo ciclo)
        // ----------------------------------------------------
        if (characterAnimImage != null)
        {
            characterAnimImage.gameObject.SetActive(true);
            characterAnimImage.color = Color.white;

            if (characterDeathSprites != null && characterDeathSprites.Length > 0)
            {
                // Si contiene los 2 ciclos completos (204 frames), reproducir solo el primer ciclo (102 frames)
                int totalFrames = characterDeathSprites.Length;
                int framesToPlay = (singleCycleDeath && totalFrames >= 150) ? (totalFrames / 2) : totalFrames;

                // Reproducción fluida y enérgica (~1.8 segundos total)
                float effectiveFps = Mathf.Max(characterFps, (float)framesToPlay / 1.8f);
                float frameDuration = 1f / effectiveFps;

                for (int i = 0; i < framesToPlay; i++)
                {
                    characterAnimImage.sprite = characterDeathSprites[i];
                    float frameTimer = 0f;
                    while (frameTimer < frameDuration)
                    {
                        frameTimer += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }

                // Congelar permanentemente en el último fotograma de la explosión
                characterAnimImage.sprite = characterDeathSprites[framesToPlay - 1];
            }
        }

        if (delayAfterCharacterAnim > 0f)
        {
            yield return new WaitForSecondsRealtime(delayAfterCharacterAnim);
        }

        // ----------------------------------------------------
        // FASE 3: Animación del Texto de Muerte en Loop Continuo (muerte texto)
        // ----------------------------------------------------
        if (deathTextAnimImage != null)
        {
            deathTextAnimImage.gameObject.SetActive(true);
            deathTextAnimImage.color = Color.white;

            if (loopTextCoroutine != null)
            {
                StopCoroutine(loopTextCoroutine);
            }
            loopTextCoroutine = StartCoroutine(LoopDeathTextAnimationCR());
        }

        if (delayBeforeButtons > 0f)
        {
            yield return new WaitForSecondsRealtime(delayBeforeButtons);
        }

        // ----------------------------------------------------
        // FASE 4: Aparición Escalonada de Botones (1 por 1)
        // ----------------------------------------------------
        if (buttonsContainer != null)
        {
            buttonsContainer.SetActive(true);
        }

        // 1. REINTENTAR (Botón principal destacado)
        if (cgRestart != null)
        {
            yield return StartCoroutine(AnimateButtonInCR(btnRestart, cgRestart));
        }
        if (delayBetweenButtons > 0f)
        {
            yield return new WaitForSecondsRealtime(delayBetweenButtons);
        }

        // 2. MENÚ PRINCIPAL
        if (cgMainMenu != null)
        {
            yield return StartCoroutine(AnimateButtonInCR(btnMainMenu, cgMainMenu));
        }
        if (delayBetweenButtons > 0f)
        {
            yield return new WaitForSecondsRealtime(delayBetweenButtons);
        }

        // 3. SALIR
        if (cgQuit != null)
        {
            yield return StartCoroutine(AnimateButtonInCR(btnQuit, cgQuit));
        }

        // Estado final: Menú de Muerte interactuable
        currentState = DeathState.DeadMenuReady;
        activeDeathSequenceCoroutine = null;
        Debug.Log("[DeathMenuController] === SECUENCIA DE MUERTE COMPLETADA - OPCIONES DISPONIBLES ===");
    }

    /// <summary>
    /// Corrutina de loop continuo para el texto de glitch ("Conexión Perdida").
    /// </summary>
    private IEnumerator LoopDeathTextAnimationCR()
    {
        if (deathTextAnimImage == null || deathTextSprites == null || deathTextSprites.Length == 0)
        {
            yield break;
        }

        float textFrameDuration = 1f / Mathf.Max(1, deathTextFps);
        int frameIndex = 0;

        while (true)
        {
            deathTextAnimImage.sprite = deathTextSprites[frameIndex];
            frameIndex = (frameIndex + 1) % deathTextSprites.Length;

            float timer = 0f;
            while (timer < textFrameDuration)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    /// <summary>
    /// Anima la entrada suave de un botón individual con fade de alfa y rebote de escala.
    /// Habilita su interactividad SOLO al terminar su animación.
    /// </summary>
    private IEnumerator AnimateButtonInCR(Button btn, CanvasGroup cg)
    {
        if (cg == null) yield break;

        cg.gameObject.SetActive(true);
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.85f;
        Vector3 endScale = Vector3.one;

        while (elapsed < buttonFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / buttonFadeDuration);
            // Curva suave elástica
            float easeT = Mathf.SmoothStep(0f, 1f, t);

            cg.alpha = easeT;
            cg.transform.localScale = Vector3.Lerp(startScale, endScale, easeT);
            yield return null;
        }

        cg.alpha = 1f;
        cg.transform.localScale = endScale;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        if (btn != null)
        {
            btn.interactable = true;
        }
    }

    // ==========================================
    // ACCIONES DE BOTONES
    // ==========================================
    /// <summary>
    /// Acción Reintentar: restaura la sesión/vida y recarga la escena actual limpiamente.
    /// </summary>
    public void Btn_Reintentar()
    {
        Debug.Log("[DeathMenuController] Botón REINTENTAR presionado.");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterOverlay("DeathMenu");
            UIManager.Instance.ResetGameOverState();
        }

        // Restaurar estado de sesión roguelite y vida a full
        if (GameSession.Instance != null)
        {
            GameSession.Instance.RestoreLevelCheckpoint();
            GameSession.Instance.FullHeal();
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    /// <summary>
    /// Acción Menú Principal: limpia la sesión y carga la escena del menú principal.
    /// </summary>
    public void Btn_MenuPrincipal()
    {
        Debug.Log("[DeathMenuController] Botón MENÚ PRINCIPAL presionado.");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterOverlay("DeathMenu");
            UIManager.Instance.ResetGameOverState();
        }

        if (GameSession.Instance != null)
        {
            GameSession.Instance.ResetAll();
            GameSession.Instance.FullHeal();
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;

        string targetScene = string.IsNullOrEmpty(mainMenuSceneName) ? "MenuScene" : mainMenuSceneName;
        SceneManager.LoadScene(targetScene);
    }

    /// <summary>
    /// Acción Salir: cierra el juego en build y detiene el Play Mode en Editor de manera segura.
    /// </summary>
    public void Btn_Salir()
    {
        Debug.Log("[DeathMenuController] Botón SALIR presionado.");

        Time.timeScale = 1f;
        AudioListener.pause = false;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
