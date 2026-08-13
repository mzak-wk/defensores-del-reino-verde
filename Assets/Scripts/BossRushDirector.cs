using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossRushDirector : MonoBehaviour
{
    public static BossRushDirector Instance { get; private set; }

    [Serializable]
    public class BossZoneConfig
    {
        public string zoneName;
        public Transform zoneCenter;
        public Transform bossSpawnPoint;
        public GameObject bossPrefab;
        public BossStatsOverride statsOverride;
        public DialogueData introDialogue;
        public Sprite backgroundSprite;
        public AudioClip zoneMusic;
        public GameObject leftWall;
        public GameObject rightWall;
        public BoxCollider2D entryTrigger;
        public bool isFinalZone;
    }

    [Header("Zonas")]
    public BossZoneConfig[] zones;

    [Header("Referencias")]
    public SpriteRenderer backgroundRenderer;
    public BossHealthBarUI bossHealthBar;
    public Transform playerSpawnPoint;

    [Header("Cinemática Final")]
    public string cinematicSceneName = "CinematicScene";

    [Header("Audio")]
    public AudioSource musicSource;

    private int currentZoneIndex = -1;
    private PlayerMovement playerMovement;
    private GameObject currentBossInstance;
    private BossAIController currentBossAI;
    private BossHealth currentBossHealth;
    private bool waitingForBossDefeat;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();

        if (playerMovement != null && playerSpawnPoint != null)
        {
            playerMovement.transform.position = playerSpawnPoint.position;
        }

        if (CameraFollow.Instance != null && zones.Length > 0 && zones[0].zoneCenter != null)
        {
            CameraFollow.Instance.SetTargetZone(zones[0].zoneCenter.position);
            CameraFollow.Instance.SnapToZone();
        }

        SetupZoneTriggers();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void SetupZoneTriggers()
    {
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i].entryTrigger != null)
            {
                var triggerGO = zones[i].entryTrigger.gameObject;
                var zoneTrigger = triggerGO.GetComponent<BossRushZoneTrigger>();
                if (zoneTrigger == null)
                    zoneTrigger = triggerGO.AddComponent<BossRushZoneTrigger>();
                zoneTrigger.Init(this, i);
            }
        }
    }

    public void OnZoneTriggered(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= zones.Length) return;
        if (currentBossInstance != null) return;

        currentZoneIndex = zoneIndex;
        BossZoneConfig zone = zones[zoneIndex];

        LockPlayer();

        if (zone.leftWall != null) zone.leftWall.SetActive(true);
        if (zone.rightWall != null) zone.rightWall.SetActive(true);

        if (CameraFollow.Instance != null && zone.zoneCenter != null)
        {
            CameraFollow.Instance.SetTargetZone(zone.zoneCenter.position);
            CameraFollow.Instance.SnapToZone();
        }

        if (backgroundRenderer != null && zone.backgroundSprite != null)
            backgroundRenderer.sprite = zone.backgroundSprite;

        if (musicSource != null && zone.zoneMusic != null)
        {
            musicSource.clip = zone.zoneMusic;
            musicSource.Play();
        }

        if (zone.bossPrefab != null && zone.bossSpawnPoint != null)
        {
            currentBossInstance = Instantiate(zone.bossPrefab, zone.bossSpawnPoint.position, Quaternion.identity);

            currentBossAI = currentBossInstance.GetComponent<BossAIController>();
            if (currentBossAI != null && zone.statsOverride != null)
                currentBossAI.SetStatsOverride(zone.statsOverride);

            currentBossHealth = currentBossInstance.GetComponent<BossHealth>();
        }
        else if (zone.bossPrefab == null)
        {
            currentBossInstance = zone.bossSpawnPoint != null ? zone.bossSpawnPoint.gameObject : null;
            if (currentBossInstance != null)
                currentBossInstance.SetActive(true);

            currentBossAI = currentBossInstance?.GetComponent<BossAIController>();
            if (currentBossAI != null && zone.statsOverride != null)
                currentBossAI.SetStatsOverride(zone.statsOverride);

            currentBossHealth = currentBossInstance?.GetComponent<BossHealth>();
        }

        if (zone.introDialogue != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueClosed += OnDialogueFinished;
            DialogueManager.Instance.StartDialogue(zone.introDialogue, playerMovement);
        }
        else
        {
            OnDialogueFinished();
        }
    }

    private void OnDialogueFinished()
    {
        DialogueManager.Instance.OnDialogueClosed -= OnDialogueFinished;

        UnlockPlayer();

        if (currentBossAI != null)
            currentBossAI.Activate();

        if (bossHealthBar != null && currentBossHealth != null && currentZoneIndex >= 0 && currentZoneIndex < zones.Length)
        {
            bossHealthBar.Show(currentBossHealth, zones[currentZoneIndex].statsOverride != null ? zones[currentZoneIndex].statsOverride.bossName : "Boss");
        }

        if (currentBossHealth != null)
        {
            waitingForBossDefeat = true;
            currentBossHealth.OnBossDeath += OnBossDefeated;
        }
    }

    private void OnBossDefeated()
    {
        if (!waitingForBossDefeat) return;
        waitingForBossDefeat = false;

        if (currentBossHealth != null)
            currentBossHealth.OnBossDeath -= OnBossDefeated;

        if (bossHealthBar != null)
            bossHealthBar.Hide();

        if (currentBossAI != null)
            currentBossAI.Deactivate();

        LockPlayer();

        if (currentBossInstance != null)
        {
            Destroy(currentBossInstance, 0.1f);
            currentBossInstance = null;
        }

        if (currentZoneIndex >= 0 && currentZoneIndex < zones.Length)
        {
            BossZoneConfig zone = zones[currentZoneIndex];
            if (zone.leftWall != null) zone.leftWall.SetActive(false);
            if (zone.rightWall != null) zone.rightWall.SetActive(false);

            if (zone.isFinalZone)
            {
                TriggerVictoryCinematic();
                return;
            }
        }

        StartCoroutine(UnlockAfterDelay(1f));
    }

    private IEnumerator UnlockAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UnlockPlayer();
    }

    private void TriggerVictoryCinematic()
    {
        if (GameSession.Instance != null)
            GameSession.Instance.ConsolidateRunRewards();

        StartCoroutine(LoadCinematicScene());
    }

    private IEnumerator LoadCinematicScene()
    {
        yield return new WaitForSeconds(2f);

        if (Application.CanStreamedLevelBeLoaded(cinematicSceneName))
        {
            SceneManager.LoadScene(cinematicSceneName);
        }
        else
        {
            Debug.LogError($"[BossRushDirector] Scene '{cinematicSceneName}' not found in Build Settings.");
        }
    }

    private void LockPlayer()
    {
        if (playerMovement != null)
            playerMovement.SetMovementLocked(true);
    }

    private void UnlockPlayer()
    {
        if (playerMovement != null)
            playerMovement.SetMovementLocked(false);
    }
}

public class BossRushZoneTrigger : MonoBehaviour
{
    private BossRushDirector director;
    private int zoneIndex;
    private bool triggered;

    public void Init(BossRushDirector dir, int index)
    {
        director = dir;
        zoneIndex = index;
        triggered = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        if (director == null) return;

        triggered = true;
        director.OnZoneTriggered(zoneIndex);
    }
}
