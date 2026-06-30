using System.Collections;
using UnityEngine;

/// <summary>
/// Aura: telegraph -> cast -> área persistente (circle) que aplica daño (singleHit o ticks).
/// Uso: StartCoroutine(aura.Execute(originTransform));
/// </summary>
public class AuraAttack : MonoBehaviour
{
    [Header("Aura")]
    public float castTime = 1.0f;
    public float radius = 3f;
    public float damage = 30f;
    public bool tickMode = false;         // false = single hit on enter, true = tick repeatedly
    public float tickInterval = 0.5f;
    public float duration = 3f;

    [Header("Telegraph / Prefab")]
    public GameObject telegraphPrefab;    // optional
    public GameObject auraPrefab;         // optional: prefab with AttackCollider (recommended)

    public LayerMask hitMask;

    IEnumerator ShowTelegraph(Vector2 origin)
    {
        if (telegraphPrefab == null) yield break;

        GameObject t = Instantiate(telegraphPrefab, origin, Quaternion.identity);
        var mbs = t.GetComponents<MonoBehaviour>();
        foreach (var mb in mbs)
        {
            if (mb is ITelegraph tele)
            {
                tele.ConfigureForAura(origin, radius, castTime, Color.yellow);
                break;
            }
        }

        Destroy(t, castTime + 0.05f);
        yield break;
    }

    public IEnumerator Execute(Transform origin)
    {
        Vector2 o = origin.position;

        // telegraph
        if (telegraphPrefab != null)
            yield return StartCoroutine(ShowTelegraph(o));

        // cast
        yield return new WaitForSeconds(castTime);

        if (auraPrefab != null)
        {
            GameObject inst = Instantiate(auraPrefab, o, Quaternion.identity);
            // if auraPrefab uses a CircleCollider2D scaled to 1 unit, scale by radius*2
            inst.transform.localScale = Vector3.one * (radius * 2f);

            var ac = inst.GetComponent<AttackCollider>();
            if (ac != null)
            {
                ac.damage = damage;
                ac.targetMask = hitMask;
                ac.lifeTime = duration;
                ac.singleHit = !tickMode;
            }
            else
            {
                // fallback: destroy after duration
                Destroy(inst, duration + 0.1f);
            }

            yield break;
        }
        else
        {
            // fallback: use OverlapCircle ticks like ImpactArea
            if (!tickMode)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(o, radius, hitMask);
                for (int i = 0; i < hits.Length; i++) ApplyDamageToCollider(hits[i], damage);
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    Collider2D[] hits = Physics2D.OverlapCircleAll(o, radius, hitMask);
                    for (int i = 0; i < hits.Length; i++) ApplyDamageToCollider(hits[i], damage);
                    yield return new WaitForSeconds(tickInterval);
                    elapsed += tickInterval;
                }
            }
        }
    }

    void ApplyDamageToCollider(Collider2D c, float dmg)
    {
        if (c == null) return;
        var root = c.transform.root.gameObject;
        var barra = root.GetComponentInChildren<BarraVida>();
        if (barra != null)
        {
            barra.RecibirDanio(dmg);
            return;
        }
        root.SendMessage("RecibirDanio", dmg, SendMessageOptions.DontRequireReceiver);
        root.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
