using UnityEngine;

public class NPCHubController : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool closeOnExit = false;

    private bool playerInRange;
    private IPlayerMovementLock currentMovementLock;

    private void OnDisable()
    {
        if (closeOnExit && DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
        {
            DialogueManager.Instance.CloseDialogue();
        }

        SetPlayerMovementLocked(false);
        currentMovementLock = null;
        playerInRange = false;
    }

    private void Update()
    {
        if (!playerInRange || dialogueData == null)
        {
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("[NPCHubController] No DialogueManager found in the scene.");
            return;
        }

        if (Input.GetKeyDown(interactionKey) && !DialogueManager.Instance.IsOpen)
        {
            DialogueManager.Instance.StartDialogue(dialogueData, currentMovementLock);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = true;
        currentMovementLock = other.GetComponent<IPlayerMovementLock>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = false;

        if (closeOnExit && DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
        {
            DialogueManager.Instance.CloseDialogue();
        }

        SetPlayerMovementLocked(false);
        currentMovementLock = null;
    }

    private void SetPlayerMovementLocked(bool locked)
    {
        if (currentMovementLock != null)
        {
            currentMovementLock.SetMovementLocked(locked);
        }
    }
}
