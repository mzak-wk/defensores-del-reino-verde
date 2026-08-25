using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HurtFlash : MonoBehaviour
{
    public BarraVida vida;                 // arrastra tu BarraVida (andrius_0)
    public float flashTime = 0.1f;
    public int flashes = 2;

    SpriteRenderer sr;
    Color original;
    Coroutine flashCoroutine;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        original = sr.color;
        if (!vida) vida = GetComponent<BarraVida>();
        if (!vida) vida = GetComponentInParent<BarraVida>();
    }

    void OnEnable()
    {
        if (vida) vida.OnDamaged += HandleDamaged;
    }

    void OnDisable()
    {
        if (vida) vida.OnDamaged -= HandleDamaged;
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        if (sr) sr.color = original;
    }

    private void HandleDamaged(float amount)
    {
        if (!gameObject.activeInHierarchy) return;
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashCR());
    }

    System.Collections.IEnumerator FlashCR()
    {
        for (int i = 0; i < flashes; i++)
        {
            if (sr) sr.color = new Color(1f, 0.25f, 0.25f); // rojo
            yield return new WaitForSeconds(flashTime);
            if (sr) sr.color = original;
            yield return new WaitForSeconds(flashTime);
        }
        flashCoroutine = null;
    }
}
