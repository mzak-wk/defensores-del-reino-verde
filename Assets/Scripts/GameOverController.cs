using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverController : MonoBehaviour
{
    [Header("Refs")]
    public BarraVida playerVida;      // opcional: arrastra el BarraVida del jugador en el inspector
    public GameObject gameOverUI;     // panel de Game Over
    public MonoBehaviour[] toDisable; // scripts de control a desactivar al morir

    [Header("Opcional")]
    public bool clearEnemiesOnDeath = false;

    // estado interno
    bool bound = false;
    bool isGameOver = false;
    Coroutine waitCoroutine;

    void Awake()
    {
        if (GetComponent<DeathMenuController>() != null || GetComponentInParent<DeathMenuController>() != null)
        {
            enabled = false;
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        CO2Manager.OnGameOver += OnGameOver;
        TemperatureManager.OnTemperatureGameOver += OnGameOver;
    }

    void Start()
    {
        if (GetComponent<DeathMenuController>() == null && gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }
        TryBindPlayerVidaOrWait();
    }

    void OnDestroy()
    {
        UnbindPlayerVida();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CO2Manager.OnGameOver -= OnGameOver;
        TemperatureManager.OnTemperatureGameOver -= OnGameOver;
        if (waitCoroutine != null) StopCoroutine(waitCoroutine);
    }

    // ---------- BIND / UNBIND ----------
    void TryBindPlayerVidaOrWait()
    {
        if (playerVida != null)
        {
            BindPlayerVida(playerVida);
            return;
        }

#if UNITY_2023_1_OR_NEWER
        BarraVida found = FindFirstObjectByType<BarraVida>();
#else
        BarraVida found = FindObjectOfType<BarraVida>();
#endif
        if (found != null)
        {
            BindPlayerVida(found);
            return;
        }

        if (waitCoroutine != null) StopCoroutine(waitCoroutine);
        waitCoroutine = StartCoroutine(PollForBarraVida());
    }

    IEnumerator PollForBarraVida()
    {
        float timeout = 30f;
        float timer = 0f;

        while (timer < timeout)
        {
            var player = FindAnyObjectByType<BarraVida>(FindObjectsInactive.Include);

            if (player != null)
            {
                BindPlayerVida(player);
                waitCoroutine = null;
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning("[GameOverController] Timeout esperando BarraVida en la escena.");
        waitCoroutine = null;
    }

    void BindPlayerVida(BarraVida bv)
    {
        if (bv == null) return;
        if (bound && playerVida == bv) return;

        UnbindPlayerVida();

        playerVida = bv;
        playerVida.OnDeath += OnGameOver;
        bound = true;

        if (GetComponent<DeathMenuController>() == null && gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }

        Debug.Log($"[GameOverController] Vinculado a BarraVida: {bv.name}");
    }

    void UnbindPlayerVida()
    {
        if (!bound) return;
        if (playerVida != null)
            playerVida.OnDeath -= OnGameOver;

        playerVida = null;
        bound = false;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        TryBindPlayerVidaOrWait();
    }

    // --- Lógica Game Over ---
    void OnGameOver()
    {
        if (isGameOver)
            return;

        // Si existe un DeathMenuController, delegar la secuencia cinemática completa a él
        DeathMenuController deathMenu = null;
        if (gameOverUI != null)
            deathMenu = gameOverUI.GetComponent<DeathMenuController>() ?? gameOverUI.GetComponentInChildren<DeathMenuController>(true);
        if (deathMenu == null)
            deathMenu = FindFirstObjectByType<DeathMenuController>();

        if (deathMenu != null)
        {
            isGameOver = true;
            deathMenu.TriggerDeathSequence();
            return;
        }

        isGameOver = true;
        Debug.Log("[GameOverController] Game Over activado (Fallback).");

        // Forzar cierre de cualquier overlay abierto (diálogo, pausa, tienda) y marcar estado Game Over
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ForceCloseAllOverlays();
            UIManager.Instance.NotifyGameOver();
        }

        // Desactivar controles
        foreach (var mb in toDisable)
            if (mb) mb.enabled = false;

        // Mostrar UI
        if (gameOverUI) gameOverUI.SetActive(true);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterOverlay("GameOver", () => CloseGameOver());
        }

        // Pausar juego
        Time.timeScale = 0f;
        AudioListener.pause = true;

        // (Opcional) borrar enemigos
        if (clearEnemiesOnDeath)
        {
            Enemy[] enemies = FindObjectsByType<Enemy>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            foreach (var e in enemies)
                if (e != null) Destroy(e.gameObject);
        }
    }

    public void OpenGameOver() => OnGameOver();

    public void CloseGameOver()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterOverlay("GameOver");
            UIManager.Instance.ResetGameOverState();
        }

        if (gameOverUI) gameOverUI.SetActive(false);
        isGameOver = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    /// <summary>Restaura el estado de entrada al nivel y vuelve a cargar la escena actual.</summary>
    public void Btn_Reintentar()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterOverlay("GameOver");
            UIManager.Instance.ResetGameOverState();
        }

        GameSession.Instance?.RestoreLevelCheckpoint();

        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // --- BOTÓN VOLVER AL MENÚ PRINCIPAL ---
    public void Btn_SalirMenu(string sceneName = "MenuScene")
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterOverlay("GameOver");
            UIManager.Instance.ResetGameOverState();
        }

        if (GameSession.Instance != null)
            GameSession.Instance.ResetAll();

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (string.IsNullOrEmpty(sceneName))
            sceneName = "MenuScene";

        SceneManager.LoadScene(sceneName);
    }

    // --- BOTÓN SALIR DEL JUEGO ---
    public void Btn_Salir()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
