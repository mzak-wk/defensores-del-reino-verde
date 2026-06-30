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
    
    [Header("Configuración")]
    public float cameraZoneCheckInterval = 0.5f;

    // Estado interno
    private HashSet<SpawnZone> allZones = new HashSet<SpawnZone>();
    private HashSet<SpawnZone> activeZones = new HashSet<SpawnZone>();
    private SpawnZone playerCurrentZone;
    
    private Dictionary<SpawnZone, Coroutine> spawnCoroutines = new Dictionary<SpawnZone, Coroutine>();
    private Dictionary<SpawnZone, Coroutine> despawnCoroutines = new Dictionary<SpawnZone, Coroutine>();

    private bool isSpawningPaused = false;
    
    public event Action<SpawnZone> OnPlayerZoneChanged;

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
            GameObject pGO = GameObject.FindGameObjectWithTag("Player");
            if (pGO != null) player = pGO.transform;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        LevelLayoutGenerator.OnLayoutGenerated -= OnLayoutReady;
        LevelLayoutGenerator.OnLayoutGenerated += OnLayoutReady;
    }

    private void OnDestroy()
    {
        LevelLayoutGenerator.OnLayoutGenerated -= OnLayoutReady;
    }

    public void OnLayoutReady(List<ZoneInstance> zones)
    {
        allZones.Clear();
        foreach (var zi in zones)
        {
            SpawnZone sz = zi.instanceGO.GetComponent<SpawnZone>();
            if (sz != null)
            {
                allZones.Add(sz);
            }
        }
        
        StartCoroutine(UpdateActiveZonesRoutine());
    }

    private IEnumerator UpdateActiveZonesRoutine()
    {
        while (true)
        {
            if (!isSpawningPaused)
            {
                UpdateActiveZones();
            }
            yield return new WaitForSeconds(cameraZoneCheckInterval);
        }
    }

    private void UpdateActiveZones()
    {
        if (mainCamera == null) return;

        float height = mainCamera.orthographicSize * 2f;
        float width = height * mainCamera.aspect;
        Vector3 camCenter = mainCamera.transform.position;
        Bounds camBounds = new Bounds(camCenter, new Vector3(width, height, 0f));
        
        // Agregar un pequeño padding para no despawnear cosas en el borde
        camBounds.Expand(new Vector3(4f, 4f, 0f)); 

        HashSet<SpawnZone> currentlyActive = new HashSet<SpawnZone>();

        foreach (var zone in allZones)
        {
            Collider2D col = zone.GetComponent<Collider2D>();
            if (col == null) continue;

            // La zona está activa si el jugador está en ella O si la cámara la ve
            bool playerInZone = (zone == playerCurrentZone);
            bool cameraSeesZone = col.bounds.Intersects(camBounds); // Aproximación con AABB bounds

            if (playerInZone || cameraSeesZone)
            {
                currentlyActive.Add(zone);
            }
        }

        // Zonas que se acaban de desactivar
        foreach (var zone in activeZones)
        {
            if (!currentlyActive.Contains(zone))
            {
                DeactivateZone(zone);
            }
        }

        // Zonas que se acaban de activar
        foreach (var zone in currentlyActive)
        {
            if (!activeZones.Contains(zone))
            {
                ActivateZone(zone);
            }
        }

        activeZones = currentlyActive;
    }

    private void ActivateZone(SpawnZone zone)
    {
        // Cancelar despawn pendiente si lo hubiera
        if (despawnCoroutines.ContainsKey(zone) && despawnCoroutines[zone] != null)
        {
            StopCoroutine(despawnCoroutines[zone]);
            despawnCoroutines.Remove(zone);
        }

        // Iniciar spawning
        if (!spawnCoroutines.ContainsKey(zone) || spawnCoroutines[zone] == null)
        {
            spawnCoroutines[zone] = StartCoroutine(SpawnLoopForZone(zone));
        }
    }

    private void DeactivateZone(SpawnZone zone)
    {
        // Detener spawning
        if (spawnCoroutines.ContainsKey(zone) && spawnCoroutines[zone] != null)
        {
            StopCoroutine(spawnCoroutines[zone]);
            spawnCoroutines.Remove(zone);
        }

        // Iniciar despawn con delay
        if (!despawnCoroutines.ContainsKey(zone) || despawnCoroutines[zone] == null)
        {
            despawnCoroutines[zone] = StartCoroutine(DespawnZoneRoutine(zone, zone.despawnDelay));
        }
    }

    private IEnumerator SpawnLoopForZone(SpawnZone zone)
    {
        while (true)
        {
            if (isSpawningPaused)
            {
                yield return null;
                continue;
            }

            if (zone.activeEnemyCount >= zone.maxActiveEnemies)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            EnemyData dataToSpawn = zone.GetRandomEnemyData();
            if (dataToSpawn != null && dataToSpawn.prefab != null && EnemyPool.Instance != null)
            {
                Vector2 spawnPos = zone.GetRandomOffscreenPoint(mainCamera);
                GameObject enemyGO = EnemyPool.Instance.Get(dataToSpawn.prefab, spawnPos);
                
                Enemy enemyComp = enemyGO.GetComponent<Enemy>();
                if (enemyComp != null)
                {
                    enemyComp.ResetForPool();
                    enemyComp.originZone = zone;
                    enemyComp.Init(dataToSpawn);
                    
                    zone.activeEnemyCount++;

                    // Limpiar suscripciones previas para evitar duplicados al reciclar
                    enemyComp.OnDeath -= HandleEnemyDeath;
                    enemyComp.OnDeath += HandleEnemyDeath;

                    EnemyChase chase = enemyGO.GetComponent<EnemyChase>();
                    if (chase != null)
                    {
                        chase.ResetChaseState();
                        chase.originZone = zone;
                        chase.leashDistance = zone.leashDistance;
                        chase.SetTarget(player);
                    }
                }
            }

            yield return new WaitForSeconds(zone.spawnInterval);
        }
    }

    private void HandleEnemyDeath(Enemy enemy)
    {
        enemy.OnDeath -= HandleEnemyDeath;
        if (enemy.originZone != null)
        {
            enemy.originZone.activeEnemyCount = Mathf.Max(0, enemy.originZone.activeEnemyCount - 1);
        }
    }

    private IEnumerator DespawnZoneRoutine(SpawnZone zone, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Encontrar todos los enemigos que pertenecen a esta zona y devolverlos al pool
        // (Podríamos llevar una lista por zona para optimizar, pero usar FindObjectsByType es aceptable 
        // dado que no se llama cada frame, sino tras un delay al salir de una zona)
        
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in allEnemies)
        {
            if (enemy.originZone == zone && enemy.gameObject.activeInHierarchy)
            {
                // Devolver al pool sin disparar muerte/drops
                enemy.SuppressDropsAndDestroy(); 
                zone.activeEnemyCount = Mathf.Max(0, zone.activeEnemyCount - 1);
            }
        }

        despawnCoroutines.Remove(zone);
    }

    // --- API Pública ---

    public void OnPlayerEnteredZone(SpawnZone zone)
    {
        if (playerCurrentZone != zone)
        {
            playerCurrentZone = zone;
            OnPlayerZoneChanged?.Invoke(zone);
        }
    }

    public void OnPlayerExitedZone(SpawnZone zone)
    {
        if (playerCurrentZone == zone)
        {
            playerCurrentZone = null;
            // No disparamos evento de 'salida', asumimos que entrará a otra pronto
        }
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
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in allEnemies)
        {
            if (enemy.gameObject.activeInHierarchy)
            {
                if (enemy.originZone != null)
                {
                    enemy.originZone.activeEnemyCount = Mathf.Max(0, enemy.originZone.activeEnemyCount - 1);
                }
                enemy.SuppressDropsAndDestroy();
            }
        }
    }
}
