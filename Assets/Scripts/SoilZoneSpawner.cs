using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Genera objetivos de siembra dentro de la zona de combate activa en cada oleada.
/// </summary>
public class SoilZoneSpawner : MonoBehaviour
{
    private const int AbsoluteMaxZonesPerEncounter = 3;

    [Header("Prefab y presupuesto")]
    [SerializeField] private ErodedSoilZone erodedSoilPrefab;
    [Tooltip("Cantidad maxima total de zonas/árboles que pueden aparecer durante un encuentro (evita saturar la pantalla).")]
    [FormerlySerializedAs("maxZonesPerWave")]
    [SerializeField, Range(1, 4)] private int maxZonesPerEncounter = 3;

    [Header("Presion progresiva")]
    [Tooltip("Zonas sin sembrar que pueden coexistir como maximo en pantalla.")]
    [SerializeField, Min(1)] private int maxActiveZones = 2;
    [Tooltip("Tiempo de espera desde el inicio de oleada hasta la primera zona.")]
    [SerializeField, Min(0f)] private float initialSpawnDelay = 3f;
    [Tooltip("Tiempo en segundos entre apariciones de una nueva zona sin sembrar.")]
    [SerializeField, Min(0f)] private float spawnInterval = 12f;

    [Header("Ubicacion valida")]
    [SerializeField] private Camera gameplayCamera;
    [Tooltip("Capas solidas que impiden crear una zona de suelo.")]
    [SerializeField] private LayerMask blockingLayers;
    [SerializeField, Min(0f)] private float clearanceRadius = 2.2f;
    [Tooltip("Distancia minima a los bordes fisicos de la zona caminable.")]
    [SerializeField, Min(0f)] private float spawnMargin = 3.5f;
    [Tooltip("Distancia minima entre cualquier zona creada en este encuentro, incluso tras convertirse en arbol.")]
    [SerializeField, Min(0f)] private float minSeparationDistance = 3.5f;
    [Tooltip("Intentos maximos para encontrar una posicion libre antes de omitir esta aparicion.")]
    [SerializeField, Min(1)] private int maxSpawnAttempts = 40;
    [Tooltip("Margen en unidades de mundo que mantiene la zona completa dentro de la camara.")]
    [SerializeField, Min(0f)] private float viewportPadding = 0.5f;

    private readonly List<ErodedSoilZone> spawnedZones = new List<ErodedSoilZone>();
    private readonly List<Vector2> encounterSpawnPositions = new List<Vector2>();
    private SpawnZone ownerZone;
    private SpawnZone activeZone;
    private Collider2D activeZoneBounds;
    private float spawnTimer;
    private bool isSpawning;
    private int preparedWaveNumber = -1;
    private int zonesSpawnedThisWave;
    private int totalSpawnedInCurrentEncounter;
    private readonly List<int> zonesPerWave = new List<int>();

    private void Awake()
    {
        ownerZone = GetComponent<SpawnZone>();
        if (ownerZone == null)
        {
            Debug.LogError($"[{nameof(SoilZoneSpawner)}] Debe estar en el mismo GameObject que su {nameof(SpawnZone)}.", this);
            enabled = false;
            return;
        }

        // Si blockingLayers está vacío o solo tiene Wall, asegurar Default y Muros_Espectrales
        int defaultLayer = LayerMask.NameToLayer("Default");
        int wallLayer = LayerMask.NameToLayer("Wall");
        int spectralLayer = LayerMask.NameToLayer("Muros_Espectrales");
        if (defaultLayer >= 0) blockingLayers |= (1 << defaultLayer);
        if (wallLayer >= 0) blockingLayers |= (1 << wallLayer);
        if (spectralLayer >= 0) blockingLayers |= (1 << spectralLayer);
    }

    private void Start()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        SubscribeToWaveManager();
    }

    private void Update()
    {
        if (!isSpawning)
            return;

        if (activeZone != ownerZone || preparedWaveNumber != activeZone.CurrentWave)
        {
            CancelWaveSpawning();
            return;
        }

        PruneCompletedZones();

        if (CountActiveUnplantedZones() >= EffectiveMaxActiveZones)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;

        SpawnOneZone();
        spawnTimer = spawnInterval;
    }

    private void OnDisable()
    {
        UnsubscribeFromWaveManager();
    }

    private void OnEnable()
    {
        SubscribeToWaveManager();
    }

    private void SubscribeToWaveManager()
    {
        if (ZoneSpawner.Instance == null)
            return;

        ZoneSpawner.Instance.OnPlayerZoneChanged -= HandlePlayerZoneChanged;
        ZoneSpawner.Instance.OnPlayerZoneChanged += HandlePlayerZoneChanged;
        ZoneSpawner.Instance.OnWaveStarted -= HandleWaveStarted;
        ZoneSpawner.Instance.OnWaveStarted += HandleWaveStarted;
        ZoneSpawner.Instance.OnWaveEnded -= HandleWaveEnded;
        ZoneSpawner.Instance.OnWaveEnded += HandleWaveEnded;

        HandlePlayerZoneChanged(ZoneSpawner.Instance.GetPlayerZone());
    }

    private void UnsubscribeFromWaveManager()
    {
        if (ZoneSpawner.Instance == null)
            return;

        ZoneSpawner.Instance.OnPlayerZoneChanged -= HandlePlayerZoneChanged;
        ZoneSpawner.Instance.OnWaveStarted -= HandleWaveStarted;
        ZoneSpawner.Instance.OnWaveEnded -= HandleWaveEnded;
        UnsubscribeFromActiveZone();
    }

    private void HandlePlayerZoneChanged(SpawnZone newZone)
    {
        if (newZone == activeZone)
            return;

        CancelWaveSpawning();
        ClearUnplantedZones();
        UnsubscribeFromActiveZone();
        activeZone = newZone;
        activeZoneBounds = activeZone == ownerZone ? activeZone.GetComponent<Collider2D>() : null;

        if (activeZone == ownerZone)
        {
            ResetEncounterBudget();
            activeZone.OnEncounterCompleted += HandleEncounterCompleted;
        }
    }

    private void UnsubscribeFromActiveZone()
    {
        if (activeZone != null)
            activeZone.OnEncounterCompleted -= HandleEncounterCompleted;
    }

    private void HandleWaveStarted(SpawnZone zone, int waveNumber)
    {
        if (zone == ownerZone && zone == activeZone)
            SpawnZonesForCurrentWave(waveNumber);
    }

    private void HandleWaveEnded(SpawnZone zone, int waveNumber)
    {
        if (zone == ownerZone && zone == activeZone)
        {
            CancelWaveSpawning();
            ClearUnplantedZones();
        }
    }

    private void HandleEncounterCompleted(SpawnZone zone)
    {
        if (zone == ownerZone && zone == activeZone)
        {
            CancelWaveSpawning();
            ClearUnplantedZones();
        }
    }

    /// <summary>Puede invocarse desde un UnityEvent al comenzar una oleada.</summary>
    public void SpawnZonesForCurrentWave()
    {
        SpawnZonesForCurrentWave(activeZone != null ? activeZone.CurrentWave : 0);
    }

    private void SpawnZonesForCurrentWave(int waveNumber)
    {
        if (erodedSoilPrefab == null || activeZone != ownerZone || activeZone.IsBossZone || activeZone.IsCleared)
            return;

        if (waveNumber <= 0 || waveNumber == preparedWaveNumber)
            return;

        if (waveNumber == 1 || zonesPerWave.Count == 0)
            ResetEncounterBudget();

        CancelWaveSpawning();
        ClearUnplantedZones();
        zonesSpawnedThisWave = 0;
        preparedWaveNumber = waveNumber;
        isSpawning = true;
        spawnTimer = initialSpawnDelay;
    }

    private bool SpawnOneZone()
    {
        int activeUnplanted = CountActiveUnplantedZones();
        if (activeZone != ownerZone
            || preparedWaveNumber != activeZone.CurrentWave
            || activeUnplanted >= EffectiveMaxActiveZones
            || !isSpawning)
            return false;

        if (!TryGetValidSpawnPosition(out Vector2 position))
        {
            Debug.LogWarning($"[{nameof(SoilZoneSpawner)}] No se encontro una posicion valida para suelo erosionado en '{activeZone.name}'.", activeZone);
            return false;
        }

        ErodedSoilZone zone = Instantiate(erodedSoilPrefab, position, Quaternion.identity, transform);
        spawnedZones.Add(zone);
        encounterSpawnPositions.Add(position);
        zonesSpawnedThisWave++;
        totalSpawnedInCurrentEncounter++;
        Debug.Log($"[{nameof(SoilZoneSpawner)}] Zona creada en {position}. Activas: {CountActiveUnplantedZones()}/{EffectiveMaxActiveZones}. Total Encuentro: {totalSpawnedInCurrentEncounter}/{EncounterZoneLimit}", zone);
        return true;
    }

    private void ResetEncounterBudget()
    {
        totalSpawnedInCurrentEncounter = 0;
        zonesSpawnedThisWave = 0;
        zonesPerWave.Clear();
        encounterSpawnPositions.Clear();

        int waveCount = activeZone != null ? Mathf.Max(1, activeZone.MaxWaves) : 1;
        int baseBudget = EncounterZoneLimit / waveCount;
        int remaining = EncounterZoneLimit % waveCount;

        for (int i = 0; i < waveCount; i++)
            zonesPerWave.Add(baseBudget);

        // Reparte el resto sin sesgar siempre las primeras oleadas.
        List<int> waveIndexes = new List<int>(waveCount);
        for (int i = 0; i < waveCount; i++)
            waveIndexes.Add(i);

        for (int i = waveIndexes.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (waveIndexes[i], waveIndexes[swapIndex]) = (waveIndexes[swapIndex], waveIndexes[i]);
        }

        for (int i = 0; i < remaining; i++)
            zonesPerWave[waveIndexes[i]]++;
    }

    private int GetBudgetForCurrentWave()
    {
        if (activeZone == null)
            return 0;

        int waveIndex = activeZone.CurrentWave - 1;
        return waveIndex >= 0 && waveIndex < zonesPerWave.Count ? zonesPerWave[waveIndex] : 0;
    }

    private int EncounterZoneLimit => Mathf.Min(maxZonesPerEncounter, AbsoluteMaxZonesPerEncounter);
    private int EffectiveMaxActiveZones => Mathf.Min(maxActiveZones, 2);

    private void CancelWaveSpawning()
    {
        isSpawning = false;
        spawnTimer = 0f;
        zonesSpawnedThisWave = 0;
        preparedWaveNumber = -1;
    }

    /// <summary>Elimina solo las zonas de la oleada que todavia no fueron sembradas.</summary>
    public void ClearUnplantedZones()
    {
        CancelWaveSpawning();

        for (int i = spawnedZones.Count - 1; i >= 0; i--)
        {
            ErodedSoilZone soilZone = spawnedZones[i];
            if (soilZone == null || soilZone.IsCompleted)
            {
                spawnedZones.RemoveAt(i);
                continue;
            }

            Destroy(soilZone.gameObject);
            spawnedZones.RemoveAt(i);
        }
    }

    private void PruneCompletedZones()
    {
        for (int i = spawnedZones.Count - 1; i >= 0; i--)
        {
            ErodedSoilZone soilZone = spawnedZones[i];
            if (soilZone == null || soilZone.IsCompleted)
                spawnedZones.RemoveAt(i);
        }
    }

    private int CountActiveUnplantedZones()
    {
        int count = 0;
        foreach (ErodedSoilZone soilZone in spawnedZones)
        {
            if (soilZone != null && !soilZone.IsCompleted)
                count++;
        }

        return count;
    }

    private bool TryGetValidSpawnPosition(out Vector2 position)
    {
        position = default;
        if (activeZone != ownerZone || activeZoneBounds == null)
            return false;

        Bounds bounds = activeZoneBounds.bounds;
        float marginX = Mathf.Max(spawnMargin, ownerZone != null ? ownerZone.SpawnMarginX : 3.5f, 3.5f);
        float marginY = Mathf.Max(spawnMargin, ownerZone != null ? ownerZone.SpawnMarginY : 2.0f, 2.0f);

        float minX = bounds.min.x + marginX;
        float maxX = bounds.max.x - marginX;
        float minY = bounds.min.y + marginY;
        float maxY = bounds.max.y - marginY;

        // Si la zona es reducida y el margen excede las dimensiones, ajustar al centro con margen seguro
        if (minX >= maxX)
        {
            float halfSpan = Mathf.Max(0.5f, bounds.extents.x * 0.4f);
            minX = bounds.center.x - halfSpan;
            maxX = bounds.center.x + halfSpan;
        }
        if (minY >= maxY)
        {
            float halfSpan = Mathf.Max(0.5f, bounds.extents.y * 0.4f);
            minY = bounds.center.y - halfSpan;
            maxY = bounds.center.y + halfSpan;
        }

        // Construir la máscara de capas bloqueantes (Default, Wall, Muros_Espectrales + capas configuradas)
        int effectiveBlockingMask = blockingLayers.value;
        int defaultLayer = LayerMask.NameToLayer("Default");
        int wallLayer = LayerMask.NameToLayer("Wall");
        int spectralLayer = LayerMask.NameToLayer("Muros_Espectrales");
        if (defaultLayer >= 0) effectiveBlockingMask |= (1 << defaultLayer);
        if (wallLayer >= 0) effectiveBlockingMask |= (1 << wallLayer);
        if (spectralLayer >= 0) effectiveBlockingMask |= (1 << spectralLayer);

        float effectiveClearance = Mathf.Max(clearanceRadius, 2.0f);
        Vector2 fissureExtentBox = new Vector2(3.4f, 1.8f);

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 candidate = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));

            // 1. Verificar que el centro y los 4 extremos de la grieta estén ESTRICTAMENTE dentro de los límites de la zona activa
            if (!activeZoneBounds.OverlapPoint(candidate))
                continue;

            if (!activeZoneBounds.OverlapPoint(candidate + new Vector2(1.7f, 0f)) ||
                !activeZoneBounds.OverlapPoint(candidate - new Vector2(1.7f, 0f)) ||
                !activeZoneBounds.OverlapPoint(candidate + new Vector2(0f, 0.9f)) ||
                !activeZoneBounds.OverlapPoint(candidate - new Vector2(0f, 0.9f)))
            {
                continue;
            }

            // 2. Mantener la zona completamente visible dentro de la cámara
            if (!IsInsideCamera(candidate))
                continue;

            // 3. Comprobar que no colisione con NINGÚN obstáculo sólido (muros, rocas, árboles del escenario, límites)
            bool collisionFound = false;

            // Chequeo A: Caja de cobertura de la grieta
            Collider2D[] boxHits = Physics2D.OverlapBoxAll(candidate, fissureExtentBox, 0f, effectiveBlockingMask);
            for (int i = 0; i < boxHits.Length; i++)
            {
                Collider2D col = boxHits[i];
                if (col == null) continue;
                if (col.gameObject == activeZone.gameObject) continue;

                // Si es un trigger, solo bloquea si pertenece a otra zona erosionada o árbol plantado
                if (col.isTrigger)
                {
                    if (col.GetComponentInParent<ErodedSoilZone>() != null || col.GetComponentInParent<RestoredTreeInteraction>() != null)
                    {
                        collisionFound = true;
                        break;
                    }
                    continue;
                }

                // Ignorar colisiones de personajes dinámicos
                if (col.CompareTag("Player") || col.CompareTag("Enemy"))
                    continue;

                // Cualquier colisionador sólido ambiental/muro/árbol bloquea el spawn
                collisionFound = true;
                break;
            }

            if (collisionFound)
                continue;

            // Chequeo B: Radio de despeje circular
            Collider2D[] circleHits = Physics2D.OverlapCircleAll(candidate, effectiveClearance, effectiveBlockingMask);
            for (int i = 0; i < circleHits.Length; i++)
            {
                Collider2D col = circleHits[i];
                if (col == null) continue;
                if (col.gameObject == activeZone.gameObject) continue;

                if (col.isTrigger)
                {
                    if (col.GetComponentInParent<ErodedSoilZone>() != null || col.GetComponentInParent<RestoredTreeInteraction>() != null)
                    {
                        collisionFound = true;
                        break;
                    }
                    continue;
                }

                if (col.CompareTag("Player") || col.CompareTag("Enemy"))
                    continue;

                collisionFound = true;
                break;
            }

            if (collisionFound)
                continue;

            // 4. Distancia mínima respecto a otras zonas generadas en este encuentro
            if (IsTooCloseToEncounterZone(candidate))
                continue;

            position = candidate;
            return true;
        }

        return false;
    }

    private bool IsInsideCamera(Vector2 position)
    {
        if (gameplayCamera == null)
            return true;

        Vector3 center = gameplayCamera.WorldToViewportPoint(position);
        float halfHeight = gameplayCamera.orthographicSize;
        float halfWidth = halfHeight * gameplayCamera.aspect;
        float paddingX = (Mathf.Max(clearanceRadius, 2.0f) + viewportPadding) / Mathf.Max(halfWidth * 2f, 0.01f);
        float paddingY = (Mathf.Max(clearanceRadius, 1.2f) + viewportPadding) / Mathf.Max(halfHeight * 2f, 0.01f);

        return center.z > 0f
            && center.x >= paddingX && center.x <= 1f - paddingX
            && center.y >= paddingY && center.y <= 1f - paddingY;
    }

    private bool IsTooCloseToEncounterZone(Vector2 candidate)
    {
        float minimumDistanceSqr = minSeparationDistance * minSeparationDistance;

        foreach (Vector2 existingPosition in encounterSpawnPositions)
        {
            if ((existingPosition - candidate).sqrMagnitude < minimumDistanceSqr)
                return true;
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxZonesPerEncounter = Mathf.Clamp(maxZonesPerEncounter, 1, AbsoluteMaxZonesPerEncounter);
        maxActiveZones = Mathf.Max(1, maxActiveZones);
        initialSpawnDelay = Mathf.Max(0f, initialSpawnDelay);
        spawnInterval = Mathf.Max(0f, spawnInterval);
        clearanceRadius = Mathf.Max(0f, clearanceRadius);
        spawnMargin = Mathf.Max(0f, spawnMargin);
        minSeparationDistance = Mathf.Max(0f, minSeparationDistance);
        maxSpawnAttempts = Mathf.Max(1, maxSpawnAttempts);
        viewportPadding = Mathf.Max(0f, viewportPadding);
    }
#endif
}
