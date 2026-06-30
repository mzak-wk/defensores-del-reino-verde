using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class TailAttack : MonoBehaviour
{
    [Header("Daño")]
    public float damage = 40f;
    public LayerMask damageLayerMask;   // pon aquí Player

    [Header("Tiempos")]
    public float castTime = 0.4f;       // “carga” del ataque
    public float hitDuration = 0.35f;   // ventana en la que hace daño

    [Header("Offset / posición relativa")]
    public Vector2 localOffset = new Vector2(-10f, 0f);
    public float extraRange = 10f;

    [Header("Orientación")]
    [Tooltip("Marca esto si el sprite original mira hacia la IZQUIERDA.")]
    public bool spriteFacesLeft = true;

    BoxCollider2D box;
    Rigidbody2D rb;
    SpriteRenderer sr;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        rb  = GetComponent<Rigidbody2D>();
        sr  = GetComponent<SpriteRenderer>();

        // Física básica
        if (box != null)
            box.isTrigger = true;            // da igual para OverlapBoxAll, pero ayuda

        if (rb != null)
        {
            rb.bodyType  = RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }
    }

    // ==== MÉTODO QUE LLAMA EL BOSS POR REFLEXIÓN ====
    // Debe existir EXACTAMENTE con esta firma
    public IEnumerator Execute(Transform origin, Transform player, Vector3 scale)
    {
        if (origin == null)
        {
            Debug.LogWarning("[TailAttack] origin nulo en Execute");
            yield break;
        }

        // 1) Decidir lado del jugador
        float dir = 1f;
        if (player != null)
            dir = (player.position.x >= origin.position.x) ? 1f : -1f;

        // 2) Orientar el sprite (teniendo en cuenta si el sprite original mira a la izquierda)
        float baseX = spriteFacesLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        float finalScaleX = baseX * dir;
        transform.localScale = new Vector3(finalScaleX, scale.y, scale.z);

        // 3) Colocar slash con offset
        Vector3 worldOffset = new Vector3((localOffset.x + extraRange) * dir, localOffset.y, 0f);
        transform.position = origin.position + worldOffset;

        // 4) Esperar el “cast”
        yield return new WaitForSeconds(castTime);

        // 5) Ventana de daño: usamos OverlapBoxAll cada frame
        float t = 0f;
        while (t < hitDuration)
        {
            if (box != null)
            {
                // Tamaño real del collider en mundo (ya incluye escala)
                Vector2 size = box.bounds.size;
                Vector2 center = box.bounds.center;

                Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, damageLayerMask);
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider2D c = hits[i];
                    if (c == null) continue;

                    GameObject root = c.transform.root.gameObject;
                    BarraVida vida = root.GetComponentInChildren<BarraVida>();
                    if (vida != null)
                    {
                        vida.RecibirDanio(damage);
                        // Debug opcional:
                        // Debug.Log("[TailAttack] Golpe a " + root.name);
                    }
                }
            }

            t += Time.deltaTime;
            yield return null;
        }

        // 6) Fin del ataque
        Destroy(gameObject);
    }

    // Ya NO necesitamos OnTriggerEnter2D ni corutinas extra
}
