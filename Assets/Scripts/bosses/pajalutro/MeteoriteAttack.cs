using System.Collections;
using UnityEngine;

/// <summary>
/// Meteorito: lanza N meteoritos alrededor de targetPoint (o player). Cada meteorito:
/// - muestra telegraph en el punto
/// - espera castTime
/// - spawnea impactPrefab con AttackCollider (recomendado) o OverlapCircle como fallback
/// Uso: StartCoroutine(meteorite.Execute(originTransform, targetCenter));
/// </summary>
public class MeteoriteAttack : MonoBehaviour
{
    [Header("Meteorito")]
    public int count = 2;
    public float castTime = 1.5f;          // por meteorito (telegraph -> cast)
    public float intervalBetween = 2f;     // tiempo entre meteoritos
    public float landingRadius = 2f;       // radio alrededor del targetCenter donde caen
    public float damage = 80f;
    public float impactRadius = 2f;
    public LayerMask hitMask;

    [Header("Prefabs")]
    public GameObject telegraphPrefab;     // telegraph para el punto
    public GameObject impactPrefab;        // prefab que debe tener AttackCollider (recomendado)

    // Ejecuta la secuencia de meteoritos centrados en targetCenter
    public IEnumerator Execute(Transform origin, Vector2 targetCenter)
    {
        for (int i = 0; i < count; i++)
        {
            // escoger un punto aleatorio cerca del targetCenter
            Vector2 offset = Random.insideUnitCircle * landingRadius;
            Vector2 impactPos = targetCenter + offset;

            // show telegraph
            if (telegraphPrefab != null)
            {
                GameObject t = Instantiate(telegraphPrefab, impactPos, Quaternion.identity);
                var mbs = t.GetComponents<MonoBehaviour>();
                foreach (var mb in mbs)
                {
                    if (mb is ITelegraph tele)
                    {
                        tele.ConfigureForPoint(impactPos, impactRadius, castTime, Color.red);
                        break;
                    }
                }
                Destroy(t, castTime + 0.1f);
            }

            // wait cast time
            yield return new WaitForSeconds(castTime);

            // spawn impact (preferir impactPrefab con AttackCollider)
            if (impactPrefab != null)
            {
                GameObject imp = Instantiate(impactPrefab, impactPos, Quaternion.identity);
                // ajustar escala si necesario (si impacto usa CircleCollider de radio 1)
                imp.transform.localScale = Vector3.one * (impactRadius * 2f);
                var ac = imp.GetComponent<AttackCollider>();
                if (ac != null)
                {
                    ac.damage = damage;
                    ac.targetMask = hitMask;
                    ac.singleHit = true;
                    // sugerencia: lifeTime corto (ej: 1s)
                    ac.lifeTime = 1f;
                }
                else
                {
                    // si no hay AttackCollider, intentar aplicar OverlapCircle instant
                    var hits = Physics2D.OverlapCircleAll(impactPos, impactRadius, hitMask);
                    for (int j = 0; j < hits.Length; j++) ApplyDamageToCollider(hits[j], damage);
                    Destroy(imp, 1f);
                }
            }
            else
            {
                // fallback instant damage
                var hits = Physics2D.OverlapCircleAll(impactPos, impactRadius, hitMask);
                for (int j = 0; j < hits.Length; j++) ApplyDamageToCollider(hits[j], damage);
            }

            // esperar intervalo antes del siguiente meteorito (salvo el último)
            if (i < count - 1)
                yield return new WaitForSeconds(intervalBetween);
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
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, landingRadius);
    }
#endif
}
