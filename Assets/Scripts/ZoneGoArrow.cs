using UnityEngine;

public class ZoneGoArrow : MonoBehaviour
{
    [Header("Referencia de la Flecha")]
    [Tooltip("El GameObject hijo que contiene el sprite de la flecha (->). Se activa al limpiar la sala.")]
    public GameObject arrowObject;

    [Header("Opcional")]
    [Tooltip("Si true, la flecha se desactiva al entrar el jugador a la zona.")]
    public bool hideOnPlayerEnter = true;

    public string playerTag = "Player";

    private SpawnZone spawnZone;

    private void Awake()
    {
        spawnZone = GetComponent<SpawnZone>();

        if (arrowObject != null)
            arrowObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (spawnZone != null)
            spawnZone.OnZoneCleared += ShowArrow;
    }

    private void OnDisable()
    {
        if (spawnZone != null)
            spawnZone.OnZoneCleared -= ShowArrow;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hideOnPlayerEnter) return;
        if (!other.CompareTag(playerTag)) return;

        HideArrow();
    }

    private void ShowArrow()
    {
        if (arrowObject != null)
            arrowObject.SetActive(true);
    }

    private void HideArrow()
    {
        if (arrowObject != null)
            arrowObject.SetActive(false);
    }
}
