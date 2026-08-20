using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UIManager centralizado para controlar la visibilidad del HUD y la prioridad/bloqueo de overlays (Pausa, Diálogo, Game Over, Tienda).
/// Persiste entre escenas (DontDestroyOnLoad).
/// </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<UIManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("[UIManager]");
                    _instance = go.AddComponent<UIManager>();
                }
            }
            return _instance;
        }
    }

    private GameObject hudRoot;
    private readonly Dictionary<int, bool> savedHudChildStates = new Dictionary<int, bool>();
    private readonly Dictionary<string, Action> registeredOverlays = new Dictionary<string, Action>();

    public int OverlayCount => registeredOverlays.Count;
    public bool IsGameOver { get; private set; } = false;
    public bool CanPause => OverlayCount == 0 && !IsGameOver;
    public bool IsHUDVisible => OverlayCount == 0;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ==========================================
    // GESTIÓN DEL HUD ROOT
    // ==========================================

    /// <summary>
    /// Registra el GameObject raíz del HUD de la escena actual.
    /// </summary>
    public void RegisterHUD(GameObject hudRootObject)
    {
        hudRoot = hudRootObject;
        savedHudChildStates.Clear();

        // Si ya hay overlays activos al cargar la escena, ocultar inmediatamente el HUD recién registrado
        if (OverlayCount > 0 && hudRoot != null)
        {
            HideHUD();
        }
    }

    /// <summary>
    /// Desregistra el HUD actual al destruirse la escena.
    /// </summary>
    public void UnregisterHUD()
    {
        hudRoot = null;
        savedHudChildStates.Clear();
    }

    // ==========================================
    // GESTIÓN DE OVERLAYS (PANEL LOCKS)
    // ==========================================

    /// <summary>
    /// Registra un overlay activo (Diálogo, Pausa, Game Over, Tienda, etc.).
    /// </summary>
    /// <param name="id">Identificador único del overlay (ej. "Dialogue", "Pause").</param>
    /// <param name="closeCallback">Acción para cerrar forzosamente este panel (usado por Game Over).</param>
    public void RegisterOverlay(string id, Action closeCallback = null)
    {
        if (string.IsNullOrEmpty(id)) return;

        bool wasEmpty = registeredOverlays.Count == 0;
        registeredOverlays[id] = closeCallback;

        if (wasEmpty)
        {
            HideHUD();
        }
    }

    /// <summary>
    /// Remueve un overlay cuando se cierra.
    /// </summary>
    public void UnregisterOverlay(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (registeredOverlays.Remove(id))
        {
            if (registeredOverlays.Count == 0)
            {
                ShowHUD();
            }
        }
    }

    /// <summary>
    /// Cierra forzosamente todos los paneles registrados e invoca sus callbacks de cierre.
    /// Útil para dar prioridad absoluta a Game Over.
    /// </summary>
    public void ForceCloseAllOverlays()
    {
        if (registeredOverlays.Count == 0) return;

        // Copiar el diccionario para evitar errores de modificación durante la iteración
        var overlaysCopy = new Dictionary<string, Action>(registeredOverlays);
        registeredOverlays.Clear();

        foreach (var kvp in overlaysCopy)
        {
            try
            {
                kvp.Value?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIManager] Error cerrando overlay '{kvp.Key}': {ex.Message}");
            }
        }

        // Si ya no quedan overlays, restaurar el HUD (Game Over registrará su propio overlay inmediatamente si es necesario)
        ShowHUD();
    }

    // ==========================================
    // ESTADO DE GAME OVER
    // ==========================================

    public void NotifyGameOver()
    {
        IsGameOver = true;
    }

    public void ResetGameOverState()
    {
        IsGameOver = false;
    }

    // ==========================================
    // OCULTACIÓN Y RESTAURACIÓN DEL HUD
    // ==========================================

    private void HideHUD()
    {
        if (hudRoot == null) return;

        savedHudChildStates.Clear();

        for (int i = 0; i < hudRoot.transform.childCount; i++)
        {
            Transform child = hudRoot.transform.GetChild(i);
            savedHudChildStates[child.gameObject.GetInstanceID()] = child.gameObject.activeSelf;
            child.gameObject.SetActive(false);
        }
    }

    private void ShowHUD()
    {
        if (hudRoot == null) return;

        for (int i = 0; i < hudRoot.transform.childCount; i++)
        {
            Transform child = hudRoot.transform.GetChild(i);
            int id = child.gameObject.GetInstanceID();

            if (savedHudChildStates.TryGetValue(id, out bool wasActive))
            {
                child.gameObject.SetActive(wasActive);
            }
            else
            {
                // Si no tenemos registro del estado, mantener el estado actual o reactivar por defecto
                child.gameObject.SetActive(true);
            }
        }

        savedHudChildStates.Clear();
    }
}
