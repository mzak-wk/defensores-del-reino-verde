using UnityEngine;

[System.Serializable]
public class SpriteWalkSet
{
    [Header("Sprites mirando a DERECHA (usa si NO usas Animator)")]
    public Sprite idle;          // opcional; si está vacío se usa stepA
    public Sprite stepA;         // frame 1
    public Sprite stepB;         // frame 2
    [Tooltip("Marca true si los sprites miran a la DERECHA. Si miran a IZQUIERDA, desmarca.")]
    public bool spriteMiraADerechaPorDefecto = true;

    [Header("Opcional: usar Animator en esta variante (en lugar de sprites)")]
    public bool useAnimatorVariant = false;
    [Tooltip("Asignar un RuntimeAnimatorController para usar esta variante con Animator.")]
    public RuntimeAnimatorController animatorController;
    [Tooltip("Nombre del parámetro bool que controla 'isWalking' en el Animator local (opcional). Si está vacío se usará el global).")]
    public string animatorBoolParameter = "";
    [Tooltip("Opcional: nombre del state para walk (si quieres forzar Play).")]
    public string walkStateName = "";
    [Tooltip("Opcional: nombre del state para idle (si quieres forzar Play).")]
    public string idleStateName = "";
}

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyWalkRandomAnim : MonoBehaviour
{
    [Header("Variantes (elige 1 al azar al habilitar)")]
    public SpriteWalkSet[] variantes;

    [Header("Animación por sprites (fallback)")]
    public float framesPerSecond = 6f;
    [Tooltip("Velocidad mínima (unidades/seg) para considerar que camina.")]
    public float velocityThreshold = 0.05f;

    [Header("Opcional - Animator global (si prefieres asignarlo manualmente)")]
    [Tooltip("Si arrastras un Animator aquí, el script lo usará cuando la variante lo requiera. Si está vacío, el script puede crear uno en runtime cuando haga falta.")]
    public Animator animator; // opcional: puedes asignar uno desde el prefab

    [Tooltip("Nombre por defecto del parámetro bool que controla si está caminando (se hace animator.SetBool(param,true/false) ).")]
    public string defaultAnimatorBoolParameter = "isWalking";

    [Tooltip("Si usas animators por variante sin parámetro bool, puedes especificar nombres de estado por variante (walkStateName / idleStateName).")]
    // (nota: variantes también permiten override del parameter y nombres de estado)

    // --- internos
    SpriteRenderer sr;
    SpriteWalkSet set;

    // sprite workflow
    float timer;
    bool toggle;
    float ultimaDirX = 1f;
    Vector2 lastPos;
    bool haveSample;

    // runtime animator de respaldo (si creamos uno)
    Animator runtimeAnimator = null;
    bool createdAnimator = false;
    bool lastMovementState = false;
    bool stateInitialized = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ElegirVarianteAlAzar();
        AplicarIdle();
    }

    void OnEnable()
    {
        ElegirVarianteAlAzar();
        AplicarIdle();
        haveSample = false;
        timer = 0f; toggle = false;
        stateInitialized = false;
        lastMovementState = false;
    }

    void ElegirVarianteAlAzar()
    {
        if (variantes == null || variantes.Length == 0) { set = null; return; }
        set = variantes[Random.Range(0, variantes.Length)];
        if (set != null && set.idle == null) set.idle = set.stepA;
        ApplyVariantAnimatorState(); // configurar animator/sprites según la variante elegida
    }

void AplicarIdle()
{
    if (!sr) return;

    if (UsingAnimatorForCurrentVariant())
    {
        var anim = GetActiveAnimator();

        if (anim != null)
        {
            // si usamos animator, dejamos que el Animator controle la visual
            var param = GetVariantAnimatorBoolParam();
            if (!string.IsNullOrEmpty(param) && AnimatorHasBool(anim, param))
            {
                anim.SetBool(param, false);
            }
            else
            {
                var st = GetVariantIdleStateName();
                if (!string.IsNullOrEmpty(st))
                    anim.Play(st);
            }
        }
    }
    else
    {
        var idle = (set != null && set.idle) ? set.idle : (set != null ? set.stepA : null);
        if (idle) sr.sprite = idle;
    }

    bool mirarIzq = (ultimaDirX < 0f);
    if (set != null)
        sr.flipX = set.spriteMiraADerechaPorDefecto ? mirarIzq : !mirarIzq;
}

    bool UsingAnimatorForCurrentVariant()
    {
        return set != null && set.useAnimatorVariant && set.animatorController != null;
    }

    Animator GetActiveAnimator()
    {
        if (animator != null) return animator;
        return runtimeAnimator;
    }

    // Cuando elegimos variante, preparar/activar o desactivar animator segun corresponda
    void ApplyVariantAnimatorState()
    {
        if (set == null) return;

        if (UsingAnimatorForCurrentVariant())
        {
            // asegurar que hay un Animator con el controller de la variante
            if (animator != null)
            {
                animator.runtimeAnimatorController = set.animatorController;
                // no destruimos el animator si es uno que asignaste manualmente
                createdAnimator = false;
                runtimeAnimator = null;
            }
            else
            {
                // si ya creamos uno previamente y es distinto, actualizamos controller
                if (runtimeAnimator == null)
                {
                    runtimeAnimator = gameObject.AddComponent<Animator>();
                    createdAnimator = true;
                }
                runtimeAnimator.runtimeAnimatorController = set.animatorController;
            }
        }
        else
        {
            // la variante no usa animator => si creamos uno en runtime lo destruimos / limpiamos controller
            if (createdAnimator && runtimeAnimator != null)
            {
                Destroy(runtimeAnimator);
                runtimeAnimator = null;
                createdAnimator = false;
            }
            else if (animator != null)
            {
                // si el Animator fue asignado en inspector, no lo destruimos pero limpiamos su controller si quieres
                // IMPORTANTE: NO limpiamos automáticamente el runtimeAnimatorController del animator público,
                // porque puede estar compartido por otros sistemas. Si quieres que lo limpie, descomenta:
                // animator.runtimeAnimatorController = null;
            }
        }
    }

    bool AnimatorHasBool(Animator anim, string paramName)
{
    if (anim == null || string.IsNullOrEmpty(paramName)) return false;
    foreach (var p in anim.parameters)
    {
        if (p.type == AnimatorControllerParameterType.Bool && p.name == paramName)
            return true;
    }
    return false;
}


    void Update()
    {
        if (!sr) return;
        if (set == null) return;

        // Medir velocidad por delta de posición SIEMPRE (independiente de Rigidbody)
        Vector2 pos = transform.position;
        if (!haveSample) { lastPos = pos; haveSample = true; }
        Vector2 delta = pos - lastPos;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.000001f);
        lastPos = pos;

        // Determinar orientación por delta.x (si hay)
        if (Mathf.Abs(delta.x) > 0.0001f)
            ultimaDirX = Mathf.Sign(delta.x);

        bool mirarIzq = (ultimaDirX < 0f);
        if (set != null)
            sr.flipX = set.spriteMiraADerechaPorDefecto ? mirarIzq : !mirarIzq;

        bool hayMovimiento = speed > velocityThreshold;

        if (UsingAnimatorForCurrentVariant())
        {
            // Animator workflow por variante
            var act = GetActiveAnimator();
            if (act == null)
            {
                // si no hay animator activo, caemos al sprite fallback
                HandleSpriteAnimation(hayMovimiento, speed);
            }
            else
            {
                // Preferimos usar el parámetro bool si existe el nombre
                string param = GetVariantAnimatorBoolParam();
                if (!string.IsNullOrEmpty(param) && AnimatorHasBool(act, param))
                {
                    bool current = act.GetBool(param);
                    if (current != hayMovimiento || !stateInitialized)
                    {
                        act.SetBool(param, hayMovimiento);

                        if (hayMovimiento && !string.IsNullOrEmpty(GetVariantWalkStateName()))
                            act.Play(GetVariantWalkStateName());
                        else if (!hayMovimiento && !string.IsNullOrEmpty(GetVariantIdleStateName()))
                            act.Play(GetVariantIdleStateName());

                        stateInitialized = true;
                    }
                }
                else
                {
                    // No hay parámetro o el Animator no lo tiene → solo llamar Play cuando cambia el estado
                    if (hayMovimiento != lastMovementState || !stateInitialized)
                    {
                        if (hayMovimiento && !string.IsNullOrEmpty(GetVariantWalkStateName()))
                            act.Play(GetVariantWalkStateName());
                        else if (!hayMovimiento && !string.IsNullOrEmpty(GetVariantIdleStateName()))
                            act.Play(GetVariantIdleStateName());

                        lastMovementState = hayMovimiento;
                        stateInitialized = true;
                    }
                }

            }
        }
        else
        {
            // Sprite-based workflow (original)
            HandleSpriteAnimation(hayMovimiento, speed);
        }
    }

    void HandleSpriteAnimation(bool hayMovimiento, float speed)
    {
        if (hayMovimiento)
        {
            timer += Time.deltaTime;
            float frameTime = 1f / Mathf.Max(1f, framesPerSecond);
            if (timer >= frameTime)
            {
                timer = 0f;
                toggle = !toggle;
                sr.sprite = toggle ? set.stepA : set.stepB;
            }
        }
        else
        {
            toggle = false; timer = 0f;
            sr.sprite = set.idle ? set.idle : set.stepA;
        }
    }

    string GetVariantAnimatorBoolParam()
    {
        if (set != null && !string.IsNullOrEmpty(set.animatorBoolParameter))
            return set.animatorBoolParameter;
        return defaultAnimatorBoolParameter;
    }

    string GetVariantWalkStateName()
    {
        if (set != null && !string.IsNullOrEmpty(set.walkStateName))
            return set.walkStateName;
        return "";
    }

    string GetVariantIdleStateName()
    {
        if (set != null && !string.IsNullOrEmpty(set.idleStateName))
            return set.idleStateName;
        return "";
    }

    // Forzar la variante (útil en editores y pruebas)
    public void ForceSetIndex(int i)
    {
        if (variantes == null || variantes.Length == 0) return;
        i = Mathf.Clamp(i, 0, variantes.Length - 1);
        set = variantes[i];
        if (set != null && set.idle == null) set.idle = set.stepA;
        ApplyVariantAnimatorState();
        AplicarIdle();
    }

    void OnDisable()
    {
        // limpiar Animator creado si existe (para no dejar componentes basura)
        if (createdAnimator && runtimeAnimator != null)
        {
            Destroy(runtimeAnimator);
            runtimeAnimator = null;
            createdAnimator = false;
        }
    }
}
