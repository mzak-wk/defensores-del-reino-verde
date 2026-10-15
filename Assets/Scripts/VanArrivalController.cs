using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Controla la llegada y estacionamiento de la Van de Opina en los niveles.
/// Al terminar la cinemática de entrada o cuando la Van llega a su destino,
/// detiene inmediatamente la animación de ruedas/vibración para que quede
/// correctamente apagada y estacionada.
/// </summary>
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class VanArrivalController : MonoBehaviour
{
    [Header("Cinemática y Detección")]
    [Tooltip("PlayableDirector de la entrada del nivel. Si está vacío, se busca automáticamente.")]
    [SerializeField] private PlayableDirector entranceDirector;

    [Tooltip("Tiempo de seguridad tras el cual la Van se asume estacionada si no hay director (segundos).")]
    [SerializeField] private float fallbackStopTime = 5.0f;

    [Tooltip("Margen de movimiento: si la posición cambia menos de este valor en un frame, se considera quieta.")]
    [SerializeField] private float movementThreshold = 0.005f;

    [Header("Efectos al Parar")]
    [Tooltip("Animation Override Controller con la animación quieta/apagada de la camioneta.")]
    [SerializeField] private RuntimeAnimatorController idleOverrideController;

    private Animator[] allAnimators;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Vector3 lastPosition;
    private bool hasStopped = false;
    private float stationaryTimer = 0f;
    private bool hasMoved = false;

    private void Awake()
    {
        allAnimators = GetComponents<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        lastPosition = transform.position;

        if (idleOverrideController == null)
        {
            idleOverrideController = Resources.Load<RuntimeAnimatorController>("Van_Idle_Override");
        }

        FindEntranceDirector();
    }

    private void FindEntranceDirector()
    {
        if (entranceDirector != null) return;

        var directors = Object.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None);
        foreach (var dir in directors)
        {
            if (dir == null) continue;
            string dName = dir.gameObject.name.ToLower();
            string aName = dir.playableAsset != null ? dir.playableAsset.name.ToLower() : "";
            
            // Prioridad a directores de entrada o de la furgoneta
            if (dName.Contains("entrada") || dName.Contains("van") || dName.Contains("cinematica") ||
                aName.Contains("entrada") || aName.Contains("van") || aName.Contains("cinematica"))
            {
                entranceDirector = dir;
                break;
            }
        }

        // Si no encontramos por nombre, buscar el que esté en reproducción activa
        if (entranceDirector == null)
        {
            foreach (var dir in directors)
            {
                if (dir != null && dir.state == PlayState.Playing)
                {
                    entranceDirector = dir;
                    break;
                }
            }
        }
    }

    private void OnEnable()
    {
        if (entranceDirector != null)
        {
            entranceDirector.stopped += OnDirectorStopped;
        }
    }

    private void OnDisable()
    {
        if (entranceDirector != null)
        {
            entranceDirector.stopped -= OnDirectorStopped;
        }
    }

    private void Start()
    {
        if (idleOverrideController == null)
        {
            idleOverrideController = Resources.Load<RuntimeAnimatorController>("Van_Idle_Override");
        }

        FindEntranceDirector();
        StartCoroutine(WatchVanArrival());
    }

    private IEnumerator WatchVanArrival()
    {
        float timer = 0f;
        while (!hasStopped)
        {
            timer += Time.deltaTime;

            // 1. Si hay director de entrada activo
            if (entranceDirector != null && entranceDirector.playableAsset != null)
            {
                // Solo si el director ya arrancó o superó su tiempo
                if (entranceDirector.time >= (entranceDirector.duration - 0.1) || 
                    (timer > 1.5f && entranceDirector.state != PlayState.Playing))
                {
                    StopVan();
                    yield break;
                }
            }

            // 2. Detección por inmovilidad física tras haber avanzado
            float deltaMove = Vector3.Distance(transform.position, lastPosition);
            lastPosition = transform.position;

            if (deltaMove > movementThreshold)
            {
                hasMoved = true;
                stationaryTimer = 0f;
            }
            else
            {
                // Si ya se movió y se detuvo, o si ya pasaron 1.2s y nunca se movió (ya está estacionada)
                if (hasMoved || timer > 1.2f)
                {
                    stationaryTimer += Time.deltaTime;
                    if (stationaryTimer >= 0.35f)
                    {
                        StopVan();
                        yield break;
                    }
                }
            }

            // 3. Fallback absoluto de tiempo de entrada
            if (timer >= fallbackStopTime)
            {
                StopVan();
                yield break;
            }

            yield return null;
        }
    }

    private void LateUpdate()
    {
        if (!hasStopped) return;

        // Mantener la animación quieta usando el Animation Override Controller
        if (allAnimators != null)
        {
            for (int i = 0; i < allAnimators.Length; i++)
            {
                var anim = allAnimators[i];
                if (anim != null)
                {
                    if (idleOverrideController != null && anim.runtimeAnimatorController != idleOverrideController)
                    {
                        anim.runtimeAnimatorController = idleOverrideController;
                    }
                    anim.speed = 0f;
                }
            }
        }
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        StopVan();
    }

    /// <summary>
    /// Frena la animación de la furgoneta y la deja en reposo estático usando el Animation Override Controller.
    /// </summary>
    public void StopVan()
    {
        if (hasStopped) return;
        hasStopped = true;

        if (idleOverrideController == null)
        {
            idleOverrideController = Resources.Load<RuntimeAnimatorController>("Van_Idle_Override");
        }

        if (allAnimators == null || allAnimators.Length == 0)
        {
            allAnimators = GetComponents<Animator>();
        }

        foreach (var anim in allAnimators)
        {
            if (anim != null)
            {
                if (idleOverrideController != null)
                {
                    anim.runtimeAnimatorController = idleOverrideController;
                }
                anim.enabled = true;
                anim.Play(0, 0, 0f);
                anim.Update(0f);
                anim.speed = 0f;
            }
        }

        // Si la furgoneta tenía sonido de motor en bucle, apagarlo
        if (audioSource != null && audioSource.isPlaying && audioSource.loop)
        {
            audioSource.Stop();
        }

        Debug.Log("[VanArrivalController] Van estacionada con éxito. Animation Override Controller aplicado (camioneta quieta).");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttachToSceneVans()
    {
        var animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
        foreach (var anim in animators)
        {
            if (anim == null) continue;
            string objName = anim.gameObject.name.ToLower();
            bool isVan = objName.Contains("van") || 
                         (anim.runtimeAnimatorController != null && anim.runtimeAnimatorController.name.ToLower().Contains("van"));
            if (isVan && anim.GetComponent<VanArrivalController>() == null)
            {
                anim.gameObject.AddComponent<VanArrivalController>();
            }
        }
    }
}

