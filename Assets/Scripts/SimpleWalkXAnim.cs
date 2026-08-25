using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SimpleWalkXAnim : MonoBehaviour
{
    [Header("Sprites (mirando a DERECHA)")]
    public Sprite idleSprite;   // si lo dejas vacío, usa stepA
    public Sprite stepA;        // frame 1 caminar
    public Sprite stepB;        // frame 2 caminar

    [Header("Tuning")]
    public float framesPerSecond = 6f;
    public float inputThreshold = 0.01f;

    [Header("Dirección base")]
    [Tooltip("Marca true si tu sprite original mira a la DERECHA. Si mira a la IZQUIERDA, desmárcalo.")]
    public bool spriteMiraADerechaPorDefecto = true;

    SpriteRenderer sr;
    Animator anim;
    float timer;
    bool toggle;

    // Dirección elegida por A/D. 1=derecha, -1=izquierda
    float ultimaDirX = 1f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (!idleSprite) idleSprite = stepA;
        if (sr)
        {
            // Solo establecer sprite por defecto si no hay Animator activo
            if (sr.sprite == null && (anim == null || anim.runtimeAnimatorController == null))
            {
                sr.sprite = idleSprite;
            }

            // Leer la orientación inicial por defecto del sprite/prefab
            bool initialFlip = sr.flipX;
            if (spriteMiraADerechaPorDefecto)
            {
                ultimaDirX = initialFlip ? -1f : 1f;
            }
            else
            {
                ultimaDirX = initialFlip ? 1f : -1f;
            }
        }
    }

    void Update()
    {
        // Si el objeto tiene un Animator activo controlando la animación, este script no debe sobreescribir sr.sprite
        bool hasActiveAnimator = anim != null && anim.enabled && anim.runtimeAnimatorController != null;

        // Leer input crudo (mismas teclas que tu movimiento)
        float x = Input.GetAxisRaw("Horizontal"); // A/D
        float y = Input.GetAxisRaw("Vertical");   // W/S

        // ¿Se está pulsando algo de movimiento?
        bool hayMovimiento = Mathf.Abs(x) > inputThreshold || Mathf.Abs(y) > inputThreshold;

        // Solo A/D deciden la orientación. Si x ≠ 0, actualizamos ultimaDirX y aplicamos flip.
        if (Mathf.Abs(x) > inputThreshold)
        {
            ultimaDirX = Mathf.Sign(x);
            bool mirarIzquierda = ultimaDirX < 0f;
            if (sr != null)
            {
                sr.flipX = spriteMiraADerechaPorDefecto ? mirarIzquierda : !mirarIzquierda;
            }
        }

        // Si hay un Animator controlando el sprite, no forzar frames manuales
        if (hasActiveAnimator)
            return;

        if (hayMovimiento && (stepA || stepB))
        {
            // Alternar A/B constantemente mientras haya movimiento (ya sea por X o por Y)
            timer += Time.deltaTime;
            float frameTime = 1f / Mathf.Max(1f, framesPerSecond);
            if (timer >= frameTime)
            {
                timer = 0f;
                toggle = !toggle;
                sr.sprite = toggle ? stepA : stepB;
            }
        }
        else
        {
            // Sin ninguna tecla de movimiento: idle (manteniendo la orientación)
            toggle = false;
            timer = 0f;
            if (idleSprite || stepA)
            {
                sr.sprite = idleSprite ? idleSprite : stepA;
            }
        }
    }
}
