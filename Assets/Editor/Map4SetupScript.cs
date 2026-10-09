using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using System.Reflection;
using System;

[InitializeOnLoad]
public class Map4SetupScript
{
    static Map4SetupScript()
    {
        // Execute once
        if (!SessionState.GetBool("Map4SetupScript_Run", false))
        {
            SessionState.SetBool("Map4SetupScript_Run", true);
            EditorApplication.delayCall += Setup;
        }
    }

    [MenuItem("Tools/Setup Map 4 (Auto)")]
    public static void Setup()
    {
        Scene map4 = EditorSceneManager.OpenScene("Assets/Scenes/Maps/Map4_Desert.unity");

        GameObject map4Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sprites/TileMaps/Map4/Map_4.prefab");
        GameObject decorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sprites/TileMaps/Map4/Decor_Map4.prefab");
        
        if (map4Prefab == null || decorPrefab == null) {
            Debug.LogError("Could not find Map_4.prefab or Decor_Map4.prefab!");
            return;
        }

        GameObject grid = GameObject.Find("Grid");
        if (grid == null)
        {
            grid = new GameObject("Grid");
            grid.AddComponent<Grid>();
        }

        // Setup Ground
        GameObject groundObj = GameObject.Find("Ground");
        if (groundObj == null) {
            groundObj = new GameObject("Ground");
            groundObj.transform.SetParent(grid.transform);
        }
        groundObj.layer = LayerMask.NameToLayer("Ground");
        
        Tilemap groundTm = groundObj.GetComponent<Tilemap>();
        if (groundTm == null) groundTm = groundObj.AddComponent<Tilemap>();
        
        TilemapRenderer groundTr = groundObj.GetComponent<TilemapRenderer>();
        if (groundTr == null) groundTr = groundObj.AddComponent<TilemapRenderer>();
        groundTr.sortingLayerName = "Ground";
        groundTr.sortingOrder = 0;

        TilemapCollider2D groundCol = groundObj.GetComponent<TilemapCollider2D>();
        if (groundCol == null) groundCol = groundObj.AddComponent<TilemapCollider2D>();
        groundCol.usedByComposite = true;

        CompositeCollider2D compCol = groundObj.GetComponent<CompositeCollider2D>();
        if (compCol == null) compCol = groundObj.AddComponent<CompositeCollider2D>();
        compCol.geometryType = CompositeCollider2D.GeometryType.Polygons;

        Rigidbody2D rb = groundObj.GetComponent<Rigidbody2D>();
        if (rb == null) rb = groundObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        // Setup Decor (Foreground)
        GameObject fgObj = GameObject.Find("Foreground");
        if (fgObj == null) {
            fgObj = new GameObject("Foreground");
            fgObj.transform.SetParent(grid.transform);
        }
        Tilemap fgTm = fgObj.GetComponent<Tilemap>();
        if (fgTm == null) fgTm = fgObj.AddComponent<Tilemap>();
        TilemapRenderer fgTr = fgObj.GetComponent<TilemapRenderer>();
        if (fgTr == null) fgTr = fgObj.AddComponent<TilemapRenderer>();
        fgTr.sortingLayerName = "Foreground";
        fgTr.sortingOrder = 1;

        // Copy tiles
        Tilemap prefabMap4Tm = map4Prefab.GetComponentInChildren<Tilemap>();
        if (prefabMap4Tm != null) {
            BoundsInt bounds = prefabMap4Tm.cellBounds;
            TileBase[] allTiles = prefabMap4Tm.GetTilesBlock(bounds);
            groundTm.SetTilesBlock(bounds, allTiles);
            Debug.Log("Ground tiles copied successfully.");
        }

        Tilemap prefabDecorTm = decorPrefab.GetComponentInChildren<Tilemap>();
        if (prefabDecorTm != null) {
            BoundsInt bounds = prefabDecorTm.cellBounds;
            TileBase[] allTiles = prefabDecorTm.GetTilesBlock(bounds);
            fgTm.SetTilesBlock(bounds, allTiles);
            Debug.Log("Foreground decor tiles copied successfully.");
        }

        // Setup Parallax Backgrounds
        Type bgControllerType = GetType("BackgroundController");

        SetupBackground("BG1", "Assets/Sprites/Tilesets/Map4/Backgrounds/BG-sky.png", -10, bgControllerType);
        SetupBackground("BG2", "Assets/Sprites/Tilesets/Map4/Backgrounds/BG-sun.png", -9, bgControllerType);
        SetupBackground("BG3", "Assets/Sprites/Tilesets/Map4/Backgrounds/BG-mountains.png", -8, bgControllerType);
        SetupBackground("BG4", "Assets/Sprites/Tilesets/Map4/Backgrounds/BG-ruins.png", -7, bgControllerType);

        EditorSceneManager.SaveScene(map4);
        Debug.Log("Map 4 Setup Script Finished successfully! Map4_Desert has been built.");
    }

    private static void SetupBackground(string bgName, string spritePath, int sortingOrder, Type bgControllerType)
    {
        GameObject bgObj = GameObject.Find(bgName);
        if (bgObj == null) bgObj = new GameObject(bgName);

        SpriteRenderer sr = bgObj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = bgObj.AddComponent<SpriteRenderer>();
        
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (bgSprite != null) sr.sprite = bgSprite;
        
        sr.sortingLayerName = "Background";
        sr.sortingOrder = sortingOrder;
        
        if (bgControllerType != null && bgObj.GetComponent(bgControllerType) == null)
        {
            bgObj.AddComponent(bgControllerType);
        }
    }

    private static Type GetType(string typeName)
    {
        var type = Type.GetType(typeName);
        if (type != null) return type;
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = a.GetType(typeName);
            if (type != null)
                return type;
        }
        return null;
    }
}
