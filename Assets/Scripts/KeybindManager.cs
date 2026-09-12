using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Acciones de combate asignables por el jugador.
/// </summary>
public enum CombatAction
{
    BasicAttack,
    Skill1,
    Skill2,
    Ultimate
}

/// <summary>
/// Gestor centralizado y persistente de asignación de teclas (Keybinding).
/// Permite remapeo de teclas, guardado en PlayerPrefs y notificación en tiempo real a la UI.
/// </summary>
public class KeybindManager : MonoBehaviour
{
    private static KeybindManager _instance;
    public static KeybindManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<KeybindManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("[KeybindManager]");
                    _instance = go.AddComponent<KeybindManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    public static event Action<CombatAction, KeyCode> OnKeybindChanged;

    private readonly Dictionary<CombatAction, KeyCode> keyBindings = new Dictionary<CombatAction, KeyCode>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadKeybinds();
    }

    /// <summary>
    /// Carga las teclas configuradas en PlayerPrefs o establece las predeterminadas.
    /// </summary>
    public void LoadKeybinds()
    {
        keyBindings.Clear();

        keyBindings[CombatAction.BasicAttack] = LoadKey(CombatAction.BasicAttack, KeyCode.Mouse0);
        keyBindings[CombatAction.Skill1] = LoadKey(CombatAction.Skill1, KeyCode.Alpha1);
        keyBindings[CombatAction.Skill2] = LoadKey(CombatAction.Skill2, KeyCode.Alpha2);
        keyBindings[CombatAction.Ultimate] = LoadKey(CombatAction.Ultimate, KeyCode.Alpha3);
    }

    private KeyCode LoadKey(CombatAction action, KeyCode defaultKey)
    {
        string prefKey = "Keybind_" + action;
        if (PlayerPrefs.HasKey(prefKey))
        {
            string val = PlayerPrefs.GetString(prefKey);
            if (Enum.TryParse(val, out KeyCode parsedKey))
            {
                return parsedKey;
            }
        }
        return defaultKey;
    }

    /// <summary>
    /// Retorna el KeyCode configurado para una acción específica.
    /// </summary>
    public KeyCode GetKey(CombatAction action)
    {
        if (keyBindings.TryGetValue(action, out KeyCode key))
        {
            return key;
        }

        // Fallback si no está mapeado aún
        switch (action)
        {
            case CombatAction.BasicAttack: return KeyCode.Mouse0;
            case CombatAction.Skill1: return KeyCode.Alpha1;
            case CombatAction.Skill2: return KeyCode.Alpha2;
            case CombatAction.Ultimate: return KeyCode.Alpha3;
            default: return KeyCode.None;
        }
    }

    /// <summary>
    /// Asigna una nueva tecla a la acción dada, la persiste en PlayerPrefs y notifica a la UI.
    /// </summary>
    public void SetKey(CombatAction action, KeyCode newKey)
    {
        keyBindings[action] = newKey;
        PlayerPrefs.SetString("Keybind_" + action, newKey.ToString());
        PlayerPrefs.Save();

        Debug.Log($"[KeybindManager] Acción {action} remapeada a tecla: {newKey}");
        OnKeybindChanged?.Invoke(action, newKey);
    }

    /// <summary>
    /// Restablece todas las teclas a los valores predeterminados (Mouse0, 1, 2, 3).
    /// </summary>
    public void ResetToDefaults()
    {
        SetKey(CombatAction.BasicAttack, KeyCode.Mouse0);
        SetKey(CombatAction.Skill1, KeyCode.Alpha1);
        SetKey(CombatAction.Skill2, KeyCode.Alpha2);
        SetKey(CombatAction.Ultimate, KeyCode.Alpha3);
    }

    /// <summary>
    /// Retorna una cadena limpia y legible para mostrar en el HUD o botón de la UI.
    /// </summary>
    public string GetDisplayString(CombatAction action)
    {
        KeyCode code = GetKey(action);

        switch (code)
        {
            case KeyCode.Mouse0: return "CLICK";
            case KeyCode.Mouse1: return "CLICK D";
            case KeyCode.Mouse2: return "CLICK M";
            case KeyCode.Alpha0: return "0";
            case KeyCode.Alpha1: return "1";
            case KeyCode.Alpha2: return "2";
            case KeyCode.Alpha3: return "3";
            case KeyCode.Alpha4: return "4";
            case KeyCode.Alpha5: return "5";
            case KeyCode.Alpha6: return "6";
            case KeyCode.Alpha7: return "7";
            case KeyCode.Alpha8: return "8";
            case KeyCode.Alpha9: return "9";
            case KeyCode.LeftShift: return "SHIFT";
            case KeyCode.RightShift: return "R-SHIFT";
            case KeyCode.LeftControl: return "CTRL";
            case KeyCode.Space: return "ESPACIO";
            default: return code.ToString().ToUpper();
        }
    }
}
