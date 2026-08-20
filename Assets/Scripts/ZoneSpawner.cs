using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneSpawner : MonoBehaviour
{
    public static ZoneSpawner Instance { get; private set; }

    [Header("Referencias")]
    public Transform player;
    public Camera mainCamera;

    private readonly HashSet<SpawnZone> allZones = new HashSet<SpawnZone>();
    private readonly Dictionary<SpawnZone, Coroutine> spawnCoroutines = new Dictionary<SpawnZone, Coroutine>();
    private SpawnZone playerCurrentZone;
    private bool isSpawningPaused;

    public event Action<SpawnZone> OnPlayerZoneChanged;
    public event Action<SpawnZone, int> OnWaveStarted;
    public event Action<SpawnZone, int> OnWaveEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }

        if (mainCamera == null) mainCamera = Camera.main;

        LevelLayoutGenerator.OnLayoutGenerated -= OnLayoutReady;
        LevelLayoutGenerator.OnLayoutGenerated += OnLayoutReady;
    }

    private void OnDestroy()
    {
        LevelLayoutGenerator.OnLayoutGenerated -= OnLayoutReady;
        if (Instance == this) Instance = null;
    }

    public void OnLayoutReady(List<ZoneInstance> zones)
    {
        allZones.Clear();
        foreach (ZoneInstance zoneInstance in zones)
        {
            SpawnZone spawnZone = zoneInstance.instanceGO.GetComponent<SpawnZone>();
            if (spawnZone != null) allZones.Add(spawnZone);
        }
    }

    public bool TryEnterZone(SpawnZone destination, ZoneCameraTrigger cameraTrigger)
    {
        if (destination == null) return false;
        if (destination == playerCurrentZone) return true;

        if (playerCurrentZone != null && !playerCurrentZone.IsCleared)
        {
            return false;
        }

        playerCurrentZone = destination;
        cameraTrigger?.ActivateCamera();
        OnPlayerZoneChanged?.Invoke(destination);

        // La EndZone / zona de boss NO procesa oleadas de enemigos comunes:
        // su orquestación (diálogo, spawn del jefe, barreras) la gestiona
        // BossEncounterManager. Se detecta por el flag IsBossZone, por
        // ZoneMetadata.ZoneType == End o por la presencia del manager.
        bool isBossZone = destination.IsBossZone
            || destination.GetComponent<ZoneMetadata>()?.zoneType == ZoneType.End
            || destination.GetComponentInChildren<BossEncounterManager>() != null;

        if (!destination.IsCleared && !isBossZone && destination.BeginEncounter())
        {
            StartZoneWaves(destination);
        }

        return true;
    }

    private void StartZoneWaves(SpawnZone zone)
    {
        if (spawnCoroutines.TryGetValue(zone, out Coroutine existing) && existing != null)
        {
            StopCoroutine(existing);
        }

        spawnCoroutines[zone] = StartCoroutine(SpawnWavesForZone(zone));
    }

    private IEnumerator SpawnWavesForZone(SpawnZone zone)
    {
        // Las zonas de jefe jamás generan oleadas comunes (ni LogError): el
        // combate lo orquesta BossEncounterManager.
        if (zone == null
            || zone.IsBossZone
            || zone.GetComponent<ZoneMetadata>()?.zoneType == ZoneType.End)
        {
            if (zone != null) spawnCoroutines.Remove(zone);
            Debug.Log($"[ZoneSpawner] {zone?.name ?? "Zona"} es de jefe; no se generan oleadas comunes.");
            yield break;
        }

        int failedWaveAttempts = 0;
        const int maxFailedWaveAttempts = 3;
        bool waveCompletionPending = false;
        int pendingWaveNumber = 0;

        while (zone != null && zone.IsEncounterActive)
        {
            while (isSpawningPaused || zone.activeEnemyCount > 0)
            {
                yield return null;
            }

            if (waveCompletionPending)
            {
                OnWaveEnded?.Invoke(zone, pendingWaveNumber);
                waveCompletionPending = false;
            }

            // Watchdog: si ya se lanzaron todas las oleadas y no quedan enemigos,
            // la zona DEBE cerrarse siempre. Algunas rutas (suppressDrops, pool)
            // podían dejar el contador pegado y bloquear las Paredes Espectrales.
            if (zone.CurrentWave >= zone.MaxWaves)
            {
                zone.CompleteEncounter();
                break;
            }

            if (zone.CurrentWave > 0 && zone.waveInterval > 0f)
            {
                yield return new WaitForSeconds(zone.waveInterval);
                if (isSpawningPaused || zone.activeEnemyCount > 0) continue;
            }

            if (!zone.BeginNextWave())
            {
                // Anti-bucle infinito: si BeginNextWave no puede avanzar de forma
                // repetida (estado desincronizado), forzamos el cierre en lugar de
                // quedarnos atascados con las barreras activas.
                failedWaveAttempts++;
                if (failedWaveAttempts >= maxFailedWaveAttempts)
                {
                    Debug.LogError($"[ZoneSpawner] La oleada {zone.CurrentWave} de {zone.name} no pudo iniciarse tras {maxFailedWaveAttempts} intentos. Forzando cierre de la zona.", zone);
                    zone.ForceComplete();
                    break;
                }
                continue;
            }

            failedWaveAttempts = 0;
            OnWaveStarted?.Invoke(zone, zone.CurrentWave);

            int spawnedEnemies = SpawnWave(zone);
            if (spawnedEnemies == 0)
            {
                Debug.LogError($"[ZoneSpawner] La oleada {zone.CurrentWave} de {zone.name} no pudo generar enemigos. La zona se abre para evitar un bloqueo.", zone);
                zone.CompleteEncounter();
                break;
            }

            waveCompletionPending = true;
            pendingWaveNumber = zone.CurrentWave;
        }

        if (zone != null) spawnCoroutines.Remove(zone);
    }

    private int SpawnWave(SpawnZone zone)
    {
        int waveCount = UnityEngine.Random.Range(zone.minEnemiesPerWave, zone.maxEnemiesPerWave + 1);
        int spawnedCount = 0;
        int waveIndex = zone.CurrentWave - 1;

        List<Vector2> waveAssignedPositions = new List<Vector2>();

        for (int i = 0; i < waveCount && zone.activeEnemyCount < zone.maxActiveEnemies; i++)
        {
            EnemyData data = zone.GetRandomEnemyData();
            if (data == null || data.prefab == null || EnemyPool.Instance == null) continue;

            Vector2 spawnPos = zone.GetEnemySpawnPoint(i, waveAssignedPositions, mainCamera);
            GameObject enemyObject = EnemyPool.Instance.Get(data.prefab, spawnPos);
            if (enemyObject == null) continue;

            Enemy enemy = enemyObject.GetComponent<Enemy>();
            if (enemy == null)
            {
                Debug.LogWarning($"[ZoneSpawner] {enemyObject.name} no tiene Enemy; no cuenta para la oleada.", enemyObject);
                continue;
            }

            enemy.ResetForPool();
            enemy.originZone = zone;
            enemy.Init(
                data.maxHP + (waveIndex * zone.hpBonusPerWave),
                data.moveSpeed + (waveIndex * zone.speedBonusPerWave),
                data.xpReward + Mathf.RoundToInt(waveIndex * zone.xpBonusPerWave),
                data.contactDamage + (waveIndex * zone.damageBonusPerWave));
            enemy.data = data;
            enemy.OnDeath -= HandleEnemyDeath;
            enemy.OnDeath += HandleEnemyDeath;
            zone.RegisterEnemySpawned();
            spawnedCount++;

            EnemyChase chase = enemyObject.GetComponent<EnemyChase>();
            if (chase != null)
            {
                if (waveIndex == 0 && zone.firstWaveChaseDelay > 0f)
                {
                    StartCoroutine(DelayedChaseRoutine(chase, zone.firstWaveChaseDelay));
                }
                else
                {
                    chase.SetTarget(player);
                }
            }
        }

        // Sincronizar el árbol de físicas 2D tras colocar el lote de enemigos
        if (spawnedCount > 0)
        {
            Physics2D.SyncTransforms();
        }

        return spawnedCount;
    }

    private IEnumerator DelayedChaseRoutine(EnemyChase chase, float delay)
    {
        chase.SetTarget(null);
        yield return new WaitForSeconds(delay);
        if (chase != null) chase.SetTarget(player);
    }

    private void HandleEnemyDeath(Enemy enemy)
    {
        enemy.OnDeath -= HandleEnemyDeath;
        // La notificación es idempotente (flag en Enemy); el OnDisable posterior
        // del pool no descuenta dos veces.
        enemy.NotifyZoneRemoval();
    }

    public SpawnZone GetPlayerZone()
    {
        return playerCurrentZone;
    }

    public void PauseAllSpawning()
    {
        isSpawningPaused = true;
    }

    public void ResumeAllSpawning()
    {
        isSpawningPaused = false;
    }

    public void ClearAllEnemies()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.gameObject.activeInHierarchy) continue;
            // Idempotente: no doble descuenta aunque OnDisable vuelva a dispararse.
            enemy.NotifyZoneRemoval();
            enemy.SuppressDropsAndDestroy();
        }
    }
}
