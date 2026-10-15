using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistema centralizado de guardado y carga usando archivos JSON locales.
/// Se encarga de leer/escribir en disco y manejar las transiciones de escena.
/// Funciona como Singleton para ser accesible desde cualquier parte.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Configuración")]
    [Tooltip("El slot actualmente activo o cargado (0 = ninguno)")]
    public int currentSlot = 0;

    [Tooltip("Nombre de la escena de cinemática inicial")]
    public string cinematicSceneName = "CinematicScene";

    [Tooltip("Nombre de la escena del HUB")]
    public string hubSceneName = "LevelHUB";

    private void Awake()
    {
        // Configuración del Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        transform.SetParent(null); // <-- Asegura que el GameObject esté en la raíz
        DontDestroyOnLoad(gameObject);

        // Suscribirse al evento de cambio de escena para el autoguardado en el HUB
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Obtiene la ruta completa del archivo de guardado para un slot específico.
    /// </summary>
    private string GetSavePath(int slotId)
    {
        return Path.Combine(Application.persistentDataPath, $"save_slot_{slotId}.json");
    }

    /// <summary>
    /// Verifica si un slot contiene datos guardados comprobando si el archivo existe.
    /// </summary>
    public bool HasSave(int slotId)
    {
        return File.Exists(GetSavePath(slotId));
    }

    /// <summary>
    /// Lee los datos de un archivo de guardado sin aplicarlos al GameSession (útil para la UI).
    /// Retorna null si el archivo no existe o hay un error.
    /// </summary>
    public SaveData GetSaveData(int slotId)
    {
        string path = GetSavePath(slotId);
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Error leyendo el archivo de guardado del slot {slotId}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Crea una nueva partida en el slot indicado.
    /// Inicia el GameSession a valores por defecto y carga la escena inicial.
    /// </summary>
    public void StartNewGame(int slotId)
    {
        currentSlot = slotId;

        // Limpiar cualquier progreso residual en GameSession
        if (GameSession.Instance != null)
        {
            GameSession.Instance.ResetAll();
        }

        // Cargar la escena de la cinemática inicial
        SceneManager.LoadScene(cinematicSceneName);
        Debug.Log($"[SaveManager] Iniciando nueva partida en el Slot {slotId}");
    }

    /// <summary>
    /// Carga los datos de un slot específico en el GameSession y transiciona al HUB.
    /// </summary>
    public void LoadGame(int slotId)
    {
        if (!HasSave(slotId))
        {
            Debug.LogWarning($"[SaveManager] Intento de cargar un slot vacío ({slotId}).");
            return;
        }

        SaveData data = GetSaveData(slotId);
        if (data != null && GameSession.Instance != null)
        {
            currentSlot = slotId;
            GameSession.Instance.ImportSaveData(data);
            
            // Cargar la escena del HUB. El jugador aparecerá en el punto de spawn predeterminado.
            SceneManager.LoadScene(hubSceneName);
            Debug.Log($"[SaveManager] Partida cargada del Slot {slotId}");
        }
        else
        {
            Debug.LogError("[SaveManager] Error al cargar: SaveData es null o falta GameSession.");
        }
    }

    /// <summary>
    /// Guarda el estado actual del GameSession en el archivo del slot activo.
    /// </summary>
    public void SaveCurrentGame()
    {
        if (currentSlot == 0)
        {
            Debug.LogWarning("[SaveManager] No hay slot activo para guardar.");
            return;
        }

        if (GameSession.Instance == null)
        {
            Debug.LogError("[SaveManager] GameSession no encontrado, no se puede guardar.");
            return;
        }

        SaveData data = new SaveData(GameSession.Instance);
        string json = JsonUtility.ToJson(data, true); // true para pretty print

        try
        {
            File.WriteAllText(GetSavePath(currentSlot), json);
            Debug.Log($"[SaveManager] Partida guardada exitosamente en el Slot {currentSlot}.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Error al guardar la partida en el slot {currentSlot}: {e.Message}");
        }
    }

    /// <summary>
    /// Elimina el archivo de guardado de un slot específico si existe.
    /// </summary>
    public bool DeleteSave(int slotId)
    {
        string path = GetSavePath(slotId);
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
                Debug.Log($"[SaveManager] Partida eliminada del Slot {slotId}.");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Error al borrar el slot {slotId}: {e.Message}");
                return false;
            }
        }
        return false;
    }

    // =====================================
    // Autoguardado
    // =====================================

    /// <summary>
    /// Hook que se ejecuta cada vez que se carga una escena.
    /// Si la escena es el HUB, autoguarda el progreso actual.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Solo autoguardar si hay una partida activa y entramos al HUB
        if (scene.name == hubSceneName && currentSlot != 0)
        {
            Debug.Log("[SaveManager] Entrando al HUB. Activando autoguardado...");
            SaveCurrentGame();
        }
    }
}
