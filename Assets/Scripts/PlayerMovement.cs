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
    private bool movementLocked;

    public Vector2 LastFacingDirection { get; private set; } = Vector2.right;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
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

        if (GameSession.Instance != null && !GameSession.Instance.hasCompletedIntro)
        {
            movementLocked = true;

            if (animator != null)
            {
                animator.SetFloat("movement", 0f);
            }
        }
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
        // Bloquear lectura de input, desplazamiento, giros y animaciones durante la pausa
        if (Mathf.Approximately(Time.timeScale, 0f))
            return;

        if (movementLocked)
        {
            moveInput = Vector2.zero;
            SetMovementAnimation(0f);
            return;
        }

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            LastFacingDirection = moveInput;
        }

        SetMovementAnimation(moveInput.magnitude);
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

        int currentWeaponIndex = animator.GetInteger("WeaponIndex");

        // REGLA DE VELOCIDAD DEL ANIMATOR:
        // 1. Si el jugador se mueve (movementValue > 0.01f) -> speed = 1f
        // 2. Si el jugador está desarmado (WeaponIndex == -1) -> speed = 1f (idle continuo)
        // 3. Si tiene arma equipada y está quieto -> speed = 0f (congela en el fotograma del arma)
        if (movementValue > 0.01f || currentWeaponIndex == -1)
        {
            animator.speed = 1f;
        }
        else
        {
            animator.speed = 0f;
        }
    }
}
