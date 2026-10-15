using UnityEngine;

/// <summary>
/// Representa una mancha de hollín / vórtice contaminante en el Nivel 2 (Polo Norte).
/// Reduce el albedo y acelera el derretimiento polar si no se atiende rápidamente.
/// Al acercarse y presionar la tecla 'E', el jugador la limpia, enfriando el medidor térmico.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SootZone : MonoBehaviour
{
    [Header("Interacción con Tecla 'E'")]
    [Tooltip("Tecla requerida para limpiar la mancha de hollín.")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Tooltip("Indicador visual flotante sobre el hollín.")]
    [SerializeField] private WorldInteractionPrompt promptUI;

    [Header("Configuración Térmica")]
    [Tooltip("Reducción de temperatura al ser limpiada. Si es <= 0, tomará el valor por defecto del gestor.")]
    [SerializeField, Min(0f)] private float tempReduction = 0f;

    private bool isRegisteredInHazard = false;
    private bool isRegisteredInTemp = false;
    private bool isCollected = false;
    private bool playerInRange = false;

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
    }

    private void OnEnable()
    {
        if (isCollected) return;
        RegisterInManagers();
    }

    private void Start()
    {
        RegisterInManagers();
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void Update()
    {
        if (isCollected || !playerInRange) return;

        if (Input.GetKeyDown(interactionKey))
        {
            if (promptUI != null)
                promptUI.SetKeyHeld(true);

            CollectSoot();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected || !other.CompareTag("Player")) return;

        playerInRange = true;
        if (promptUI != null)
            promptUI.SetPromptVisible(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Comprobación de seguridad: si el jugador sigue dentro del radio, ignorar salidas causadas por ataques
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && Vector2.Distance(transform.position, player.transform.position) <= 2.5f)
        {
            return;
        }

        playerInRange = false;
        if (promptUI != null)
        {
            promptUI.SetPromptVisible(false);
            promptUI.SetKeyHeld(false);
        }
    }

    private void CollectSoot()
    {
        isCollected = true;
        playerInRange = false;

        if (promptUI != null)
            promptUI.SetPromptVisible(false);

        Unregister();

        if (HazardMeterManager.Instance != null)
        {
            float reduction = (tempReduction > 0f) ? tempReduction : HazardMeterManager.Instance.DefaultHazardReduction;
            HazardMeterManager.Instance.ReduceHazard(reduction);
        }

        if (TemperatureManager.Instance != null)
        {
            float reduction = (tempReduction > 0f) ? tempReduction : TemperatureManager.Instance.TempReductionPerClean;
            TemperatureManager.Instance.ReduceTemperature(reduction);
        }

        Destroy(gameObject);
    }

    private void RegisterInManagers()
    {
        if (isCollected) return;

        if (!isRegisteredInHazard && HazardMeterManager.Instance != null)
        {
            HazardMeterManager.Instance.RegisterDamageZone(this);
            isRegisteredInHazard = true;
        }

        if (!isRegisteredInTemp && TemperatureManager.Instance != null)
        {
            TemperatureManager.Instance.RegisterSoot();
            isRegisteredInTemp = true;
        }
    }

    private void Unregister()
    {
        if (isRegisteredInHazard && HazardMeterManager.Instance != null)
        {
            HazardMeterManager.Instance.UnregisterDamageZone(this);
            isRegisteredInHazard = false;
        }

        if (isRegisteredInTemp && TemperatureManager.Instance != null)
        {
            TemperatureManager.Instance.UnregisterSoot();
            isRegisteredInTemp = false;
        }
    }
}
