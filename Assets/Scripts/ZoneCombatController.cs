using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ZoneCombatController : MonoBehaviour
{
    [Header("Referencias de Entorno")]
    [Tooltip("Pared izquierda a activar (debe tener un collider sólido)")]
    public GameObject leftSpectralWall;
    [Tooltip("Pared derecha a activar (debe tener un collider sólido)")]
    public GameObject rightSpectralWall;

    [Header("Configuración de Enemigos")]
    [Tooltip("Tipos de enemigos que pueden aparecer en esta zona")]
    public GameObject[] enemyPrefabs;
    [Tooltip("Cantidad base de enemigos a spawnear")]
    public int baseEnemyCount = 3;
    [Tooltip("Multiplicador de dificultad basado en el índice de la zona")]
    public float difficultyMultiplier = 0.5f;

    // Estado interno
    private bool isCleared = false;
    private int activeEnemiesCount = 0;
    private bool combatTriggered = false;
    
    private BoxCollider2D zoneCollider;
    private ZoneMetadata metadata;

    private void Awake()
    {
        zoneCollider = GetComponent<BoxCollider2D>();
        zoneCollider.isTrigger = true;
        
        metadata = GetComponent<ZoneMetadata>();
        if (metadata == null)
        {
            Debug.LogWarning($"[ZoneCombatController] {name} no tiene componente ZoneMetadata.");
        }
        
        // Asegurar que las paredes están desactivadas al inicio
        if (leftSpectralWall != null) leftSpectralWall.SetActive(false);
        if (rightSpectralWall != null) rightSpectralWall.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCleared || combatTriggered) return;

        if (other.CompareTag("Player"))
        {
            combatTriggered = true;
            
            // Activar paredes de confinamiento
            if (leftSpectralWall != null) leftSpectralWall.SetActive(true);
            if (rightSpectralWall != null) rightSpectralWall.SetActive(true);
            
            // Obtener el transform del jugador directamente del collider para evitar buscar en Start
            Transform playerTransform = other.transform;
            
            SpawnEnemies(playerTransform);
        }
    }

    private void SpawnEnemies(Transform playerTransform)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError($"[ZoneCombatController] {name} no tiene enemyPrefabs asignados.");
            OpenZone();
            return;
        }

        int zoneIndex = metadata != null ? metadata.zoneIndex : 0;
        int totalCount = baseEnemyCount + Mathf.RoundToInt(zoneIndex * difficultyMultiplier);
        
        activeEnemiesCount = totalCount;
        
        for (int i = 0; i < totalCount; i++)
        {
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Vector2 spawnPos = GetRandomPointInBounds(zoneCollider);
            
            // Instanciar usando el pool
            GameObject enemyGO = EnemyPool.Instance.Get(prefab, spawnPos);
            
            if (enemyGO != null)
            {
                Enemy enemyComp = enemyGO.GetComponent<Enemy>();
                if (enemyComp != null)
                {
                    enemyComp.ResetForPool();
                    
                    // Suscribirse a la muerte
                    enemyComp.OnDeath -= OnEnemyDied;
                    enemyComp.OnDeath += OnEnemyDied;
                }
                
                // Alertar a la IA forzando la persecución absoluta del jugador
                EnemyChase chaseComp = enemyGO.GetComponent<EnemyChase>();
                if (chaseComp != null)
                {
                    chaseComp.ResetChaseState();
                    chaseComp.SetTarget(playerTransform);
                }
            }
        }
        
        // Prevención: si no se instanció ninguno (por algún error)
        if (activeEnemiesCount <= 0)
        {
            OpenZone();
        }
    }

    private void OnEnemyDied(Enemy enemy)
    {
        enemy.OnDeath -= OnEnemyDied;
        activeEnemiesCount--;
        
        if (activeEnemiesCount <= 0)
        {
            OpenZone();
        }
    }

    private void OpenZone()
    {
        if (leftSpectralWall != null) leftSpectralWall.SetActive(false);
        if (rightSpectralWall != null) rightSpectralWall.SetActive(false);
        
        isCleared = true;
        Debug.Log($"[ZoneCombatController] Zona {name} completada. Paso liberado.");
    }

    private Vector2 GetRandomPointInBounds(BoxCollider2D col)
    {
        if (col == null) return transform.position;
        
        Bounds bounds = col.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        
        return new Vector2(x, y);
    }
}
