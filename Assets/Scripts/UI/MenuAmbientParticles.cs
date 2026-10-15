using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generador ambiental de partículas/hojas para el Menú Principal.
/// Crea hojas y esporas brillantes flotantes en el Canvas que caen y se mecen suavemente
/// con la brisa del Reino Verde para darle vida dinámica y magia a la pantalla de título.
/// </summary>
public class MenuAmbientParticles : MonoBehaviour
{
    private class ParticleItem
    {
        public RectTransform rect;
        public Image image;
        public float speedY;
        public float speedX;
        public float swayFreq;
        public float swayAmp;
        public float rotationSpeed;
        public float currentAngle;
        public float startX;
    }

    [Header("Configuración de Partículas")]
    [SerializeField] private int maxParticles = 18;
    [SerializeField] private float minSpeed = 25f;
    [SerializeField] private float maxSpeed = 70f;
    [SerializeField] private float screenWidth = 1920f;
    [SerializeField] private float screenHeight = 1080f;

    private List<ParticleItem> particles = new List<ParticleItem>();
    private Sprite particleSprite;

    private void Awake()
    {
        CreateParticleSprite();
    }

    private void Start()
    {
        var parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            var scaler = parentCanvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.referenceResolution.x > 0)
            {
                screenWidth = scaler.referenceResolution.x;
                screenHeight = scaler.referenceResolution.y;
            }
        }

        for (int i = 0; i < maxParticles; i++)
        {
            SpawnParticle(true);
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float halfW = screenWidth * 0.5f;
        float halfH = screenHeight * 0.5f;

        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            Vector2 pos = p.rect.anchoredPosition;

            // Movimiento vertical descendente y oscilación horizontal (viento)
            pos.y -= p.speedY * dt;
            float sway = Mathf.Sin(Time.unscaledTime * p.swayFreq + i) * p.swayAmp;
            pos.x = p.startX + sway - (Time.unscaledTime * p.speedX) % screenWidth;

            // Rotación suave de la hoja
            p.currentAngle += p.rotationSpeed * dt;
            p.rect.localEulerAngles = new Vector3(0, 0, p.currentAngle);

            // Reaparición si sale de la pantalla por abajo o la izquierda
            if (pos.y < -halfH - 50f || pos.x < -halfW - 50f)
            {
                pos.y = halfH + Random.Range(20f, 100f);
                p.startX = Random.Range(-halfW * 0.5f, halfW + 100f);
                pos.x = p.startX;
            }

            p.rect.anchoredPosition = pos;
        }
    }

    private void SpawnParticle(bool randomInitialY)
    {
        GameObject go = new GameObject("Menu_Leaf_Particle");
        go.transform.SetParent(transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        float size = Random.Range(10f, 22f);
        rt.sizeDelta = new Vector2(size, size * 1.5f);

        float halfW = screenWidth * 0.5f;
        float halfH = screenHeight * 0.5f;

        float spawnX = Random.Range(-halfW, halfW);
        float spawnY = randomInitialY ? Random.Range(-halfH, halfH) : (halfH + Random.Range(10f, 60f));
        rt.anchoredPosition = new Vector2(spawnX, spawnY);

        Image img = go.AddComponent<Image>();
        img.sprite = particleSprite;
        img.raycastTarget = false; // No bloquea clics

        // Colores de la paleta: esmeralda suave, dorado brillante y verde musgo con transparencia
        Color[] palette = new Color[]
        {
            new Color(0.35f, 0.85f, 0.45f, Random.Range(0.35f, 0.70f)), // Hoja verde
            new Color(0.85f, 0.95f, 0.40f, Random.Range(0.30f, 0.65f)), // Brote dorado
            new Color(0.20f, 0.70f, 0.55f, Random.Range(0.25f, 0.55f))  // Espora mágica
        };
        img.color = palette[Random.Range(0, palette.Length)];

        ParticleItem item = new ParticleItem
        {
            rect = rt,
            image = img,
            speedY = Random.Range(minSpeed, maxSpeed),
            speedX = Random.Range(15f, 40f),
            swayFreq = Random.Range(1.2f, 2.5f),
            swayAmp = Random.Range(20f, 60f),
            rotationSpeed = Random.Range(-60f, 60f),
            currentAngle = Random.Range(0f, 360f),
            startX = spawnX
        };

        particles.Add(item);
    }

    private void CreateParticleSprite()
    {
        if (particleSprite != null) return;

        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Forma estilizada de hoja (ovalada cónica)
                float dx = (x - center.x) / (size * 0.35f);
                float dy = (y - center.y) / (size * 0.55f);
                float distSq = dx * dx + dy * dy;
                float alpha = Mathf.Clamp01(1f - distSq);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }
        tex.Apply();
        particleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
