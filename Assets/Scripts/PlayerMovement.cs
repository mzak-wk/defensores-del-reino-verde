using UnityEngine;
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL || UNITY_ANDROID || UNITY_IOS
// (Opcional) Si en tu proyecto tienes Cinemachine instalado, descomenta esta linea:
// using Cinemachine;
#endif

public class PlayerMovement : MonoBehaviour, IPlayerMovementLock
{
    [Header("Base Movement")]
    public float moveSpeedBase = 5f;
    [HideInInspector] public float moveSpeed;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    private WeaponSlotManager weaponSlotManager;
    private bool movementLocked;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        weaponSlotManager = GetComponent<WeaponSlotManager>();
    }

    private void OnEnable()
    {
        LevelLayoutGenerator.OnPlayerSpawnPointReady += OnSpawnPointReady;
    }

    private void OnDisable()
    {
        LevelLayoutGenerator.OnPlayerSpawnPointReady -= OnSpawnPointReady;
    }

    private void Start()
    {
        moveSpeed = moveSpeedBase * (GameSession.Instance ? GameSession.Instance.GetMoveSpeedMultiplier() : 1f);
    }

    private void OnSpawnPointReady(Transform targetSpawnPoint)
    {
        if (targetSpawnPoint == null)
        {
            return;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = targetSpawnPoint.position;
        }

        transform.position = targetSpawnPoint.position;

        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                Camera.main.transform.position.z
            );
        }

        Debug.Log("[PlayerMovement] Jugador teletransportado al Spawn Point generado y camara reseteada.");
    }

    private void Update()
    {
        if (movementLocked)
        {
            moveInput = Vector2.zero;
            SetMovementAnimation(0f);
            return;
        }

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        SetMovementAnimation(moveInput.magnitude);

        HandleWeaponInputs();
    }

    private void HandleWeaponInputs()
    {
        if (weaponSlotManager == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) weaponSlotManager.SwitchWeapon(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) weaponSlotManager.SwitchWeapon(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) weaponSlotManager.SwitchWeapon(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) weaponSlotManager.SwitchWeapon(3);

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0f)
        {
            int next = (weaponSlotManager.currentSlotIndex + 1) % weaponSlotManager.equippedWeapons.Length;
            weaponSlotManager.SwitchWeapon(next);
        }
        else if (scroll < 0f)
        {
            int prev = weaponSlotManager.currentSlotIndex - 1;
            if (prev < 0) prev = weaponSlotManager.equippedWeapons.Length - 1;
            weaponSlotManager.SwitchWeapon(prev);
        }
    }

    private void FixedUpdate()
    {
        if (rb != null && !movementLocked)
        {
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;

        if (locked)
        {
            moveInput = Vector2.zero;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            SetMovementAnimation(0f);
        }
    }

    private void SetMovementAnimation(float movementValue)
    {
        if (animator == null) return;

        animator.SetFloat("movement", movementValue);

        // Freeze on current frame when idle, play normally when moving
        if (movementValue > 0.01f)
        {
            animator.speed = 1f;
        }
        else
        {
            animator.speed = 0f;
        }
    }
}
