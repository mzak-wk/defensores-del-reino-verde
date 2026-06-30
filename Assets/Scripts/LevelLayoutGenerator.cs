using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelLayoutGenerator : MonoBehaviour
{
    public static event Action<List<ZoneInstance>> OnLayoutGenerated;
    public static event Action<Transform> OnPlayerSpawnPointReady;

    [Header("Configuración")]
    public LevelLayoutData layoutData;
    
    [Header("UI")]
    public LoadingScreenUI loadingScreen;

    [Header("Depuración")]
    public bool drawGizmos = true;

    private List<ZoneInstance> generatedZones = new List<ZoneInstance>();

    private void Awake()
    {
        if (layoutData == null)
        {
            Debug.LogError("[LevelLayoutGenerator] Faltan datos de Layout Data.");
            return;
        }

        StartCoroutine(GenerateLayoutRoutine());
    }

    private IEnumerator GenerateLayoutRoutine()
    {
        if (loadingScreen != null)
        {
            loadingScreen.Show();
            loadingScreen.UpdateProgress(0f);
        }

        // 1. Inicializar semilla aleatoria pura
        UnityEngine.Random.InitState(System.DateTime.Now.Millisecond);

        // 2. Barajar prefabs intermedios
        GameObject[] shuffledMiddleZones = (GameObject[])layoutData.middleZonePrefabs.Clone();
        ShuffleArray(shuffledMiddleZones);

        int totalSlots = layoutData.columns * layoutData.rows;
        int middleZonesNeeded = totalSlots - 2; // Descontamos Start y End

        if (shuffledMiddleZones.Length < middleZonesNeeded)
        {
            Debug.LogWarning("[LevelLayoutGenerator] No hay suficientes prefabs de zonas intermedias para llenar el grid. Se repetirán algunas.");
        }

        int middleZoneIndex = 0;
        int generatedCount = 0;
        
        // Variable para guardar el punto de spawn del jugador y dispararlo al final
        Transform finalPlayerSpawnPoint = null;

        // 3. Generar el grid
        for (int r = 0; r < layoutData.rows; r++)
        {
            for (int c = 0; c < layoutData.columns; c++)
            {
                GameObject prefabToInstantiate = null;
                ZoneType expectedType = ZoneType.Middle;

                if (c == 0 && r == 0)
                {
                    prefabToInstantiate = layoutData.startZonePrefab;
                    expectedType = ZoneType.Start;
                }
                else if (c == layoutData.columns - 1 && r == 0) 
                {
                    prefabToInstantiate = layoutData.endZonePrefab;
                    expectedType = ZoneType.End;
                }
                else
                {
                    if (shuffledMiddleZones.Length > 0)
                    {
                        prefabToInstantiate = shuffledMiddleZones[middleZoneIndex % shuffledMiddleZones.Length];
                        middleZoneIndex++;
                    }
                }

                if (prefabToInstantiate != null)
                {
                    Vector2 targetWorldPos = GetCellWorldPosition(c, r);
                    
                    // Instanciamos en el punto objetivo inicialmente
                    GameObject instance = Instantiate(prefabToInstantiate, targetWorldPos, Quaternion.identity, transform);
                    instance.name = $"Zone_{c}_{r}_{expectedType}";
                    instance.transform.localScale = Vector3.one;

                    // 1. Posicionamiento Inicial y Escala 1:1
                    instance.transform.position = new Vector3(targetWorldPos.x, targetWorldPos.y, instance.transform.position.z);
                    instance.transform.localScale = Vector3.one;

                    // 2. Escalado Automático Exacto (Grid Fit)
                    Bounds visualBounds = CalculateVisualBounds(instance);
                    if (visualBounds.size.x > 0.001f && visualBounds.size.y > 0.001f)
                    {
                        float scaleX = layoutData.cellWidth / visualBounds.size.x;
                        float scaleY = layoutData.cellHeight / visualBounds.size.y;
                        
                        instance.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                    }

                    // 3. Compensación Fija por Centro Local (Vértices)
                    PolygonCollider2D polyCol = instance.GetComponentInChildren<PolygonCollider2D>();
                    if (polyCol != null && polyCol.points.Length > 0)
                    {
                        // Promediar vectores locales
                        Vector2 sum = Vector2.zero;
                        foreach (Vector2 point in polyCol.points)
                        {
                            sum += point;
                        }
                        Vector2 localCenter = sum / polyCol.points.Length;

                        // Convertir a posición global considerando la escala actual
                        Vector3 globalCenter = polyCol.transform.TransformPoint(localCenter);
                        
                        // Aplicar desfase para que el centro geométrico quede en targetWorldPos
                        Vector3 offset = new Vector3(targetWorldPos.x, targetWorldPos.y, instance.transform.position.z) - globalCenter;
                        instance.transform.position += offset;
                    }

                    ZoneMetadata metadata = instance.GetComponent<ZoneMetadata>();
                    if (metadata == null)
                    {
                        Debug.LogWarning($"[LevelLayoutGenerator] El prefab {prefabToInstantiate.name} no tiene ZoneMetadata.");
                        metadata = instance.AddComponent<ZoneMetadata>();
                        metadata.zoneType = expectedType;
                        metadata.spawnAnchors = new Transform[0];
                    }

                    // Recopilar posiciones globales de los anchors
                    Vector3[] anchorPositions = new Vector3[metadata.spawnAnchors.Length];
                    for (int i = 0; i < metadata.spawnAnchors.Length; i++)
                    {
                        if (metadata.spawnAnchors[i] != null)
                        {
                            anchorPositions[i] = metadata.spawnAnchors[i].position;
                        }
                    }

                    ZoneInstance zoneInstance = new ZoneInstance
                    {
                        instanceGO = instance,
                        metadata = metadata,
                        gridCoord = new Vector2Int(c, r),
                        worldPosition = instance.transform.position, // Guardamos la posición final corregida
                        spawnAnchorPositions = anchorPositions,
                        zoneType = expectedType
                    };

                    generatedZones.Add(zoneInstance);

                    // Si es zona de inicio, guardamos el spawn point para dispararlo AL FINAL
                    if (expectedType == ZoneType.Start && metadata.playerSpawnPoint != null)
                    {
                        finalPlayerSpawnPoint = metadata.playerSpawnPoint;
                    }
                }

                generatedCount++;
                if (loadingScreen != null)
                {
                    loadingScreen.UpdateProgress((float)generatedCount / totalSlots);
                }

                // Esperar al siguiente frame para no causar lag spikes
                yield return null;
            }
        }

        // 4. Disparo Seguro del Evento del Jugador (Al terminar toda la generación)
        if (finalPlayerSpawnPoint != null)
        {
            Debug.Log($"[LevelLayoutGenerator] Disparando evento de spawn. Posición destino: {finalPlayerSpawnPoint.position}");
            OnPlayerSpawnPointReady?.Invoke(finalPlayerSpawnPoint);
        }
        else
        {
            Debug.LogWarning("[LevelLayoutGenerator] No se encontró ningún PlayerSpawnPoint en la Zona de Inicio.");
        }

        // 5. Finalizar UI y notificar al Spawner
        if (loadingScreen != null)
        {
            loadingScreen.UpdateProgress(1f);
            yield return new WaitForSeconds(0.2f); // Pequeña pausa para que se vea lleno
            loadingScreen.Hide();
        }

        OnLayoutGenerated?.Invoke(generatedZones);
    }

    private void ShuffleArray<T>(T[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }
    }

    private Vector2 GetCellWorldPosition(int col, int row)
    {
        float x = layoutData.gridOrigin.x + (col * layoutData.cellWidth) + (layoutData.cellWidth * 0.5f);
        float y = layoutData.gridOrigin.y + (row * layoutData.cellHeight) + (layoutData.cellHeight * 0.5f);
        return new Vector2(x, y);
    }

    private Bounds CalculateVisualBounds(GameObject obj)
    {
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
        bool hasBounds = false;

        // Buscamos componentes visuales (SpriteRenderer o TilemapRenderer)
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r is UnityEngine.Tilemaps.TilemapRenderer || r is SpriteRenderer)
            {
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }
        }
        
        return bounds;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || layoutData == null) return;

        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        
        for (int r = 0; r < layoutData.rows; r++)
        {
            for (int c = 0; c < layoutData.columns; c++)
            {
                Vector2 center = GetCellWorldPosition(c, r);
                Vector3 size = new Vector3(layoutData.cellWidth, layoutData.cellHeight, 0.1f);
                Gizmos.DrawWireCube(center, size);
                
#if UNITY_EDITOR
                UnityEditor.Handles.Label(center, $"[{c},{r}]");
#endif
            }
        }
    }
}
