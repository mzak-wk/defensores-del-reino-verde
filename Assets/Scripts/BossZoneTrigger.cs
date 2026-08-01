using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Script de activación del evento de pre-combate contra el Jefe.
/// Hereda de MonoBehaviour y requiere un componente Collider2D (Física 2D por defecto).
/// Detecta la llegada del jugador ("Player"), bloquea su movimiento, activa la cinemática de diálogo
/// y dispara un UnityEvent al finalizar para activar la IA o comportamientos del Jefe.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossZoneTrigger : MonoBehaviour
{
    [Header("Configuración de Diálogo")]
    [Tooltip("Datos del diálogo que se mostrarán antes de comenzar la batalla.")]
    [SerializeField] private DialogueData bossDialogueData;

    [Header("Eventos de Combate")]
    [Tooltip("Evento que se invoca cuando el diálogo del jefe finaliza para iniciar el combate (ej: encender IA del jefe).")]
    public UnityEvent OnBossFightStart;

    [Header("Referencias Opcionales")]
    [Tooltip("Referencia opcional al PlayerMovement. Si se deja vacía, se buscará dinámicamente.")]
    [SerializeField] private PlayerMovement playerMovement;

    private Collider2D triggerCollider;
    private bool isTriggered = false;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
    }

    /// <summary>
    /// Detección de entrada del jugador en la zona de activación.
    /// (Nota: Si tu proyecto utilizara Física 3D, solo debes reemplazar Collider2D por Collider y OnTriggerEnter2D por OnTriggerEnter).
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Evitar ejecuciones duplicadas si ya fue activado
        if (isTriggered) return;

        // Validar que el objeto que entra tenga el Tag "Player"
        if (other.CompareTag("Player"))
        {
            isTriggered = true;

            // 1. Desactivar su propio componente Collider inmediatamente para evitar múltiples ejecuciones
            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }

            // 2. Localizar y obtener la referencia del jugador
            if (playerMovement == null)
            {
                playerMovement = other.GetComponent<PlayerMovement>();
                if (playerMovement == null)
                {
                    playerMovement = FindFirstObjectByType<PlayerMovement>();
                }
            }

            // Bloquear el movimiento del jugador
            if (playerMovement != null)
            {
                playerMovement.SetMovementLocked(true);
            }
            else
            {
                Debug.LogWarning("[BossZoneTrigger] No se pudo encontrar el componente PlayerMovement.");
            }

            // 3. Iniciar el diálogo con DialogueManager y suscribirse al evento de cierre
            if (bossDialogueData != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueClosed += HandleDialogueEnded;
                DialogueManager.Instance.StartDialogue(bossDialogueData, playerMovement);
            }
            else
            {
                Debug.LogWarning("[BossZoneTrigger] DialogueData o DialogueManager no asignado/disponible. Desbloqueando e iniciando combate de inmediato.");
                HandleDialogueEnded();
            }
        }
    }

    /// <summary>
    /// Función ejecutada al finalizar el diálogo.
    /// Desbloquea el movimiento del jugador e invoca OnBossFightStart.
    /// </summary>
    private void HandleDialogueEnded()
    {
        // Desuscribirse del evento para evitar fugas de memoria
        UnsubscribeFromDialogue();

        // Desbloquear movimiento del jugador
        if (playerMovement != null)
        {
            playerMovement.SetMovementLocked(false);
        }

        Debug.Log("[BossZoneTrigger] Diálogo finalizado. Ejecutando OnBossFightStart.");

        // Invocar el UnityEvent para encender la IA o elementos visuales del jefe
        OnBossFightStart?.Invoke();
    }

    /// <summary>
    /// Método auxiliar para desuscribir de forma limpia el evento OnDialogueClosed.
    /// </summary>
    private void UnsubscribeFromDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueClosed -= HandleDialogueEnded;
        }
    }

    private void OnDestroy()
    {
        // Limpieza de seguridad al destruir el objeto para prevenir fugas de memoria (Memory Leaks)
        UnsubscribeFromDialogue();
    }
}
