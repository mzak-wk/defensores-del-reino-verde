using UnityEngine;

public class XPMagnet : MonoBehaviour
{
    [Header("Valores base")]
    public float baseRadius = 5f;
    public float basePullSpeed = 16f;

    [Header("Qué atraer")]
    public LayerMask attractMask;          // capa(s) de orbes/monedas/pollos

    [Header("Condiciones")]
    public BarraVida playerHealth;         // arrastra aquí tu BarraVida real (Canvas)
    public bool skipChickenWhenFull = true;

    private float radius;
    private float pullSpeed;
    private readonly Collider2D[] hitBuffer = new Collider2D[64];

    private ContactFilter2D contactFilter;

    void Awake()
    {
        contactFilter = new ContactFilter2D { useLayerMask = true, layerMask = attractMask };
        if (!playerHealth) playerHealth = FindAnyObjectByType<BarraVida>();
        RecomputeFromShop();
    }

    void OnEnable()
    {
        RecomputeFromShop();
    }

    void Update()
    {
        contactFilter.layerMask = attractMask;
        int count = Physics2D.OverlapCircle(transform.position, radius, contactFilter, hitBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D h = hitBuffer[i];
            if (h == null) continue;

            // Si es pollo y estás full vida → NO atraer
            var chicken = h.GetComponent<ChickenPickup>();
            if (skipChickenWhenFull && chicken && playerHealth && playerHealth.currentHP >= playerHealth.maxHP)
                continue;

            var rb = h.attachedRigidbody;
            if (!rb) continue;

            Vector2 dir = ((Vector2)transform.position - rb.position).normalized;
            rb.MovePosition(rb.position + dir * pullSpeed * Time.deltaTime);
        }
    }

    void RecomputeFromShop()
    {
        if (GameSession.Instance != null)
        {
            var gs = GameSession.Instance;
            radius    = baseRadius   + gs.GetMagnetRadiusBonus();
            pullSpeed = basePullSpeed + gs.GetMagnetPullBonus();
        }
        else
        {
            radius    = baseRadius;
            pullSpeed = basePullSpeed;
        }
    }

    void OnDrawGizmosSelected()
    {
        float r = (GameSession.Instance != null)
            ? baseRadius + GameSession.Instance.GetMagnetRadiusBonus()
            : baseRadius;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, r);
    }
}
