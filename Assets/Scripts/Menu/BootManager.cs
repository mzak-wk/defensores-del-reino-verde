using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// BootManager — reemplaza IntroManager.cs
/// Maneja la secuencia de splash screens al arrancar el juego:
///   1. Fade-in → hold 2s → fade-out del logo de Unity
///   2. Fade-in → hold 2s → fade-out del logo del juego
///   3. Carga la escena del Menú Principal
///
/// El jugador puede saltar la secuencia con cualquier tecla.
/// </summary>
public class BootManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Referencias de UI
    // ─────────────────────────────────────────────

    [Header("Logos (Image components)")]
    [Tooltip("Imagen del logo de Unity (arrástrate el componente Image)")]
    public Image unityLogoImage;

    [Tooltip("Imagen del logo del juego (arrástrate el componente Image)")]
    public Image gameLogoImage;

    // ─────────────────────────────────────────────
    // Configuración de tiempos
    // ─────────────────────────────────────────────

    [Header("Tiempos (segundos)")]
    [Tooltip("Duración del fade-in de cada logo")]
    public float fadeDuration = 0.5f;

    [Tooltip("Tiempo que cada logo permanece completamente visible")]
    public float holdDuration = 2f;

    // ─────────────────────────────────────────────
    // Configuración de escena
    // ─────────────────────────────────────────────

    [Header("Escena destino")]
    [Tooltip("Nombre exacto de la escena del Menú Principal en Build Settings")]
    public string menuSceneName = "MenuScene";

    // ─────────────────────────────────────────────
    // Estado interno
    // ─────────────────────────────────────────────

    /// <summary>Flag que la corrutina revisa para saltar la secuencia.</summary>
    private bool _skipRequested = false;

    // ─────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────

    private void Start()
    {
        // Validar referencias en el Inspector
        if (unityLogoImage == null || gameLogoImage == null)
        {
            Debug.LogError("[BootManager] Faltan referencias de Image en el Inspector. Cargando menú directo.");
            LoadMenu();
            return;
        }

        // Ambos logos comienzan invisibles
        SetAlpha(unityLogoImage, 0f);
        SetAlpha(gameLogoImage, 0f);

        // Lanzar la secuencia
        StartCoroutine(BootSequence());
    }

    private void Update()
    {
        // Cualquier tecla salta la secuencia
        if (Input.anyKeyDown)
        {
            _skipRequested = true;
        }
    }

    // ─────────────────────────────────────────────
    // Corrutina principal
    // ─────────────────────────────────────────────

    /// <summary>
    /// Ejecuta la secuencia completa de splash screens.
    /// Cada logo hace: fade-in → hold → fade-out.
    /// Si el jugador presiona cualquier tecla, se salta todo y carga el menú.
    /// </summary>
    private IEnumerator BootSequence()
    {
        // ── 1. Logo de Unity ──────────────────────
        yield return StartCoroutine(ShowLogo(unityLogoImage));
        if (_skipRequested) { LoadMenu(); yield break; }

        // ── 2. Logo del Juego ─────────────────────
        yield return StartCoroutine(ShowLogo(gameLogoImage));

        // ── 3. Cargar el Menú Principal ───────────
        LoadMenu();
    }

    /// <summary>
    /// Muestra un logo con fade-in → hold → fade-out.
    /// Se interrumpe inmediatamente si se solicita skip.
    /// </summary>
    private IEnumerator ShowLogo(Image logo)
    {
        // Fade IN
        yield return StartCoroutine(FadeImage(logo, 0f, 1f, fadeDuration));
        if (_skipRequested) yield break;

        // Hold — espera mientras no se solicite skip
        float elapsed = 0f;
        while (elapsed < holdDuration && !_skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (_skipRequested) yield break;

        // Fade OUT
        yield return StartCoroutine(FadeImage(logo, 1f, 0f, fadeDuration));
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    /// <summary>
    /// Interpola el alpha de una Image entre dos valores en un tiempo dado.
    /// Usa Time.unscaledDeltaTime para que funcione aunque el juego esté en pausa.
    /// </summary>
    private IEnumerator FadeImage(Image image, float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (_skipRequested) yield break;

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(image, Mathf.Lerp(from, to, t));
            yield return null;
        }

        // Asegurar el valor final exacto
        SetAlpha(image, to);
    }

    /// <summary>Asigna el canal alpha de una Image sin modificar su color RGB.</summary>
    private static void SetAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    /// <summary>Carga la escena del Menú Principal.</summary>
    private void LoadMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }
}
