using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Modo Legacy (seguir jugador)")]
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    [Header("Modo Zona Fija")]
    [Tooltip("Si true, la cámara se centra en la zona activa en lugar de seguir al jugador.")]
    public bool useZoneMode = false;

    private Vector3 zoneTarget;
    private Coroutine panCoroutine;
    private bool isPanning;

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
        if (useZoneMode)
        {
            if (!isPanning)
            {
                transform.position = new Vector3(zoneTarget.x, zoneTarget.y, transform.position.z);
            }
        }
        else
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z);
        }
    }

    public void SetTargetZone(Vector3 zoneCenter)
    {
        zoneTarget = zoneCenter;
        useZoneMode = true;
    }

    public void SnapToZone()
    {
        if (panCoroutine != null)
        {
            StopCoroutine(panCoroutine);
            panCoroutine = null;
        }
        isPanning = false;
        transform.position = new Vector3(zoneTarget.x, zoneTarget.y, transform.position.z);
    }

    public void PanToZone(float duration)
    {
        if (panCoroutine != null)
        {
            StopCoroutine(panCoroutine);
        }
        panCoroutine = StartCoroutine(PanCoroutine(duration));
    }

    public void FollowTarget(Transform newTarget)
    {
        target = newTarget;
        useZoneMode = false;
        if (panCoroutine != null)
        {
            StopCoroutine(panCoroutine);
            panCoroutine = null;
        }
        isPanning = false;
    }

    private IEnumerator PanCoroutine(float duration)
    {
        isPanning = true;
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(zoneTarget.x, zoneTarget.y, transform.position.z);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;
        isPanning = false;
        panCoroutine = null;
    }
}
