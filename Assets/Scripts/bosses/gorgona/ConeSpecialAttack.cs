using System.Collections;
using UnityEngine;

public class ConeSpecialAttack : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject telegraphPrefab;   // SOLO el aviso rojo

    [Header("Parámetros Visuales")]
    public float castTime = 2.5f;
    public float coneDuration = 2f;
    public float coneDistance = 6f;
    public float spawnOffset = 1.5f;

    [Header("Daño")]
    public float damage = 60f;
    public LayerMask damageLayerMask;

    public IEnumerator Execute(Transform origin, Transform player, Vector3 bossScale)
    {
        if (telegraphPrefab == null || player == null)
        {
            Debug.LogWarning("[ConeSpecialAttack] Faltan referencias.");
            yield break;
        }

        // --- OPCIONAL: ocultar mi sprite mientras se telegrapea ---
        var myRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in myRenderers) sr.enabled = false;

        // --- 1. CÁLCULO POSICIÓN / ROTACIÓN ---
        Vector2 dir = (player.position - origin.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        Vector3 spawnPos = origin.position + (Vector3)(dir * spawnOffset);

        // --- 2. TELEGRAPH ---
        GameObject tele = Instantiate(telegraphPrefab, spawnPos, rotation);

        var resizer = tele.GetComponent<TelegraphResizer>();
        if (resizer != null)
            resizer.SetRadius(coneDistance);
        else
            tele.transform.localScale = new Vector3(coneDistance, coneDistance, 1f);

        yield return new WaitForSeconds(castTime);

        if (tele) Destroy(tele);

        // --- 3. CONFIGURAR ESTE MISMO OBJETO COMO CONO ---
        transform.position = spawnPos;
        transform.rotation = rotation;
        transform.localScale = new Vector3(coneDistance, coneDistance, 1f);

        foreach (var sr in myRenderers) sr.enabled = true;

        SetupImpactArea(gameObject);

        // --- 4. VIDA DEL ATAQUE ---
        yield return new WaitForSeconds(coneDuration);

        Destroy(gameObject);
    }

    void SetupImpactArea(GameObject coneObj)
    {
        var col = coneObj.GetComponent<Collider2D>();
        if (col == null)
        {
            var c = coneObj.AddComponent<PolygonCollider2D>();
            c.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }

        var area = coneObj.GetComponent<ImpactArea>();
        if (area == null)
        {
            if (coneObj.GetComponent<Rigidbody2D>() == null)
            {
                var rb = coneObj.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            area = coneObj.AddComponent<ImpactArea>();
        }

        area.duration = coneDuration;
        area.damage = damage;
        area.singleHit = false;
        area.tickInterval = 0.5f;
        if (damageLayerMask.value != 0) area.damageLayerMask = damageLayerMask;
    }
}
