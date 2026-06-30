using UnityEngine;

public class Duende_animaciones : MonoBehaviour
{
    [Header("Nombres de estados en el Animator")]
    public string baseIdleState = "idle0";          // tu estado naranja
    public string[] variantStates = { "Idle1", "Idle2", "Idle3" };

    [Header("Tiempos")]
    public float minIdleTime = 2f;   // tiempo mínimo quieto en idle0
    public float maxIdleTime = 4f;   // tiempo máximo quieto en idle0

    private Animator anim;
    private int variantIndex = 0;    // 0 -> Idle1, 1 -> Idle2, 2 -> Idle3
    private float timer;
    private bool playingVariant = false;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void OnEnable()
    {
        // Entrar en idle0
        anim.Play(baseIdleState, 0, 0f);
        ResetIdleTimer();
    }

    void ResetIdleTimer()
    {
        timer = Random.Range(minIdleTime, maxIdleTime);
        playingVariant = false;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        if (!playingVariant)
        {
            // Pasamos de idle0 a la siguiente variante
            string nextState = variantStates[variantIndex];

            // Reproducir la animación variante
            anim.CrossFadeInFixedTime(nextState, 0.1f);

            // Calcular duración aproximada del clip y volver a idle0 después
        float clipLength = GetClipLength(nextState);
        float extra = 3.6f; // pequeño margen para ver el final
        timer = clipLength > 0f ? clipLength + extra : 3.6f;

            playingVariant = true;

            // Preparar la siguiente: 1 -> 2 -> 3 -> 1...
            variantIndex = (variantIndex + 1) % variantStates.Length;
        }
        else
        {
            // Terminó la variante, volvemos a idle0 y esperamos otra vez
            anim.CrossFadeInFixedTime(baseIdleState, 0.1f);
            ResetIdleTimer();
        }
    }

    float GetClipLength(string stateName)
    {
        var clips = anim.runtimeAnimatorController.animationClips;
        foreach (var c in clips)
        {
            if (c.name == stateName)
                return c.length;
        }
        return 0f;
    }
}
