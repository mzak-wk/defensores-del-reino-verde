using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Gestiona la secuencia de victoria tras derrotar al Jefe.
/// Bloquea al jugador, limpia la escena de enemigos/proyectiles, reproduce la cinemática de muerte del jefe
/// y finalmente despliega la pantalla de recompensas desinhibiendo el cursor del ratón.
/// </summary>
public class BossDefeatManager : MonoBehaviour
{
    [Header("Referencias de Cinemática y UI")]
    [Tooltip("PlayableDirector asignado con la cinemática de derrota/muerte del jefe.")]
    [SerializeField] private PlayableDirector defeatCinematic;

    [Tooltip("Panel de interfaz de usuario (BossRewardUI) que se activará al concluir la cinemática.")]
    [SerializeField] private GameObject rewardPanel;

    [Header("Configuración de Limpieza de Escena")]
    [Tooltip("Si es true, buscará y destruirá automáticamente los objetos con Tag 'Enemy' y 'Projectile' al iniciar la derrota.")]
    [SerializeField] private bool cleanEnemiesAndProjectiles = true;

    private PlayerMovement playerMovement;

    private void Start()
    {
        // Asegurar que el panel de recompensas esté oculto al iniciar
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Inicia la secuencia de derrota del Jefe.
    /// Puede ser invocado por el evento de muerte del BossHealth o por un UnityEvent.
    /// </summary>
    public void StartDefeatSequence()
    {
        Debug.Log("[BossDefeatManager] ¡Jefe derrotado! Iniciando secuencia de victoria.");

        // 1. Bloquear el movimiento del jugador
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.SetMovementLocked(true);
        }
        else
        {
            Debug.LogWarning("[BossDefeatManager] PlayerMovement no encontrado para bloquear movimiento.");
        }

        // 2. Limpiar proyectiles y enemigos secundarios si está activado
        if (cleanEnemiesAndProjectiles)
        {
            CleanSceneEntities();
        }

        // 3. Reproducir cinemática de victoria y suscribirse al evento de finalización
        if (defeatCinematic != null)
        {
            defeatCinematic.stopped += OnCinematicFinished;
            defeatCinematic.Play();
        }
        else
        {
            Debug.LogWarning("[BossDefeatManager] PlayableDirector no asignado. Mostrando UI de recompensa inmediatamente.");
            OnCinematicFinished(null);
        }
    }

    /// <summary>
    /// Callback ejecutado cuando la cinemática finaliza.
    /// </summary>
    private void OnCinematicFinished(PlayableDirector director)
    {
        // Desuscribir para prevenir memory leaks
        UnsubscribeFromCinematic();

        // 1. Activar el panel de recompensas
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("[BossDefeatManager] Panel de Recompensas no asignado en el Inspector.");
        }

        // 2. Mostrar y desbloquear el cursor del mouse para interactuar con la UI
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("[BossDefeatManager] Cinemática finalizada. Panel de recompensas activado y cursor liberado.");
    }

    /// <summary>
    /// Elimina proyectiles activos y enemigos comunes secundarios en el área para evitar interferencias durante la cinemática.
    /// </summary>
    private void CleanSceneEntities()
    {
        // Destruir proyectiles
        GameObject[] projectiles = GameObject.FindGameObjectsWithTag("Projectile");
        foreach (GameObject proj in projectiles)
        {
            Destroy(proj);
        }

        // Destruir u ocultar enemigos secundarios
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            // Evitar destruir la raíz del boss si el boss tiene la etiqueta Enemy
            if (enemy != gameObject && enemy != defeatCinematic?.gameObject)
            {
                Destroy(enemy);
            }
        }
    }

    private void UnsubscribeFromCinematic()
    {
        if (defeatCinematic != null)
        {
            defeatCinematic.stopped -= OnCinematicFinished;
        }
    }

    private void OnDestroy()
    {
        // Desuscripción de seguridad ante destrucción del objeto
        UnsubscribeFromCinematic();
    }
}
