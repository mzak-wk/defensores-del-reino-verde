using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// OptionsMenu — Gestiona el panel de opciones del Menú Principal.
///
/// Funcionalidades:
///   • Slider de volumen maestro → AudioMixer (curva logarítmica correcta con Mathf.Log10)
///   • TMP_Dropdown de resolución → detecta automáticamente las resoluciones del monitor,
///     filtra duplicados y las ordena de mayor a menor.
///   • Toggle de Pantalla Completa (Fullscreen / Windowed)
///   • Persistencia de preferencias con PlayerPrefs entre sesiones.
///
/// ────────────────────────────────────────────────────
/// SETUP EN UNITY:
///   1. Crea un AudioMixer en el proyecto (Assets > Create > Audio Mixer).
///   2. Expón el parámetro de volumen del grupo Master:
///      En el AudioMixer → selecciona el grupo Master → clic derecho en "Volume"
///      → "Expose 'Volume' to script" → renómbralo exactamente "MasterVolume".
///   3. Asigna ese AudioMixer al campo 'audioMixer' en el Inspector.
///   4. Arrastra los componentes UI a los campos correspondientes.
/// ────────────────────────────────────────────────────
/// </summary>
public class OptionsMenu : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Claves para PlayerPrefs
    // ─────────────────────────────────────────────

    private const string PREF_VOLUME     = "MasterVolume";
    private const string PREF_RES_INDEX  = "ResolutionIndex";
    private const string PREF_FULLSCREEN = "Fullscreen";

    // ─────────────────────────────────────────────
    // Referencias de Audio
    // ─────────────────────────────────────────────

    [Header("Audio")]
    [Tooltip("El AudioMixer del proyecto. Debe tener el parámetro 'MasterVolume' expuesto.")]
    public AudioMixer audioMixer;

    [Tooltip("Slider que controla el volumen maestro. Min = 0.0001, Max = 1.")]
    public Slider volumeSlider;

    // ─────────────────────────────────────────────
    // Referencias de Resolución
    // ─────────────────────────────────────────────

    [Header("Resolución")]
    [Tooltip("Dropdown de TMP que listará las resoluciones disponibles del monitor.")]
    public TMP_Dropdown resolutionDropdown;

    // ─────────────────────────────────────────────
    // Referencias de Pantalla Completa
    // ─────────────────────────────────────────────

    [Header("Pantalla Completa")]
    [Tooltip("Toggle que permite alternar entre Fullscreen y modo ventana.")]
    public Toggle fullscreenToggle;

    // ─────────────────────────────────────────────
    // Estado interno
    // ─────────────────────────────────────────────

    /// <summary>Lista de resoluciones únicas detectadas, sin duplicados, de mayor a menor.</summary>
    private List<Resolution> _availableResolutions = new List<Resolution>();

    /// <summary>Índice actualmente seleccionado en el dropdown.</summary>
    private int _currentResolutionIndex = 0;

    // ─────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────

    private void OnEnable()
    {
        // Cada vez que el panel de opciones se activa, inicializamos los controles.
        // Usamos OnEnable en lugar de Start porque el panel puede desactivarse y reactivarse.
        InitializeResolutionDropdown();
        InitializeVolumeSlider();
        InitializeFullscreenToggle();
    }

    // ─────────────────────────────────────────────
    // Inicialización
    // ─────────────────────────────────────────────

    /// <summary>
    /// Detecta las resoluciones soportadas por el monitor del jugador,
    /// filtra duplicados (misma anchura x altura),
    /// las ordena de mayor a menor y las carga en el TMP_Dropdown.
    /// Luego selecciona la resolución guardada en PlayerPrefs (o la actual del monitor).
    /// </summary>
    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            Debug.LogWarning("[OptionsMenu] resolutionDropdown no asignado en el Inspector.");
            return;
        }

        // ── 1. Obtener resoluciones y filtrar duplicados ──────────────────
        Resolution[] allResolutions = Screen.resolutions;
        _availableResolutions.Clear();

        // Usamos un HashSet para detectar pares (ancho, alto) ya vistos
        var seen = new HashSet<string>();

        // Screen.resolutions ya viene ordenado de menor a mayor;
        // recorremos en reverso para obtener mayor → menor directamente.
        for (int i = allResolutions.Length - 1; i >= 0; i--)
        {
            Resolution res = allResolutions[i];
            string key = $"{res.width}x{res.height}";

            if (seen.Contains(key)) continue; // duplicado, ignorar

            seen.Add(key);
            _availableResolutions.Add(res);
        }

        // ── 2. Construir las opciones del Dropdown ────────────────────────
        resolutionDropdown.ClearOptions();

        var options = new List<string>();
        int savedIndex  = PlayerPrefs.GetInt(PREF_RES_INDEX, -1);
        int matchIndex  = 0; // índice que coincide con la resolución actual

        for (int i = 0; i < _availableResolutions.Count; i++)
        {
            Resolution res = _availableResolutions[i];
            options.Add($"{res.width} x {res.height}");

            // Detectar coincidencia con la resolución actual de pantalla
            if (res.width == Screen.currentResolution.width &&
                res.height == Screen.currentResolution.height)
            {
                matchIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);

        // ── 3. Seleccionar la resolución guardada (o la actual) ───────────
        _currentResolutionIndex = (savedIndex >= 0 && savedIndex < _availableResolutions.Count)
            ? savedIndex
            : matchIndex;

        // Desuscribir antes de cambiar el valor para evitar disparo prematuro
        resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        resolutionDropdown.value = _currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    /// <summary>
    /// Carga el volumen guardado en PlayerPrefs y configura el Slider.
    /// El slider opera en rango lineal [0.0001 … 1]; internamente convertimos a dB.
    /// </summary>
    private void InitializeVolumeSlider()
    {
        if (volumeSlider == null)
        {
            Debug.LogWarning("[OptionsMenu] volumeSlider no asignado en el Inspector.");
            return;
        }

        // Rango del slider: 0.0001 a 1 (evitamos log10(0) que da -infinito)
        volumeSlider.minValue = 0.0001f;
        volumeSlider.maxValue = 1f;

        // Recuperar el valor guardado; por defecto volumen al 80%
        float savedVolume = PlayerPrefs.GetFloat(PREF_VOLUME, 0.8f);

        // Desuscribir antes de asignar el valor para evitar disparo prematuro
        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        volumeSlider.value = savedVolume;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        // Aplicar al mixer de inmediato
        ApplyVolumeToMixer(savedVolume);
    }

    /// <summary>
    /// Carga el estado de pantalla completa guardado en PlayerPrefs
    /// y configura el Toggle.
    /// </summary>
    private void InitializeFullscreenToggle()
    {
        if (fullscreenToggle == null)
        {
            Debug.LogWarning("[OptionsMenu] fullscreenToggle no asignado en el Inspector.");
            return;
        }

        // Por defecto: pantalla completa activada (1 = true)
        bool savedFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, 1) == 1;

        fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggled);
        fullscreenToggle.isOn = savedFullscreen;
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);

        // Aplicar al arrancar
        Screen.fullScreen = savedFullscreen;
    }

    // ─────────────────────────────────────────────
    // Callbacks de UI (llamados en tiempo real)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Llamado por el Slider cuando el jugador mueve el control de volumen.
    /// Convierte el valor lineal [0.0001…1] a dB usando Mathf.Log10 para
    /// respetar la curva logarítmica de percepción sonora humana.
    /// </summary>
    /// <param name="linearValue">Valor lineal del slider [0.0001 … 1].</param>
    public void OnVolumeChanged(float linearValue)
    {
        ApplyVolumeToMixer(linearValue);
        PlayerPrefs.SetFloat(PREF_VOLUME, linearValue);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Llamado por el TMP_Dropdown cuando el jugador selecciona una resolución.
    /// Aplica inmediatamente la resolución conservando el modo fullscreen actual.
    /// </summary>
    /// <param name="index">Índice seleccionado en el dropdown.</param>
    public void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= _availableResolutions.Count) return;

        _currentResolutionIndex = index;
        Resolution res = _availableResolutions[index];

        Screen.SetResolution(res.width, res.height, Screen.fullScreen);

        PlayerPrefs.SetInt(PREF_RES_INDEX, index);
        PlayerPrefs.Save();

        Debug.Log($"[OptionsMenu] Resolución aplicada: {res.width}x{res.height} | Fullscreen: {Screen.fullScreen}");
    }

    /// <summary>
    /// Llamado por el Toggle de pantalla completa.
    /// Aplica el cambio inmediatamente manteniendo la resolución actual.
    /// </summary>
    /// <param name="isFullscreen">true = pantalla completa, false = modo ventana.</param>
    public void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;

        PlayerPrefs.SetInt(PREF_FULLSCREEN, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[OptionsMenu] Modo pantalla: {(isFullscreen ? "Fullscreen" : "Windowed")}");
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    /// <summary>
    /// Convierte un valor lineal [0.0001…1] a decibeles y lo aplica al parámetro
    /// "MasterVolume" del AudioMixer.
    ///
    /// Fórmula: dB = 20 × log10(linearValue)
    ///   • linearValue = 1.0   → dB =   0  (volumen máximo, sin atenuación)
    ///   • linearValue = 0.5   → dB ≈ −6  (mitad de potencia percibida)
    ///   • linearValue = 0.0001→ dB = −80  (silencio práctico)
    /// </summary>
    /// <param name="linearValue">Valor lineal del volumen [0.0001 … 1].</param>
    private void ApplyVolumeToMixer(float linearValue)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("[OptionsMenu] audioMixer no asignado. No se puede aplicar el volumen.");
            return;
        }

        // Clamp de seguridad: log10(0) = -∞
        float safeValue = Mathf.Max(linearValue, 0.0001f);
        float dB = 20f * Mathf.Log10(safeValue);

        audioMixer.SetFloat("MasterVolume", dB);
    }
}
