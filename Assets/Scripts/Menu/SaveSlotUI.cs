using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla la UI y el comportamiento de un botón de ranura de guardado.
/// Permite cargar/iniciar partida y ofrece la opción QoL de borrar un slot con confirmación.
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    [Header("Configuración del Slot")]
    [Tooltip("El ID de este slot (ej. 1, 2, 3)")]
    public int slotId = 1;

    [Header("Referencias de UI")]
    [Tooltip("El texto donde se mostrará el estado ('Vacío' o info de la partida)")]
    public TextMeshProUGUI statusText;
    
    [Tooltip("El botón principal de este slot")]
    public Button slotButton;

    [Tooltip("Botón opcional para borrar esta partida. Si no se asigna, se autogenera.")]
    public Button deleteButton;

    private bool hasSaveData = false;
    private bool isConfirmingDelete = false;
    private Coroutine confirmCoroutine;
    private TextMeshProUGUI deleteBtnText;
    private Image deleteBtnImage;

    private void Awake()
    {
        EnsureDeleteButton();
        BeautifySlotCard();
    }

    private void OnEnable()
    {
        ResetDeleteConfirmation();
        BeautifySlotCard();
        RefreshUI();
    }

    private void Start()
    {
        if (slotButton != null)
        {
            slotButton.onClick.RemoveListener(OnSlotClicked);
            slotButton.onClick.AddListener(OnSlotClicked);
        }

        EnsureDeleteButton();
        BeautifySlotCard();
    }

    /// <summary>
    /// Aplica estilo visual cibernético de alta tecnología al botón de ranura y al modal contenedor.
    /// </summary>
    private void BeautifySlotCard()
    {
        // 1. Redimensionar el slot como tarjeta vertical estilizada
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(200f, 130f);
            float posX = (slotId == 1) ? -230f : (slotId == 3) ? 230f : 0f;
            rt.anchoredPosition = new Vector2(posX, -15f);
        }

        // 2. Fondo de tarjeta con bordes y estilo cibernético
        Image img = GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(0.05f, 0.08f, 0.12f, 0.94f);
        }

        Outline outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0f, 0.85f, 0.65f, 0.45f);
        outline.effectDistance = new Vector2(2f, -2f);

        // 3. Colores del botón
        if (slotButton == null) slotButton = GetComponent<Button>();
        if (slotButton != null)
        {
            ColorBlock cb = slotButton.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.2f, 1f, 0.8f, 1f);
            cb.pressedColor = new Color(0.1f, 0.6f, 0.5f, 1f);
            cb.selectedColor = Color.white;
            slotButton.colors = cb;
        }

        // 4. Margen del texto para que no solape con el botón de borrar
        if (statusText != null)
        {
            statusText.alignment = TextAlignmentOptions.Top;
            RectTransform textRT = statusText.rectTransform;
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10f, 34f); // Espacio inferior para el botón de borrar
            textRT.offsetMax = new Vector2(-10f, -10f);
        }

        // 5. Botón de borrar integrado dentro de la tarjeta
        if (deleteButton != null)
        {
            RectTransform delRT = deleteButton.GetComponent<RectTransform>();
            if (delRT != null)
            {
                delRT.anchorMin = new Vector2(0.5f, 0f);
                delRT.anchorMax = new Vector2(0.5f, 0f);
                delRT.pivot = new Vector2(0.5f, 0f);
                delRT.anchoredPosition = new Vector2(0f, 6f);
                delRT.sizeDelta = new Vector2(140f, 24f);
            }
        }

        // 6. Embellecer el panel contenedor y su botón volver
        BeautifyParentPanel();
    }

    private void BeautifyParentPanel()
    {
        if (transform.parent == null) return;
        Transform parentPanel = transform.parent;

        // Fondo oscuro futurista translúcido para SaveSlotsPanel
        Image parentImg = parentPanel.GetComponent<Image>();
        if (parentImg != null)
        {
            parentImg.color = new Color(0.02f, 0.04f, 0.07f, 0.92f);
        }

        // Título del panel de saves si no existe aún
        Transform header = parentPanel.Find("Header_SaveSlots");
        if (header == null)
        {
            GameObject headerGO = new GameObject("Header_SaveSlots", typeof(RectTransform));
            headerGO.transform.SetParent(parentPanel, false);
            RectTransform hRT = headerGO.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0.5f, 0.5f);
            hRT.anchorMax = new Vector2(0.5f, 0.5f);
            hRT.pivot = new Vector2(0.5f, 0.5f);
            hRT.anchoredPosition = new Vector2(0f, 130f);
            hRT.sizeDelta = new Vector2(500f, 60f);

            TextMeshProUGUI titleTMP = headerGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "<size=22><b><color=#00FFA3>REGISTRO DE EXPEDICIONES</color></b></size>\n<size=12><color=#8898AA>SELECCIONA UN ARCHIVO DE MISIÓN PARA DESPLEGAR</color></size>";
            titleTMP.alignment = TextAlignmentOptions.Center;
        }

        // Botón volver
        Transform backBtn = parentPanel.Find("Btn_Volver") 
            ?? parentPanel.Find("Volver") 
            ?? parentPanel.Find("Back") 
            ?? parentPanel.Find("Button");

        if (backBtn != null)
        {
            RectTransform bRT = backBtn.GetComponent<RectTransform>();
            if (bRT != null)
            {
                bRT.anchorMin = new Vector2(0.5f, 0.5f);
                bRT.anchorMax = new Vector2(0.5f, 0.5f);
                bRT.pivot = new Vector2(0.5f, 0.5f);
                bRT.anchoredPosition = new Vector2(0f, -140f);
                bRT.sizeDelta = new Vector2(160f, 36f);
            }

            TextMeshProUGUI bTMP = backBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (bTMP != null)
            {
                bTMP.text = "◀ VOLVER";
                bTMP.fontSize = 14;
                bTMP.fontStyle = FontStyles.Bold;
                bTMP.color = Color.white;
                bTMP.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                Text legacyText = backBtn.GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    legacyText.text = "◀ VOLVER";
                    legacyText.fontSize = 14;
                    legacyText.fontStyle = UnityEngine.FontStyle.Bold;
                    legacyText.color = Color.white;
                    legacyText.alignment = TextAnchor.MiddleCenter;
                }
            }

            Image bImg = backBtn.GetComponent<Image>();
            if (bImg != null)
            {
                bImg.color = new Color(0.08f, 0.32f, 0.42f, 0.95f);
            }

            Outline bOutline = backBtn.GetComponent<Outline>() ?? backBtn.gameObject.AddComponent<Outline>();
            bOutline.effectColor = new Color(0f, 0.85f, 1f, 0.65f);
            bOutline.effectDistance = new Vector2(1.5f, -1.5f);

            if (backBtn.GetComponent<UIButtonJuice>() == null)
            {
                backBtn.gameObject.AddComponent<UIButtonJuice>();
            }
        }
    }

    /// <summary>
    /// Genera automáticamente un botón de borrado estilizado si no existe en la jerarquía.
    /// </summary>
    private void EnsureDeleteButton()
    {
        if (deleteButton != null)
        {
            deleteBtnText = deleteButton.GetComponentInChildren<TextMeshProUGUI>();
            deleteBtnImage = deleteButton.GetComponent<Image>();
            deleteButton.onClick.RemoveListener(OnDeleteClicked);
            deleteButton.onClick.AddListener(OnDeleteClicked);
            return;
        }

        // Buscar si ya se había creado previamente
        Transform existing = transform.Find("Btn_BorrarSlot");
        if (existing != null)
        {
            deleteButton = existing.GetComponent<Button>();
            deleteBtnText = existing.GetComponentInChildren<TextMeshProUGUI>();
            deleteBtnImage = existing.GetComponent<Image>();
            deleteButton.onClick.RemoveListener(OnDeleteClicked);
            deleteButton.onClick.AddListener(OnDeleteClicked);
            return;
        }

        // Crear botón de borrado integrado en la base de la tarjeta
        GameObject delGO = new GameObject("Btn_BorrarSlot", typeof(RectTransform));
        delGO.transform.SetParent(transform, false);

        RectTransform rt = delGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 6f);
        rt.sizeDelta = new Vector2(140f, 24f);

        deleteBtnImage = delGO.AddComponent<Image>();
        deleteBtnImage.color = new Color(0.65f, 0.12f, 0.12f, 0.90f);

        deleteButton = delGO.AddComponent<Button>();
        ColorBlock cb = deleteButton.colors;
        cb.normalColor = new Color(0.65f, 0.12f, 0.12f, 0.90f);
        cb.highlightedColor = new Color(0.95f, 0.20f, 0.20f, 1f);
        cb.pressedColor = new Color(0.45f, 0.08f, 0.08f, 1f);
        deleteButton.colors = cb;

        // Texto del botón
        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(delGO.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        deleteBtnText = textGO.AddComponent<TextMeshProUGUI>();
        deleteBtnText.text = "BORRAR";
        deleteBtnText.fontSize = 11;
        deleteBtnText.fontStyle = FontStyles.Bold;
        deleteBtnText.alignment = TextAlignmentOptions.Center;
        deleteBtnText.color = Color.white;

        deleteButton.onClick.AddListener(OnDeleteClicked);

        // Añadir micro-animación Juice
        if (delGO.GetComponent<UIButtonJuice>() == null)
        {
            delGO.AddComponent<UIButtonJuice>();
        }
    }

    /// <summary>
    /// Actualiza el texto del botón consultando al SaveManager si existe partida.
    /// </summary>
    public void RefreshUI()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[SaveSlotUI] SaveManager no está instanciado aún.");
            return;
        }

        hasSaveData = SaveManager.Instance.HasSave(slotId);

        if (statusText != null)
        {
            if (hasSaveData)
            {
                SaveData data = SaveManager.Instance.GetSaveData(slotId);
                if (data != null)
                {
                    statusText.text = $"<size=14><b><color=#00FFA3>EXPEDICIÓN 0{slotId}</color></b></size>\n" +
                                      $"<size=10><color=#00E5FF>[ACTIVA]</color></size>\n\n" +
                                      $"<size=12><b>NIVEL {data.playerLevel}</b></size>\n" +
                                      $"<size=11><color=#FFD700>ORO: {data.gold}</color></size>";
                }
                else
                {
                    statusText.text = $"<size=14><b><color=#FF5555>EXPEDICIÓN 0{slotId}</color></b></size>\n<size=10>Error al leer datos</size>";
                }
            }
            else
            {
                statusText.text = $"<size=14><b><color=#8898AA>EXPEDICIÓN 0{slotId}</color></b></size>\n" +
                                  $"<size=10><color=#5A6E82>[VACÍA]</color></size>\n\n" +
                                  $"<size=11><color=#00FFA3>+ NUEVA PARTIDA</color></size>\n" +
                                  $"<size=9><color=#6E8294>Despliegue inicial</color></size>";
            }
        }

        // El botón de borrar solo es visible si hay datos que borrar
        if (deleteButton != null)
        {
            deleteButton.gameObject.SetActive(hasSaveData);
        }
    }

    /// <summary>
    /// Evento disparado al hacer clic en este slot.
    /// </summary>
    private void OnSlotClicked()
    {
        if (isConfirmingDelete)
        {
            ResetDeleteConfirmation();
            return;
        }

        if (SaveManager.Instance == null) return;

        if (hasSaveData)
        {
            // Cargar la partida existente
            SaveManager.Instance.LoadGame(slotId);
        }
        else
        {
            // Crear partida nueva
            SaveManager.Instance.StartNewGame(slotId);
        }
    }

    /// <summary>
    /// Lógica de borrado con confirmación doble segura.
    /// </summary>
    private void OnDeleteClicked()
    {
        if (!hasSaveData || SaveManager.Instance == null) return;

        if (!isConfirmingDelete)
        {
            // Primer clic: pedir confirmación
            isConfirmingDelete = true;
            if (deleteBtnText != null)
            {
                deleteBtnText.text = "¿CONFIRMAR?";
            }
            if (deleteBtnImage != null)
            {
                deleteBtnImage.color = new Color(0.95f, 0.45f, 0.1f, 1f); // Naranja alerta
            }

            if (confirmCoroutine != null) StopCoroutine(confirmCoroutine);
            confirmCoroutine = StartCoroutine(ResetConfirmAfterDelay(3.5f));
        }
        else
        {
            // Segundo clic confirmado: borrar archivo del disco
            if (confirmCoroutine != null) StopCoroutine(confirmCoroutine);
            ResetDeleteConfirmation();

            bool deleted = SaveManager.Instance.DeleteSave(slotId);
            if (deleted)
            {
                Debug.Log($"[SaveSlotUI] Slot {slotId} borrado correctamente.");
            }

            RefreshUI();
        }
    }

    private IEnumerator ResetConfirmAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetDeleteConfirmation();
    }

    private void ResetDeleteConfirmation()
    {
        isConfirmingDelete = false;
        if (deleteBtnText != null)
        {
            deleteBtnText.text = "BORRAR";
        }
        if (deleteBtnImage != null)
        {
            deleteBtnImage.color = new Color(0.65f, 0.12f, 0.12f, 0.90f);
        }
    }
}
