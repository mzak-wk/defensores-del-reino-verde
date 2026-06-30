using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ChickenPickup : MonoBehaviour
{
    [Header("Curación")]
    public float healAmount = 30f;

    [Header("Referencias")]
    [Tooltip("BarraVida real del jugador (puede estar en el Canvas). Si lo dejas vacío, se busca en escena.")]
    public BarraVida playerHealth;

    [Header("Vida útil")]
    public float lifetime = 0f; // 0 = infinito

    [Header("Detección")]
    public string playerTag = "Player";

    void Awake()
    {
        // Trigger + RB2D
        var c = GetComponent<Collider2D>(); c.isTrigger = true;
        var rb = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0f; rb.simulated = true;

        if (!playerHealth) playerHealth = FindAnyObjectByType<BarraVida>(); // busca la del Canvas si es única
    }

    void Update()
    {
        if (lifetime > 0f)
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f) Destroy(gameObject);
        }
    }

    void TryPickup()
    {
        if (!playerHealth) return;

        // Si NO estás al máximo → cura y se destruye.
        if (playerHealth.currentHP < playerHealth.maxHP)
        {
            playerHealth.Curar(healAmount);
            Destroy(gameObject);
        }
        // Si estás full, no hace nada (permanece en el suelo).
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        TryPickup();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        TryPickup(); // si estabas full y luego bajaste vida, se recogerá automáticamente
    }
}
