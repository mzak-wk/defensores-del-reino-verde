using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HurtFlash : MonoBehaviour
{
    public BarraVida vida;                 // arrastra tu BarraVida (andrius_0)
    public float flashTime = 0.1f;
    public int flashes = 2;

    SpriteRenderer sr;
    Color original;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        original = sr.color;
        if (!vida) vida = GetComponent<BarraVida>();
    }

    void OnEnable()
    {
        if (vida) vida.OnDamaged += _ => StartCoroutine(FlashCR());
    }
    void OnDisable()
    {
        if (vida) vida.OnDamaged -= _ => StartCoroutine(FlashCR());
    }

    System.Collections.IEnumerator FlashCR()
    {
        for (int i = 0; i < flashes; i++)
        {
            sr.color = new Color(1f, 0.25f, 0.25f); // rojo
            yield return new WaitForSeconds(flashTime);
            sr.color = original;
            yield return new WaitForSeconds(flashTime);
        }
    }
}
