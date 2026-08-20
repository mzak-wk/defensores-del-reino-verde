using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class FixEnemyPrefabs : EditorWindow
{
    private static readonly string[] enemyPrefabPaths = new[]
    {
        "Assets/Prefabs/Enemigo_normal.prefab",
        "Assets/Prefabs/Enemigo_normal 2.prefab",
        "Assets/Prefabs/Enemigo_normal 3.prefab",
        "Assets/Prefabs/Enemigo_normal 4.prefab",
        "Assets/Prefabs/Mini-boss.prefab",
        "Assets/Prefabs/Mini-boss 2.prefab",
        "Assets/Prefabs/Mini-boss 3.prefab",
        "Assets/Prefabs/Mini-boss 4.prefab",
        "Assets/Prefabs/Gorgona.prefab",
        "Assets/Prefabs/Pajalutro.prefab",
        "Assets/Prefabs/Pajalutro2.prefab",
        "Assets/Prefabs/rojasidle_0.prefab",
        "Assets/Prefabs/Bosses/Deforestador.prefab",
    };

    private static readonly string hitboxLayerName = "EnemyHitbox";
    private static int hitboxLayer;

    [MenuItem("Tools/Enemy Prefabs/Fix All Enemy Prefabs")]
    public static void FixAllEnemyPrefabs()
    {
        EnsureHitboxLayer();

        int fixedCount = 0;
        int errorCount = 0;

        foreach (string path in enemyPrefabPaths)
        {
            try
            {
                FixPrefab(path);
                fixedCount++;
                Debug.Log($"[FixEnemyPrefabs] Fixed: {path}");
            }
            catch (System.Exception e)
            {
                errorCount++;
                Debug.LogError($"[FixEnemyPrefabs] Error fixing {path}: {e.Message}\n{e.StackTrace}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Fix Complete",
            $"Fixed: {fixedCount} prefabs\nErrors: {errorCount}",
            "OK");
    }

    [MenuItem("Tools/Enemy Prefabs/Fix Single Prefab")]
    public static void FixSinglePrefab()
    {
        string path = EditorUtility.OpenFilePanel("Select Enemy Prefab", "Assets/Prefabs", "prefab");
        if (!string.IsNullOrEmpty(path))
        {
            string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
            EnsureHitboxLayer();
            FixPrefab(relativePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void EnsureHitboxLayer()
    {
        hitboxLayer = LayerMask.NameToLayer(hitboxLayerName);
        if (hitboxLayer == -1)
        {
            // Layer doesn't exist, we'll create it via SerializedObject on TagManager
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < 31; i++)
            {
                SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
                if (layerSP.stringValue == "")
                {
                    layerSP.stringValue = hitboxLayerName;
                    tagManager.ApplyModifiedProperties();
                    hitboxLayer = i;
                    Debug.Log($"[FixEnemyPrefabs] Created layer '{hitboxLayerName}' at index {i}");
                    break;
                }
            }
        }
        else
        {
            Debug.Log($"[FixEnemyPrefabs] Layer '{hitboxLayerName}' already exists at index {hitboxLayer}");
        }
    }

    private static void FixPrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[FixEnemyPrefabs] Prefab not found: {prefabPath}");
            return;
        }

        // Create a prefab editing scope
        var prefabStage = PrefabUtility.LoadPrefabContents(prefabPath);
        GameObject root = prefabStage;

        bool modified = false;

        // 1. Fix Rigidbody2D on root
        Rigidbody2D rb = root.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Undo.RecordObject(rb, "Fix Rigidbody2D");
            rb.linearDamping = 6f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.gravityScale = 0f;
            modified = true;
        }

        // 2. Ensure solid body collider on root (non-trigger)
        Collider2D bodyCollider = root.GetComponent<Collider2D>();
        bool needsBodyCollider = false;

        if (bodyCollider == null)
        {
            needsBodyCollider = true;
        }
        else if (bodyCollider.isTrigger)
        {
            // Existing collider is a trigger, we need a solid one
            needsBodyCollider = true;
        }

        if (needsBodyCollider)
        {
            Undo.AddComponent<CircleCollider2D>(root);
            CircleCollider2D newCollider = root.GetComponent<CircleCollider2D>();
            if (newCollider != null && newCollider.isTrigger)
            {
                Undo.RecordObject(newCollider, "Fix Body Collider");
                newCollider.isTrigger = false;
                newCollider.radius = 1.0f;
                newCollider.offset = Vector2.zero;
                modified = true;
            }
        }
        else
        {
            // Existing solid collider - just ensure it's not a trigger
            if (bodyCollider.isTrigger)
            {
                Undo.RecordObject(bodyCollider, "Fix Body Collider Trigger");
                bodyCollider.isTrigger = false;
                modified = true;
            }
        }

        // 3. Fix existing Hitbox child or create new one
        Transform hitboxTransform = root.transform.Find("Hitbox");
        GameObject hitboxObj = hitboxTransform?.gameObject;

        if (hitboxObj == null)
        {
            // Create Hitbox child
            hitboxObj = new GameObject("Hitbox");
            Undo.RegisterCreatedObjectUndo(hitboxObj, "Create Hitbox");
            hitboxObj.transform.SetParent(root.transform);
            hitboxObj.transform.localPosition = Vector3.zero;
            hitboxObj.transform.localScale = Vector3.one;
            hitboxObj.transform.localRotation = Quaternion.identity;
            hitboxObj.layer = hitboxLayer;
            modified = true;
        }
        else
        {
            // Ensure existing hitbox has correct layer
            if (hitboxObj.layer != hitboxLayer)
            {
                Undo.RecordObject(hitboxObj, "Fix Hitbox Layer");
                hitboxObj.layer = hitboxLayer;
                modified = true;
            }
        }

        // Ensure Hitbox has CircleCollider2D with isTrigger = true
        CircleCollider2D hitboxCollider = hitboxObj.GetComponent<CircleCollider2D>();
        if (hitboxCollider == null)
        {
            Undo.AddComponent<CircleCollider2D>(hitboxObj);
            hitboxCollider = hitboxObj.GetComponent<CircleCollider2D>();
        }

        if (hitboxCollider != null)
        {
            Undo.RecordObject(hitboxCollider, "Fix Hitbox Collider");
            hitboxCollider.isTrigger = true;
            hitboxCollider.radius = 0.6f;
            hitboxCollider.offset = Vector2.zero;
            modified = true;
        }

        // 4. Remove any duplicate/broken colliders on Hitbox (keep only one CircleCollider2D)
        var allColliders = hitboxObj.GetComponents<Collider2D>();
        if (allColliders.Length > 1)
        {
            foreach (var col in allColliders)
            {
                if (col != hitboxCollider)
                {
                    Undo.DestroyObjectImmediate(col);
                    modified = true;
                }
            }
        }

        if (modified)
        {
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }

        PrefabUtility.UnloadPrefabContents(prefabStage);
    }
}