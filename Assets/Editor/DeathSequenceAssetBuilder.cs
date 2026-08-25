using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Herramienta de Editor para construir y actualizar automáticamente el Prefab
/// 'Panel_GameOver' y los Animation Clips de Muerte con los spritesheets
/// 'Andrius muerte' y 'muerte texto'.
/// </summary>
public static class DeathSequenceAssetBuilder
{
    private const string AndriusMuertePath = "Assets/sprites(r)/UI/Andrius muerte.png";
    private const string MuerteTextoPath = "Assets/sprites(r)/UI/muerte texto.png";
    private const string GameOverPrefabPath = "Assets/Prefabs/Panel_GameOver.prefab";
    private const string NunitoFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Nunito SDF.asset";

    [MenuItem("Tools/Death System/Build Death Menu UI Prefab")]
    public static void BuildDeathMenuPrefab()
    {
        Debug.Log("[DeathSequenceAssetBuilder] Iniciando construcción del Prefab de Menú de Muerte...");

        // 1. Cargar sprites de Andrius muerte
        Sprite[] andriusSprites = LoadSortedSprites(AndriusMuertePath);
        Debug.Log($"[DeathSequenceAssetBuilder] Sprites 'Andrius muerte' cargados: {andriusSprites.Length}");

        // 2. Cargar sprites de Muerte texto
        Sprite[] textoSprites = LoadSortedSprites(MuerteTextoPath);
        Debug.Log($"[DeathSequenceAssetBuilder] Sprites 'muerte texto' cargados: {textoSprites.Length}");

        // 3. Cargar fuente Nunito SDF
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NunitoFontPath);
        if (fontAsset == null)
        {
            fontAsset = TMPFontBatchUpdater.CreateOrGetNunitoFontAsset();
        }

        // 4. Crear o cargar GameObject temporal para construir el Prefab
        GameObject root = new GameObject("Panel_GameOver");
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        CanvasRenderer rootCr = root.AddComponent<CanvasRenderer>();
        DeathMenuController deathController = root.AddComponent<DeathMenuController>();

        // También mantenemos GameOverController para retrocompatibilidad
        GameOverController oldController = root.AddComponent<GameOverController>();
        oldController.gameOverUI = root;

        // ----------------------------------------------------
        // CAPA 1: DarkOverlay (Fondo Oscuro Gradual)
        // ----------------------------------------------------
        GameObject overlayGo = new GameObject("DarkOverlay");
        overlayGo.transform.SetParent(root.transform, false);
        RectTransform overlayRect = overlayGo.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        overlayRect.anchoredPosition = Vector2.zero;
        Image overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0.035f, 0.055f, 0.045f, 0.92f);
        overlayImg.raycastTarget = true;

        // ----------------------------------------------------
        // CAPA 2: ContentRoot (Contenedor de Elementos Visuales)
        // ----------------------------------------------------
        GameObject contentGo = new GameObject("ContentRoot");
        contentGo.transform.SetParent(root.transform, false);
        RectTransform contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(1920, 1080);
        contentRect.anchoredPosition = Vector2.zero;

        // Vista de Animación del Personaje (Zona Superior-Media)
        GameObject charGo = new GameObject("CharacterDeathView");
        charGo.transform.SetParent(contentGo.transform, false);
        RectTransform charRect = charGo.AddComponent<RectTransform>();
        charRect.anchorMin = new Vector2(0.5f, 0.5f);
        charRect.anchorMax = new Vector2(0.5f, 0.5f);
        charRect.anchoredPosition = new Vector2(0f, 175f);
        charRect.sizeDelta = new Vector2(300f, 300f);
        Image charImg = charGo.AddComponent<Image>();
        charImg.preserveAspect = true;
        charImg.raycastTarget = false;
        if (andriusSprites.Length > 0) charImg.sprite = andriusSprites[0];

        Animator charAnimator = charGo.AddComponent<Animator>();
        charAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        var charAnimController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/animations/Andrius_Death_Controller.controller");
        if (charAnimController != null) charAnimator.runtimeAnimatorController = charAnimController;

        // Vista de Animación del Texto de Muerte (Debajo del Personaje)
        GameObject textGo = new GameObject("DeathTextView");
        textGo.transform.SetParent(contentGo.transform, false);
        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 10f);
        textRect.sizeDelta = new Vector2(460f, 130f);
        Image textImg = textGo.AddComponent<Image>();
        textImg.preserveAspect = true;
        textImg.raycastTarget = false;
        if (textoSprites.Length > 0) textImg.sprite = textoSprites[0];

        Animator textAnimator = textGo.AddComponent<Animator>();
        textAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        var textAnimController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/animations/Death_Text_Controller.controller");
        if (textAnimController != null) textAnimator.runtimeAnimatorController = textAnimController;

        // ----------------------------------------------------
        // CAPA 3: ButtonsContainer (Botones en Cascada)
        // ----------------------------------------------------
        GameObject buttonsGo = new GameObject("ButtonsContainer");
        buttonsGo.transform.SetParent(contentGo.transform, false);
        RectTransform buttonsRect = buttonsGo.AddComponent<RectTransform>();
        buttonsRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonsRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonsRect.anchoredPosition = new Vector2(0f, -195f);
        buttonsRect.sizeDelta = new Vector2(400f, 250f);

        // Botón 1: REINTENTAR (Acción Principal Destacada)
        var (btnRestart, cgRestart) = CreateStyledButton(
            buttonsGo.transform,
            "Btn_Reintentar",
            "REINTENTAR",
            new Vector2(320f, 54f),
            new Vector2(0f, 50f),
            new Color(0.16f, 0.42f, 0.22f, 0.95f), // Verde esmeralda bosque
            new Color(0.24f, 0.60f, 0.32f, 1f),
            fontAsset,
            24,
            FontStyles.Bold
        );

        // Botón 2: MENÚ PRINCIPAL
        var (btnMenu, cgMenu) = CreateStyledButton(
            buttonsGo.transform,
            "Btn_MenuPrincipal",
            "MENÚ PRINCIPAL",
            new Vector2(300f, 48f),
            new Vector2(0f, -10f),
            new Color(0.18f, 0.26f, 0.22f, 0.90f), // Verde grisáceo oscuro
            new Color(0.28f, 0.38f, 0.32f, 1f),
            fontAsset,
            20,
            FontStyles.Bold
        );

        // Botón 3: SALIR
        var (btnQuit, cgQuit) = CreateStyledButton(
            buttonsGo.transform,
            "Btn_Salir",
            "SALIR",
            new Vector2(260f, 42f),
            new Vector2(0f, -66f),
            new Color(0.25f, 0.15f, 0.15f, 0.85f), // Rojo oscuro sutil
            new Color(0.40f, 0.20f, 0.20f, 1f),
            fontAsset,
            18,
            FontStyles.Bold
        );

        // ----------------------------------------------------
        // 5. Configurar SerializedObject de DeathMenuController
        // ----------------------------------------------------
        SerializedObject so = new SerializedObject(deathController);
        so.FindProperty("darkOverlay").objectReferenceValue = overlayImg;
        so.FindProperty("overlayColor").colorValue = new Color(0.035f, 0.055f, 0.045f, 0.92f);
        so.FindProperty("overlayFadeDuration").floatValue = 0.8f;

        so.FindProperty("characterAnimImage").objectReferenceValue = charImg;
        so.FindProperty("characterFps").intValue = 24;
        so.FindProperty("delayAfterCharacterAnim").floatValue = 0.25f;

        SerializedProperty charSpritesProp = so.FindProperty("characterDeathSprites");
        charSpritesProp.arraySize = andriusSprites.Length;
        for (int i = 0; i < andriusSprites.Length; i++)
            charSpritesProp.GetArrayElementAtIndex(i).objectReferenceValue = andriusSprites[i];

        so.FindProperty("deathTextAnimImage").objectReferenceValue = textImg;
        so.FindProperty("deathTextFps").intValue = 20;
        so.FindProperty("delayBeforeButtons").floatValue = 0.35f;

        SerializedProperty textSpritesProp = so.FindProperty("deathTextSprites");
        textSpritesProp.arraySize = textoSprites.Length;
        for (int i = 0; i < textoSprites.Length; i++)
            textSpritesProp.GetArrayElementAtIndex(i).objectReferenceValue = textoSprites[i];

        so.FindProperty("buttonsContainer").objectReferenceValue = buttonsGo;
        so.FindProperty("btnRestart").objectReferenceValue = btnRestart;
        so.FindProperty("cgRestart").objectReferenceValue = cgRestart;
        so.FindProperty("btnMainMenu").objectReferenceValue = btnMenu;
        so.FindProperty("cgMainMenu").objectReferenceValue = cgMenu;
        so.FindProperty("btnQuit").objectReferenceValue = btnQuit;
        so.FindProperty("cgQuit").objectReferenceValue = cgQuit;
        so.FindProperty("buttonFadeDuration").floatValue = 0.25f;
        so.FindProperty("delayBetweenButtons").floatValue = 0.18f;

        so.ApplyModifiedProperties();

        // 6. Guardar como Prefab en Assets/Prefabs/Panel_GameOver.prefab
        PrefabUtility.SaveAsPrefabAsset(root, GameOverPrefabPath);
        GameObject.DestroyImmediate(root);

        // 7. Crear Animation Clips (.anim)
        CreateAnimationClip("Assets/animations/Andrius_Death.anim", andriusSprites, 24);
        CreateAnimationClip("Assets/animations/Death_Text.anim", textoSprites, 20);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[DeathSequenceAssetBuilder] ¡Prefab '{GameOverPrefabPath}' creado y configurado exitosamente!");
    }

    private static (Button btn, CanvasGroup cg) CreateStyledButton(
        Transform parent,
        string name,
        string textLabel,
        Vector2 size,
        Vector2 anchoredPosition,
        Color normalColor,
        Color highlightedColor,
        TMP_FontAsset fontAsset,
        int fontSize,
        FontStyles fontStyle)
    {
        GameObject btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);

        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPosition;

        CanvasGroup cg = btnGo.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        cg.transform.localScale = Vector3.one * 0.85f;

        Image img = btnGo.AddComponent<Image>();
        img.color = normalColor;
        img.raycastTarget = true;

        Button btn = btnGo.AddComponent<Button>();
        btn.interactable = false;
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor = normalColor;
        cb.highlightedColor = highlightedColor;
        cb.pressedColor = normalColor * 0.75f;
        cb.selectedColor = highlightedColor;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        // Texto del botón
        GameObject textObj = new GameObject("Text (TMP)");
        textObj.transform.SetParent(btnGo.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        textRt.anchoredPosition = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = textLabel;
        if (fontAsset != null) tmp.font = fontAsset;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return (btn, cg);
    }

    private static Sprite[] LoadSortedSprites(string texturePath)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        List<Sprite> sprites = new List<Sprite>();

        foreach (var obj in assets)
        {
            if (obj is Sprite s)
            {
                sprites.Add(s);
            }
        }

        // Ordenar numéricamente por sufijo (ej: "Andrius muerte_0", "Andrius muerte_1", ...)
        return sprites.OrderBy(s => ExtractTrailingNumber(s.name)).ToArray();
    }

    private static int ExtractTrailingNumber(string name)
    {
        int lastUnderscore = name.LastIndexOf('_');
        if (lastUnderscore >= 0 && lastUnderscore < name.Length - 1)
        {
            if (int.TryParse(name.Substring(lastUnderscore + 1), out int result))
                return result;
        }
        return 0;
    }

    private static void CreateAnimationClip(string path, Sprite[] sprites, float frameRate)
    {
        if (sprites == null || sprites.Length == 0) return;

        AnimationClip clip = new AnimationClip();
        clip.frameRate = frameRate;

        EditorCurveBinding binding = new EditorCurveBinding();
        binding.type = typeof(Image);
        binding.path = "";
        binding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe();
            keyframes[i].time = i / frameRate;
            keyframes[i].value = sprites[i];
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        // Desactivar Loop Time
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // Guardar clip
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
    }
}
