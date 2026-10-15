using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente genérico y modular de daño ecológico (Erosión en Bosque, Vórtice en Hielo, Basura en Playa).
/// Requiere interactuar con la tecla 'E' para limpiar el daño, enfriar el termómetro y detener el calentamiento.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EcoHazardZone : MonoBehaviour
{
    [Header("Interacción con Tecla 'E'")]
    [Tooltip("Tecla requerida para interactuar con este daño ecológico.")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Tooltip("Tiempo en segundos que se debe mantener presionada 'E' (0 para pulsar una sola vez).")]
    [SerializeField, Min(0f)] private float holdDuration = 1.5f;

    [Tooltip("Indicador visual flotante sobre el objeto.")]
    [SerializeField] private WorldInteractionPrompt promptUI;

    [Header("Efecto Ambiental")]
    [Tooltip("Reducción de temperatura al ser limpiada (<= 0 toma el valor por defecto del nivel).")]
    [SerializeField, Min(0f)] private float tempReduction = 0f;

    [Tooltip("Prefab opcional a instanciar al completar (árbol, efecto de limpieza, etc.).")]
    [SerializeField] private GameObject spawnOnCompletePrefab;

    [Header("UI de Progreso Opcional")]
    [SerializeField] private Slider progressBarUI;

    private float currentProgress = 0f;
    private bool isCompleted = false;
    private bool playerInRange = false;
    private bool isRegistered = false;

    public bool IsCompleted => isCompleted;

    private void Awake()
    {
        if (promptUI == null)
        {
            promptUI = GetComponent<WorldInteractionPrompt>() ?? GetComponentInChildren<WorldInteractionPrompt>();
            if (promptUI == null)
            {
                promptUI = gameObject.AddComponent<WorldInteractionPrompt>();
            }
        }

        if (promptUI != null)
        {
            promptUI.SetActionText("Limpiar");
            promptUI.SetAlwaysShowCountdown(false);
            promptUI.SetPromptVisible(false);
        }

        UpdateProgressUI(false);
    }

    private void OnEnable()
    {
        if (isCompleted) return;
        RegisterInManager();
    }

    private void Start()
    {
        RegisterInManager();
    }

    private void OnDisable()
    {
        UnregisterFromManager();
    }

    private void Update()
    {
        if (isCompleted || !playerInRange)
        {
            if (promptUI != null && !playerInRange)
                promptUI.SetPromptVisible(false);
            return;
        }

        if (promptUI != null)
            promptUI.SetPromptVisible(true);

        bool isHolding = Input.GetKey(interactionKey);
        bool isKeyDown = Input.GetKeyDown(interactionKey);

        if (promptUI != null)
        {
            promptUI.SetKeyHeld(isHolding);
            promptUI.SetProgress(holdDuration <= 0f ? 1f : (currentProgress / holdDuration));
        }

        if (holdDuration <= 0f)
        {
            // Interacción instantánea con un toque de E
            if (isKeyDown)
            {
                CompleteInteraction();
            }
        }
        else
        {
            // Interacción manteniendo presionada E
            if (isHolding)
            {
                currentProgress = Mathf.Min(holdDuration, currentProgress + Time.deltaTime);
                UpdateProgressUI(true);

                if (currentProgress >= holdDuration)
                {
                    CompleteInteraction();
                }
            }
            else
            {
                UpdateProgressUI(currentProgress > 0f);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCompleted || !other.CompareTag("Player")) return;

        playerInRange = true;
        if (promptUI != null)
            promptUI.SetPromptVisible(true);

        UpdateProgressUI(currentProgress > 0f);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (promptUI != null)
        {
            promptUI.SetPromptVisible(false);
            promptUI.SetKeyHeld(false);
        }
        UpdateProgressUI(false);
    }

    private void CompleteInteraction()
    {
        isCompleted = true;
        playerInRange = false;

        if (promptUI != null)
            promptUI.SetPromptVisible(false);

        UpdateProgressUI(false);
        UnregisterFromManager();

        if (spawnOnCompletePrefab != null)
        {
            Instantiate(spawnOnCompletePrefab, transform.position, Quaternion.identity);
        }

        if (HazardMeterManager.Instance != null)
        {
            float reduction = (tempReduction > 0f) ? tempReduction : HazardMeterManager.Instance.DefaultHazardReduction;
            HazardMeterManager.Instance.ReduceHazard(reduction);
        }

        Destroy(gameObject);
    }

    private void RegisterInManager()
    {
        if (isCompleted || isRegistered) return;

        if (HazardMeterManager.Instance != null)
        {
            HazardMeterManager.Instance.RegisterDamageZone(this);
            isRegistered = true;
        }
    }

    private void UnregisterFromManager()
    {
        if (!isRegistered) return;

        if (HazardMeterManager.Instance != null)
        {
            HazardMeterManager.Instance.UnregisterDamageZone(this);
            isRegistered = false;
        }
    }

    private void UpdateProgressUI(bool visible)
    {
        if (progressBarUI == null) return;

        progressBarUI.gameObject.SetActive(visible && !isCompleted);
        progressBarUI.minValue = 0f;
        progressBarUI.maxValue = 1f;
        progressBarUI.value = holdDuration <= 0f ? 1f : Mathf.Clamp01(currentProgress / holdDuration);
    }
}
