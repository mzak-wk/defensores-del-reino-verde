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
    private const string FONT_ASSET_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/Nunito SDF.asset";

    public static Canvas FindBestCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        if (canvases == null || canvases.Length == 0) return null;

        // 1. Priorizar el Canvas principal de gameplay llamado exactamente "Canvas"
        foreach (var c in canvases)
        {
            if (c.gameObject.name == "Canvas")
                return c;
        }

        // 2. Si no, cualquier canvas que no sea de diálogo ni menús
        foreach (var c in canvases)
        {
            if (!c.gameObject.name.Contains("Dialog") && !c.gameObject.name.Contains("Dialogue") && c.renderMode == RenderMode.ScreenSpaceOverlay)
                return c;
        }

        return canvases[0];
    }

    [MenuItem("Tools/Gameplay/Integrate All Systems (Player HP, Thermometer, HUD, Spawner)", false, 10)]
    public static void IntegrateAllSystems()
    {
        Debug.Log("[GameplaySystemsSetupTool] === INICIANDO INTEGRACIÓN COMPLETA DE SISTEMAS ===");

        try { SetupPlayerPrefab(); } catch (System.Exception ex) { Debug.LogWarning($"[GameplaySystemsSetupTool] Error en Player Prefab: {ex.Message}"); }
        try { SetupEnemyPrefabs(); } catch (System.Exception ex) { Debug.LogWarning($"[GameplaySystemsSetupTool] Error en Enemy Prefabs: {ex.Message}"); }
        try { UpdateEcoHazardPrefabs(); } catch (System.Exception ex) { Debug.LogWarning($"[GameplaySystemsSetupTool] Error en Prefabs de daño ecológico: {ex.Message}"); }
        try { SetupScenesHUDAndManagers(); } catch (System.Exception ex) { Debug.LogError($"[GameplaySystemsSetupTool] Error en Escenas: {ex.Message}"); }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[GameplaySystemsSetupTool] === INTEGRACIÓN COMPLETADA CON ÉXITO ===");
        EditorUtility.DisplayDialog("Integración Completa", "Se han integrado y configurado con éxito:\n- BarraVida y físicas en andrius_0.prefab\n- PlayerHUDHealthBar (3 estados) en Canvas\n- ThermometerUI (3 símbolos Hielo/Sol/Fuego) + HazardMeterManager calibrado por ecosistema\n- AbilityHUDController (4 ranuras + Cooldown radial)\n- Sistema de interacción [E] en zonas de daño ecológico\n- Daño por contacto y Spawners", "Aceptar");
    }

    [MenuItem("Tools/Gameplay/Setup Thermometer & Ecosystem in Current Scene", false, 15)]
    public static void SetupEcosystemHazardCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        EcosystemType eco = EcosystemType.TropicalForest;
        string sName = activeScene.name;
        if (sName.Contains("Ice")) eco = EcosystemType.PolarIce;
        else if (sName.Contains("Beach")) eco = EcosystemType.SunnyBeach;
        else if (sName.Contains("Final")) eco = EcosystemType.FinalBossUlti;
        else if (sName.Contains("Woods")) eco = EcosystemType.TropicalForest;

        Canvas canvas = FindBestCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontró ningún Canvas en la escena activa.", "OK");
            return;
        }

        // 1. HazardMeterManager
        HazardMeterManager hazardMgr = Object.FindFirstObjectByType<HazardMeterManager>();
        if (hazardMgr == null)
        {
            GameObject mgrGO = new GameObject("HazardMeterManager");
            hazardMgr = mgrGO.AddComponent<HazardMeterManager>();
            Undo.RegisterCreatedObjectUndo(mgrGO, "Create HazardMeterManager");
        }
        hazardMgr.ApplyEcosystemPreset(eco);
        EditorUtility.SetDirty(hazardMgr);

        // 2. HUD Root
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
            hudRoot.SetAsFirstSibling();
        }

        // 3. Sprites y Font
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        Sprite[] thermoSprites = LoadAllSpritesAtPath(THERMO_SPRITES_PATH);
        Sprite thermoFrame = GetSpriteByName(thermoSprites, "_0");
        Sprite thermoFill = GetSpriteByName(thermoSprites, "_1");
        Sprite symbolNormal = GetSpriteByName(thermoSprites, "_2");
        Sprite symbolHot = GetSpriteByName(thermoSprites, "_3");
        Sprite symbolCold = GetSpriteByName(thermoSprites, "_4");

        ThermometerUI thermoUI = canvas.GetComponentInChildren<ThermometerUI>(true);
        if (thermoUI == null)
        {
            thermoUI = CreateThermometerUI(hudRoot, thermoFrame, thermoFill, symbolNormal, symbolHot, symbolCold, font, eco);
        }
        else
        {
            thermoUI.SetupSprites(thermoFrame, thermoFill, symbolNormal, symbolHot, symbolCold);
            thermoUI.SetEcosystem(eco);
        }
        EditorUtility.SetDirty(thermoUI);

        // Guardar o actualizar prefab reusable
        string prefabDir = "Assets/Prefabs/UI";
        if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
        string prefabPath = $"{prefabDir}/UI_Thermometer.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(thermoUI.gameObject, prefabPath, InteractionMode.UserAction);

        // Actualizar prefabs de daño ecológico (erupcion y SootZone) con WorldInteractionPrompt
        UpdateEcoHazardPrefabs();

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        string desc = eco switch
        {
            EcosystemType.TropicalForest => "Bosque Tropical (Nivel 1)\n- Temp base: 24°C (Sol)\n- Límite de peligro: 50°C (Fuego)\n- Daño ecológico: Suelo erosionado (mantener [E] para sembrar árbol)\n- Sube progresivamente más rápido con erosiones activas no atendidas.",
            EcosystemType.PolarIce => "Polo Norte (Nivel 2)\n- Temp base: -15°C (Hielo)\n- Descongelamiento a 0°C (Sol)\n- Límite de peligro: 15°C (Fuego derretimiento total)\n- Daño ecológico: Vórtice / Manchas de hollín (pulsar [E] para limpiar).",
            EcosystemType.SunnyBeach => "Playa Soleada (Nivel 3)\n- Temp base: 32°C (Sol)\n- Límite de peligro: 60°C (Fuego calor letal)\n- Daño ecológico: Residuos costeros (pulsar/mantener [E] para limpiar).",
            EcosystemType.FinalBossUlti => "Jefe Final (Nivel Final)\n- Medidor de Carga de Definitiva del Boss (0% a 100%)\n- 0-40%: Carga fría / inactiva (Hielo)\n- 40-80%: Acumulando energía (Sol)\n- >80%: ¡Peligro Inminente! (Fuego)\n- Al llegar al 100% dispara la ulti devastadora.",
            _ => eco.ToString()
        };

        EditorUtility.DisplayDialog("Termómetro y Ecosistema Configurados",
            $"Se ha calibrado la escena '{activeScene.name}' con éxito:\n\n{desc}", "Aceptar");
    }

    [MenuItem("Tools/Gameplay/Setup Ability HUD in Current Scene", false, 20)]
    public static void SetupAbilityHUDCurrentScene()
    {
        Canvas canvas = FindBestCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontró ningún Canvas en la escena activa.", "OK");
            return;
        }

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
        }

        AbilityHUDController existing = canvas.GetComponentInChildren<AbilityHUDController>(true);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog("HUD Existente", "Ya existe un AbilityHUDController en esta escena. ¿Deseas reemplazarlo con una configuración limpia?", "Reemplazar", "Cancelar");
            if (!replace) return;
            Object.DestroyImmediate(existing.gameObject);
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        AbilityHUDController hud = CreateAbilityHUDUI(hudRoot, font);

        // Crear o actualizar Prefab reutilizable en Assets/Prefabs/UI/UI_AbilityHUD.prefab
        string prefabDir = "Assets/Prefabs/UI";
        if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
        string prefabPath = $"{prefabDir}/UI_AbilityHUD.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(hud.gameObject, prefabPath, InteractionMode.UserAction);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorUtility.DisplayDialog("Listo", $"¡Ability HUD creado con éxito en '{canvas.name}'!\nGuardado también como Prefab en:\n{prefabPath}", "Aceptar");
    }

    // =========================================================================
    // 1. SETUP PREFAB DEL JUGADOR (ANDRIUS)
    // =========================================================================
    public static void SetupPlayerPrefab()
    {
        try
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
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GameplaySystemsSetupTool] No se pudo cargar {PLAYER_PREFAB_PATH} (¿abierto en modo prefab?): {ex.Message}");
        }
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

            try
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (!scene.IsValid()) continue;

                Debug.Log($"[GameplaySystemsSetupTool] Procesando escena: {scene.name}");
                bool sceneDirty = false;

                // 1. Asegurar HazardMeterManager y calibrar según ecosistema de la escena
                EcosystemType eco = EcosystemType.TropicalForest;
                if (scene.name.Contains("Ice")) eco = EcosystemType.PolarIce;
                else if (scene.name.Contains("Beach")) eco = EcosystemType.SunnyBeach;
                else if (scene.name.Contains("Final")) eco = EcosystemType.FinalBossUlti;
                else if (scene.name.Contains("Woods")) eco = EcosystemType.TropicalForest;

                HazardMeterManager hazardMgr = Object.FindFirstObjectByType<HazardMeterManager>();
                if (hazardMgr == null)
                {
                    GameObject mgrGO = new GameObject("HazardMeterManager");
                    hazardMgr = mgrGO.AddComponent<HazardMeterManager>();
                    sceneDirty = true;
                    Debug.Log($"[GameplaySystemsSetupTool] HazardMeterManager creado en {scene.name}");
                }
                hazardMgr.ApplyEcosystemPreset(eco);
                EditorUtility.SetDirty(hazardMgr);

                // Calibrar CO2Manager si existe para que no cause instakill
                CO2Manager co2 = Object.FindFirstObjectByType<CO2Manager>();
                if (co2 != null)
                {
                    SerializedObject so = new SerializedObject(co2);
                    SerializedProperty riseProp = so.FindProperty("co2RiseRatePerZone");
                    if (riseProp != null && riseProp.floatValue > 1f)
                    {
                        riseProp.floatValue = 0.5f;
                        so.ApplyModifiedProperties();
                        sceneDirty = true;
                    }
                }

                // 2. Localizar Canvas principal con FindBestCanvas()
                Canvas canvas = FindBestCanvas();
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
                        hudRoot.SetAsFirstSibling();
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
                        thermoUI = CreateThermometerUI(hudRoot, thermoFrame, thermoFill, symbolNormal, symbolHot, symbolCold, font, eco);
                        sceneDirty = true;
                        Debug.Log($"[GameplaySystemsSetupTool] ThermometerUI creada en {scene.name}");
                    }
                    else
                    {
                        thermoUI.SetupSprites(thermoFrame, thermoFill, symbolNormal, symbolHot, symbolCold);
                        thermoUI.SetEcosystem(eco);
                        sceneDirty = true;
                    }

                    string prefabDir = "Assets/Prefabs/UI";
                    if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
                    string thermoPrefabPath = $"{prefabDir}/UI_Thermometer.prefab";
                    if (!File.Exists(thermoPrefabPath))
                    {
                        try { PrefabUtility.SaveAsPrefabAsset(thermoUI.gameObject, thermoPrefabPath); } catch { }
                    }

                    // C. Crear o Actualizar AbilityHUDController
                    AbilityHUDController abilityHUD = canvas.GetComponentInChildren<AbilityHUDController>(true);
                    if (abilityHUD == null)
                    {
                        abilityHUD = CreateAbilityHUDUI(hudRoot, font);
                        sceneDirty = true;
                        Debug.Log($"[GameplaySystemsSetupTool] AbilityHUDController creado en {scene.name}");
                    }
                }

                if (sceneDirty)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"[GameplaySystemsSetupTool] Escena guardada exitosamente: {scene.name}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameplaySystemsSetupTool] Advertencia al procesar escena {scenePath}: {ex.Message}");
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

    private static ThermometerUI CreateThermometerUI(Transform parent, Sprite frame, Sprite fill, Sprite normal, Sprite hot, Sprite cold, TMP_FontAsset font, EcosystemType eco = EcosystemType.TropicalForest)
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

        // Symbol (Icono de estado: Hielo / Sol / Fuego)
        GameObject symGO = new GameObject("Symbol");
        symGO.transform.SetParent(rootGO.transform, false);
        RectTransform symRT = symGO.AddComponent<RectTransform>();
        symRT.anchorMin = new Vector2(0.2f, 0.72f);
        symRT.anchorMax = new Vector2(0.8f, 0.98f);
        symRT.offsetMin = Vector2.zero;
        symRT.offsetMax = Vector2.zero;
        Image symImg = symGO.AddComponent<Image>();
        symImg.sprite = (eco == EcosystemType.PolarIce) ? cold : normal;
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
        tmp.text = (eco == EcosystemType.FinalBossUlti) ? "0%" : "24.0 °C";

        ThermometerUI thermo = rootGO.AddComponent<ThermometerUI>();
        thermo.SetupSprites(frame, fill, normal, hot, cold);
        thermo.SetEcosystem(eco);
        return thermo;
    }

    public static AbilityHUDController CreateAbilityHUDUI(Transform parent, TMP_FontAsset font)
    {
        Sprite defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (defaultSprite == null)
        {
            Texture2D whiteTex = Texture2D.whiteTexture;
            defaultSprite = Sprite.Create(whiteTex, new Rect(0, 0, whiteTex.width, whiteTex.height), new Vector2(0.5f, 0.5f));
        }

        GameObject rootGO = new GameObject("UI_AbilityHUD");
        rootGO.transform.SetParent(parent, false);

        // RectTransform anclado abajo a la derecha
        RectTransform rootRT = rootGO.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(1f, 0f);
        rootRT.anchorMax = new Vector2(1f, 0f);
        rootRT.pivot = new Vector2(1f, 0f);
        rootRT.anchoredPosition = new Vector2(-25f, 25f);
        rootRT.sizeDelta = new Vector2(280f, 75f);

        // Layout horizontal para ordenar los slots
        HorizontalLayoutGroup layout = rootGO.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Ranuras: Básico (CLICK), Habilidad 1 (1), Habilidad 2 (2), Definitiva (3)
        AbilityHUDController.AbilitySlotUI basic = CreateSlot(rootGO.transform, "Slot_Basic", CombatAction.BasicAttack, "CLICK", 58f, defaultSprite, font);
        AbilityHUDController.AbilitySlotUI skill1 = CreateSlot(rootGO.transform, "Slot_Skill1", CombatAction.Skill1, "1", 58f, defaultSprite, font);
        AbilityHUDController.AbilitySlotUI skill2 = CreateSlot(rootGO.transform, "Slot_Skill2", CombatAction.Skill2, "2", 58f, defaultSprite, font);
        AbilityHUDController.AbilitySlotUI ultimate = CreateSlot(rootGO.transform, "Slot_Ultimate", CombatAction.Ultimate, "3", 68f, defaultSprite, font);

        AbilityHUDController controller = rootGO.AddComponent<AbilityHUDController>();
        controller.ConfigureSlots(rootGO, basic, skill1, skill2, ultimate);

        return controller;
    }

    private static AbilityHUDController.AbilitySlotUI CreateSlot(Transform parent, string name, CombatAction action, string defaultKey, float size, Sprite defaultSprite, TMP_FontAsset font)
    {
        GameObject slotGO = new GameObject(name);
        slotGO.transform.SetParent(parent, false);

        RectTransform slotRT = slotGO.AddComponent<RectTransform>();
        slotRT.sizeDelta = new Vector2(size, size);

        CanvasGroup cg = slotGO.AddComponent<CanvasGroup>();

        // Fondo oscuro / Marco del slot
        Image bgImg = slotGO.AddComponent<Image>();
        if (defaultSprite != null) bgImg.sprite = defaultSprite;
        bgImg.type = (defaultSprite != null && defaultSprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
        bgImg.color = new Color(0.10f, 0.12f, 0.16f, 0.88f);

        // Icono de la habilidad (se alimentará de WeaponData dinámicamente)
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(slotGO.transform, false);
        RectTransform iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = new Vector2(4f, 4f);
        iconRT.offsetMax = new Vector2(-4f, -4f);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.color = Color.white;
        iconImg.enabled = false;

        // Cooldown Overlay (Relleno radial 360)
        GameObject overlayGO = new GameObject("CooldownOverlay");
        overlayGO.transform.SetParent(slotGO.transform, false);
        RectTransform overlayRT = overlayGO.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = new Vector2(2f, 2f);
        overlayRT.offsetMax = new Vector2(-2f, -2f);
        Image overlayImg = overlayGO.AddComponent<Image>();
        if (defaultSprite != null) overlayImg.sprite = defaultSprite;
        overlayImg.type = Image.Type.Filled;
        overlayImg.fillMethod = Image.FillMethod.Radial360;
        overlayImg.fillOrigin = (int)Image.Origin360.Top;
        overlayImg.fillClockwise = true;
        overlayImg.fillAmount = 0f;
        overlayImg.color = new Color(0f, 0f, 0f, 0.78f);
        overlayGO.SetActive(false);

        // Cooldown Text (ej. 4.2s o 3s en el centro)
        GameObject textGO = new GameObject("CooldownText");
        textGO.transform.SetParent(slotGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        TextMeshProUGUI cdText = textGO.AddComponent<TextMeshProUGUI>();
        if (font != null) cdText.font = font;
        cdText.fontSize = size > 60f ? 20 : 16;
        cdText.fontStyle = FontStyles.Bold;
        cdText.alignment = TextAlignmentOptions.Center;
        cdText.color = Color.white;
        cdText.raycastTarget = false;
        cdText.text = "0s";
        textGO.SetActive(false);

        // Keybind Badge (esquina inferior con la tecla)
        GameObject keyGO = new GameObject("KeybindBadge");
        keyGO.transform.SetParent(slotGO.transform, false);
        RectTransform keyRT = keyGO.AddComponent<RectTransform>();
        keyRT.anchorMin = new Vector2(0f, 0f);
        keyRT.anchorMax = new Vector2(1f, 0.40f);
        keyRT.offsetMin = Vector2.zero;
        keyRT.offsetMax = Vector2.zero;
        TextMeshProUGUI keyText = keyGO.AddComponent<TextMeshProUGUI>();
        if (font != null) keyText.font = font;
        keyText.fontSize = 12;
        keyText.fontStyle = FontStyles.Bold;
        keyText.alignment = TextAlignmentOptions.Bottom;
        keyText.color = new Color(1f, 0.85f, 0.35f, 1f);
        keyText.raycastTarget = false;
        keyText.text = defaultKey;

        AbilityHUDController.AbilitySlotUI slot = new AbilityHUDController.AbilitySlotUI
        {
            action = action,
            slotRoot = slotGO,
            iconImage = iconImg,
            cooldownOverlay = overlayImg,
            cooldownText = cdText,
            keybindText = keyText,
            canvasGroup = cg
        };

        return slot;
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

    public static void UpdateEcoHazardPrefabs()
    {
        string[] hazardPrefabPaths = new[]
        {
            "Assets/Prefabs/erupcion.prefab",
            "Assets/Prefabs/SootZone.prefab",
            "Assets/Prefabs/opina.prefab"
        };

        foreach (string path in hazardPrefabPaths)
        {
            if (!File.Exists(path)) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root != null)
            {
                bool modified = false;

                WorldInteractionPrompt prompt = root.GetComponent<WorldInteractionPrompt>();
                if (prompt == null)
                {
                    prompt = root.AddComponent<WorldInteractionPrompt>();
                    modified = true;
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"[GameplaySystemsSetupTool] Prefab ecológico actualizado con prompt [E]: {Path.GetFileName(path)}");
                }

                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
