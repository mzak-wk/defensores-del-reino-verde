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

    [Header("Physics Interaction Detection")]
    [SerializeField] private bool useRadiusDetection = true;
    [SerializeField] private float interactionRadius = 2.5f;

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
        CheckProximityDetection();

        if (!playerInRange || hubUIManager == null)
        {
            return;
        }

        // Bloqueo 1.1: Evitar abrir el ordenador si el sistema de diálogos está abierto
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
        {
            return;
        }

        if (Input.GetKeyDown(interactionKey))
        {
            OpenComputer();
        }
    }

    private void CheckProximityDetection()
    {
        if (!useRadiusDetection) return;

        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactionRadius);
        if (hit != null && hit.CompareTag(playerTag))
        {
            if (!playerInRange)
            {
                playerInRange = true;
                currentMovementLock = hit.GetComponent<IPlayerMovementLock>();
            }
        }
        else
        {
            if (playerInRange && useRadiusDetection)
            {
                playerInRange = false;
                if (closeOnExit && hubUIManager != null && hubUIManager.IsOpen)
                {
                    hubUIManager.CloseComputer();
                }
                SetPlayerMovementLocked(false);
                currentMovementLock = null;
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
        currentMovementLock = other.GetComponent<IPlayerMovementLock>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (useRadiusDetection || !other.CompareTag(playerTag))
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

    private void OnDrawGizmosSelected()
    {
        if (useRadiusDetection)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
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
