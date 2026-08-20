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
    /// Calcula una posición de spawn única e independiente para un enemigo de la oleada.
    /// Soporta Spawn Anchors con dispersión radial y fallback a Bounds de la zona,
    /// validando distancia en memoria contra la oleada actual y detección de obstáculos mediante Physics2D.
    /// </summary>
    public Vector2 GetEnemySpawnPoint(int enemyIndexInWave, System.Collections.Generic.List<Vector2> waveAssignedPositions, Camera cam = null)
    {
        if (zoneCollider == null) zoneCollider = GetComponent<Collider2D>();
        if (metadata == null) metadata = GetComponent<ZoneMetadata>();

        // 1. Filtrar anchors válidos configurados en ZoneMetadata
        System.Collections.Generic.List<Transform> validAnchors = new System.Collections.Generic.List<Transform>();
        if (metadata != null && metadata.spawnAnchors != null)
        {
            for (int a = 0; a < metadata.spawnAnchors.Length; a++)
            {
                if (metadata.spawnAnchors[a] != null)
                {
                    validAnchors.Add(metadata.spawnAnchors[a]);
                }
            }
        }

        // ====================================================
        // MODO A: Spawn basado en Spawn Anchors con Dispersión
        // ====================================================
        if (validAnchors.Count > 0)
        {
            // Distribuir de forma balanceada o round-robin entre los anchors disponibles
            Transform anchor = validAnchors[enemyIndexInWave % validAnchors.Count];
            Vector2 anchorPos = anchor.position;

            for (int attempt = 0; attempt < 15; attempt++)
            {
                // Dispersión obligatoria radial alrededor del anchor
                float distance = Random.Range(anchorMinDispersion, anchorMaxDispersion);
                Vector2 offset = Random.insideUnitCircle.normalized * distance;
                Vector2 candidate = anchorPos + offset;

                // Confinamiento dentro de los bounds del collider de la zona
                if (zoneCollider != null)
                {
                    Bounds b = zoneCollider.bounds;
                    candidate.x = Mathf.Clamp(candidate.x, b.min.x + 0.5f, b.max.x - 0.5f);
                    candidate.y = Mathf.Clamp(candidate.y, b.min.y + 0.5f, b.max.y - 0.5f);
                }

                // Validación 1: Distancia mínima en memoria contra las posiciones ya asignadas en esta oleada
                if (!IsSeparatedFromWave(candidate, waveAssignedPositions))
                    continue;

                // Validación 2: Chequeo de colisión física (muros, obstáculos u otros enemigos)
                if (IsCollidingWithObstacle(candidate))
                    continue;

                waveAssignedPositions?.Add(candidate);
                return candidate;
            }

            // Fallback con jitter angular si se agotaron los intentos para evitar el mismo punto exacto
            float fallbackAngle = (enemyIndexInWave * 45f) * Mathf.Deg2Rad;
            Vector2 fallbackOffset = new Vector2(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle)) * anchorMinDispersion;
            Vector2 fallbackPoint = anchorPos + fallbackOffset;
            if (zoneCollider != null)
            {
                Bounds b = zoneCollider.bounds;
                fallbackPoint.x = Mathf.Clamp(fallbackPoint.x, b.min.x + 0.5f, b.max.x - 0.5f);
                fallbackPoint.y = Mathf.Clamp(fallbackPoint.y, b.min.y + 0.5f, b.max.y - 0.5f);
            }
            waveAssignedPositions?.Add(fallbackPoint);
            return fallbackPoint;
        }

        // ====================================================
        // MODO B: Spawn basado en Bounds del Collider de la Zona
        // ====================================================
        if (zoneCollider != null)
        {
            bounds = zoneCollider.bounds;
            float minX = bounds.min.x + spawnMarginX;
            float maxX = bounds.max.x - spawnMarginX;
            float minY = bounds.min.y + spawnMarginY;
            float maxY = bounds.max.y - spawnMarginY;

            if (minX >= maxX) { minX = bounds.center.x - 1f; maxX = bounds.center.x + 1f; }
            if (minY >= maxY) { minY = bounds.center.y - 1f; maxY = bounds.center.y + 1f; }

            Bounds camBounds = default;
            bool checkCamera = false;
            if (cam != null)
            {
                float height = cam.orthographicSize * 2f;
                camBounds = new Bounds(cam.transform.position, new Vector3(height * cam.aspect, height, 0f));
                camBounds.Expand(new Vector3(4f, 4f, 0f));
                checkCamera = true;
            }

            Vector2 bestCandidate = bounds.center;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                Vector2 candidate = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));

                // Preferir puntos fuera de la cámara si la cámara está activa
                if (checkCamera && camBounds.Contains(candidate))
                    continue;

                // Validación 1: Distancia en memoria contra la oleada
                if (!IsSeparatedFromWave(candidate, waveAssignedPositions))
                    continue;

                // Validación 2: Chequeo de colisión física
                if (IsCollidingWithObstacle(candidate))
                    continue;

                waveAssignedPositions?.Add(candidate);
                return candidate;
            }

            // Fallback en bounds con offset por índice
            float fallbackAngle = (enemyIndexInWave * 60f) * Mathf.Deg2Rad;
            Vector2 finalPoint = bestCandidate + new Vector2(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle)) * minEnemySeparation;
            waveAssignedPositions?.Add(finalPoint);
            return finalPoint;
        }

        Vector2 transformFallback = (Vector2)transform.position + Random.insideUnitCircle * minEnemySeparation;
        waveAssignedPositions?.Add(transformFallback);
        return transformFallback;
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
            return false;

        Collider2D hit = Physics2D.OverlapCircle(position, 0.6f, obstacleAndEnemyLayers);
        return hit != null;
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
