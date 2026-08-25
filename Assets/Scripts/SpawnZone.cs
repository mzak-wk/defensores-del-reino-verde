using UnityEngine;

[System.Serializable]
public struct SpawnEntry
{
    public EnemyData enemyData;
    [Range(0f, 100f)] public float weight;
}

public class SpawnZone : MonoBehaviour
{
    [Header("Identificacion")]
    public string zoneName = "Nueva Zona";
    public int zoneLevel = 1;

    [Header("Configuracion de Spawning")]
    [Tooltip("Zona de boss: no genera oleadas comunes ni barreras; la orquesta BossEncounterManager.")]
    [SerializeField] private bool isBossZone;
    public SpawnEntry[] enemyTable;
    public int maxActiveEnemies = 10;
    public float spawnInterval = 2f;

    [Header("Configuracion de Oleadas")]
    public int minEnemiesPerWave = 3;
    public int maxEnemiesPerWave = 6;
    public float waveInterval = 3f;
    public float firstWaveChaseDelay = 1.5f;
    [SerializeField] private int maxWaves = 4;

    [Header("Aumento de Stats por Oleada")]
    public float hpBonusPerWave = 5f;
    public float damageBonusPerWave = 5f;
    public int xpBonusPerWave = 5;
    public float speedBonusPerWave = 0.2f;

    [Header("Bloqueo de Transicion")]
    [SerializeField] private Collider2D cameraTriggerCollider;
    [SerializeField] private GameObject leftSpectralWall;
    [SerializeField] private GameObject rightSpectralWall;

    [Header("Márgenes de Aparición (Padding)")]
    [SerializeField] private float spawnMarginX = 3.5f;
    [SerializeField] private float spawnMarginY = 2.0f;

    [Header("Dispersión de Enemigos")]
    [Tooltip("Distancia mínima entre enemigos al spawnear. Evita apilamiento visual.")]
    [SerializeField] private float minEnemySeparation = 1.8f;

    [Tooltip("Radio de dispersión mínima alrededor de un Spawn Anchor.")]
    [SerializeField] private float anchorMinDispersion = 1.0f;

    [Tooltip("Radio de dispersión máxima alrededor de un Spawn Anchor.")]
    [SerializeField] private float anchorMaxDispersion = 2.5f;

    [Tooltip("LayerMask de obstáculos y enemigos para verificar solapamiento al spawnear.")]
    [SerializeField] private LayerMask obstacleAndEnemyLayers;

    [Header("Comportamiento")]
    public float despawnDelay = 7f;
    public float leashDistance = 15f;

    [HideInInspector] public int activeEnemyCount = 0;

    public int CurrentWave => currentWave;
    public int MaxWaves => maxWaves;
    public bool IsCleared => isCleared;
    public bool IsEncounterActive => encounterStarted && !isCleared;
    public bool IsBossZone => isBossZone;
    public event System.Action OnZoneCleared;
    public event System.Action<SpawnZone> OnEncounterCompleted;

    private int currentWave;
    private bool isCleared;
    private bool encounterStarted;
    private Collider2D zoneCollider;
    private Bounds bounds;
    private ZoneMetadata metadata;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
            bounds = zoneCollider.bounds;
        }
        else
        {
            Debug.LogWarning($"[SpawnZone] {name} no tiene un Collider2D adjunto.");
        }

        metadata = GetComponent<ZoneMetadata>();

        maxWaves = Mathf.Max(1, maxWaves);

        // Auto-inicializar la LayerMask si no fue configurada en el Inspector
        if (obstacleAndEnemyLayers.value == 0)
        {
            obstacleAndEnemyLayers = LayerMask.GetMask("Enemy", "Wall", "Default");
            if (obstacleAndEnemyLayers.value == 0)
            {
                obstacleAndEnemyLayers = LayerMask.GetMask("Enemy");
            }
        }

        SetCombatBarriers(false);
        SetCameraTransitionEnabled(true);
        OnZoneCleared?.Invoke();

        // Zona de jefe (EndZone): queda despejada por diseño y sin barreras; no
        // procesa oleadas comunes (la batalla la orquesta BossEncounterManager).
        if (isBossZone || GetComponent<ZoneMetadata>()?.zoneType == ZoneType.End)
        {
            isBossZone = true;
            isCleared = true;
            encounterStarted = false;
            SetCombatBarriers(false);
            SetCameraTransitionEnabled(true);
        }
    }

    public bool BeginEncounter()
    {
        if (isCleared || encounterStarted)
        {
            return false;
        }

        encounterStarted = true;
        SetCameraTransitionEnabled(false);
        SetCombatBarriers(true);
        return true;
    }

    public bool BeginNextWave()
    {
        if (!IsEncounterActive || activeEnemyCount > 0 || currentWave >= maxWaves)
        {
            return false;
        }

        currentWave++;
        return true;
    }

    public void RegisterEnemySpawned()
    {
        activeEnemyCount++;
    }

    public void RegisterEnemyRemoved()
    {
        activeEnemyCount = Mathf.Max(0, activeEnemyCount - 1);
    }

    public void CompleteEncounter()
    {
        if (isCleared)
        {
            return;
        }

        isCleared = true;
        encounterStarted = false;
        activeEnemyCount = 0;
        SetCombatBarriers(false);
        SetCameraTransitionEnabled(true);
        OnEncounterCompleted?.Invoke(this);
    }

    /// <summary>
    /// Red de seguridad para cerrar la zona de forma forzada y segura desde
    /// cualquier director (ej. BossEncounterManager). Idempotente: si la zona
    /// ya está despejada no hace nada. Desactiva las Paredes Espectrales y
    /// re-habilita la transición de cámara aunque el contador esté desincronizado.
    /// </summary>
    public void ForceComplete()
    {
        CompleteEncounter();
    }

    public EnemyData GetRandomEnemyData()
    {
        if (enemyTable == null || enemyTable.Length == 0) return null;

        float totalWeight = 0f;
        foreach (SpawnEntry entry in enemyTable)
        {
            totalWeight += entry.weight;
        }

        if (totalWeight <= 0f) return null;

        float randomVal = Random.Range(0f, totalWeight);
        float currentSum = 0f;
        foreach (SpawnEntry entry in enemyTable)
        {
            currentSum += entry.weight;
            if (randomVal <= currentSum)
            {
                return entry.enemyData;
            }
        }

        return enemyTable[enemyTable.Length - 1].enemyData;
    }

    /// <summary>
    /// Calcula una posición de spawn única, aleatoria y bien distribuida para un enemigo de la oleada.
    /// Distribuye a los enemigos en diferentes sectores visibles de la pantalla, evitando que se apelotonen,
    /// validando que no spawneen dentro de obstáculos con colisión, fuera de la cámara ni sobre las barreras de transición de nivel.
    /// </summary>
    public Vector2 GetEnemySpawnPoint(int enemyIndexInWave, System.Collections.Generic.List<Vector2> waveAssignedPositions, Camera cam = null)
    {
        if (zoneCollider == null) zoneCollider = GetComponent<Collider2D>();
        if (metadata == null) metadata = GetComponent<ZoneMetadata>();
        if (cam == null) cam = Camera.main;

        // 1. Obtener límites base del collider de la zona
        Bounds zBounds = (zoneCollider != null) ? zoneCollider.bounds : new Bounds(transform.position, new Vector3(22f, 16f, 0f));

        float minX = zBounds.min.x + spawnMarginX;
        float maxX = zBounds.max.x - spawnMarginX;
        float minY = zBounds.min.y + spawnMarginY;
        float maxY = zBounds.max.y - spawnMarginY;

        // 2. Confinar estrictamente al área visible de la cámara activa si está disponible
        if (cam != null)
        {
            float camHeight = cam.orthographicSize * 2f;
            float camWidth = camHeight * cam.aspect;
            Vector3 camPos = cam.transform.position;

            float camMarginX = Mathf.Min(spawnMarginX, camWidth * 0.12f);
            float camMarginY = Mathf.Min(spawnMarginY, camHeight * 0.12f);

            float camMinX = camPos.x - (camWidth * 0.5f) + camMarginX;
            float camMaxX = camPos.x + (camWidth * 0.5f) - camMarginX;
            float camMinY = camPos.y - (camHeight * 0.5f) + camMarginY;
            float camMaxY = camPos.y + (camHeight * 0.5f) - camMarginY;

            minX = Mathf.Max(minX, camMinX);
            maxX = Mathf.Min(maxX, camMaxX);
            minY = Mathf.Max(minY, camMinY);
            maxY = Mathf.Min(maxY, camMaxY);
        }

        // Red de seguridad si los márgenes colapsan
        if (minX >= maxX)
        {
            float midX = zBounds.center.x;
            minX = midX - 3.5f;
            maxX = midX + 3.5f;
        }
        if (minY >= maxY)
        {
            float midY = zBounds.center.y;
            minY = midY - 2.5f;
            maxY = midY + 2.5f;
        }

        float spanX = maxX - minX;
        float spanY = maxY - minY;

        // 3. Revisar si hay múltiples Spawn Anchors configurados
        System.Collections.Generic.List<Transform> validAnchors = new System.Collections.Generic.List<Transform>();
        if (metadata != null && metadata.spawnAnchors != null)
        {
            for (int a = 0; a < metadata.spawnAnchors.Length; a++)
            {
                if (metadata.spawnAnchors[a] != null) validAnchors.Add(metadata.spawnAnchors[a]);
            }
        }

        // 4. Búsqueda por Sectores Multidireccionales (Norte, Sur, Este, Oeste y Esquinas)
        // Esto garantiza que los enemigos provengan de diferentes direcciones en lugar de un solo grupo
        int preferredSector = (enemyIndexInWave * 3 + Random.Range(0, 3)) % 8;

        for (int attempt = 0; attempt < 35; attempt++)
        {
            Vector2 candidate;
            int currentSector = (preferredSector + attempt) % 8;

            if (validAnchors.Count > 1 && attempt < 10)
            {
                // Si hay varios anchors, repartirlos equitativamente
                Transform anchor = validAnchors[(enemyIndexInWave + attempt) % validAnchors.Count];
                float dist = Random.Range(anchorMinDispersion, anchorMaxDispersion + 1.0f);
                candidate = (Vector2)anchor.position + (Random.insideUnitCircle.normalized * dist);
            }
            else
            {
                // Generar posición según el sector de la pantalla
                candidate = GetPointInSector(currentSector, minX, maxX, minY, maxY, spanX, spanY);
            }

            // Confinar dentro de los límites visibles válidos
            candidate.x = Mathf.Clamp(candidate.x, minX, maxX);
            candidate.y = Mathf.Clamp(candidate.y, minY, maxY);

            // Validación A: Separación contra enemigos de la misma oleada (evitar apelotonamiento)
            if (!IsSeparatedFromWave(candidate, waveAssignedPositions))
                continue;

            // Validación B: Excluir obstáculos de cambio de nivel (Paredes Espectrales y Triggers)
            if (IsNearTransitionObstacle(candidate, 2.2f))
                continue;

            // Validación C: Chequeo de colisión física con obstáculos del mapa (árboles, rocas, muros)
            if (IsCollidingWithObstacle(candidate))
                continue;

            waveAssignedPositions?.Add(candidate);
            return candidate;
        }

        // 5. Fallback seguro: dispersión angular amplia respecto al centro visible
        Vector2 centerPoint = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        float fallbackAngle = ((enemyIndexInWave * 53f) + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
        float fallbackRadius = Mathf.Clamp(minEnemySeparation * 1.5f, 2.0f, Mathf.Min(spanX, spanY) * 0.45f);
        Vector2 fallbackCandidate = centerPoint + new Vector2(Mathf.Cos(fallbackAngle) * (spanX * 0.4f), Mathf.Sin(fallbackAngle) * (spanY * 0.4f));

        fallbackCandidate.x = Mathf.Clamp(fallbackCandidate.x, minX, maxX);
        fallbackCandidate.y = Mathf.Clamp(fallbackCandidate.y, minY, maxY);

        waveAssignedPositions?.Add(fallbackCandidate);
        return fallbackCandidate;
    }

    private Vector2 GetPointInSector(int sector, float minX, float maxX, float minY, float maxY, float spanX, float spanY)
    {
        float x, y;
        switch (sector)
        {
            case 0: // Oeste (Borde Izquierdo)
                x = Random.Range(minX, minX + spanX * 0.35f);
                y = Random.Range(minY, maxY);
                break;
            case 1: // Este (Borde Derecho)
                x = Random.Range(maxX - spanX * 0.35f, maxX);
                y = Random.Range(minY, maxY);
                break;
            case 2: // Norte (Borde Superior)
                x = Random.Range(minX, maxX);
                y = Random.Range(maxY - spanY * 0.35f, maxY);
                break;
            case 3: // Sur (Borde Inferior)
                x = Random.Range(minX, maxX);
                y = Random.Range(minY, minY + spanY * 0.35f);
                break;
            case 4: // Noroeste (Esquina Superior Izquierda)
                x = Random.Range(minX, minX + spanX * 0.40f);
                y = Random.Range(maxY - spanY * 0.40f, maxY);
                break;
            case 5: // Noreste (Esquina Superior Derecha)
                x = Random.Range(maxX - spanX * 0.40f, maxX);
                y = Random.Range(maxY - spanY * 0.40f, maxY);
                break;
            case 6: // Suroeste (Esquina Inferior Izquierda)
                x = Random.Range(minX, minX + spanX * 0.40f);
                y = Random.Range(minY, minY + spanY * 0.40f);
                break;
            case 7: // Sureste (Esquina Inferior Derecha)
            default:
                x = Random.Range(maxX - spanX * 0.40f, maxX);
                y = Random.Range(minY, minY + spanY * 0.40f);
                break;
        }
        return new Vector2(x, y);
    }

    private bool IsNearTransitionObstacle(Vector2 position, float margin = 2.2f)
    {
        if (leftSpectralWall != null)
        {
            if (Vector2.Distance(position, leftSpectralWall.transform.position) < margin) return true;
        }

        if (rightSpectralWall != null)
        {
            if (Vector2.Distance(position, rightSpectralWall.transform.position) < margin) return true;
        }

        if (cameraTriggerCollider != null)
        {
            if (cameraTriggerCollider.bounds.Contains(position) ||
                Vector2.Distance(position, cameraTriggerCollider.bounds.center) < margin)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSeparatedFromWave(Vector2 candidate, System.Collections.Generic.List<Vector2> waveAssignedPositions)
    {
        if (waveAssignedPositions == null || waveAssignedPositions.Count == 0 || minEnemySeparation <= 0f)
            return true;

        float minSqr = minEnemySeparation * minEnemySeparation;
        for (int i = 0; i < waveAssignedPositions.Count; i++)
        {
            if ((candidate - waveAssignedPositions[i]).sqrMagnitude < minSqr)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsCollidingWithObstacle(Vector2 position)
    {
        if (obstacleAndEnemyLayers.value == 0)
        {
            obstacleAndEnemyLayers = LayerMask.GetMask("Enemy", "Wall", "Obstacle", "Obstacles", "Default");
            if (obstacleAndEnemyLayers.value == 0)
            {
                obstacleAndEnemyLayers = LayerMask.GetMask("Enemy");
            }
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(position, 0.65f, obstacleAndEnemyLayers);
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            // Ignorar el propio trigger de la zona
            if (hit == zoneCollider) continue;
            // Ignorar triggers generales a menos que sean enemigos o muros
            if (hit.isTrigger && !hit.CompareTag("Enemy") && !hit.name.Contains("Wall")) continue;

            return true;
        }

        return false;
    }

    public Vector2 GetRandomPointInZone()
    {
        return GetEnemySpawnPoint(0, null, null);
    }

    public Vector2 GetRandomOffscreenPoint(Camera cam, float padding = 2f)
    {
        return GetEnemySpawnPoint(0, null, cam);
    }

    private void SetCameraTransitionEnabled(bool enabled)
    {
        if (cameraTriggerCollider != null)
        {
            cameraTriggerCollider.enabled = enabled;
        }
    }

    public void SetCombatBarriers(bool enabled)
    {
        if (leftSpectralWall != null) leftSpectralWall.SetActive(enabled);
        if (rightSpectralWall != null) rightSpectralWall.SetActive(enabled);
    }

    private void OnDrawGizmos()
    {
        Collider2D collider = zoneCollider != null ? zoneCollider : GetComponent<Collider2D>();
        if (collider == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
    }
}
