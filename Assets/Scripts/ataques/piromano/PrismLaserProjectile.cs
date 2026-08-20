using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PrismLaserProjectile : MonoBehaviour
{
    [Header("Prism Visuals")]
    [SerializeField] private Color[] prismColors = new Color[]
    {
        new Color(1f, 0.2f, 0.2f),
        new Color(0.2f, 1f, 0.3f),
        new Color(0.2f, 0.5f, 1f),
        new Color(0.7f, 0.2f, 1f),
        new Color(1f, 0.6f, 0.1f),
        new Color(0.1f, 0.9f, 0.9f)
    };
    [SerializeField] private float baseLineWidth = 0.3f;
    [SerializeField] private int holdFrames = 2;

    private LineRenderer lineRenderer;
    private WeaponData weaponData;
    private BaseStats scaledStats;
    private Vector2 fireDirection;
    private Vector3 endPoint;
    private float laserWidth;
    private float laserLifetime;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = false;

    }

    public void Init(WeaponData weaponData, BaseStats scaledStats, Vector2 fireDirection)
    {
        this.weaponData = weaponData;
        this.scaledStats = scaledStats;
        this.fireDirection = fireDirection.normalized;

        laserWidth = baseLineWidth * scaledStats.areaSize;
        laserLifetime = Mathf.Min(scaledStats.projectileMaxLifetime, weaponData.BasicCooldown * 0.5f);

        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth;

        Color chosenColor = prismColors[Random.Range(0, prismColors.Length)];
        lineRenderer.startColor = chosenColor;
        lineRenderer.endColor = chosenColor;

        AcquireTarget();
        SetupVisuals();
        ExecuteHitscan();
        StartCoroutine(FadeAndDestroy());
    }

    private void AcquireTarget()
    {
        Vector2 origin = transform.position;
        float range = weaponData.AttackRange;

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range);
        Transform nearest = null;
        float minDistSq = float.MaxValue;

        foreach (var hit in hits)
        {
            Transform targetTransform = null;
            float dSq = float.MaxValue;

            BossHealth bossHealth = hit.GetComponent<BossHealth>() ?? hit.GetComponentInParent<BossHealth>();
            if (bossHealth != null)
            {
                targetTransform = bossHealth.transform;
                dSq = ((Vector2)targetTransform.position - origin).sqrMagnitude;
            }
            else if ((hit.GetComponent<BossAIController>() ?? hit.GetComponentInParent<BossAIController>()) is BossAIController bossAI)
            {
                targetTransform = bossAI.transform;
                dSq = ((Vector2)targetTransform.position - origin).sqrMagnitude;
            }
            else if ((hit.GetComponent<Enemy>() ?? hit.GetComponentInParent<Enemy>()) is Enemy enemy)
            {
                targetTransform = enemy.transform;
                dSq = ((Vector2)targetTransform.position - origin).sqrMagnitude;
            }

            // Asignar el objetivo más cercano
            if (targetTransform != null && dSq < minDistSq)
            {
                minDistSq = dSq;
                nearest = targetTransform;
            }
        }

        if (nearest != null)
        {
            endPoint = nearest.position;
        }
        else
        {
            endPoint = origin + (Vector2)fireDirection * range;
        }
    }

    private void SetupVisuals()
    {
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, endPoint);
        lineRenderer.enabled = true;
    }

    private void ExecuteHitscan()
    {
        Vector2 origin = transform.position;
        Vector2 direction = ((Vector2)endPoint - origin).normalized;
        float distance = Vector2.Distance(origin, endPoint);

        if (distance < 0.01f) return;

        Vector2 boxSize = new Vector2(laserWidth, laserWidth);
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, boxSize, 0f, direction, distance);

        foreach (var hit in hits)
        {
            BossHealth boss = hit.collider.GetComponent<BossHealth>() ?? hit.collider.GetComponentInParent<BossHealth>();
            if (boss != null)
            {
                boss.TakeDamage(scaledStats.damage);
                continue;
            }

            Enemy enemy = hit.collider.GetComponent<Enemy>() ?? hit.collider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(scaledStats.damage);
            }
        }
    }

    private IEnumerator FadeAndDestroy()
    {
        float holdDuration = holdFrames * Time.deltaTime;
        float fadeDuration = Mathf.Max(0f, laserLifetime - holdDuration);

        yield return new WaitForSeconds(holdDuration);

        if (fadeDuration <= 0f)
        {
            Destroy(gameObject);
            yield break;
        }

        float timer = 0f;
        Color startColor = lineRenderer.startColor;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (timer < fadeDuration)
        {
            float t = timer / fadeDuration;
            Color c = Color.Lerp(startColor, endColor, t);
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;
            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
