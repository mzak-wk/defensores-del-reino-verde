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

    private int currentWave;
    private bool isCleared;
    private bool encounterStarted;
    private Collider2D zoneCollider;
    private Bounds bounds;

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

        maxWaves = Mathf.Max(1, maxWaves);
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

    public Vector2 GetRandomPointInZone()
    {
        if (zoneCollider == null) zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider == null) return transform.position;

        bounds = zoneCollider.bounds;
        float minX = bounds.min.x + spawnMarginX;
        float maxX = bounds.max.x - spawnMarginX;
        float minY = bounds.min.y + spawnMarginY;
        float maxY = bounds.max.y - spawnMarginY;

        if (minX >= maxX) { minX = bounds.center.x; maxX = bounds.center.x; }
        if (minY >= maxY) { minY = bounds.center.y; maxY = bounds.center.y; }

        for (int i = 0; i < 30; i++)
        {
            Vector2 point = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            if (zoneCollider.OverlapPoint(point)) return point;
        }

        return bounds.center;
    }

    public Vector2 GetRandomOffscreenPoint(Camera cam, float padding = 2f)
    {
        if (cam == null) return GetRandomPointInZone();

        float height = cam.orthographicSize * 2f;
        Bounds camBounds = new Bounds(cam.transform.position, new Vector3(height * cam.aspect, height, 0f));
        camBounds.Expand(new Vector3(padding * 2f, padding * 2f, 0f));

        for (int i = 0; i < 30; i++)
        {
            Vector2 point = GetRandomPointInZone();
            if (!camBounds.Contains(point)) return point;
        }

        return GetRandomPointInZone();
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
