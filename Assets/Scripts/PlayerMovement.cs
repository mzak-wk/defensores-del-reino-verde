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

    [Header("Movement & Animation Thresholds")]
    [Tooltip("Umbral de velocidad física real (unidades/seg) para validar que el personaje se mueve.")]
    [SerializeField] private float velocityThreshold = 0.05f;

    [Tooltip("Umbral mínimo de input analógico para iniciar movimiento.")]
    [SerializeField] private float inputThreshold = 0.01f;

    [Tooltip("Multiplicador de velocidad de reproducción de la animación de caminar.")]
    [SerializeField] private float walkAnimationSpeedMultiplier = 1.0f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool movementLocked;
    private Vector2 lastFixedPosition;
    private float currentPhysicalSpeed;

    [Header("Sprite Orientation")]
    [Tooltip("Marcar true si el sprite original mira a la DERECHA por defecto.")]
    [SerializeField] private bool spriteFacesRightByDefault = true;

    public Vector2 LastFacingDirection { get; private set; } = Vector2.right;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        lastFixedPosition = rb != null ? rb.position : (Vector2)transform.position;
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
        }

        UpdateWalkAnimation(false, 0f);
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
            lastFixedPosition = rb.position;
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
            UpdateWalkAnimation(false, 0f);
            return;
        }

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        float inputSq = moveInput.sqrMagnitude;
        bool hasInput = inputSq > (inputThreshold * inputThreshold);
        bool isPhysicallyMoving = currentPhysicalSpeed > velocityThreshold;
        bool isMoving = hasInput && isPhysicallyMoving;

        if (hasInput)
        {
            LastFacingDirection = moveInput;

            // Manejar orientación horizontal del sprite (flipX)
            if (Mathf.Abs(moveInput.x) > inputThreshold && spriteRenderer != null)
            {
                bool movingLeft = moveInput.x < 0f;
                spriteRenderer.flipX = spriteFacesRightByDefault ? movingLeft : !movingLeft;
            }
        }

        UpdateWalkAnimation(isMoving, moveInput.magnitude);
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (!movementLocked && moveInput.sqrMagnitude > (inputThreshold * inputThreshold))
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Medir velocidad física real considerando posibles colisiones con obstáculos/paredes
        Vector2 displacement = rb.position - lastFixedPosition;
        currentPhysicalSpeed = displacement.magnitude / Mathf.Max(0.0001f, Time.fixedDeltaTime);
        lastFixedPosition = rb.position;
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;

        if (locked)
        {
            moveInput = Vector2.zero;
            currentPhysicalSpeed = 0f;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                lastFixedPosition = rb.position;
            }

            UpdateWalkAnimation(false, 0f);
        }
    }

    /// <summary>
    /// Controla la reproducción y congelamiento de la animación de caminata.
    /// Estilo Vampire Survivors:
    /// - Al moverse: animator.speed avanza a velocidad de caminata.
    /// - Al detenerse: animator.speed = 0 congela instantáneamente el frame exacto del clip.
    /// </summary>
    private void UpdateWalkAnimation(bool isMoving, float movementValue)
    {
        if (animator == null) return;

        animator.SetFloat("movement", isMoving ? movementValue : 0f);

        float targetSpeed = isMoving ? walkAnimationSpeedMultiplier : 0f;
        if (!Mathf.Approximately(animator.speed, targetSpeed))
        {
            animator.speed = targetSpeed;
        }
    }
}
