using UnityEngine;
using UnityEngine.Playables;

public class LevelEntryDirector : MonoBehaviour
{
    [Header("Cinematic")]
    [Tooltip("The PlayableDirector that handles the level entry cinematic.")]
    [SerializeField] private PlayableDirector cinematicDirector;

    [Header("Dialogue")]
    [Tooltip("The dialogue data to be played after the cinematic ends.")]
    [SerializeField] private DialogueData entryDialogueData;

    [Header("Player Reference (Optional)")]
    [Tooltip("If left empty, the script will try to find the player dynamically.")]
    [SerializeField] private PlayerMovement playerMovement;

    private void Start()
    {
        // 1. Acquire Player Reference dynamically if not provided
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
            
            if (playerMovement == null)
            {
                Debug.LogWarning("[LevelEntryDirector] PlayerMovement no encontrado en la escena.");
                return; // Si no hay jugador, no podemos bloquearlo ni interactuar correctamente
            }
        }

        // 2. Lock player movement immediately at the start of the level
        playerMovement.SetMovementLocked(true);

        // 3. Setup and play the cinematic
        if (cinematicDirector != null)
        {
            // Suscribir al evento stopped para saber cuando termina la cinemática
            cinematicDirector.stopped += OnCinematicFinished;
            cinematicDirector.Play();
        }
        else
        {
            Debug.LogWarning("[LevelEntryDirector] PlayableDirector no asignado. Saltando a la fase de diálogo.");
            // Si no hay cinemática, procedemos directamente al diálogo
            OnCinematicFinished(null);
        }
    }

    private void OnCinematicFinished(PlayableDirector director)
    {
        // Desuscribir para evitar fugas de memoria o múltiples llamadas
        if (cinematicDirector != null)
        {
            cinematicDirector.stopped -= OnCinematicFinished;
        }

        // Iniciar el diálogo
        if (entryDialogueData != null && DialogueManager.Instance != null)
        {
            // Suscribirse al evento de cierre de diálogo
            DialogueManager.Instance.OnDialogueClosed += OnEntryDialogueEnded;
            
            // Iniciar el diálogo pasando la data y la referencia para el bloqueo del jugador
            DialogueManager.Instance.StartDialogue(entryDialogueData, playerMovement);
        }
        else
        {
            Debug.LogWarning("[LevelEntryDirector] DialogueData o DialogueManager no configurado. Desbloqueando jugador de inmediato.");
            // Si no hay diálogo configurado, simplemente desbloqueamos al jugador
            OnEntryDialogueEnded();
        }
    }

    /// <summary>
    /// Llamada pública que se ejecuta al finalizar el diálogo de entrada.
    /// Desbloquea al jugador y limpia el sistema.
    /// </summary>
    public void OnEntryDialogueEnded()
    {
        // Desuscribirse del manejador de diálogos
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueClosed -= OnEntryDialogueEnded;
        }

        // Desbloquear el movimiento del jugador para que empiece a jugar
        if (playerMovement != null)
        {
            playerMovement.SetMovementLocked(false);
        }

        Debug.Log("[LevelEntryDirector] Secuencia de entrada finalizada. Jugador desbloqueado.");

        // Destruir este script o el GameObject ya que cumplió su propósito inicial
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Limpieza de seguridad de los eventos por si el objeto es destruido antes de tiempo
        if (cinematicDirector != null)
        {
            cinematicDirector.stopped -= OnCinematicFinished;
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueClosed -= OnEntryDialogueEnded;
        }
    }
}
