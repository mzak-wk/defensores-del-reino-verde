using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CoinPickup : MonoBehaviour
{
    public int amount = 1;
    public float lifetime = 0f; // 0 = infinito

    void Awake()
    {
        var c = GetComponent<Collider2D>(); c.isTrigger = true;
        var rb = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0f; rb.simulated = true;
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
        if (!other.CompareTag("Player")) return;
        if (GoldManager.Instance) GoldManager.Instance.AddGold(amount);
        Destroy(gameObject);
    }
}
