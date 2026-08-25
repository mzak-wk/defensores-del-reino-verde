using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Herramienta de Editor para validar, reparar y configurar de forma automatizada:
/// 1. Componente BarraVida y físicas balanceadas en andrius_0.prefab.
/// 2. Barra de vida dinámica del jugador (PlayerHUDHealthBar) con 3 estados en el Canvas/HUD.
/// 3. Medidor de peligro/termómetro (HazardMeterManager + ThermometerUI) con 3 símbolos en el Canvas/HUD.
/// 4. Verificación de DamageOnContact y colliders en prefabs de enemigos.
/// </summary>
public static class GameplaySystemsSetupTool
{
    private const string PLAYER_PREFAB_PATH = "Assets/Prefabs/andrius_0.prefab";
    private const string HEALTH_BAR_SPRITES_PATH = "Assets/sprites(r)/UI/barra de vida UI + relleno (s).png";
    private const string THERMO_SPRITES_PATH = "Assets/sprites(r)/UI/termomeñemaconassetsdefuegiothieloyvaina+rellenodeldiablasasaso.png";
    private const string FONT_ASSET_PATH = "Assets/Fonts/Nunito-VariableFont_wght SDF.asset";

    [MenuItem("Tools/Gameplay/Integrate All Systems (Player HP, Thermometer, HUD, Spawner)", false, 10)]
    public static void IntegrateAllSystems()
    {
        Debug.Log("[GameplaySystemsSetupTool] === INICIANDO INTEGRACIÓN COMPLETA DE SISTEMAS ===");

        // 1. Configurar Prefab del Jugador (Andrius)
        SetupPlayerPrefab();

        // 2. Configurar Prefabs de Enemigos
        SetupEnemyPrefabs();

        // 3. Integrar en todas las escenas del build
        SetupScenesHUDAndManagers();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[GameplaySystemsSetupTool] === INTEGRACIÓN COMPLETADA CON ÉXITO ===");
        EditorUtility.DisplayDialog("Integración Completa", "Se han integrado y configurado con éxito:\n- BarraVida y físicas en andrius_0.prefab\n- PlayerHUDHealthBar (3 estados) en Canvas\n- ThermometerUI (3 símbolos) + HazardMeterManager\n- Daño por contacto y Spawners", "Aceptar");
    }

    // =========================================================================
    // 1. SETUP PREFAB DEL JUGADOR (ANDRIUS)
    // =========================================================================
    public static void SetupPlayerPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PLAYER_PREFAB_PATH);
        if (prefabRoot == null)
        {
            Debug.LogError($"[GameplaySystemsSetupTool] No se pudo cargar el prefab en {PLAYER_PREFAB_PATH}");
            return;
        }

        bool modified = false;

        // BarraVida
        BarraVida bv = prefabRoot.GetComponent<BarraVida>();
        if (bv == null)
        {
            bv = prefabRoot.AddComponent<BarraVida>();
            modified = true;
            Debug.Log("[GameplaySystemsSetupTool] BarraVida añadida a andrius_0.prefab.");
        }

        bv.maxHP = 100f;
        bv.currentHP = 100f;
        bv.invulnTimeOnHit = 0.3f;
        bv.regenPerSecond = 0.2f;

        // Rigidbody2D balanceado
        Rigidbody2D rb = prefabRoot.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (rb.mass != 10f)
            {
                rb.mass = 10f;
                modified = true;
            }
            rb.linearDamping = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (modified)
        {
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PLAYER_PREFAB_PATH);
            Debug.Log("[GameplaySystemsSetupTool] andrius_0.prefab actualizado y guardado.");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    // =========================================================================
    // 2. SETUP PREFABS DE ENEMIGOS
    // =========================================================================
    public static void SetupEnemyPrefabs()
    {
        string[] enemyGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        foreach (string guid in enemyGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            Enemy enemy = root.GetComponent<Enemy>();
            if (enemy != null)
            {
                bool modified = false;

                // Asegurar DamageOnContact
                DamageOnContact doc = root.GetComponent<DamageOnContact>();
                if (doc == null)
                {
                    doc = root.AddComponent<DamageOnContact>();
                    modified = true;
                }
                doc.damageRate = 0.5f;

                // Balancear Rigidbody2D
                Rigidbody2D rb = root.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    if (rb.mass != 10f) { rb.mass = 10f; modified = true; }
                    if (rb.linearDamping < 2f) { rb.linearDamping = 3f; modified = true; }
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"[GameplaySystemsSetupTool] Prefab enemigo actualizado: {Path.GetFileName(path)}");
                }
            }

            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // =========================================================================
    // 3. SETUP ESCENAS: CANVAS HUD + MANAGERS
    // =========================================================================
    public static void SetupScenesHUDAndManagers()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        Sprite[] healthSprites = LoadAllSpritesAtPath(HEALTH_BAR_SPRITES_PATH);
        Sprite[] thermoSprites = LoadAllSpritesAtPath(THERMO_SPRITES_PATH);

        // Extraer sprites de salud (6 sprites)
        Sprite frameHigh = GetSpriteByName(healthSprites, "_0");
        Sprite fillHigh = GetSpriteByName(healthSprites, "_1");
        Sprite frameMid = GetSpriteByName(healthSprites, "_2");
        Sprite fillMid = GetSpriteByName(healthSprites, "_3");
        Sprite frameLow = GetSpriteByName(healthSprites, "_4");
        Sprite fillLow = GetSpriteByName(healthSprites, "_5");

        // Extraer sprites de termómetro (5 sprites)
        Sprite thermoFrame = GetSpriteByName(thermoSprites, "_0");
        Sprite thermoFill = GetSpriteByName(thermoSprites, "_1");
        Sprite symbolNormal = GetSpriteByName(thermoSprites, "_2");
        Sprite symbolHot = GetSpriteByName(thermoSprites, "_3");
        Sprite symbolCold = GetSpriteByName(thermoSprites, "_4");

        string currentScenePath = SceneManager.GetActiveScene().path;

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        List<string> scenePaths = new List<string>();

        if (scenes.Length > 0)
        {
            foreach (var s in scenes)
                if (s.enabled && !string.IsNullOrEmpty(s.path))
                    scenePaths.Add(s.path);
        }
        else
        {
            string[] found = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            foreach (var guid in found)
                scenePaths.Add(AssetDatabase.GUIDToAssetPath(guid));
        }

        foreach (string scenePath in scenePaths)
        {
            if (scenePath.ToLowerInvariant().Contains("menu") || scenePath.ToLowerInvariant().Contains("select"))
                continue; // Omitir menús puros sin gameplay

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) continue;

            Debug.Log($"[GameplaySystemsSetupTool] Procesando escena: {scene.name}");
            bool sceneDirty = false;

            // 1. Asegurar HazardMeterManager
            HazardMeterManager hazardMgr = Object.FindFirstObjectByType<HazardMeterManager>();
            if (hazardMgr == null)
            {
                GameObject mgrGO = new GameObject("HazardMeterManager");
                hazardMgr = mgrGO.AddComponent<HazardMeterManager>();
                sceneDirty = true;
                Debug.Log($"[GameplaySystemsSetupTool] HazardMeterManager creado en {scene.name}");
            }

            // 2. Localizar Canvas principal
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                // Configurar contenedor HUD si no existe
                Transform hudRoot = canvas.transform.Find("HUD_Gameplay");
                if (hudRoot == null)
                {
                    GameObject hudGO = new GameObject("HUD_Gameplay");
                    hudGO.transform.SetParent(canvas.transform, false);
                    RectTransform hudRT = hudGO.AddComponent<RectTransform>();
                    hudRT.anchorMin = Vector2.zero;
                    hudRT.anchorMax = Vector2.one;
                    hudRT.offsetMin = Vector2.zero;
                    hudRT.offsetMax = Vector2.zero;
                    hudRoot = hudGO.transform;
                    hudRoot.SetAsFirstSibling(); // Para quedar detrás de diálogos/Game Over
                    sceneDirty = true;
                }

                // A. Crear o Actualizar PlayerHUDHealthBar
                PlayerHUDHealthBar healthBarUI = canvas.GetComponentInChildren<PlayerHUDHealthBar>(true);
                if (healthBarUI == null)
                {
                    healthBarUI = CreateHealthBarUI(hudRoot, frameHigh, fillHigh, frameMid, fillMid, frameLow, fillLow, font);
                    sceneDirty = true;
                    Debug.Log($"[GameplaySystemsSetupTool] PlayerHUDHealthBar creada en {scene.name}");
                }
                else
                {
                    healthBarUI.SetupSprites(frameHigh, fillHigh, frameMid, fillMid, frameLow, fillLow);
                    sceneDirty = true;
                }

                // B. Crear o Actualizar ThermometerUI
                ThermometerUI thermoUI = canvas.GetComponentInChildren<ThermometerUI>(true);
                if (thermoUI == null)
                {
                    thermoUI = CreateThermometerUI(hudRoot, thermoFrame, thermoFill, symbolNormal, symbolHot, symbolCold, font);
                    sceneDirty = true;
                    Debug.Log($"[GameplaySystemsSetupTool] ThermometerUI creada en {scene.name}");
                }
                else
                {
                    thermoUI.SetupSprites(thermoFrame, thermoFill, symbolNormal, symbolHot, symbolCold);
                    sceneDirty = true;
                }
            }

            if (sceneDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[GameplaySystemsSetupTool] Escena guardada: {scene.name}");
            }
        }

        if (!string.IsNullOrEmpty(currentScenePath))
        {
            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
        }
    }

    private static PlayerHUDHealthBar CreateHealthBarUI(Transform parent, Sprite fHigh, Sprite fillH, Sprite fMid, Sprite fillM, Sprite fLow, Sprite fillL, TMP_FontAsset font)
    {
        GameObject rootGO = new GameObject("UI_PlayerHealthBar");
        rootGO.transform.SetParent(parent, false);

        // Posicionamiento calibrado: Arriba a la izquierda (Top-Left Anchor)
        RectTransform rootRT = rootGO.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0f, 1f);
        rootRT.anchorMax = new Vector2(0f, 1f);
        rootRT.pivot = new Vector2(0f, 1f);
        rootRT.anchoredPosition = new Vector2(30f, -30f);
        rootRT.sizeDelta = new Vector2(320f, 90f);

        // Fill Delay (Chip damage)
        GameObject delayGO = new GameObject("FillDelay");
        delayGO.transform.SetParent(rootGO.transform, false);
        RectTransform delayRT = delayGO.AddComponent<RectTransform>();
        delayRT.anchorMin = new Vector2(0.23f, 0.32f);
        delayRT.anchorMax = new Vector2(0.88f, 0.72f);
        delayRT.offsetMin = Vector2.zero;
        delayRT.offsetMax = Vector2.zero;
        Image delayImg = delayGO.AddComponent<Image>();
        delayImg.sprite = fillH;
        delayImg.color = new Color(1f, 0.9f, 0.5f, 0.85f);
        delayImg.type = Image.Type.Filled;
        delayImg.fillMethod = Image.FillMethod.Horizontal;
        delayImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        delayImg.fillAmount = 1f;

        // Fill Instant
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(rootGO.transform, false);
        RectTransform fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0.23f, 0.32f);
        fillRT.anchorMax = new Vector2(0.88f, 0.72f);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.sprite = fillH;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f;

        // Frame
        GameObject frameGO = new GameObject("Frame");
        frameGO.transform.SetParent(rootGO.transform, false);
        RectTransform frameRT = frameGO.AddComponent<RectTransform>();
        frameRT.anchorMin = Vector2.zero;
        frameRT.anchorMax = Vector2.one;
        frameRT.offsetMin = Vector2.zero;
        frameRT.offsetMax = Vector2.zero;
        Image frameImg = frameGO.AddComponent<Image>();
        frameImg.sprite = fHigh;
        frameImg.preserveAspect = true;

        // Text
        GameObject textGO = new GameObject("HPText");
        textGO.transform.SetParent(rootGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.25f, 0.32f);
        textRT.anchorMax = new Vector2(0.86f, 0.72f);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = "100 / 100";

        PlayerHUDHealthBar bar = rootGO.AddComponent<PlayerHUDHealthBar>();
        bar.SetupSprites(fHigh, fillH, fMid, fillM, fLow, fillL);
        return bar;
    }

    private static ThermometerUI CreateThermometerUI(Transform parent, Sprite frame, Sprite fill, Sprite normal, Sprite hot, Sprite cold, TMP_FontAsset font)
    {
        GameObject rootGO = new GameObject("UI_Thermometer");
        rootGO.transform.SetParent(parent, false);

        RectTransform rootRT = rootGO.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(1f, 1f);
        rootRT.anchorMax = new Vector2(1f, 1f);
        rootRT.pivot = new Vector2(1f, 1f);
        rootRT.anchoredPosition = new Vector2(-25f, -25f);
        rootRT.sizeDelta = new Vector2(90f, 180f);

        // Fill Vertical
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(rootGO.transform, false);
        RectTransform fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0.35f, 0.12f);
        fillRT.anchorMax = new Vector2(0.65f, 0.88f);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.sprite = fill;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Vertical;
        fillImg.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillImg.fillAmount = 0f;

        // Frame
        GameObject frameGO = new GameObject("Frame");
        frameGO.transform.SetParent(rootGO.transform, false);
        RectTransform frameRT = frameGO.AddComponent<RectTransform>();
        frameRT.anchorMin = Vector2.zero;
        frameRT.anchorMax = Vector2.one;
        frameRT.offsetMin = Vector2.zero;
        frameRT.offsetMax = Vector2.zero;
        Image frameImg = frameGO.AddComponent<Image>();
        frameImg.sprite = frame;
        frameImg.preserveAspect = true;

        // Symbol
        GameObject symGO = new GameObject("Symbol");
        symGO.transform.SetParent(rootGO.transform, false);
        RectTransform symRT = symGO.AddComponent<RectTransform>();
        symRT.anchorMin = new Vector2(0.2f, 0.72f);
        symRT.anchorMax = new Vector2(0.8f, 0.98f);
        symRT.offsetMin = Vector2.zero;
        symRT.offsetMax = Vector2.zero;
        Image symImg = symGO.AddComponent<Image>();
        symImg.sprite = normal;
        symImg.preserveAspect = true;

        // Text
        GameObject textGO = new GameObject("TempText");
        textGO.transform.SetParent(rootGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(-0.2f, -0.25f);
        textRT.anchorMax = new Vector2(1.2f, 0.05f);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 16;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = "0.0 °C";

        ThermometerUI thermo = rootGO.AddComponent<ThermometerUI>();
        thermo.SetupSprites(frame, fill, normal, hot, cold);
        return thermo;
    }

    private static Sprite[] LoadAllSpritesAtPath(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        List<Sprite> list = new List<Sprite>();
        foreach (var a in assets)
        {
            if (a is Sprite s)
                list.Add(s);
        }
        return list.ToArray();
    }

    private static Sprite GetSpriteByName(Sprite[] sprites, string suffixOrName)
    {
        if (sprites == null || sprites.Length == 0) return null;
        foreach (var s in sprites)
        {
            if (s != null && s.name.EndsWith(suffixOrName))
                return s;
        }
        return sprites[0];
    }
}
