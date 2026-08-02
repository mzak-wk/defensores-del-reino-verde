using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelLayoutData", menuName = "Defenders/Level Layout Data", order = 2)]
public class LevelLayoutData : ScriptableObject
{
    [Header("Metadatos")]
    public string levelName;
    public int levelIndex;

    [Header("Configuración del Grid")]
    [Min(3)] public int columns = 6;
    [Min(1)] public int rows = 1;
    public float cellWidth = 30f;
    public float cellHeight = 20f;
    public Vector2 gridOrigin = Vector2.zero;

    [Header("Tamaño Fijo de Zona")]
    [Tooltip("Dimensiones exactas de cada zona en unidades de mundo. Asume pivot centrado en todos los prefabs.")]
    public Vector2 fixedZoneSize = new Vector2(30f, 20f);

    [Header("Zonas Fijas")]
    [Tooltip("Aparecerá siempre en la columna 0.")]
    public GameObject startZonePrefab;

    [Tooltip("Aparecerá siempre en la última columna.")]
    public GameObject endZonePrefab;

    [Header("Pool de Zonas Intermedias")]
    [Tooltip("Estas zonas se barajarán y ocuparán los espacios intermedios.")]
    public GameObject[] middleZonePrefabs;
}
