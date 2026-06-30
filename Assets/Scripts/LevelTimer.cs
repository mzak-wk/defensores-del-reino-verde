using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class LevelTimer : MonoBehaviour
{
    [Header("Ajustes")]
    [Tooltip("Duración inicial en segundos (p. ej. 300 = 5:00).")]
    public float duration = 300f;
    [Tooltip("Arrancar automáticamente al habilitar el objeto.")]
    public bool autoStart = true;
    [Tooltip("Usar tiempo independiente de la pausa (true = no se detiene con timeScale=0).")]
    public bool useUnscaledTime = false;

    [Header("UI (opcionales)")]
    public TMP_Text label;   // Texto tipo "03:27"
    public Image fillBar;    // Imagen Filled para barra de progreso (opcional)

    [Header("Eventos")]
    public UnityEvent onTimerEnd;

    // NUEVO: opción para suscribirse por código (Action)
    public Action onTimerEndAction;

    float remaining;
    bool running;

    void Awake()
    {
        remaining = duration;
        UpdateUI();
    }

    void OnEnable()
    {
        if (autoStart) StartTimer(duration);
    }

    void Update()
    {
        if (!running) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        remaining -= dt;

        if (remaining <= 0f)
        {
            remaining = 0f;
            running = false;
            UpdateUI();

            // Invocamos ambos: UnityEvent (inspector) y Action (por código)
            onTimerEnd?.Invoke();
            onTimerEndAction?.Invoke();
        }
        else
        {
            UpdateUI();
        }
    }

    // === API pública ===
    public void StartTimer(float seconds)
    {
        duration = Mathf.Max(0f, seconds);
        remaining = duration;
        running = duration > 0f;
        UpdateUI();
    }

    public void StopTimer() { running = false; }
    public void Resume() { if (remaining > 0f) running = true; }
    public void AddSeconds(float s)
    {
        remaining = Mathf.Clamp(remaining + s, 0f, Mathf.Infinity);
        UpdateUI();
    }

    public float RemainingSeconds => remaining;
    public string RemainingAsString => Format(remaining);

    // === UI ===
    void UpdateUI()
    {
        if (label) label.text = Format(remaining);
        if (fillBar)
        {
            fillBar.fillAmount = (duration <= 0f) ? 0f : Mathf.Clamp01(remaining / duration);
        }
    }

    public static string Format(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}
