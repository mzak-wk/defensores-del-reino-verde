using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class XPPickup : MonoBehaviour
{
    [Header("XP")]
    public int amount = 30;

    [Header("Vida útil")]
    public float lifetime = 20f; // 0 = infinito

    [Header("Detección")]
    public string playerTag = "Player"; // para no dar XP a todo lo que toque
    public ExperienceSystem xpSystem;   // referencia al ExpSystem central

    void Awake()
    {
        // Collider trigger
        var c2d = GetComponent<Collider2D>();
        c2d.isTrigger = true;

        // RB2D kinematic (para triggers y 'magnet')
        var rb = GetComponent<Rigidbody2D>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.gravityScale = 0f;

        // Si no te lo asignaron por Inspector, lo buscamos en escena
        if (!xpSystem) xpSystem = FindAnyObjectByType<ExperienceSystem>();
    }

    void Update()
    {
        if (lifetime > 0f)
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f) Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Dar XP solo si lo tocó el Player (por tag)
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;
        if (!xpSystem) return;

        xpSystem.AddXP(amount);
        Destroy(gameObject);
    }
}
