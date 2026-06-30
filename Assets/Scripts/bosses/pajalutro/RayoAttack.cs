using System.Collections;
using UnityEngine;

/// <summary>
/// Rayo: telegraph -> cast -> daño instantáneo en una franja (box) alineada con dirección.
/// Uso: StartCoroutine(rayo.Execute(originTransform, targetPosition));
/// </summary>
public class RayoAttack : MonoBehaviour
{
    [Header("Rayo")]
    public float castTime = 1.2f;
    public float length = 20f;
    public float width = 1.2f;
    public float damage = 40f;
    public LayerMask hitMask; // asigna la capa Player en inspector

    [Header("Telegraph / VFX")]
    public GameObject telegraphPrefab; // opcional; debe implementar ITelegraph o TelegraphSimple
    public GameObject beamPrefab;      // opcional: prefab visual del rayo que contenga AttackCollider o Collider2D

    [Header("Beam fallback")]
    public float beamLifeTime = 0.5f;  // cuánto dura el beam visual si se instancia

    IEnumerator ShowTelegraph(Vector2 origin, Vector2 dir)
    {
        if (telegraphPrefab == null) yield break;

        GameObject t = Instantiate(telegraphPrefab, origin, Quaternion.identity);
        var mbs = t.GetComponents<MonoBehaviour>();
        foreach (var mb in mbs)
        {
            if (mb is ITelegraph tele)
            {
                tele.ConfigureForRay(origin, dir, length, width, castTime, Color.red);
                break;
            }
        }

        Destroy(t, castTime + 0.1f);
        yield break;
    }

    // public API: Ejecutar la habilidad
    public IEnumerator Execute(Transform origin, Vector2 targetPos)
    {
        Vector2 o = origin.position;
        Vector2 dir = (targetPos - o).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        // 1) mostrar telegraph
        if (telegraphPrefab != null)
        {
            yield return StartCoroutine(ShowTelegraph(o, dir));
        }

        // 2) esperar casteo
        yield return new WaitForSeconds(castTime);

        // 3) aplicar daño: preferimos instanciar beamPrefab que tenga AttackCollider, si no, OverlapBoxAll
        Vector2 center = o + dir * (length * 0.5f);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Vector2 size = new Vector2(length, width);

        if (beamPrefab != null)
        {
            GameObject beam = Instantiate(beamPrefab, center, Quaternion.Euler(0f, 0f, angle));
            // ajustar escala para que el sprite/colider cubran length x width
            beam.transform.localScale = new Vector3(length, width, 1f);

            var ac = beam.GetComponent<AttackCollider>();
            if (ac != null)
            {
                ac.damage = damage;
                ac.targetMask = hitMask;
                ac.lifeTime = beamLifeTime;
                ac.singleHit = true;
            }
            else
            {
                // si no hay AttackCollider, intentamos ajustar collider si existe
                var col = beam.GetComponent<Collider2D>();
                if (col != null) col.isTrigger = true;
            }

            Destroy(beam, beamLifeTime + 0.1f);
        }
        else
        {
            // fallback: OverlapBoxAll
            var hits = Physics2D.OverlapBoxAll(center, size, angle, hitMask);
            for (int i = 0; i < hits.Length; i++)
            {
                ApplyDamageToCollider(hits[i], damage);
            }
        }
    }

    void ApplyDamageToCollider(Collider2D c, float dmg)
    {
        if (c == null) return;
        var root = c.transform.root.gameObject;

        // 1) BarraVida en root o hijos
        var barra = root.GetComponentInChildren<BarraVida>();
        if (barra != null)
        {
            barra.RecibirDanio(dmg);
            return;
        }

        // 2) fallback SendMessage
        root.SendMessage("RecibirDanio", dmg, SendMessageOptions.DontRequireReceiver);
        root.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);
    }

#if UNITY_EDITOR
    // gizmo para depuración (opcional)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 o = transform.position;
        Vector3 dir = transform.right;
        Vector3 center = o + dir * (length * 0.5f);
        Matrix4x4 rot = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg), new Vector3(length, width, 1f));
        Gizmos.matrix = rot;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
#endif
}
