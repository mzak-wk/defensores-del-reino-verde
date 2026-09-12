using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlador de la secuencia cinemática de Ultimate (estilo Persona 5 Cut-In).
/// Detiene el tiempo del juego, reproduce la animación del papel rasgado con máscara y desplaza
/// la mirada del personaje antes de reanudar el tiempo y desatar el ataque definitivo.
/// </summary>
public class UltimateCutInController : MonoBehaviour
{
    public static UltimateCutInController Instance { get; private set; }

    [Header("UI Root")]
    [Tooltip("Panel principal del Cut-In (debe estar desactivado por defecto).")]
    [SerializeField] private GameObject cutInRoot;

    [Header("Papel Rasgado")]
    [Tooltip("Image que contiene el sprite de la zona de recorte (con componente Mask).")]
    [SerializeField] private Image paperMaskImage;
    [Tooltip("Image decorativa SUPERIOR (encima de la máscara) que dibuja el papel con sus bordes blancos y los signos '!'.")]
    [SerializeField] private Image paperOverlayImage;
    [Tooltip("Spritesheet de la zona de recorte/máscara (ej. Ulti rell_0..3). Si está vacío, usa paperSprites.")]
    [SerializeField] private Sprite[] maskSprites;
    [Tooltip("Spritesheet del marco exterior y signos '!' (ej. Ulti_0..3).")]
    [SerializeField] private Sprite[] paperSprites;
    [SerializeField] private int paperFps = 24;

    [Header("Ojos del Personaje")]
    [Tooltip("RectTransform contenedor de los ojos que se desplaza horizontalmente.")]
    [SerializeField] private RectTransform eyesContainer;
    [Tooltip("Image que renderiza los ojos dentro de la máscara.")]
    [SerializeField] private Image eyesImage;
    [Tooltip("Spritesheet de la mirada animada del personaje (si tiene múltiples frames).")]
    [SerializeField] private Sprite[] eyesSprites;
    [SerializeField] private int eyesFps = 12;
    [Tooltip("Desplazamiento horizontal en píxeles hacia la izquierda para dar dinamismo.")]
    [SerializeField] private float slideDistanceX = 120f;

    [Header("Tiempos y Cinemática")]
    [Tooltip("Duración total de la cinemática en segundos (tiempo real no escalado).")]
    [SerializeField] private float sequenceDuration = 0.85f;

    [Header("Audio (Opcional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cutInSFX;

    public bool IsPlaying { get; private set; } = false;

    private Coroutine activeCutInCoroutine;
    private Vector2 eyesInitialAnchoredPos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (cutInRoot != null)
        {
            cutInRoot.SetActive(false);
        }

        if (paperOverlayImage != null && (cutInRoot == null || !paperOverlayImage.transform.IsChildOf(cutInRoot.transform)))
        {
            paperOverlayImage.gameObject.SetActive(false);
        }

        if (eyesContainer != null)
        {
            eyesInitialAnchoredPos = eyesContainer.anchoredPosition;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Inicia la secuencia de Cut-In. Pausa el mundo, reproduce el rasgado y los ojos, y ejecuta onComplete al terminar.
    /// </summary>
    /// <param name="customEyesSprite">Sprite de los ojos del personaje (opcional, usa el predeterminado si es null).</param>
    /// <param name="onComplete">Callback ejecutado al terminar la animación para disparar el ataque definitivo.</param>
    public void PlayCutIn(Sprite customEyesSprite, Action onComplete)
    {
        if (IsPlaying) return;

        if (activeCutInCoroutine != null)
        {
            StopCoroutine(activeCutInCoroutine);
        }

        activeCutInCoroutine = StartCoroutine(CutInSequenceCR(customEyesSprite, onComplete));
    }

    private IEnumerator CutInSequenceCR(Sprite customEyesSprite, Action onComplete)
    {
        IsPlaying = true;

        // 1. Pausar tiempo del juego
        float previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // 2. Proteger al jugador de daño durante la pose cinemática
        BarraVida playerHealth = FindFirstObjectByType<BarraVida>();
        if (playerHealth != null)
        {
            playerHealth.SetInvulnerable(true);
        }

        // 3. Configurar sprites y posiciones
        if (customEyesSprite != null && eyesImage != null)
        {
            eyesImage.sprite = customEyesSprite;
        }

        if (eyesContainer != null)
        {
            // Comenzar ligeramente a la derecha para desplazarse a la izquierda
            eyesContainer.anchoredPosition = eyesInitialAnchoredPos + new Vector2(slideDistanceX * 0.5f, 0f);
        }

        // Resetear al primer frame de la animación
        if (paperMaskImage != null)
        {
            if (maskSprites != null && maskSprites.Length > 0)
                paperMaskImage.sprite = maskSprites[0];
            else if (paperSprites != null && paperSprites.Length > 0)
                paperMaskImage.sprite = paperSprites[0];
        }

        if (paperOverlayImage != null && paperSprites != null && paperSprites.Length > 0)
        {
            paperOverlayImage.sprite = paperSprites[0];
        }

        if (cutInRoot != null)
        {
            cutInRoot.SetActive(true);
        }

        if (paperOverlayImage != null)
        {
            paperOverlayImage.gameObject.SetActive(true);
        }

        if (audioSource != null && cutInSFX != null)
        {
            audioSource.PlayOneShot(cutInSFX);
        }

        // 4. Ejecución simultánea: Reproducción de frames del papel + Paneo continuo de los ojos + Animación de ojos
        float elapsed = 0f;
        float paperFrameDuration = paperFps > 0 ? (1f / paperFps) : 0.04f;
        int paperFrameIndex = 0;
        float paperFrameTimer = 0f;

        float eyesFrameDuration = eyesFps > 0 ? (1f / eyesFps) : 0.08f;
        int eyesFrameIndex = 0;
        float eyesFrameTimer = 0f;

        Vector2 startPos = eyesContainer != null ? eyesContainer.anchoredPosition : Vector2.zero;
        Vector2 targetPos = eyesInitialAnchoredPos - new Vector2(slideDistanceX * 0.5f, 0f);

        while (elapsed < sequenceDuration)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            paperFrameTimer += dt;
            eyesFrameTimer += dt;

            // Animación del papel rasgado y la máscara
            int maxPaperFrames = Mathf.Max(
                paperSprites != null ? paperSprites.Length : 0,
                maskSprites != null ? maskSprites.Length : 0
            );

            if (maxPaperFrames > 0)
            {
                if (paperFrameTimer >= paperFrameDuration && paperFrameIndex < maxPaperFrames - 1)
                {
                    paperFrameTimer -= paperFrameDuration;
                    paperFrameIndex++;

                    // Máscara (silueta de recorte)
                    if (paperMaskImage != null)
                    {
                        if (maskSprites != null && paperFrameIndex < maskSprites.Length)
                            paperMaskImage.sprite = maskSprites[paperFrameIndex];
                        else if (paperSprites != null && paperFrameIndex < paperSprites.Length)
                            paperMaskImage.sprite = paperSprites[paperFrameIndex];
                    }

                    // Marco decorativo con signos '!'
                    if (paperOverlayImage != null && paperSprites != null && paperFrameIndex < paperSprites.Length)
                    {
                        paperOverlayImage.sprite = paperSprites[paperFrameIndex];
                    }
                }
            }

            // Animación de los ojos (si se asignó un array de sprites)
            if (eyesSprites != null && eyesSprites.Length > 0 && eyesImage != null)
            {
                if (eyesFrameTimer >= eyesFrameDuration)
                {
                    eyesFrameTimer -= eyesFrameDuration;
                    eyesFrameIndex = (eyesFrameIndex + 1) % eyesSprites.Length;
                    eyesImage.sprite = eyesSprites[eyesFrameIndex];
                }
            }

            // Paneo dinámico de los ojos hacia la izquierda (curva suave)
            if (eyesContainer != null)
            {
                float t = Mathf.Clamp01(elapsed / sequenceDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                eyesContainer.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
            }

            yield return null;
        }

        // 5. Limpieza y Restauración del estado de juego
        if (cutInRoot != null)
        {
            cutInRoot.SetActive(false);
        }

        if (paperOverlayImage != null)
        {
            paperOverlayImage.gameObject.SetActive(false);
        }

        if (eyesContainer != null)
        {
            eyesContainer.anchoredPosition = eyesInitialAnchoredPos;
        }

        if (playerHealth != null)
        {
            playerHealth.SetInvulnerable(false);
        }

        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        IsPlaying = false;
        activeCutInCoroutine = null;

        // 6. Desatar la habilidad Ultimate en el mundo
        onComplete?.Invoke();
    }
}
