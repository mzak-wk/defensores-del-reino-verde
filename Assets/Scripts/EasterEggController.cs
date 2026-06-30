using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EasterEggController : MonoBehaviour
{
    [Header("Configuración del Easter Egg")]
    [Tooltip("Tiempo máximo en segundos que el jugador estará en la escena si no hay audio.")]
    public float maxDurationSeconds = 10.0f; 

    [Tooltip("Nombre exacto de la escena del menú principal.")]
    public string menuSceneName = "MenuScene"; 

    [Header("Referencias de Audio")]
    [Tooltip("Arrastra el AudioSource que contiene la canción del Easter Egg.")]
    public AudioSource audioSource;

    private bool isFinished = false; // Bandera para asegurar que solo cargamos una vez

    void Start()
    {
        // Inicializa audio si no se asignó en el Inspector
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Iniciar la lógica de espera basada en el AudioSource o el tiempo máximo.
        StartCoroutine(HandleEasterEggFlow());

        // Iniciar el audio si está presente y no está sonando.
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void Update()
    {
        // 1. Detección de la tecla ESCAPE para saltar
        // Usamos GetKeyDown(KeyCode.Escape) para una detección directa del evento de presión.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Si el jugador presiona ESC, saltamos inmediatamente.
            Debug.Log("[EasterEggController] Tecla ESC presionada. Saltando escena.");
            StopAllCoroutines(); // Detenemos la espera del audio/tiempo.
            LoadMenu();
        }
    }

    IEnumerator HandleEasterEggFlow()
    {
        float timer = 0f;
        
        if (audioSource != null && audioSource.clip != null)
        {
            // 1. Esperar a que el AudioSource termine de sonar
            Debug.Log("[EasterEggController] Esperando que termine el AudioSource...");
            
            // Esperamos mientras el audio esté sonando Y no se exceda el tiempo máximo.
            while (audioSource.isPlaying && timer < maxDurationSeconds)
            {
                yield return null; 
                timer += Time.deltaTime;
            }

            // Si el audio terminó por sí solo (y no por tiempo/ESC), cargamos el menú.
            if (!audioSource.isPlaying && !isFinished)
            {
                Debug.Log("[EasterEggController] Audio terminado. Redirigiendo.");
                LoadMenu();
                yield break;
            }
        }
        else
        {
            // 2. Si no hay AudioSource, esperamos el tiempo máximo
            Debug.Log($"[EasterEggController] No hay AudioSource o clip. Esperando tiempo máximo: {maxDurationSeconds} segundos.");
            yield return new WaitForSeconds(maxDurationSeconds);
        }

        // 3. Finalmente, si el loop terminó por tiempo (timer >= maxDurationSeconds) 
        // o no había audio, y no hemos cargado el menú ya (con ESC), cargamos.
        if (!isFinished)
        {
            LoadMenu();
        }
    }

    void LoadMenu()
    {
        if (isFinished) return;
        isFinished = true; // Establecemos la bandera AHORA para prevenir ejecuciones dobles.
        
        Debug.Log($"[EasterEggController] Cargando {menuSceneName}.");
        
        if (Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogError($"[EasterEggController] ¡Error! La escena '{menuSceneName}' no está en Build Settings. (MenuScene)");
        }
    }
}