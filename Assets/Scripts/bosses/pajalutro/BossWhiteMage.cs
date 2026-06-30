using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Boss/WhiteMage")]
public class BossWhiteMage : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player; // si lo dejas vacío intentará buscar por tag "Player"

    [Header("Prefabs")]
    public GameObject telegraphMarkerPrefab; // sprite círculo rojo (sin collider)
    public GameObject impactPrefab;          // prefab del efecto de impacto (puede ser VFX + script ImpactArea)

    [Header("Parámetros del ataque")]
    public int markersPerWave = 6;
    public float spawnRadiusMin = 2f;    // distancia mínima desde el player donde pueden aparecer los marcadores
    public float spawnRadiusMax = 6f;    // distancia máxima desde el player
    public float telegraphDelay = 1.5f;  // tiempo desde que aparece marcador hasta que cae el rayo
    public float timeBetweenWaves = 3.0f;
    public float staggerBetweenMarkers = 0.12f; // opcional: tiempo entre instancias de marcadores
    public float impactDamage = 100f;
    public float impactRadius = 1.5f;
    public float impactDuration = 0.6f;

    [Header("Layers a dañar")]
    public LayerMask damageLayerMask = ~0; // por defecto todos; ajusta a capa Player/Enemies

    [Header("Control")]
    public bool autoStart = true;
    public bool attacking = false;

    Coroutine attackRoutine;

    void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        if (autoStart) StartAttacks();
    }

    public void StartAttacks()
    {
        if (attacking) return;
        attacking = true;
        attackRoutine = StartCoroutine(AttackLoop());
    }

    public void StopAttacks()
    {
        if (!attacking) return;
        attacking = false;
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = null;
    }

    IEnumerator AttackLoop()
    {
        // espera un poco antes del primer wave opcional
        yield return new WaitForSeconds(0.5f);

        while (attacking)
        {
            if (player == null)
            {
                yield return null;
                continue;
            }

            // Generar lista de posiciones cerca del player
            List<Vector2> positions = new List<Vector2>(markersPerWave);

            for (int i = 0; i < markersPerWave; i++)
            {
                Vector2 pos = RandomPointNearPlayer();
                positions.Add(pos);

                // instanciar telegraph visual
                if (telegraphMarkerPrefab != null)
                {
                    var mk = Instantiate(telegraphMarkerPrefab, pos, Quaternion.identity);
                    var tm = mk.GetComponent<TelegraphMarker>();
                    if (tm != null)
                    {
                        tm.SetLifetime(telegraphDelay + 0.2f); // se auto-destruye visualmente
                    }
                }

                if (staggerBetweenMarkers > 0f)
                    yield return new WaitForSeconds(staggerBetweenMarkers);
            }

            // esperar telegraphDelay
            float t = 0f;
            while (t < telegraphDelay)
            {
                t += Time.deltaTime;
                yield return null;
            }

            // Ejecutar impactos sobre las posiciones planificadas
            foreach (var pos in positions)
            {
                SpawnImpactAt(pos);
            }

            // esperar entre waves
            float wait = timeBetweenWaves;
            float elapsed = 0f;
            while (elapsed < wait)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    Vector2 RandomPointNearPlayer()
    {
        // genera un punto aleatorio en anillo alrededor del player (entre min y max)
        float r = Random.Range(spawnRadiusMin, spawnRadiusMax);
        float a = Random.Range(0f, Mathf.PI * 2f);
        Vector2 offset = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        return (Vector2)player.position + offset;
    }

    void SpawnImpactAt(Vector2 pos)
    {
        // 1) VFX/impact prefab
        if (impactPrefab != null)
        {
            GameObject go = Instantiate(impactPrefab, pos, Quaternion.identity);
            // si el prefab tiene ImpactArea, setear parámetros
            var ia = go.GetComponent<ImpactArea>();
            if (ia != null)
            {
                ia.damage = impactDamage;
                ia.radius = impactRadius;
                ia.duration = impactDuration;
                ia.damageLayerMask = damageLayerMask;
            }
        }
        else
        {
            // fallback: aplicar daño inmediatamente (sin VFX)
            ApplyDamageAt(pos, impactRadius, impactDamage);
        }
    }

    // utilidad para aplicar daño por código (cuando no haya prefab)
    void ApplyDamageAt(Vector2 pos, float radius, float dmg)
    {
        var hits = Physics2D.OverlapCircleAll(pos, radius, damageLayerMask);
        foreach (var col in hits)
        {
            if (col == null) continue;
            // Player life script: BarraVida.RecibirDanio(float)
            var bd = col.GetComponentInParent<BarraVida>();
            if (bd != null)
            {
                bd.RecibirDanio(dmg);
                continue;
            }
            // Enemy script: Enemy.TakeDamage(float)
            var en = col.GetComponentInParent<Enemy>();
            if (en != null)
            {
                en.TakeDamage(dmg);
                continue;
            }
        }
    }

    // Gizmo útil en escena:
    void OnDrawGizmosSelected()
    {
        if (player)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, spawnRadiusMin);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, spawnRadiusMax);
        }
    }
}
