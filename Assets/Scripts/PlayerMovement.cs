using UnityEngine;
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL || UNITY_ANDROID || UNITY_IOS
// (Opcional) Si en tu proyecto tienes Cinemachine instalado, descomenta esta línea:
// using Cinemachine;
#endif

public class PlayerMovement : MonoBehaviour
{
    [Header("Base Movement")]
    public float moveSpeedBase = 5f;   // velocidad base del personaje
    [HideInInspector] public float moveSpeed; // usada en runtime

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;

    // ==========================================
    // 1. INICIALIZACIÓN TEMPRANA (AWAKE)
    // ==========================================
    private void Awake()
    {
        // Caché de componentes en Awake para evitar nulls si el evento dispara antes del Start
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // ==========================================
    // 2. SUSCRIPCIÓN AL SISTEMA DE GENERACIÓN
    // ==========================================
    private void OnEnable()
    {
        LevelLayoutGenerator.OnPlayerSpawnPointReady += OnSpawnPointReady;
    }

    private void OnDisable()
    {
        LevelLayoutGenerator.OnPlayerSpawnPointReady -= OnSpawnPointReady;
    }

    // ==========================================
    // 3. INICIALIZACIÓN (START)
    // ==========================================
    void Start()
    {
        // aplica multiplicador global si existe GameSession
        moveSpeed = moveSpeedBase * (GameSession.Instance ? GameSession.Instance.GetMoveSpeedMultiplier() : 1f);
        
        // [AQUÍ MANTIENES TU LÓGICA EXTRA SI LA TIENES]
    }

    // ==========================================
    // 4. CALLBACK DE TELETRANSPORTE
    // ==========================================
    private void OnSpawnPointReady(Transform targetSpawnPoint)
    {
        if (targetSpawnPoint == null) return;

        // 1. Resetear físicas
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // En Unity 2022 o anterior, usar rb.velocity
            rb.position = targetSpawnPoint.position;
        }

        // 2. Mover Transform
        transform.position = targetSpawnPoint.position;

        // 3. Resetear la Cámara (Para evitar paneo brusco de un extremo a otro)
        
        // OPCIÓN A: Si usas una cámara con seguimiento manual clásico
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(
                transform.position.x, 
                transform.position.y, 
                Camera.main.transform.position.z
            );
        }

        // OPCIÓN B: Si usas Cinemachine (Asegúrate de importar el namespace 'using Cinemachine;' arriba)
        /*
        CinemachineVirtualCamera cvc = FindFirstObjectByType<CinemachineVirtualCamera>();
        if (cvc != null)
        {
            // Le indicamos a Cinemachine que el objetivo se ha teletransportado
            cvc.OnTargetObjectWarped(transform, targetSpawnPoint.position - transform.position);
        }
        */

        Debug.Log("[PlayerMovement] Jugador teletransportado al Spawn Point generado y cámara reseteada.");
    }

    // ==========================================
    // 5. LÓGICA DE MOVIMIENTO ORIGINAL
    // ==========================================
    void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        if (animator != null)
        {
            float movementValue = moveInput.magnitude;
            animator.SetFloat("movement", movementValue);
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }
    }
}
