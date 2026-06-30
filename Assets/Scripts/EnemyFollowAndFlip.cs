using UnityEngine;

public class EnemyFlipByVelocity : MonoBehaviour
{
    public Rigidbody2D rb;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float vx = rb.linearVelocity.x;

        if (Mathf.Approximately(vx, 0f))
            return;

        Vector3 scale = transform.localScale;

        // sprite dibujado mirando a la IZQUIERDA
        if (vx > 0f)
            scale.x = Mathf.Abs(scale.x);      // derecha
        else if (vx < 0f)
            scale.x = -Mathf.Abs(scale.x);     // izquierda

        transform.localScale = scale;
    }
}
