using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Añade "juice" y micro-animaciones a los botones de UI:
/// - Agrandamiento suave al pasar el cursor (Hover scale 1.0 -> 1.08)
/// - Efecto de presión elástica (Punch/Squash al hacer clic)
/// - Resplandor o pulso sutil cuando está seleccionado
/// - Sonido opcional al pasar o hacer clic
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Escala y Animación")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float pressScale = 0.93f;
    [SerializeField] private float animSpeed = 12f;

    [Header("Pulso Idle al estar seleccionado")]
    [SerializeField] private bool idlePulseOnHover = true;
    [SerializeField] private float pulseFrequency = 3f;
    [SerializeField] private float pulseMagnitude = 0.03f;

    [Header("Audio (Opcional)")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isHovered = false;
    private bool isPressed = false;
    private Button button;
    private AudioSource audioSource;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
        targetScale = originalScale;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = GetComponentInParent<AudioSource>();
        }
    }

    private void OnEnable()
    {
        transform.localScale = originalScale;
        targetScale = originalScale;
        isHovered = false;
        isPressed = false;
    }

    private void Update()
    {
        if (button != null && !button.interactable)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.unscaledDeltaTime * animSpeed);
            return;
        }

        Vector3 desired = targetScale;
        if (isHovered && idlePulseOnHover && !isPressed)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * pulseFrequency) * pulseMagnitude;
            desired = originalScale * (hoverScale + pulse);
        }

        transform.localScale = Vector3.Lerp(transform.localScale, desired, Time.unscaledDeltaTime * animSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        isHovered = true;
        targetScale = originalScale * hoverScale;

        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound, 0.5f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isPressed)
        {
            targetScale = originalScale;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        isPressed = true;
        targetScale = originalScale * pressScale;

        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, 0.8f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        targetScale = isHovered ? (originalScale * hoverScale) : originalScale;
    }
}
