using UnityEngine;

[DefaultExecutionOrder(-100)]
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

        // Punto 3: Eliminar objetos Player prefijados o duplicados en la jerarquía de la escena
        GameObject[] existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject existing in existingPlayers)
        {
            if (existing != null)
            {
                Destroy(existing);
            }
        }

        var prefab = playerPrefabs[idx];
        if (!prefab)
        {
            Debug.LogError($"[PlayerSpawner] Prefab en índice {idx} es null.");
            return;
        }

        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
        var player = Instantiate(prefab, pos, Quaternion.identity);

        if (player != null)
        {
            player.tag = "Player";

            // Forzar transformación nativa sin volteo previo
            player.transform.localScale = Vector3.one;
            player.transform.rotation = Quaternion.identity;

            var weaponSlot = player.GetComponent<WeaponSlotManager>();
            if (weaponSlot != null)
            {
                weaponSlot.RefreshWeaponAnimation();
            }
            else
            {
                var anim = player.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.enabled = true;
                    anim.SetInteger("WeaponIndex", -1);
                    anim.Update(0f);
                }
            }
        }
    }
}
