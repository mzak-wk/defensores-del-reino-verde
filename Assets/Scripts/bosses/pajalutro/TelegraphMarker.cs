using UnityEngine;

public class TelegraphMarker : MonoBehaviour
{
    public float pulseSpeed = 3f;
    public float pulseAmount = 0.1f; // Reduje un poco la intensidad
    public float lifetime = 1.5f;
    
    SpriteRenderer sr;
    Vector3 baseScale; // Guardaremos la escala original aquí

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // Guardamos la escala que le asignó el script ConeSpecialAttack
        baseScale = transform.localScale; 
        
        // Destrucción automática si no se llama externamente
        Destroy(gameObject, lifetime);
    }

    public void SetLifetime(float t)
    {
        lifetime = t;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (sr == null) return;
        
        // Calculamos el factor de pulso (ej: va de 0.9 a 1.1)
        float pulseFactor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        
        // Multiplicamos la escala base por el pulso
        transform.localScale = baseScale * pulseFactor;
    }
}