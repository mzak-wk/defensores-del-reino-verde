using UnityEngine;

public class NPCHubController : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;

    [Header("Interaction Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool closeOnExit = false;

    [Header("Indicador Visual [E]")]
    [SerializeField] private WorldInteractionPrompt promptUI;

    [Header("Physics Detection Radius")]
    [SerializeField] private bool useRadiusDetection = true;
    [SerializeField] private float interactionRadius = 3.0f;
    [SerializeField] private LayerMask playerLayerMask = ~0; // Por defecto detecta todas las capas

    [Header("Debug Log")]
    [SerializeField] private bool enableDebugLogs = false;

    private bool playerInRange;
    private IPlayerMovementLock currentMovementLock;
    private Transform playerTransform;

    private void Awake()
    {
        if (dialogueData == null)
        {
            dialogueData = Resources.Load<DialogueData>("Dialogues/opina_woods_hub");
        }

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
            promptUI.SetActionText("Hablar");
            promptUI.SetAlwaysShowCountdown(false);
            promptUI.SetPromptVisible(false);
        }
    }

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

        if (promptUI != null)
        {
            promptUI.SetPromptVisible(false);
        }
    }

    private void Update()
    {
        CheckProximityDetection();

        bool isDialogueOpen = DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;
        bool isHubUIOpen = HubUIManager.Instance != null && HubUIManager.Instance.IsOpen;
        bool canInteract = playerInRange && !isDialogueOpen && !isHubUIOpen;

        if (promptUI != null)
        {
            promptUI.SetPromptVisible(canInteract);
        }

        if (!canInteract)
        {
            return;
        }

        if (DialogueManager.Instance == null)
        {
            if (enableDebugLogs) Debug.LogWarning("[NPCHubController] No DialogueManager found in scene.");
            return;
        }

        if (Input.GetKeyDown(interactionKey))
        {
            if (enableDebugLogs) Debug.Log($"[NPCHubController] Key '{interactionKey}' pressed. Starting dialogue: {dialogueData.name}");
            if (promptUI != null) promptUI.SetPromptVisible(false);
            DialogueManager.Instance.StartDialogue(dialogueData, currentMovementLock);
        }
    }

    private void CheckProximityDetection()
    {
        if (!useRadiusDetection) return;

        bool detected = false;
        Transform detectedPlayer = null;
        IPlayerMovementLock detectedLock = null;

        // 1. Detección directa por distancia al objeto con Tag 'Player' (inmune a capas o colisionadores superpuestos)
        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= interactionRadius)
        {
            detected = true;
            detectedPlayer = playerTransform;
            detectedLock = currentMovementLock;
        }
        else
        {
            var pGO = GameObject.FindWithTag(playerTag);
            if (pGO != null && Vector2.Distance(transform.position, pGO.transform.position) <= interactionRadius)
            {
                detected = true;
                detectedPlayer = pGO.transform;
                detectedLock = pGO.GetComponent<IPlayerMovementLock>();
            }
            else
            {
                // 2. OverlapCircleAll examinando todos los colisionadores en el radio
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius, playerLayerMask);
                foreach (var hit in hits)
                {
                    if (hit == null || hit.gameObject == gameObject) continue;
                    if (hit.CompareTag(playerTag) || hit.GetComponentInParent<PlayerMovement>() != null)
                    {
                        detected = true;
                        detectedPlayer = hit.transform;
                        detectedLock = hit.GetComponentInParent<IPlayerMovementLock>();
                        break;
                    }
                }
            }
        }

        if (detected)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                playerTransform = detectedPlayer;
                currentMovementLock = detectedLock;
                if (enableDebugLogs) Debug.Log($"[NPCHubController] Player detectado en radio: {detectedPlayer?.name}");
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                if (enableDebugLogs) Debug.Log("[NPCHubController] Player salió del radio de interacción.");
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
