using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private Transform poolParent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Crear un padre organizativo en la jerarquía
        GameObject parentGO = new GameObject("EnemyPoolContainer");
        poolParent = parentGO.transform;
    }

    /// <summary>
    /// Obtiene una instancia del prefab especificado.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector2 position)
    {
        if (prefab == null) return null;

        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<GameObject>();
        }

        GameObject instance;

        if (pools[prefab].Count > 0)
        {
            instance = pools[prefab].Dequeue();
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.identity;
        }
        else
        {
            instance = Instantiate(prefab, position, Quaternion.identity);
            
            // Agregar un tag para saber a qué pool debe regresar
            PoolTag tag = instance.AddComponent<PoolTag>();
            tag.prefabOriginal = prefab;
        }

        instance.transform.SetParent(null);
        instance.SetActive(true);

        return instance;
    }

    /// <summary>
    /// Devuelve la instancia al pool.
    /// </summary>
    public void Return(GameObject instance)
    {
        if (instance == null) return;

        PoolTag tag = instance.GetComponent<PoolTag>();
        if (tag == null || tag.prefabOriginal == null)
        {
            // Si no tiene el tag del pool, se destruye normalmente
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(poolParent);

        if (!pools.ContainsKey(tag.prefabOriginal))
        {
            pools[tag.prefabOriginal] = new Queue<GameObject>();
        }

        pools[tag.prefabOriginal].Enqueue(instance);
    }

    /// <summary>
    /// Pre-instancia enemigos para evitar lag (opcional)
    /// </summary>
    public void PreWarm(GameObject prefab, int count)
    {
        if (prefab == null) return;

        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<GameObject>();
        }

        for (int i = 0; i < count; i++)
        {
            GameObject instance = Instantiate(prefab, poolParent);
            PoolTag tag = instance.AddComponent<PoolTag>();
            tag.prefabOriginal = prefab;
            instance.SetActive(false);
            pools[prefab].Enqueue(instance);
        }
    }
}

// Componente auxiliar para rastrear de qué prefab salió cada instancia
public class PoolTag : MonoBehaviour
{
    public GameObject prefabOriginal;
}
