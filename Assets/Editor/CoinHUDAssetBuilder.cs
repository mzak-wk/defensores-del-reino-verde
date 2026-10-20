using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generador automático de los Prefabs de CoinHUD para inspección en el Editor y carga en runtime.
/// Crea 'Assets/Prefabs/UI/CoinHUD.prefab' y 'Assets/Resources/CoinHUD.prefab'.
/// </summary>
public static class CoinHUDAssetBuilder
{
    private const string TargetPrefabPath = "Assets/Prefabs/UI/CoinHUD.prefab";
    private const string TargetResourcesPrefabPath = "Assets/Resources/CoinHUD.prefab";
    private const string NunitoFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Nunito SDF.asset";
    private const string CoinSpritePath = "Assets/Resources/lutro_coin.png";

    [InitializeOnLoadMethod]
    private static void AutoEnsurePrefabs()
    {
        if (!File.Exists(TargetPrefabPath) || !File.Exists(TargetResourcesPrefabPath))
        {
            BuildCoinHUDPrefab();
        }
    }

    [MenuItem("Tools/UI/Build CoinHUD Prefab")]
    public static void BuildCoinHUDPrefab()
    {
        Debug.Log("[CoinHUDAssetBuilder] Iniciando construcción del Prefab de CoinHUD...");

        // 1. Directorios
        string prefabsDir = Path.GetDirectoryName(TargetPrefabPath);
        if (!Directory.Exists(prefabsDir)) Directory.CreateDirectory(prefabsDir);

        string resDir = Path.GetDirectoryName(TargetResourcesPrefabPath);
        if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);

        // 2. Cargar assets visuales
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NunitoFontPath);
        Sprite coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoinSpritePath);

        // 3. Crear jerarquía raíz
        GameObject root = new GameObject("CoinHUD");
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.sizeDelta = Vector2.zero;
        rootRt.anchoredPosition = Vector2.zero;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        CoinHUDController controller = root.AddComponent<CoinHUDController>();

        // 4. Badge Container (Pastilla Superior Derecha)
        GameObject badgeGO = new GameObject("CoinBadge");
        badgeGO.transform.SetParent(root.transform, false);

        RectTransform badgeRt = badgeGO.AddComponent<RectTransform>();
        badgeRt.anchorMin = Vector2.one;
        badgeRt.anchorMax = Vector2.one;
        badgeRt.pivot = Vector2.one;
        badgeRt.anchoredPosition = new Vector2(-28f, -28f);
        badgeRt.sizeDelta = new Vector2(150f, 44f);

        Image badgeImg = badgeGO.AddComponent<Image>();
        badgeImg.color = new Color(0.06f, 0.08f, 0.11f, 0.90f);

        Outline outline = badgeGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.92f, 0.70f, 0.12f, 0.55f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // 5. Icono de moneda
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(badgeGO.transform, false);

        RectTransform iconRt = iconGO.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(24f, 0f);
        iconRt.sizeDelta = new Vector2(34f, 34f);

        Image iconImg = iconGO.AddComponent<Image>();
        if (coinSprite != null) iconImg.sprite = coinSprite;
        iconImg.preserveAspect = true;

        // 6. Texto Contador TMP
        GameObject textGO = new GameObject("CountText");
        textGO.transform.SetParent(badgeGO.transform, false);

        RectTransform textRt = textGO.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.pivot = new Vector2(0f, 0.5f);
        textRt.offsetMin = new Vector2(48f, 0f);
        textRt.offsetMax = new Vector2(-8f, 0f);

        TextMeshProUGUI countTmp = textGO.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) countTmp.font = fontAsset;
        countTmp.text = "0";
        countTmp.fontSize = 22f;
        countTmp.fontStyle = FontStyles.Bold;
        countTmp.alignment = TextAlignmentOptions.MidlineLeft;
        countTmp.color = new Color(0.98f, 0.80f, 0.08f, 1f); // #FACC15
        countTmp.textWrappingMode = TextWrappingModes.NoWrap;
        countTmp.overflowMode = TextOverflowModes.Overflow;
        countTmp.raycastTarget = false;

        // 6b. Texto de Feedback de Incremento (+1, -5)
        GameObject deltaGO = new GameObject("DeltaText");
        deltaGO.transform.SetParent(badgeGO.transform, false);

        RectTransform dRt = deltaGO.AddComponent<RectTransform>();
        dRt.anchorMin = new Vector2(1f, 0f);
        dRt.anchorMax = new Vector2(1f, 0f);
        dRt.pivot = new Vector2(1f, 1f);
        dRt.anchoredPosition = new Vector2(-10f, -4f);
        dRt.sizeDelta = new Vector2(90f, 24f);

        TextMeshProUGUI deltaTmp = deltaGO.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) deltaTmp.font = fontAsset;
        deltaTmp.fontSize = 17f;
        deltaTmp.fontStyle = FontStyles.Bold;
        deltaTmp.alignment = TextAlignmentOptions.Right;
        deltaTmp.text = "";
        deltaTmp.raycastTarget = false;

        // 7. Asignar referencias en SerializedObject
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("hudCanvas").objectReferenceValue = canvas;
        so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("badgeContainer").objectReferenceValue = badgeRt;
        so.FindProperty("coinIconImage").objectReferenceValue = iconImg;
        so.FindProperty("coinCountText").objectReferenceValue = countTmp;
        so.FindProperty("deltaFeedbackText").objectReferenceValue = deltaTmp;
        so.ApplyModifiedProperties();

        // 8. Guardar prefabs en Assets/Prefabs/UI y Assets/Resources
        PrefabUtility.SaveAsPrefabAsset(root, TargetPrefabPath);
        PrefabUtility.SaveAsPrefabAsset(root, TargetResourcesPrefabPath);

        GameObject.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CoinHUDAssetBuilder] ¡Prefabs generados con éxito en:\n- {TargetPrefabPath}\n- {TargetResourcesPrefabPath}!");
    }
}
