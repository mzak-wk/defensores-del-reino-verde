using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gestiona la selección de recompensa de armas tras derrotar a un Jefe.
/// Muestra 2 armas aleatorias ponderadas por su rareza (Común/Raro/Épico) entre las armas aún no desbloqueadas.
/// Al seleccionar una, la guarda permanentemente en GameSession y regresa al HUB.
/// </summary>
public class BossRewardUI : MonoBehaviour
{
    [Header("Base de Datos de Armas")]
    [Tooltip("Referencia a la WeaponDatabase maestra.")]
    [SerializeField] private WeaponDatabase weaponDatabase;

    [Header("UI - Opción 1 (Botón 0)")]
    [SerializeField] private Button button1;
    [SerializeField] private Image icon1;
    [SerializeField] private TMP_Text name1;
    [SerializeField] private TMP_Text rarity1;

    [Header("UI - Opción 2 (Botón 1)")]
    [SerializeField] private Button button2;
    [SerializeField] private Image icon2;
    [SerializeField] private TMP_Text name2;
    [SerializeField] private TMP_Text rarity2;

    [Header("Pesos de Tasa de Aparición (Drop Rate Weights)")]
    [Tooltip("Peso de probabilidad para armas de rareza Común.")]
    [Range(1, 100)] [SerializeField] private int weightCommon = 60;
    [Tooltip("Peso de probabilidad para armas de rareza Rara.")]
    [Range(1, 100)] [SerializeField] private int weightRare = 30;
    [Tooltip("Peso de probabilidad para armas de rareza Épica.")]
    [Range(1, 100)] [SerializeField] private int weightEpic = 10;

    [Header("Escena de Destino")]
    [Tooltip("Nombre de la escena del HUB a la cual regresar tras reclamar la recompensa.")]
    [SerializeField] private string hubSceneName = "LevelHUB";

    [Header("Progresión de Nivel")]
    [Tooltip("El índice del nivel que el jugador acaba de completar (Ej: 1 para el Nivel 1).")]
    public int currentLevelIndex = 1;

    private WeaponData selectedOption1;
    private WeaponData selectedOption2;

    private void Awake()
    {
        // Registrar listeners de clic en los botones
        if (button1 != null) button1.onClick.AddListener(() => OnWeaponSelected(0));
        if (button2 != null) button2.onClick.AddListener(() => OnWeaponSelected(1));
    }

    private void OnEnable()
    {
        GenerateRewardOptions();
    }

    /// <summary>
    /// Genera y presenta las 2 opciones de armas basándose en disponibilidad y pesos por rareza.
    /// </summary>
    public void GenerateRewardOptions()
    {
        if (weaponDatabase == null)
        {
            Debug.LogError("[BossRewardUI] WeaponDatabase no está asignada.");
            return;
        }

        List<string> unlockedIds = GameSession.Instance != null 
            ? GameSession.Instance.unlockedWeapons 
            : new List<string>();

        // 1. Filtrar lista de armas que el jugador AÚN NO TIENE
        List<WeaponData> pool = new List<WeaponData>();
        foreach (var weapon in weaponDatabase.Weapons)
        {
            if (weapon != null && !unlockedIds.Contains(weapon.WeaponId))
            {
                pool.Add(weapon);
            }
        }

        // Si no hay suficientes armas sin desbloquear (menos de 2), rellenar el pool con todas las armas para garantizar siempre 2 opciones
        if (pool.Count < 2)
        {
            foreach (var weapon in weaponDatabase.Weapons)
            {
                if (weapon != null && !pool.Contains(weapon))
                {
                    pool.Add(weapon);
                }
            }
        }

        // En el caso extremo de contar con menos de 2 armas en toda la base de datos
        if (pool.Count == 0)
        {
            Debug.LogError("[BossRewardUI] No hay armas disponibles en la WeaponDatabase.");
            return;
        }

        // 2. Seleccionar 2 armas distintas utilizando la ruleta ponderada por rareza
        selectedOption1 = SelectWeightedWeapon(pool);
        pool.Remove(selectedOption1); // Remover la primera elección para no duplicar en la segunda opción

        if (pool.Count > 0)
        {
            selectedOption2 = SelectWeightedWeapon(pool);
        }
        else
        {
            selectedOption2 = selectedOption1; // Fallback si solo existe 1 arma registrada
        }

        // 3. Configurar la UI de ambas opciones
        SetupButtonUI(selectedOption1, icon1, name1, rarity1, button1);
        SetupButtonUI(selectedOption2, icon2, name2, rarity2, button2);
    }

    /*
     * EXPLICACIÓN MATEMÁTICA DEL DROP RATE (SELECCIÓN PONDERADA POR PESOS):
     * 
     * Cada rareza (Común, Rara, Épica) tiene un peso numérico W_i (ej: Común=60, Rara=30, Épica=10).
     * 
     * 1. Suma Total de Pesos (W_total):
     *    W_total = Suma( W(rareza_k) ) para cada arma k presente en la lista de candidatos disponibles.
     * 
     * 2. Cálculo de Probabilidad Individual:
     *    P(Arma_k) = W(rareza_k) / W_total
     *    - Ejemplo con 3 armas [Común (60), Rara (30), Épica (10)]:
     *      W_total = 60 + 30 + 10 = 100
     *      P(Común) = 60/100 = 60%
     *      P(Rara)  = 30/100 = 30%
     *      P(Épica) = 10/100 = 10%
     * 
     * 3. Muestreo Aleatorio (Algoritmo Roulette Wheel Selection):
     *    Se genera un valor aleatorio flotante r en el rango [0, W_total).
     *    Se itera el pool acumulando el peso de cada arma; el arma donde r cae dentro de su intervalo acumulado es la seleccionada.
     */
    private WeaponData SelectWeightedWeapon(List<WeaponData> candidates)
    {
        float totalWeight = 0f;

        // Calcular suma total de pesos en función de la rareza de cada candidato disponible
        foreach (var weapon in candidates)
        {
            totalWeight += GetRarityWeight(weapon);
        }

        if (totalWeight <= 0f) return candidates[0];

        // Muestreo dentro del rango acumulado [0, totalWeight)
        float randomPoint = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (var weapon in candidates)
        {
            currentSum += GetRarityWeight(weapon);
            if (randomPoint <= currentSum)
            {
                return weapon;
            }
        }

        return candidates[candidates.Count - 1];
    }

    /// <summary>
    /// Devuelve el peso numérico de aparición correspondiente a la rareza del arma.
    /// </summary>
    private int GetRarityWeight(WeaponData weapon)
    {
        if (weapon == null || weapon.Rarity == null) return weightCommon;

        switch (weapon.Rarity.Id)
        {
            case WeaponRarityId.Common:
                return weightCommon;
            case WeaponRarityId.Rare:
                return weightRare;
            case WeaponRarityId.Epic:
                return weightEpic;
            default:
                return weightCommon;
        }
    }

    private void SetupButtonUI(WeaponData weapon, Image icon, TMP_Text nameText, TMP_Text rarityText, Button btn)
    {
        if (weapon == null) return;

        if (icon != null) icon.sprite = weapon.Icon;
        if (nameText != null) nameText.text = weapon.DisplayName;

        if (rarityText != null && weapon.Rarity != null)
        {
            rarityText.text = weapon.Rarity.DisplayName;
            rarityText.color = weapon.Rarity.DisplayColor;
        }

        if (btn != null) btn.interactable = true;
    }

    /// <summary>
    /// Evento invocado al hacer clic en uno de los 2 botones de opción de recompensa.
    /// </summary>
    /// <param name="buttonIndex">0 para la opción 1, 1 para la opción 2.</param>
    public void OnWeaponSelected(int buttonIndex)
    {
        WeaponData chosen = (buttonIndex == 0) ? selectedOption1 : selectedOption2;

        if (chosen == null)
        {
            Debug.LogError("[BossRewardUI] Arma elegida es null.");
            return;
        }

        Debug.Log($"[BossRewardUI] Arma seleccionada como recompensa: {chosen.DisplayName} (ID: {chosen.WeaponId})");

        // 1. Guardar el nuevo WeaponId en GameSession y avanzar de nivel si corresponde
        if (GameSession.Instance != null)
        {
            if (!GameSession.Instance.unlockedWeapons.Contains(chosen.WeaponId))
            {
                GameSession.Instance.unlockedWeapons.Add(chosen.WeaponId);
            }

            // Desbloquear el siguiente nivel si completamos el máximo nivel actual
            if (GameSession.Instance.highestLevelUnlocked == currentLevelIndex)
            {
                GameSession.Instance.highestLevelUnlocked++;
                Debug.Log($"[BossRewardUI] ¡Nuevo nivel desbloqueado! Nivel máximo ahora es: {GameSession.Instance.highestLevelUnlocked}");
            }
        }

        // 2. Guardar progreso persistentemente en disco
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveCurrentGame();
        }
        else
        {
            Debug.LogWarning("[BossRewardUI] SaveManager.Instance no disponible. Partida no autoguardada.");
        }

        // 3. Regresar a la base (LevelHUB)
        SceneManager.LoadScene(hubSceneName);
    }
}
