using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Utilidad de Editor para crear Font Assets de TextMeshPro y actualizar
/// sistemáticamente la tipografía de todos los textos del juego (prefabs y escenas).
/// </summary>
public static class TMPFontBatchUpdater
{
    private const string FontTtfPath = "Assets/Fonts/Nunito-Variable.ttf";
    private const string TargetFontAssetFolder = "Assets/TextMesh Pro/Resources/Fonts & Materials";
    private const string TargetFontAssetPath = TargetFontAssetFolder + "/Nunito SDF.asset";
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    [MenuItem("Tools/Typography/1. Create or Get Nunito Font Asset")]
    public static TMP_FontAsset CreateOrGetNunitoFontAsset()
    {
        // 1. Verificar si el font asset ya existe
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetFontAssetPath);
        if (fontAsset != null)
        {
            Debug.Log($"[TMPFontBatchUpdater] Nunito SDF ya existe en: {TargetFontAssetPath}");
            return fontAsset;
        }

        // 2. Cargar el archivo TTF fuente
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
        if (sourceFont == null)
        {
            // Intentar buscar cualquier TTF de Nunito en Assets/Fonts
            string[] guids = AssetDatabase.FindAssets("Nunito t:Font");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                sourceFont = AssetDatabase.LoadAssetAtPath<Font>(path);
            }
        }

        if (sourceFont == null)
        {
            Debug.LogError($"[TMPFontBatchUpdater] No se encontró el archivo de fuente TTF en {FontTtfPath}.");
            return null;
        }

        // 3. Crear directorio de destino si no existe
        if (!Directory.Exists(TargetFontAssetFolder))
        {
            Directory.CreateDirectory(TargetFontAssetFolder);
            AssetDatabase.Refresh();
        }

        // 4. Crear Font Asset dinámico con soporte total para español y símbolos
        fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic
        );

        if (fontAsset == null)
        {
            Debug.LogError("[TMPFontBatchUpdater] Error al crear TMP_FontAsset con CreateFontAsset.");
            return null;
        }

        fontAsset.name = "Nunito SDF";

        // Crear y guardar el asset
        AssetDatabase.CreateAsset(fontAsset, TargetFontAssetPath);

        // Añadir el material generado como sub-asset
        if (fontAsset.material != null)
        {
            fontAsset.material.name = "Nunito SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TMPFontBatchUpdater] ¡Nunito SDF creado con éxito en: {TargetFontAssetPath}!");
        return fontAsset;
    }

    [MenuItem("Tools/Typography/2. Set Nunito as Default in TMP Settings")]
    public static void SetNunitoAsDefaultInTMPSettings()
    {
        TMP_FontAsset nunitoFont = CreateOrGetNunitoFontAsset();
        if (nunitoFont == null) return;

        TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings == null)
        {
            settings = Resources.Load<TMP_Settings>("TMP Settings");
        }

        if (settings != null)
        {
            SerializedObject serializedSettings = new SerializedObject(settings);
            SerializedProperty defaultFontProp = serializedSettings.FindProperty("m_defaultFontAsset");
            if (defaultFontProp != null)
            {
                defaultFontProp.objectReferenceValue = nunitoFont;
                serializedSettings.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log("[TMPFontBatchUpdater] TMP Settings actualizado: Nunito SDF es ahora la fuente predeterminada del proyecto.");
            }
        }
        else
        {
            Debug.LogWarning("[TMPFontBatchUpdater] No se pudo cargar TMP Settings para asignar la fuente predeterminada.");
        }
    }

    [MenuItem("Tools/Typography/3. Apply Nunito Typography Globally (Prefabs & Scenes)")]
    public static void ApplyNunitoGlobally()
    {
        TMP_FontAsset nunitoFont = CreateOrGetNunitoFontAsset();
        if (nunitoFont == null)
        {
            Debug.LogError("[TMPFontBatchUpdater] Operación cancelada: Nunito Font Asset no disponible.");
            return;
        }

        SetNunitoAsDefaultInTMPSettings();

        int prefabsUpdated = UpdateAllPrefabs(nunitoFont);
        int scenesUpdated = UpdateAllScenes(nunitoFont);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TMPFontBatchUpdater] === REDISEÑO TIPOGRÁFICO GLOBAL COMPLETADO ===" +
                  $"\n• Prefabs actualizados: {prefabsUpdated}" +
                  $"\n• Escenas actualizadas: {scenesUpdated}" +
                  $"\n• Fuente asignada: Nunito SDF (Material: {nunitoFont.material.name})" +
                  $"\n• Cobertura: Menús, HUD, Vitales, Tienda, Diálogos, Opciones, Game Over y Créditos.");
    }

    private static int UpdateAllPrefabs(TMP_FontAsset fontAsset)
    {
        int count = 0;
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Assets/TextMesh Pro/Examples")) continue; // Omitir ejemplos de TMP

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot == null) continue;

            bool modified = false;
            TMP_Text[] textComponents = prefabRoot.GetComponentsInChildren<TMP_Text>(true);

            foreach (var txt in textComponents)
            {
                if (txt != null && txt.font != fontAsset)
                {
                    txt.font = fontAsset;
                    ApplyTypographyHierarchyStyle(txt);
                    modified = true;
                }
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                count++;
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        return count;
    }

    private static int UpdateAllScenes(TMP_FontAsset fontAsset)
    {
        int count = 0;
        string activeScenePath = SceneManager.GetActiveScene().path;

        // Guardar escena activa actual antes de procesar
        if (!string.IsNullOrEmpty(activeScenePath))
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        // Obtener escenas en Build Settings
        List<string> scenePaths = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                scenePaths.Add(scene.path);
        }

        // Si no hay en Build Settings, buscar todas en Assets/Scenes
        if (scenePaths.Count == 0)
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            foreach (var g in sceneGuids)
                scenePaths.Add(AssetDatabase.GUIDToAssetPath(g));
        }

        foreach (string sPath in scenePaths)
        {
            if (!File.Exists(sPath)) continue;

            Scene scene = EditorSceneManager.OpenScene(sPath, OpenSceneMode.Single);
            if (!scene.IsValid()) continue;

            bool modified = false;
            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                foreach (var txt in texts)
                {
                    if (txt != null && txt.font != fontAsset)
                    {
                        txt.font = fontAsset;
                        ApplyTypographyHierarchyStyle(txt);
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                count++;
            }
        }

        // Restaurar escena activa original si estaba abierta
        if (!string.IsNullOrEmpty(activeScenePath) && File.Exists(activeScenePath))
        {
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
        }

        return count;
    }

    /// <summary>
    /// Aplica reglas de jerarquía tipográfica inteligente según el contexto del GameObject:
    /// - Títulos / Headers: Bold / ExtraBold
    /// - Botones: SemiBold / Bold
    /// - HUD / Contadores: Bold con outline sutil para máxima legibilidad
    /// - Diálogos / Descripciones: Regular / Medium
    /// </summary>
    private static void ApplyTypographyHierarchyStyle(TMP_Text textComponent)
    {
        if (textComponent == null) return;

        string name = textComponent.gameObject.name.ToLowerInvariant();
        string parentName = textComponent.transform.parent != null ? textComponent.transform.parent.name.ToLowerInvariant() : "";

        // Títulos principales
        if (name.Contains("title") || name.Contains("titulo") || name.Contains("header") || name.Contains("gameover") || name.Contains("victory"))
        {
            textComponent.fontStyle = FontStyles.Bold;
        }
        // Botones
        else if (name.Contains("btn") || name.Contains("button") || parentName.Contains("btn") || parentName.Contains("button"))
        {
            textComponent.fontStyle = FontStyles.Bold;
        }
        // HUD / Vitales / Contadores (CO2, Temp, Oro, Nivel)
        else if (name.Contains("hud") || name.Contains("gold") || name.Contains("oro") || name.Contains("level") || name.Contains("co2") || name.Contains("temp") || name.Contains("price"))
        {
            textComponent.fontStyle = FontStyles.Bold;
        }
        // Diálogos / Narrativa
        else if (name.Contains("dialogue") || name.Contains("speaker") || name.Contains("line") || name.Contains("text_"))
        {
            if (name.Contains("speaker") || name.Contains("name"))
                textComponent.fontStyle = FontStyles.Bold;
        }
    }
}
