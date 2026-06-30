using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Prefabs de los personajes (mismo orden que en el menú)")]
    public GameObject[] playerPrefabs;

    [Header("Punto de spawn")]
    public Transform spawnPoint;

    void Start()
    {
        if (playerPrefabs == null || playerPrefabs.Length == 0)
        {
            Debug.LogError("[PlayerSpawner] No hay prefabs asignados.");
            return;
        }

        int idx = 0;
        if (SelectedCharacter.Instance != null)
        {
            idx = Mathf.Clamp(
                SelectedCharacter.Instance.selectedIndex,
                0,
                playerPrefabs.Length - 1
            );
        }

        var prefab = playerPrefabs[idx];
        if (!prefab)
        {
            Debug.LogError($"[PlayerSpawner] Prefab en índice {idx} es null.");
            return;
        }

        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
        var player = Instantiate(prefab, pos, Quaternion.identity);

        // (más abajo le enchufamos la cámara)
        SetupCameraFollow(player.transform);
    }

    void SetupCameraFollow(Transform player)
    {
        var cam = Camera.main;
        if (!cam) return;

        var follow = cam.GetComponent<CameraFollow>();
        if (follow) follow.target = player;
    }
}
