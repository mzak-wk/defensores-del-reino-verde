using System;
using System.Collections;
using Random = UnityEngine.Random;
using UnityEngine;

public class ItemsSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject xpPrefab;       // Prefab con XPPickup
    public GameObject coinPrefab;     // Prefab con CoinPickup
    public GameObject chickenPrefab;  // Prefab con ChickenPickup
    public Transform player;          // arrastra tu jugador
    public Camera cam;                // arrastra la Main Camera
    [Header("Spawn alrededor del jugador")]
    public float minRadius = 3f;      // distancia mínima desde el player
    public float maxRadius = 6f;      // distancia máxima desde el player
    public float cameraPadding = 0.5f; // margen para no pegar los bordes de la cámara


    [Header("Máximo simultáneo por tipo")]
    public int maxXp = 3;
    public int maxCoin = 3;
    public int maxChicken = 3;

    [Header("Respawn delay por tipo (segundos)")]
    public float respawnDelayXp = 3f;
    public float respawnDelayCoin = 4f;
    public float respawnDelayChicken = 6f;

    [Header("Área de aparición (opcional)")]
    public BoxCollider2D spawnArea; // Si se deja vacío, usa la posición del spawner

    // Recuento vivo
    int _xpCount, _coinCount, _chickenCount;

    void Start()
    {
        // Llenar hasta el máximo al iniciar
        while (_xpCount < maxXp) SpawnXp();
        while (_coinCount < maxCoin) SpawnCoin();
        while (_chickenCount < maxChicken) SpawnChicken();
    }

    // -------- Spawns --------
    void SpawnXp()
    {
        if (!xpPrefab || _xpCount >= maxXp) return;
        var pos = RandomSpawnPosAroundPlayer();
        var go = Instantiate(xpPrefab, pos, Quaternion.identity);
        AttachTag(go, Kind.Xp);
        _xpCount++;
    }

    void SpawnCoin()
    {
        if (!coinPrefab || _coinCount >= maxCoin) return;
        var pos = RandomSpawnPosAroundPlayer();
        var go = Instantiate(coinPrefab, pos, Quaternion.identity);
        AttachTag(go, Kind.Coin);
        _coinCount++;
    }

    void SpawnChicken()
    {
        if (!chickenPrefab || _chickenCount >= maxChicken) return;
        var pos = RandomSpawnPosAroundPlayer();
        var go = Instantiate(chickenPrefab, pos, Quaternion.identity);
        AttachTag(go, Kind.Chicken);
        _chickenCount++;
    }

    // -------- Infra --------
    enum Kind { Xp, Coin, Chicken }

    void AttachTag(GameObject go, Kind kind)
    {
        // Este tag se destruye junto con el pickup y nos avisa para respawnear
        var tag = go.AddComponent<SpawnedItemTag>();
        tag.owner = this;
        tag.kind = kind;
    }

    void NotifyRemoved(Kind kind)   // privado por defecto
    {
        switch (kind)
        {
            case Kind.Xp:
                if (_xpCount > 0) _xpCount--;
                StartCoroutine(RespawnAfter(SpawnXp, respawnDelayXp));
                break;
            case Kind.Coin:
                if (_coinCount > 0) _coinCount--;
                StartCoroutine(RespawnAfter(SpawnCoin, respawnDelayCoin));
                break;
            case Kind.Chicken:
                if (_chickenCount > 0) _chickenCount--;
                StartCoroutine(RespawnAfter(SpawnChicken, respawnDelayChicken));
                break;
        }
    }
    IEnumerator RespawnAfter(Action spawn, float delay)
    {
        yield return new WaitForSeconds(delay);
        spawn?.Invoke();
    }

    Vector3 RandomPoint()
    {
        if (!spawnArea) return transform.position;
        var b = spawnArea.bounds;
        float x = UnityEngine.Random.Range(b.min.x, b.max.x);
        float y = UnityEngine.Random.Range(b.min.y, b.max.y);
        return new Vector3(x, y, 0f);
    }

    // Tag interno para saber qué tipo fue destruido
    class SpawnedItemTag : MonoBehaviour
    {
        public ItemsSpawner owner;
        public Kind kind;

        void OnDestroy()
        {
            // Al destruirse el pickup, el spawner actualiza recuento y agenda respawn
            if (owner) owner.NotifyRemoved(kind);
        }
    }
    Vector3 ClampToCamera(Vector3 p)
    {
        var c = (cam ? cam.transform.position : Camera.main.transform.position);
        float halfH = (cam ? cam.orthographicSize : Camera.main.orthographicSize);
        float halfW = halfH * (cam ? cam.aspect : Camera.main.aspect);

        float x = Mathf.Clamp(p.x, c.x - halfW + cameraPadding, c.x + halfW - cameraPadding);
        float y = Mathf.Clamp(p.y, c.y - halfH + cameraPadding, c.y + halfH - cameraPadding);
        return new Vector3(x, y, 0f);
    }

    Vector3 RandomSpawnPosAroundPlayer()
    {
        // centro: player si existe, si no, centro de cámara
        Vector3 center = player ? player.position :
                         (cam ? cam.transform.position : Camera.main.transform.position);

        // intentos para conseguir un punto válido
        for (int i = 0; i < 20; i++)
        {
            float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float r = Random.Range(minRadius, maxRadius);
            Vector3 pos = center + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * r;

            // evita bordes: mantén el punto dentro de la cámara
            pos = ClampToCamera(pos);

            // asegúrate de que sigue lejos del player tras el clamp
            if (Vector2.Distance(pos, center) >= minRadius * 0.9f)
                return pos;
        }

        // Fallback si no se encontró algo convincente
        return ClampToCamera(center + new Vector3(minRadius, 0f, 0f));
    }

}
