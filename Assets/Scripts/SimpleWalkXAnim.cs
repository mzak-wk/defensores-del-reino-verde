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
    float timer;
    bool toggle;

    // Dirección elegida por A/D. 1=derecha, -1=izquierda
    float ultimaDirX = 1f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (!idleSprite) idleSprite = stepA;
        if (sr) sr.sprite = idleSprite;
    }

    void Update()
    {
        // Leer input crudo (mismas teclas que tu movimiento)
        float x = Input.GetAxisRaw("Horizontal"); // A/D
        float y = Input.GetAxisRaw("Vertical");   // W/S

        // ¿Se está pulsando algo de movimiento?
        bool hayMovimiento = Mathf.Abs(x) > inputThreshold || Mathf.Abs(y) > inputThreshold;

        // Solo A/D deciden la orientación. Si x ≠ 0, actualizamos ultimaDirX.
        if (Mathf.Abs(x) > inputThreshold)
            ultimaDirX = Mathf.Sign(x);

        // Flip según la última dirección elegida por A/D
        bool mirarIzquierda = ultimaDirX < 0f;
        sr.flipX = spriteMiraADerechaPorDefecto ? mirarIzquierda : !mirarIzquierda;

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
            sr.sprite = idleSprite ? idleSprite : stepA;
        }
    }
}
