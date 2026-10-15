using UnityEngine;

/// <summary>
/// Sistema de ordenamiento de profundidad 2D (Y-Sorting).
/// Asigna dinámicamente el sortingOrder del SpriteRenderer según la coordenada Y
/// en el mundo para evitar superposiciones incorrectas entre personajes (ej. Andrius y Opina).
/// Cuando un personaje está más abajo en pantalla, se dibuja por delante del que está más arriba.
/// </summary>
[DisallowMultipleComponent]
public class DynamicYSort : MonoBehaviour
{
    [Header("Configuración de Capa")]
    [Tooltip("Capa de ordenamiento a sincronizar para evitar conflictos entre capas diferentes.")]
    [SerializeField] private string targetSortingLayer = "Player";

    [Header("Ajustes de Altura y Profundidad")]
    [Tooltip("Offset vertical respecto al transform (útil para alinear el punto de orden con los pies).")]
    [SerializeField] private float yOffset = 0f;

    [Tooltip("Precisión de cálculo de orden por unidad del mundo.")]
    [SerializeField] private float precision = 100f;

    [Tooltip("Offset base de orden para prioridades específicas.")]
    [SerializeField] private int baseOrderOffset = 0;

    [Tooltip("Si es true, sólo calcula el orden al iniciar (para props o NPCs estáticos).")]
    [SerializeField] private bool isStatic = false;

    private SpriteRenderer spriteRenderer;
    private float lastY = float.MinValue;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null && !string.IsNullOrEmpty(targetSortingLayer))
        {
            spriteRenderer.sortingLayerName = targetSortingLayer;
        }

        UpdateSortingOrder();
    }

    private void LateUpdate()
    {
        if (isStatic) return;

        // Optimización: solo recalcular si la posición Y cambió perceptiblemente
        float currentY = transform.position.y;
        if (Mathf.Abs(currentY - lastY) > 0.002f)
        {
            UpdateSortingOrder();
        }
    }

    public void UpdateSortingOrder()
    {
        if (spriteRenderer == null) return;

        lastY = transform.position.y;
        // En Unity 2D: mayor Y está más al fondo -> menor sortingOrder. Menor Y está más adelante -> mayor sortingOrder.
        int calculatedOrder = -Mathf.RoundToInt((lastY + yOffset) * precision) + baseOrderOffset;
        spriteRenderer.sortingOrder = calculatedOrder;
    }

    /// <summary>
    /// Sincroniza automáticamente Y-Sorting en Jugadores y NPCs al cargar cualquier escena.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttachToCharacters()
    {
        // 1. Vincular al jugador
        var players = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p != null && p.GetComponent<DynamicYSort>() == null)
            {
                var ys = p.gameObject.AddComponent<DynamicYSort>();
                ys.targetSortingLayer = "Player";
                ys.isStatic = false;
            }
        }

        // 2. Vincular a NPCs (Opina, etc.)
        var npcs = Object.FindObjectsByType<NPCHubController>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            if (npc != null && npc.GetComponent<DynamicYSort>() == null)
            {
                var ys = npc.gameObject.AddComponent<DynamicYSort>();
                ys.targetSortingLayer = "Player";
                ys.isStatic = false; // Permite reordenamiento dinámico si el jugador pasa alrededor
            }
        }
    }
}
