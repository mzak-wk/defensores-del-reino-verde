using UnityEngine;

[System.Serializable]
public struct ZoneInstance
{
    public GameObject instanceGO;
    public ZoneMetadata metadata;
    public Vector2Int gridCoord;        // (col, row)
    public Vector2 worldPosition;       // centro de la celda
    public Vector3[] spawnAnchorPositions; // posiciones globales post-instanciación
    public ZoneType zoneType;
}
