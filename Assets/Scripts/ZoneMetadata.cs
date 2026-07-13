using UnityEngine;

public enum ZoneType
{
    Start,
    Middle,
    End
}

public class ZoneMetadata : MonoBehaviour
{
    [Header("Identificación")]
    public string zoneName = "Nueva Zona";
    public ZoneType zoneType = ZoneType.Middle;
    
    public int zoneIndex = 0;
    [HideInInspector] public bool isCombatZone = false;

    [Header("Puntos de Spawn")]
    [Tooltip("Puntos fijos donde aparecerán los enemigos en esta zona.")]
    public Transform[] spawnAnchors;
    
    [Tooltip("Solo necesario si ZoneType es Start. Donde aparecerá el jugador.")]
    public Transform playerSpawnPoint;

    private void OnDrawGizmos()
    {
        if (spawnAnchors != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform anchor in spawnAnchors)
            {
                if (anchor != null)
                {
                    Gizmos.DrawWireSphere(anchor.position, 0.5f);
                }
            }
        }

        if (zoneType == ZoneType.Start && playerSpawnPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(playerSpawnPoint.position, 1f);
        }
    }
}
