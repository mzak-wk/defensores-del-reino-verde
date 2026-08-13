using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    private Vector2 moveDirection;
    private float moveSpeed;
    private float damage;
    private int pierceRemaining;

    public void Initialize(Vector2 direction, float speed, float dmg, int pierceCount)
    {
        moveSpeed = speed > 0f ? speed : 10f;
        moveDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.right;
        damage = dmg;
        pierceRemaining = pierceCount;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.simulated = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        Destroy(gameObject, 4f);
    }

    private void Update()
    {
        transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        enemy.TakeDamage(damage);

        if (pierceRemaining <= 0)
            Destroy(gameObject);
        else
            pierceRemaining--;
    }
}
