using UnityEngine;

[System.Serializable]
public struct SpawnEntry
{
    public EnemyData enemyData;
    [Range(0f, 100f)] public float weight;
}

[RequireComponent(typeof(PolygonCollider2D))]
public class SpawnZone : MonoBehaviour
{
    [Header("Identificación")]
    public string zoneName = "Nueva Zona";
    public int zoneLevel = 1;

    [Header("Configuración de Spawning")]
    public SpawnEntry[] enemyTable;
    public int maxActiveEnemies = 10;
    public float spawnInterval = 2f;
    
    [Header("Comportamiento")]
    public float despawnDelay = 7f;
    public float leashDistance = 15f;

    [HideInInspector] public int activeEnemyCount = 0;
    
    private PolygonCollider2D polyCollider;
    private Bounds bounds;

    private void Awake()
    {
        polyCollider = GetComponent<PolygonCollider2D>();
        polyCollider.isTrigger = true;
        bounds = polyCollider.bounds;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && ZoneSpawner.Instance != null)
        {
            ZoneSpawner.Instance.OnPlayerEnteredZone(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && ZoneSpawner.Instance != null)
        {
            ZoneSpawner.Instance.OnPlayerExitedZone(this);
        }
    }

    public EnemyData GetRandomEnemyData()
    {
        if (enemyTable == null || enemyTable.Length == 0) return null;

        float totalWeight = 0f;
        foreach (var entry in enemyTable)
        {
            totalWeight += entry.weight;
        }

        float randomVal = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (var entry in enemyTable)
        {
            currentSum += entry.weight;
            if (randomVal <= currentSum)
            {
                return entry.enemyData;
            }
        }

        return enemyTable[0].enemyData; // fallback
    }

    public Vector2 GetRandomPointInZone()
    {
        if (polyCollider == null) return transform.position;

        int maxAttempts = 30;
        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            Vector2 point = new Vector2(x, y);

            if (polyCollider.OverlapPoint(point))
            {
                return point;
            }
        }

        return bounds.center; // Fallback
    }

    public Vector2 GetRandomOffscreenPoint(Camera cam, float padding = 2f)
    {
        if (cam == null) return GetRandomPointInZone();

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        Vector3 camCenter = cam.transform.position;
        Bounds camBounds = new Bounds(camCenter, new Vector3(width, height, 0f));
        camBounds.Expand(new Vector3(padding * 2f, padding * 2f, 0f));

        int maxAttempts = 30;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 point = GetRandomPointInZone();
            if (!camBounds.Contains(point))
            {
                return point;
            }
        }

        return GetRandomPointInZone(); // Fallback si no hay punto offscreen posible
    }

    private void OnDrawGizmos()
    {
        if (polyCollider == null) polyCollider = GetComponent<PolygonCollider2D>();
        if (polyCollider != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            
            // Draw polygon shape
            for (int i = 0; i < polyCollider.pathCount; i++)
            {
                Vector2[] path = polyCollider.GetPath(i);
                for (int j = 0; j < path.Length; j++)
                {
                    Vector2 p1 = transform.TransformPoint(path[j]);
                    Vector2 p2 = transform.TransformPoint(path[(j + 1) % path.Length]);
                    Gizmos.DrawLine(p1, p2);
                }
            }
        }
    }
}
