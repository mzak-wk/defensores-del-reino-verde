using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// MainMenuManager — refactorización de MenuManager.cs
///
/// Gestiona la navegación por paneles del Menú Principal:
///   - MainPanel     → panel por defecto, visible al abrir el menú
///   - OptionsPanel  → configuraciones del juego
///   - SaveSlotsPanel → selección de partidas guardadas (UI básica, sin lógica de slots aún)
///
/// También contiene QuitGame() para cerrar la aplicación.
/// Se coordina con MenuIntroUI para arrancar la animación de entrada al menú.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Referencias de paneles
    // ─────────────────────────────────────────────

    [Header("Paneles de UI")]
    [Tooltip("Panel principal con los botones de inicio, opciones y salir")]
    public GameObject mainPanel;

    [Tooltip("Panel de opciones/configuración del juego")]
    public GameObject optionsPanel;

    [Tooltip("Panel de selección de ranuras de guardado")]
    public GameObject saveSlotsPanel;

    // ─────────────────────────────────────────────
    // Referencias opcionales de coordinación
    // ─────────────────────────────────────────────

    [Header("Intro del Menú (opcional)")]
    [Tooltip("Referencia a MenuIntroUI para arrancar la animación al cargar la escena." +
             " Déjalo vacío si no usas la animación de intro.")]
    public MenuIntroUI menuIntroUI;

    // ─────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────

    private void Start()
    {
        // Mostrar solo el MainPanel al inicio; ocultar el resto
        ShowPanel(mainPanel);

        // Si existe la referencia a MenuIntroUI, la animación de entrada
        // arranca automáticamente desde su propio Start(). No es necesario
        // llamarla aquí, pero podríamos añadir lógica extra en el futuro.
        if (menuIntroUI != null)
        {
            Debug.Log("[MainMenuManager] MenuIntroUI conectado — la animación de intro correrá automáticamente.");
        }
    }

    // ─────────────────────────────────────────────
    // Navegación de paneles
    // ─────────────────────────────────────────────

    /// <summary>
    /// Activa un único panel y desactiva todos los demás.
    /// Úsalo desde código para cambiar el panel activo de forma segura.
    /// </summary>
    /// <param name="panelToShow">El GameObject del panel que debe quedar visible.</param>
    private void ShowPanel(GameObject panelToShow)
    {
        // Iteramos sobre todos los paneles gestionados
        if (mainPanel != null)     mainPanel.SetActive(mainPanel == panelToShow);
        if (optionsPanel != null)  optionsPanel.SetActive(optionsPanel == panelToShow);
        if (saveSlotsPanel != null) saveSlotsPanel.SetActive(saveSlotsPanel == panelToShow);
    }

    // ─────────────────────────────────────────────
    // Funciones públicas — conectadas a botones de UI
    // ─────────────────────────────────────────────

    /// <summary>
    /// Muestra el Panel Principal y oculta los demás.
    /// Asigna esta función al botón "Atrás" de cada sub-panel.
    /// </summary>
    public void ShowMainPanel()
    {
        ShowPanel(mainPanel);
    }

    /// <summary>
    /// Muestra el Panel de Opciones y oculta los demás.
    /// Asigna esta función al botón "Opciones" del MainPanel.
    /// </summary>
    public void ShowOptionsPanel()
    {
        ShowPanel(optionsPanel);
    }

    /// <summary>
    /// Muestra el Panel de Ranuras de Guardado y oculta los demás.
    /// Asigna esta función al botón "Jugar" o "Cargar Partida" del MainPanel.
    /// </summary>
    public void ShowSaveSlotsPanel()
    {
        ShowPanel(saveSlotsPanel);
    }

    // ─────────────────────────────────────────────
    // Otras acciones públicas
    // ─────────────────────────────────────────────

    /// <summary>
    /// Cierra la aplicación.
    /// En el Editor de Unity, este método no tiene efecto visible;
    /// solo funciona en una build compilada.
    /// Asigna esta función al botón "Salir" del MainPanel.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        // En el editor, salimos del Play Mode
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // En build, cerramos la aplicación
        Application.Quit();
#endif
        Debug.Log("[MainMenuManager] QuitGame() llamado.");
    }
}
