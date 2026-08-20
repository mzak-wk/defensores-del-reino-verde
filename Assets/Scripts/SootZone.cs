using UnityEngine;

/// <summary>
/// Representa una mancha de hollín dejada por un enemigo en el Nivel 2.
/// Al contacto inmediato con el jugador, reduce la temperatura del TemperatureManager y se destruye.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SootZone : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Reducción de temperatura al ser limpiada. Si es <= 0, tomará el valor por defecto del TemperatureManager.")]
    [SerializeField, Min(0f)] private float tempReduction = 0f;

    private bool isRegistered = false;
    private bool isCollected = false;

    private void OnEnable()
    {
        if (isCollected) return;

        if (TemperatureManager.Instance != null)
        {
            TemperatureManager.Instance.RegisterSoot();
            isRegistered = true;
        }
    }

    private void Start()
    {
        // Respaldo por si se instanció antes de que TemperatureManager terminara su Awake
        if (!isRegistered && !isCollected && TemperatureManager.Instance != null)
        {
            TemperatureManager.Instance.RegisterSoot();
            isRegistered = true;
        }
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            CollectSoot();
        }
    }

    private void CollectSoot()
    {
        isCollected = true;
        Unregister();

        if (TemperatureManager.Instance != null)
        {
            float reduction = (tempReduction > 0f) ? tempReduction : TemperatureManager.Instance.TempReductionPerClean;
            TemperatureManager.Instance.ReduceTemperature(reduction);
        }
        else
        {
            Debug.LogWarning("[SootZone] Se recogió la mancha de hollín pero no hay TemperatureManager activo en la escena.", this);
        }

        Destroy(gameObject);
    }

    private void Unregister()
    {
        if (!isRegistered) return;

        if (TemperatureManager.Instance != null)
        {
            TemperatureManager.Instance.UnregisterSoot();
        }

        isRegistered = false;
    }
}
