using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Herramienta para auditar y limpiar todas las escenas del proyecto,
/// eliminando paneles de Game Over antiguos/duplicados y asegurando que
/// se use únicamente la instancia del Prefab 'Panel_GameOver.prefab'.
/// </summary>
public static class SceneGameOverLinker
{
    private const string PrefabPath = "Assets/Prefabs/Panel_GameOver.prefab";

    [MenuItem("Tools/Death System/Replace Legacy Game Over Panels in All Scenes")]
    public static void CleanAndLinkAllScenes()
    {
        string[] scenePaths = new string[]
        {
            "Assets/Scenes/LevelWoods.unity",
            "Assets/Scenes/LevelIce.unity",
            "Assets/Scenes/LevelBeach.unity",
            "Assets/Scenes/LevelFinal.unity",
            "Assets/Scenes/LevelHUB.unity"
        };

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[SceneGameOverLinker] No se encontró el prefab en {PrefabPath}. Construye el prefab primero.");
            return;
        }

        string currentActiveScene = SceneManager.GetActiveScene().path;

        foreach (string scenePath in scenePaths)
        {
            if (!System.IO.File.Exists(scenePath)) continue;

            Debug.Log($"[SceneGameOverLinker] Procesando escena: {scenePath}...");
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 1. Buscar todos los objetos llamados 'Panel_GameOver' o que tengan GameOverController/DeathMenuController
            List<GameObject> toDestroy = new List<GameObject>();
            GameObject mainCanvasGo = null;

            // Encontrar Canvas principal
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.name == "Canvas" || c.name == "Canvas_HUD" || c.name == "HUDCanvas")
                {
                    mainCanvasGo = c.gameObject;
                    break;
                }
            }
            if (mainCanvasGo == null && canvases.Length > 0)
            {
                mainCanvasGo = canvases[0].gameObject;
            }

            if (mainCanvasGo == null)
            {
                Debug.LogWarning($"[SceneGameOverLinker] No se encontró ningún Canvas en {scenePath}");
                continue;
            }

            // Identificar paneles viejos
            GameObject[] allObjects = scene.GetRootGameObjects();
            List<GameObject> allDescendants = new List<GameObject>();
            foreach (var root in allObjects)
            {
                GetDescendantsRecursive(root.transform, allDescendants);
            }

            GameObject validPrefabPanel = null;

            foreach (var go in allDescendants)
            {
                if (go == null) continue;

                bool isGameOverPanel = go.name == "Panel_GameOver" || go.name == "Panel_Gameover";
                bool isGameOverControllerHolder = go.name == "GameOver_Controller" || go.name == "GameOverManager";

                if (isGameOverPanel)
                {
                    // Si ya es una instancia del nuevo prefab que tiene DeathMenuController y ContentRoot
                    if (go.GetComponent<DeathMenuController>() != null && go.transform.Find("ContentRoot") != null)
                    {
                        if (validPrefabPanel == null && go.transform.IsChildOf(mainCanvasGo.transform))
                        {
                            validPrefabPanel = go;
                        }
                        else
                        {
                            toDestroy.Add(go);
                        }
                    }
                    else
                    {
                        toDestroy.Add(go);
                    }
                }
                else if (isGameOverControllerHolder)
                {
                    toDestroy.Add(go);
                }
            }

            // Eliminar paneles viejos y duplicados
            foreach (var old in toDestroy)
            {
                Debug.Log($"[SceneGameOverLinker] Eliminando objeto legado/duplicado: '{old.name}' en {scenePath}");
                Object.DestroyImmediate(old);
            }

            // Si no hay un Panel_GameOver válido del prefab bajo el Canvas, instanciarlo
            if (validPrefabPanel == null)
            {
                GameObject newPanel = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mainCanvasGo.transform);
                newPanel.name = "Panel_GameOver";
                RectTransform rt = newPanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                }
                newPanel.SetActive(false);
                validPrefabPanel = newPanel;
                Debug.Log($"[SceneGameOverLinker] Instancia de '{PrefabPath}' agregada bajo {mainCanvasGo.name} en {scenePath}");
            }

            // Desactivar el panel en inicio para que esté listo cuando muera el personaje
            validPrefabPanel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // Restaurar escena previa si es válida
        if (!string.IsNullOrEmpty(currentActiveScene) && System.IO.File.Exists(currentActiveScene))
        {
            EditorSceneManager.OpenScene(currentActiveScene, OpenSceneMode.Single);
        }

        Debug.Log("[SceneGameOverLinker] ¡Todas las escenas han sido actualizadas con el nuevo Panel_GameOver!");
    }

    private static void GetDescendantsRecursive(Transform parent, List<GameObject> results)
    {
        results.Add(parent.gameObject);
        for (int i = 0; i < parent.childCount; i++)
        {
            GetDescendantsRecursive(parent.GetChild(i), results);
        }
    }
}
