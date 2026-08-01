using UnityEngine;

public class NPCHubController : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;

    [Header("Interaction Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool closeOnExit = false;

    [Header("Physics Detection Radius")]
    [SerializeField] private bool useRadiusDetection = true;
    [SerializeField] private float interactionRadius = 2.5f;
    [SerializeField] private LayerMask playerLayerMask = ~0; // Por defecto detecta todas las capas

    [Header("Debug Log")]
    [SerializeField] private bool enableDebugLogs = false;

    private bool playerInRange;
    private IPlayerMovementLock currentMovementLock;
    private Transform playerTransform;

    private void OnDisable()
    {
        if (closeOnExit && DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
        {
            DialogueManager.Instance.CloseDialogue();
        }

        SetPlayerMovementLocked(false);
        currentMovementLock = null;
        playerInRange = false;
        playerTransform = null;
    }

    private void Update()
    {
        CheckProximityDetection();

        if (!playerInRange)
        {
            return;
        }

        if (DialogueManager.Instance == null)
        {
            if (enableDebugLogs) Debug.LogWarning("[NPCHubController] No DialogueManager found in scene.");
            return;
        }

        // Evitar interacción si el canvas de la computadora está abierto
        if (HubUIManager.Instance != null && HubUIManager.Instance.IsOpen)
        {
            if (enableDebugLogs) Debug.Log("[NPCHubController] HubUIManager is open. Interaction blocked.");
            return;
        }

        if (Input.GetKeyDown(interactionKey) && !DialogueManager.Instance.IsOpen)
        {
            if (enableDebugLogs) Debug.Log($"[NPCHubController] Key '{interactionKey}' pressed. Starting dialogue: {dialogueData.name}");
            DialogueManager.Instance.StartDialogue(dialogueData, currentMovementLock);
        }
    }

    private void CheckProximityDetection()
    {
        if (!useRadiusDetection) return;

        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactionRadius, playerLayerMask);
        if (hit != null && hit.CompareTag(playerTag))
        {
            if (!playerInRange)
            {
                playerInRange = true;
                playerTransform = hit.transform;
                currentMovementLock = hit.GetComponent<IPlayerMovementLock>();
                if (enableDebugLogs) Debug.Log($"[NPCHubController] Player detected in radius: {hit.name}");
            }
        }
        else
        {
            if (playerInRange && useRadiusDetection)
            {
                playerInRange = false;
                if (enableDebugLogs) Debug.Log("[NPCHubController] Player left interaction radius.");
                if (closeOnExit && DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
                {
                    DialogueManager.Instance.CloseDialogue();
                }
                SetPlayerMovementLocked(false);
                currentMovementLock = null;
                playerTransform = null;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (useRadiusDetection || !other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = true;
        playerTransform = other.transform;
        currentMovementLock = other.GetComponent<IPlayerMovementLock>();
        if (enableDebugLogs) Debug.Log($"[NPCHubController] Player entered Trigger: {other.name}");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (useRadiusDetection || !other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = false;
        if (enableDebugLogs) Debug.Log("[NPCHubController] Player exited Trigger.");

        if (closeOnExit && DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
        {
            DialogueManager.Instance.CloseDialogue();
        }

        SetPlayerMovementLocked(false);
        currentMovementLock = null;
        playerTransform = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (useRadiusDetection)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }

    private void SetPlayerMovementLocked(bool locked)
    {
        if (currentMovementLock != null)
        {
            currentMovementLock.SetMovementLocked(locked);
        }
    }
}
