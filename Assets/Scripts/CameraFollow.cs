using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Modo Zona Fija")]
    [Tooltip("Centro de zona al que la cámara se mueve.")]
    public Vector3 zoneTarget;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void LateUpdate()
    {
        Vector3 targetPos = new Vector3(zoneTarget.x, zoneTarget.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
    }

    public void SetTargetZone(Vector3 zoneCenter)
    {
        zoneTarget = zoneCenter;
    }

    public void SnapToZone()
    {
        transform.position = new Vector3(zoneTarget.x, zoneTarget.y, transform.position.z);
    }
}
