using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class CoinPickup : MonoBehaviour
{
    [Header("Valor")]
    public int amount = 1;
    public float lifetime = 0f; // 0 = infinito

    [Header("Animación de Suelo")]
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float bobHeight = 0.12f;

    [Header("Atracción Magnética")]
    [SerializeField] private float baseMagnetRange = 3f;
    [SerializeField] private float magnetSpeed = 8f;

    private Vector3 initialPosition;
    private float timeOffset;
    private Transform playerTransform;
    private bool isBeingPulled = false;
    private bool isCollected = false;

    private void Awake()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;

        var rb = GetComponent<Rigidbody2D>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.simulated = true;

        initialPosition = transform.position;
        timeOffset = Random.value * Mathf.PI * 2f;
    }

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    private void Update()
    {
        if (isCollected) return;

        if (lifetime > 0f)
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (playerTransform == null)
        {
            FindPlayer();
        }

        // Atracción magnética si el jugador está cerca
        if (playerTransform != null)
        {
            float bonusRange = 0f;
            if (GameSession.Instance != null)
            {
                bonusRange = GameSession.Instance.GetMagnetRadiusBonus() 
                           + (GameSession.Instance.GetTotalPickupRangeBonusPercent() * 0.05f);
            }

            float currentMagnetRange = baseMagnetRange + bonusRange;
            float dist = Vector2.Distance(transform.position, playerTransform.position);

            if (dist <= currentMagnetRange)
            {
                isBeingPulled = true;
            }

            if (isBeingPulled)
            {
                float speed = magnetSpeed + (GameSession.Instance ? GameSession.Instance.GetMagnetPullBonus() : 0f);
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    playerTransform.position,
                    speed * Time.deltaTime
                );
                return;
            }
        }

        // Flotación suave cuando está en el suelo
        if (!isBeingPulled)
        {
            float newY = initialPosition.y + Mathf.Sin((Time.time * bobSpeed) + timeOffset) * bobHeight;
            transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;

        Collect();
    }

    private void Collect()
    {
        isCollected = true;

        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.AddGold(amount);
        }
        else if (GameSession.Instance != null)
        {
            GameSession.Instance.gold += amount;
            GameSession.Instance.AddTempGold(amount);
        }

        SpawnFloatingCoinText(amount, transform.position);
        Destroy(gameObject);
    }

    private void SpawnFloatingCoinText(int coinAmount, Vector3 pos)
    {
        GameObject go = new GameObject("FloatingCoinText");
        go.transform.position = pos + Vector3.up * 0.4f;

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 300;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(3f, 1f);
        rt.localScale = Vector3.one * 0.02f;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = $"+{coinAmount}";
        tmp.fontSize = 32;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.85f, 0.2f, 1f); // Dorado brillante
        tmp.fontStyle = FontStyles.Bold;

        // Auto-destrucción y animación suave
        CoinFloatingTextAnimator animator = go.AddComponent<CoinFloatingTextAnimator>();
        animator.Init(tmp);
    }
}

/// <summary>
/// Componente auxiliar para animar el texto flotante de monedas hacia arriba y desvanecerlo.
/// </summary>
public class CoinFloatingTextAnimator : MonoBehaviour
{
    private TextMeshProUGUI tmp;
    private float duration = 0.8f;
    private float floatSpeed = 1.2f;

    public void Init(TextMeshProUGUI text)
    {
        tmp = text;
        StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Color startColor = tmp.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = startPos + Vector3.up * (floatSpeed * elapsed);

            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t * t);
            tmp.color = c;

            yield return null;
        }

        Destroy(gameObject);
    }
}
