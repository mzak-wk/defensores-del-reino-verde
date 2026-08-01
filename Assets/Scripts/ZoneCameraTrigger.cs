using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ZoneCameraTrigger : MonoBehaviour
{
    [Header("Zona")]
    [Tooltip("Ancla visual colocada en el centro exacto de la sala (hijo vacío del prefab).")]
    public Transform cameraAnchor;

    [Tooltip("Centro de la zona para la cámara. Se auto-calcula desde el anchor o la posición del objeto.")]
    public Vector3 zoneCenter;

    [Tooltip("Duración del paneo suave (0 = snap instantáneo).")]
    public float panDuration = 0.3f;

    [Tooltip("Si es true, la cámara hace snap inmediato al centro en Start(), sin esperar trigger.")]
    public bool isStartZone = false;

    [Header("Tags")]
    public string playerTag = "Player";

    private bool triggered = false;

    private void Awake()
    {
        zoneCenter = cameraAnchor != null ? cameraAnchor.position : transform.position;
    }

    private void Start()
    {
        if (isStartZone && CameraFollow.Instance != null)
        {
            CameraFollow.Instance.SetTargetZone(zoneCenter);
            CameraFollow.Instance.SnapToZone();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;

        triggered = true;

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.SetTargetZone(zoneCenter);
            if (panDuration > 0f)
                CameraFollow.Instance.PanToZone(panDuration);
            else
                CameraFollow.Instance.SnapToZone();
        }
    }

    private void Reset()
    {
        zoneCenter = transform.position;
    }
}
