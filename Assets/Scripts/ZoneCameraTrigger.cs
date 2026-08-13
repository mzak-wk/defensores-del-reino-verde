using UnityEngine;

public class ZoneCameraTrigger : MonoBehaviour
{
    [Header("Zona")]
    public Transform cameraAnchor;
    public Vector3 zoneCenter;
    public bool isStartZone;

    [Header("Transicion")]
    [SerializeField] private bool isTransitionTrigger;
    [SerializeField] private string playerTag = "Player";

    private SpawnZone targetZone;
    private void Awake()
    {
        zoneCenter = cameraAnchor != null ? cameraAnchor.position : transform.position;
        targetZone = GetComponentInParent<SpawnZone>();
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
        TryEnterZone(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryEnterZone(other);
    }

    private void TryEnterZone(Collider2D other)
    {
        if (!isTransitionTrigger || !other.CompareTag(playerTag) || ZoneSpawner.Instance == null)
        {
            return;
        }

        ZoneSpawner.Instance.TryEnterZone(targetZone, this);
    }

    public void ActivateCamera()
    {
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.SetTargetZone(zoneCenter);
        }
    }

    private void Reset()
    {
        zoneCenter = transform.position;
    }
}
