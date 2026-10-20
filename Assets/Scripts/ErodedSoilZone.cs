using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zona de suelo erosionado (Nivel 1: Bosque Tropical).
/// Mecánica ambiental actualizada:
/// - Cada segundo que la erosión permanezca activa, incrementa la temperatura ambiental (+0.1°C)
///   y proyecta un texto rojo flotante que asciende y desaparece gradualmente.
/// - Ya no requiere presionar 'E': basta con acercarse a la zona para canalizar y restaurar el suelo.
/// - Al completarse, siembra un árbol, reduce la temperatura del termómetro y cura vida al jugador.
/// </summary>
public class ErodedSoilZone : MonoBehaviour
{
    [Header("Detección de Proximidad")]
    [Tooltip("Distancia máxima en unidades de mundo para detectar al jugador y restaurar automáticamente.")]
    [SerializeField] private float interactionRadius = 2.4f;

    [Tooltip("Indicador visual flotante sobre la zona.")]
    [SerializeField] private WorldInteractionPrompt promptUI;

    [Header("Calentamiento Continuo (+0.1°C / seg)")]
    [Tooltip("Incremento de temperatura por cada segundo que la erosión esté sin restaurar.")]
    [SerializeField] private float temperaturePerSecond = 0.1f;

    [Tooltip("Intervalo en segundos entre cada aumento de temperatura.")]
    [SerializeField] private float tickInterval = 1.0f;

    [Header("Restauración y Siembra")]
    [Tooltip("Tiempo en segundos permaneciendo cerca para restaurar la zona por completo.")]
    [SerializeField, Min(0.1f)] private float requiredTime = 2.0f;

    [Tooltip("Cantidad de temperatura/CO2 reducida al sanar la tierra.")]
    [SerializeField, Min(0f)] private float co2ReductionAmount = 8.0f;

    [Tooltip("Cantidad de vida que recupera el jugador al sembrar el árbol.")]
    [SerializeField] private float playerHealAmount = 15.0f;

    [SerializeField] private GameObject treePrefab;

    [Header("UI de progreso heredada (opcional)")]
    [SerializeField] private Slider progressBarUI;

    private readonly HashSet<Collider2D> playerColliders = new HashSet<Collider2D>();
    private readonly List<GameObject> activeFloatingTexts = new List<GameObject>();
    private float currentProgress;
    private float tickTimer = 0f;
    private bool isCompleted;
    private bool playerInRange;
    private bool isRegisteredInHazard = false;
    private bool isRegisteredInCO2 = false;

    public bool IsCompleted => isCompleted;
    public float CurrentProgress => currentProgress;

    private void Awake()
    {
        if (promptUI == null)
        {
            promptUI = GetComponent<WorldInteractionPrompt>() ?? GetComponentInChildren<WorldInteractionPrompt>();
            if (promptUI == null)
            {
                promptUI = gameObject.AddComponent<WorldInteractionPrompt>();
            }
        }

        if (promptUI != null)
        {
            promptUI.SetActionText("Restaurando suelo...");
            promptUI.SetKeyText("🌱");
            promptUI.SetAlwaysShowCountdown(false);
            promptUI.SetCountdown(-1f, 1f);
        }

        UpdateProgressUI(false);
    }

    private void OnEnable()
    {
        if (isCompleted) return;
        RegisterInManagers();
    }

    private void Start()
    {
        RegisterInManagers();

        if (promptUI != null)
        {
            promptUI.SetActionText("Restaurando suelo...");
            promptUI.SetKeyText("🌱");
            promptUI.SetAlwaysShowCountdown(false);
            promptUI.SetCountdown(-1f, 1f);
        }
    }

    private void OnDisable()
    {
        UnregisterFromManagers();
        CleanupFloatingTexts();
    }

    private void OnDestroy()
    {
        CleanupFloatingTexts();
    }

    private void Update()
    {
        if (isCompleted) return;

        // 1. CALENTAMIENTO CONTINUO POR CADA SEGUNDO DE EROSIÓN ACTIVA (+0.1°C)
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            ApplyContinuousTemperaturePenalty();
        }

        // 2. DETECCIÓN AUTOMÁTICA DE PROXIMIDAD
        CheckPlayerProximity();

        // 3. RESTAURACIÓN AUTOMÁTICA POR PROXIMIDAD (Sin necesidad de presionar 'E')
        if (playerInRange)
        {
            currentProgress = Mathf.Min(requiredTime, currentProgress + Time.deltaTime);
            UpdateProgressUI(true);

            if (promptUI != null)
            {
                promptUI.SetPromptVisible(true);
                promptUI.SetKeyHeld(true);
                promptUI.SetProgress(requiredTime <= 0f ? 1f : (currentProgress / requiredTime));
            }

            if (currentProgress >= requiredTime)
            {
                CompletePlanting();
            }
        }
        else
        {
            // Decaimiento suave si el jugador se aleja
            if (currentProgress > 0f)
            {
                currentProgress = Mathf.Max(0f, currentProgress - Time.deltaTime * 0.75f);
            }

            if (promptUI != null)
            {
                promptUI.SetPromptVisible(currentProgress > 0f);
                promptUI.SetKeyHeld(false);
                promptUI.SetProgress(requiredTime <= 0f ? 0f : (currentProgress / requiredTime));
            }

            UpdateProgressUI(currentProgress > 0f);
        }
    }

    private void ApplyContinuousTemperaturePenalty()
    {
        if (HazardMeterManager.Instance != null)
        {
            HazardMeterManager.Instance.IncreaseHazard(temperaturePerSecond);
        }
        else if (CO2Manager.Instance != null)
        {
            CO2Manager.Instance.IncreaseCO2(temperaturePerSecond * 2f);
        }

        // Proyectar texto rojo ascendente que desaparece suavemente
        SpawnFloatingTemperatureText($"+{temperaturePerSecond:0.0}°C");
    }

    private void SpawnFloatingTemperatureText(string text)
    {
        GameObject textGO = new GameObject("FloatingTempPenalty");
        textGO.transform.position = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 0.85f, 0f);

        Canvas canvas = textGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 75;

        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(140f, 30f);
        textGO.transform.localScale = Vector3.one * 0.024f;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.2f, 0.2f, 1f); // Rojo vivo
        tmp.outlineColor = new Color32(40, 0, 0, 220);
        tmp.outlineWidth = 0.25f;

        // El componente corre en textGO mismo para nunca quedar huérfano si la zona es destruida
        FloatingPenaltyText fpt = textGO.AddComponent<FloatingPenaltyText>();
        fpt.Init(tmp);

        activeFloatingTexts.Add(textGO);
    }

    private void CleanupFloatingTexts()
    {
        for (int i = 0; i < activeFloatingTexts.Count; i++)
        {
            if (activeFloatingTexts[i] != null)
            {
                Destroy(activeFloatingTexts[i]);
            }
        }
        activeFloatingTexts.Clear();
    }

    private void CheckPlayerProximity()
    {
        bool detected = false;

        // Comprobación 1: Colliders trigger
        if (playerColliders.Count > 0)
        {
            detected = true;
        }
        else
        {
            // Comprobación 2: OverlapCircle físico
            Collider2D hit = Physics2D.OverlapCircle(transform.position, interactionRadius);
            if (hit != null && hit.CompareTag("Player"))
            {
                detected = true;
            }
            else
            {
                // Comprobación 3: Distancia directa al Player GameObject
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null && Vector2.Distance(transform.position, player.transform.position) <= interactionRadius)
                {
                    detected = true;
                }
            }
        }

        playerInRange = detected;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCompleted || !other.CompareTag("Player"))
            return;

        playerColliders.Add(other);
        playerInRange = true;
        if (promptUI != null)
            promptUI.SetPromptVisible(true);

        UpdateProgressUI(currentProgress > 0f);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerColliders.Remove(other);

        if (playerColliders.Count == 0)
        {
            // Comprobación de seguridad: si el jugador sigue dentro del radio de interacción, ignorar salidas por ataque
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null && Vector2.Distance(transform.position, player.transform.position) <= interactionRadius)
            {
                return;
            }

            playerInRange = false;
            if (promptUI != null)
            {
                promptUI.SetPromptVisible(false);
                promptUI.SetKeyHeld(false);
            }
            UpdateProgressUI(false);
        }
    }

    private void CompletePlanting()
    {
        isCompleted = true;
        playerColliders.Clear();
        playerInRange = false;
        UpdateProgressUI(false);
        CleanupFloatingTexts(); // Eliminar de inmediato cualquier texto flotante residual antes de que brote el árbol

        if (promptUI != null)
        {
            promptUI.SetPromptVisible(false);
        }

        if (treePrefab != null)
        {
            GameObject spawnedObj = Instantiate(treePrefab, transform.position, Quaternion.identity);
            if (spawnedObj != null && spawnedObj.GetComponent<RestoredTreeInteraction>() == null)
            {
                spawnedObj.AddComponent<RestoredTreeInteraction>();
            }
        }
        else
        {
            Debug.LogWarning("[ErodedSoilZone] treePrefab no está asignado.", this);
        }

        UnregisterFromManagers();

        // 1. Reducir la temperatura/peligro ambiental
        if (HazardMeterManager.Instance != null)
        {
            float reduction = (co2ReductionAmount > 0f) ? co2ReductionAmount : HazardMeterManager.Instance.DefaultHazardReduction;
            HazardMeterManager.Instance.ReduceHazard(reduction);
        }

        if (CO2Manager.Instance != null)
        {
            CO2Manager.Instance.ReduceCO2(co2ReductionAmount);
        }

        // 2. Curar vida al jugador por haber restaurado el suelo
        if (playerHealAmount > 0f)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                BarraVida barra = player.GetComponent<BarraVida>();
                if (barra != null)
                {
                    barra.Curar(playerHealAmount);
                    Debug.Log($"[ErodedSoilZone] Suelo restaurado con éxito. Curando al jugador +{playerHealAmount} HP.");
                }
            }
        }

        Destroy(gameObject);
    }

    private void RegisterInManagers()
    {
        if (isCompleted) return;

        if (!isRegisteredInHazard)
        {
            if (HazardMeterManager.Instance == null)
            {
                HazardMeterManager found = FindFirstObjectByType<HazardMeterManager>();
                if (found == null)
                {
                    GameObject mgrGO = new GameObject("HazardMeterManager");
                    found = mgrGO.AddComponent<HazardMeterManager>();
                    string sName = gameObject.scene.name;
                    EcosystemType eco = EcosystemType.TropicalForest;
                    if (sName.Contains("Ice")) eco = EcosystemType.PolarIce;
                    else if (sName.Contains("Beach")) eco = EcosystemType.SunnyBeach;
                    else if (sName.Contains("Final")) eco = EcosystemType.FinalBossUlti;
                    found.ApplyEcosystemPreset(eco);
                }
            }

            if (HazardMeterManager.Instance != null)
            {
                HazardMeterManager.Instance.RegisterDamageZone(this);
                isRegisteredInHazard = true;
            }
        }

        if (!isRegisteredInCO2 && CO2Manager.Instance != null)
        {
            CO2Manager.Instance.RegisterActiveZone();
            isRegisteredInCO2 = true;
        }
    }

    private void UnregisterFromManagers()
    {
        if (isRegisteredInHazard && HazardMeterManager.Instance != null)
        {
            HazardMeterManager.Instance.UnregisterDamageZone(this);
            isRegisteredInHazard = false;
        }

        if (isRegisteredInCO2 && CO2Manager.Instance != null)
        {
            CO2Manager.Instance.UnregisterActiveZone();
            isRegisteredInCO2 = false;
        }
    }

    private void UpdateProgressUI(bool visible)
    {
        if (progressBarUI == null)
            return;

        // Ocultar si está en pausa o hay overlay modal activo (menú de muerte o pausa)
        if (Mathf.Approximately(Time.timeScale, 0f) || (UIManager.Instance != null && !UIManager.Instance.IsHUDVisible))
        {
            visible = false;
        }

        progressBarUI.gameObject.SetActive(visible && !isCompleted);
        progressBarUI.minValue = 0f;
        progressBarUI.maxValue = 1f;
        progressBarUI.value = requiredTime <= 0f ? 1f : Mathf.Clamp01(currentProgress / requiredTime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }

    private void OnValidate()
    {
        requiredTime = Mathf.Max(0.1f, requiredTime);
        co2ReductionAmount = Mathf.Max(0f, co2ReductionAmount);
        temperaturePerSecond = Mathf.Max(0.01f, temperaturePerSecond);
        tickInterval = Mathf.Max(0.1f, tickInterval);
        interactionRadius = Mathf.Max(0.5f, interactionRadius);
        playerHealAmount = Mathf.Max(0f, playerHealAmount);
    }
#endif
}

/// <summary>
/// Componente autosuficiente para animar y autodestruir el texto flotante de penalización térmica.
/// Evita que el GameObject quede congelado en la escena si la zona de erosión es destruida.
/// </summary>
public class FloatingPenaltyText : MonoBehaviour
{
    private TextMeshProUGUI tmp;

    public void Init(TextMeshProUGUI textMesh)
    {
        tmp = textMesh;
        StartCoroutine(AnimateAndDestroy());
    }

    private IEnumerator AnimateAndDestroy()
    {
        float duration = 1.35f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * 1.1f;
        Color startColor = (tmp != null) ? tmp.color : Color.red;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));

            if (tmp != null)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t * t);
                tmp.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
