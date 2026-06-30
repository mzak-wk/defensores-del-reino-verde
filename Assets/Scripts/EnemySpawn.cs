using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public ExperienceSystem xpSystem;

    [Header("Prefabs")]
    public Enemy enemyNormalPrefab;
    public Enemy enemyMiniBossPrefab;
    public Enemy enemyBossPrefab;

    [Header("Base - Normal")]
    public float normalBaseHP = 50f;
    public float normalBaseSpeed = 1.6f;
    public int normalBaseXP = 5;
    public float normalContactDamage = 5f;
    public float normalRatePerSec = 0.8f;
    public float offscreenMargin = 2f;

    [Header("Escalado")]
    public float speedIncreasePerMinute = 0.1f;
    public float rateIncreasePerMinute = 0.1f;

    [Header("Escalado extra por minuto (x^min)")]
    public float normalHPPerMinute = 1.5f;
    public float normalDmgPerMinute = 1.5f;
    public float miniHPPerMinute = 1.2f;
    public float miniDmgPerMinute = 1.2f;

    [Header("Mini-jefe")]
    public float miniBossBaseHP = 300f;
    public float miniBossBaseSpeed = 1.8f;
    public int miniBossBaseXP = 50;
    public float miniBossContactDamage = 10f;

    [Header("Jefe")]
    public float bossSpawnMinute = 10f;
    public float bossHP = 5000f;
    public float bossSpeed = 1.9f;
    public int bossXP = 1000;
    public float bossContactDamage = 20f;

    float startTime;

    // NUEVOS campos para controlar coroutines y estado de boss phase
    Coroutine normalLoopCoroutine;
    Coroutine miniLoopCoroutine;
    Coroutine bossCoroutine;
    bool bossPhaseStarted = false;
    float timeScaleLocked = 0f;
    //bool bossSpawned = false;

    void Start()
    {
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!xpSystem) xpSystem = Object.FindFirstObjectByType<ExperienceSystem>();

        if (!player) Debug.LogError("[EnemySpawner] Falta asignar Player.");
        if (!xpSystem) Debug.LogError("[EnemySpawner] Falta asignar ExperienceSystem.");
        if (!enemyNormalPrefab) Debug.LogError("[EnemySpawner] Falta asignar Enemy Normal Prefab.");
        if (!enemyMiniBossPrefab) Debug.LogWarning("[EnemySpawner] MiniBoss Prefab no asignado (se omitirá).");
        if (!enemyBossPrefab) Debug.LogWarning("[EnemySpawner] Boss Prefab no asignado (opcional).");
        if (!CameraBounds2D.Instance) Debug.LogError("[EnemySpawner] No hay CameraBounds2D en la cámara.");

        // Si existe GameSession, mantener continuidad del tiempo
        if (GameSession.Instance != null)
        {
            startTime = Time.time - GameSession.Instance.totalMinutesPlayed * 60f;
        }
        else
        {
            startTime = Time.time;
        }

        // arrancar coroutines y guardar referencias
        normalLoopCoroutine = StartCoroutine(NormalLoop());
        if (enemyMiniBossPrefab) miniLoopCoroutine = StartCoroutine(MiniBossLoop());
    }

    float ElapsedMinutes()
    {
        float minutes = (Time.time - startTime) / 60f;

        // Guardar minutos globales en GameSession
        if (GameSession.Instance != null)
            GameSession.Instance.totalMinutesPlayed = minutes;

        return minutes;
    }

    // MODIFICACIÓN: Ahora MinutesInt usa el tiempo bloqueado si la fase de jefe comenzó
    int MinutesInt()
    {
        // Si la fase del jefe ha iniciado, usa el tiempo que fue bloqueado
        if (bossPhaseStarted)
            return Mathf.Max(0, Mathf.FloorToInt(timeScaleLocked));

        // De lo contrario, usa el tiempo transcurrido real para el escalado
        return Mathf.Max(0, Mathf.FloorToInt(ElapsedMinutes()));
    }

    IEnumerator NormalLoop()
    {
        float nextSpawnTimer = 0f;
        while (true)
        {
            // SALIR si comienza boss phase
            if (bossPhaseStarted) yield break;

            float minutes = ElapsedMinutes();

            float rate = normalRatePerSec + Mathf.Max(0f, minutes) * rateIncreasePerMinute;
            float timeBetween = 1f / Mathf.Max(0.1f, rate);

            nextSpawnTimer -= Time.deltaTime;
            if (nextSpawnTimer <= 0f)
            {
                // comprobar de nuevo por si boss phase arrancó justo ahora
                if (!bossPhaseStarted) SpawnNormal(minutes);
                nextSpawnTimer = timeBetween;
            }
            yield return null;
        }
    }

    IEnumerator MiniBossLoop()
    {
        int lastWave = -1;
        while (true)
        {
            if (bossPhaseStarted) yield break;

            int waveIndex = Mathf.FloorToInt(ElapsedMinutes() / 2f);
            if (enemyMiniBossPrefab && waveIndex > lastWave && ElapsedMinutes() >= 2f)
            {
                if (!bossPhaseStarted) SpawnMiniBoss(waveIndex);
                lastWave = waveIndex;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // PUBLIC: iniciar la fase del boss desde otro script (ej: LevelTimer.onTimerEnd)
    public void StartBossPhase()
    {
        timeScaleLocked = ElapsedMinutes();

        if (bossPhaseStarted) return;
        bossPhaseStarted = true;

        // Parar coroutines de spawn (si siguen en marcha)
        if (normalLoopCoroutine != null) StopCoroutine(normalLoopCoroutine);
        if (miniLoopCoroutine != null) StopCoroutine(miniLoopCoroutine);
        if (bossCoroutine != null) StopCoroutine(bossCoroutine);

        // 1) Destruir todos los enemigos actuales sin drops
        Enemy[] allEnemies = UnityEngine.Object.FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude,
         FindObjectsSortMode.None
        );

        foreach (var e in allEnemies)
        {
            e.SuppressDropsAndDestroy();
        }


        // 2) No spawnear más normales / minibosses: ya lo impedimos con bossPhaseStarted

        // 3) Spawnear el Boss (si está asignado)
        if (!enemyBossPrefab)
        {
            Debug.LogWarning("[EnemySpawner] StartBossPhase llamado pero no hay enemyBossPrefab asignado.");
            return;
        }

        // Escoge posición y crea boss
        Vector2 pos = GetSpawnPos(offscreenMargin + 2f);
        Enemy b = Instantiate(enemyBossPrefab, pos, Quaternion.identity);
        b.Init(bossHP, bossSpeed, bossXP, bossContactDamage);
        SetupEnemy(b);

        //bossSpawned = true;
        Debug.Log("[EnemySpawner] StartBossPhase: boss spawneado y spawns normales detenidos.");
    }

    Vector2 GetSpawnPos(float margin)
    {
        if (CameraBounds2D.Instance != null)
            return CameraBounds2D.Instance.RandomPointOffscreen(margin);

        Debug.LogWarning("[EnemySpawner] CameraBounds2D.Instance es null, usando fallback.");
        return (Vector2)player.position + (Random.insideUnitCircle.normalized * 8f);
    }

    void SpawnNormal(float minutes)
    {
        if (bossPhaseStarted) return; // seguridad

        if (!enemyNormalPrefab) { Debug.LogError("[EnemySpawner] Normal prefab null."); return; }

        int m = Mathf.Max(0, Mathf.FloorToInt(minutes));

        float hp = normalBaseHP * Mathf.Pow(2f, Mathf.Max(0, m - 2));
        int xp = Mathf.RoundToInt(normalBaseXP * Mathf.Pow(2f, Mathf.Max(0, m - 2)));
        float speed = normalBaseSpeed + Mathf.Max(0f, minutes) * speedIncreasePerMinute;

        // Escalado extra por minuto
        hp *= Mathf.Pow(normalHPPerMinute, m);
        float contactDmg = normalContactDamage * Mathf.Pow(normalDmgPerMinute, m);

        Vector2 pos = GetSpawnPos(offscreenMargin);
        Enemy e = Instantiate(enemyNormalPrefab, pos, Quaternion.identity);
        if (!e) { Debug.LogError("[EnemySpawner] Instantiation devolvió null."); return; }

        e.Init(hp, speed, xp, contactDmg);
        SetupEnemy(e);
    }

    void SpawnMiniBoss(int waveIndex)
    {
        if (bossPhaseStarted) return; // seguridad
        if (!enemyMiniBossPrefab) return;

        int m = MinutesInt();

        float hp = miniBossBaseHP * Mathf.Pow(2f, Mathf.Max(0, waveIndex));
        int xp = Mathf.RoundToInt(miniBossBaseXP * Mathf.Pow(2f, Mathf.Max(0, waveIndex)));
        float speed = miniBossBaseSpeed + (waveIndex * 0.1f);

        hp *= Mathf.Pow(miniHPPerMinute, m);
        float contactDmg = miniBossContactDamage * Mathf.Pow(miniDmgPerMinute, m);

        Vector2 pos = GetSpawnPos(offscreenMargin + 1f);
        Enemy e = Instantiate(enemyMiniBossPrefab, pos, Quaternion.identity);
        if (!e) { Debug.LogError("[EnemySpawner] Instantiation (MiniBoss) devolvió null."); return; }

        e.Init(hp, speed, xp, contactDmg);
        SetupEnemy(e);
    }

    void SetupEnemy(Enemy e)
    {
        // Perseguir al jugador
        var chase = e.GetComponent<EnemyChase>();
        if (chase) chase.SetTarget(player);
        else Debug.LogWarning("[EnemySpawner] El prefab no tiene EnemyChase.");

        // No tocar XP aquí (tu ExperienceSystem ya lo maneja si usas OnDeath)
    }
}
