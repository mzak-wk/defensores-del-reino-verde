using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MenuIntroUI : MonoBehaviour
{
    [Header("UI Roots")]
    public RectTransform content;      // Content (fondo + botones)
    public RectTransform logo;         // Logo

    [Header("Overlays")]
    public Image backdropBlack;        // Negro de fondo (cubre desde el frame 0)
    public Image flashImage;           // Blanco de flash (cortina)

    [Header("Duraciones")]
    public float backdropFadeOut = 0.25f;  // negro -> transparente
    public float contentMoveDuration = 2f; // content sube
    public float logoMoveDuration    = 2f; // logo baja
    public float flashIn  = 0.12f;         // blanco sube
    public float flashOut = 0.25f;         // blanco baja
    public float buttonsFadeDuration = 0.6f;
    public float buttonsStagger      = 0.1f;

    [Header("Offsets iniciales (UI px)")]
    public float contentStartYOffset = -400f; // empieza abajo
    public float logoStartYOffset    = +300f; // empieza arriba

    [Header("Curvas")]
    public AnimationCurve contentEase = AnimationCurve.EaseInOut(0,0,1,1);
    public AnimationCurve logoEase    = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Botones (empiezan desactivados)")]
    public GameObject[] buttonObjects;   // arrastra los 3 botones aquí

    [Header("Audio")]
    public AudioSource whoosh;           // SFX woosh
    public AudioSource musicaFondo;      // música (PlayOnAwake OFF)
    public float delayMusica = 0.6f;

    // internos
    Vector2 contentStart, contentEnd;
    Vector2 logoStart, logoEnd;

    void Start()
    {
        // Seguridad: logs si faltan refs
        if (!content || !logo || !flashImage || !backdropBlack)
            Debug.LogWarning("[MenuIntroUI] Faltan referencias. Revisa el inspector.");

        // Posiciones UI
        contentEnd = content.anchoredPosition;
        logoEnd    = logo.anchoredPosition;
        contentStart = contentEnd + new Vector2(0, contentStartYOffset);
        logoStart    = logoEnd    + new Vector2(0, logoStartYOffset);
        content.anchoredPosition = contentStart;
        logo.anchoredPosition    = logoStart;

        // Backdrop negro visible al 100%
        if (backdropBlack) backdropBlack.color = new Color(0,0,0,1);

        // Flash en 0
        if (flashImage) flashImage.color = new Color(1,1,1,0);

        // Botones desactivados por si acaso
        foreach (var b in buttonObjects)
            if (b && b.activeSelf) b.SetActive(false);

        StartCoroutine(IntroCR());
    }

    IEnumerator IntroCR()
    {
        // 0) Fundido de negro (para ocultar cualquier flash azul de la cámara)
        if (backdropBlack)
        {
            float t0 = 0f;
            while (t0 < backdropFadeOut)
            {
                t0 += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t0 / Mathf.Max(0.0001f, backdropFadeOut));
                backdropBlack.color = new Color(0,0,0,a);
                yield return null;
            }
            backdropBlack.color = new Color(0,0,0,0);
        }

        // 1) Mover Content (sube) y Logo (baja) en paralelo
        float t = 0f;
        float maxT = Mathf.Max(contentMoveDuration, logoMoveDuration);
        while (t < maxT)
        {
            t += Time.unscaledDeltaTime;
            float ct = Mathf.Clamp01(t / contentMoveDuration);
            float lt = Mathf.Clamp01(t / logoMoveDuration);
            content.anchoredPosition = Vector2.LerpUnclamped(contentStart, contentEnd, contentEase.Evaluate(ct));
            logo.anchoredPosition    = Vector2.LerpUnclamped(logoStart,    logoEnd,    logoEase.Evaluate(lt));
            yield return null;
        }

        // 2) WOOSH justo al iniciar la cortina blanca
        if (whoosh) whoosh.Play();

        // 3) Flash sube (cubre)
        if (flashImage)
        {
            float a = 0f;
            while (a < 1f)
            {
                a += Time.unscaledDeltaTime / Mathf.Max(0.0001f, flashIn);
                flashImage.color = new Color(1,1,1, Mathf.Clamp01(a));
                yield return null;
            }

            // 3.1) Con pantalla cubierta: activar botones y arrancar su fade
            for (int i = 0; i < buttonObjects.Length; i++)
            {
                var go = buttonObjects[i];
                if (!go) continue;

                // activar si estaba desactivado
                if (!go.activeSelf) go.SetActive(true);

                // asegurar CanvasGroup para el fade
                var cg = go.GetComponent<CanvasGroup>();
                if (!cg) cg = go.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;

                StartCoroutine(FadeButtonIn(cg, buttonsFadeDuration));
                yield return new WaitForSecondsRealtime(buttonsStagger);
            }

            // 3.2) Flash baja (revela los botones)
            while (a > 0f)
            {
                a -= Time.unscaledDeltaTime / Mathf.Max(0.0001f, flashOut);
                flashImage.color = new Color(1,1,1, Mathf.Clamp01(a));
                yield return null;
            }
            flashImage.color = new Color(1,1,1,0);
        }

        // 4) Lanza la música un poco después
        if (musicaFondo)
        {
            yield return new WaitForSecondsRealtime(delayMusica);
            musicaFondo.Play();
        }
    }

    IEnumerator FadeButtonIn(CanvasGroup cg, float dur)
    {
        float t = 0f;
        // pequeño “pop” opcional:
        // var tr = cg.transform; Vector3 s0 = Vector3.one * 0.92f, s1 = Vector3.one; tr.localScale = s0;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            cg.alpha = k;
            // tr.localScale = Vector3.Lerp(s0, s1, k);
            yield return null;
        }
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }
}
