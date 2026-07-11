using UnityEngine;

public interface IPlayerMovementLock
{
    void SetMovementLocked(bool locked);
}

public class HubComputerController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private HubUIManager hubUIManager;

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool closeOnExit = false;

    private bool playerInRange;
    private IPlayerMovementLock currentMovementLock;

    private void OnEnable()
    {
        if (hubUIManager != null)
        {
            hubUIManager.OnComputerClosed += HandleComputerClosed;
        }
    }

    private void OnDisable()
    {
        if (hubUIManager != null)
        {
            hubUIManager.OnComputerClosed -= HandleComputerClosed;
        }

        SetPlayerMovementLocked(false);
    }

    private void Update()
    {
        if (!playerInRange || hubUIManager == null)
        {
            return;
        }

        if (Input.GetKeyDown(interactionKey))
        {
            OpenComputer();
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

        if (closeOnExit && hubUIManager != null && hubUIManager.IsOpen)
        {
            hubUIManager.CloseComputer();
        }

        SetPlayerMovementLocked(false);
        currentMovementLock = null;
    }

    private void OpenComputer()
    {
        if (hubUIManager.IsOpen)
        {
            return;
        }

        hubUIManager.OpenComputer();
        SetPlayerMovementLocked(true);
    }

    private void HandleComputerClosed()
    {
        SetPlayerMovementLocked(false);
    }

    private void SetPlayerMovementLocked(bool locked)
    {
        if (currentMovementLock != null)
        {
            currentMovementLock.SetMovementLocked(locked);
        }
    }
}
