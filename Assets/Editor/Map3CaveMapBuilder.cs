#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Menu: Tools > Musashi > Build Cave Map 3 (OakForest Style)
/// 
/// DUA DUNG THEO CACH XAY DUNG CUA MAP 2 (OakForestMapBuilder):
/// - Su dung file layout Assets/Editor/Map3CaveLayout.txt
/// - Ground autotile voi ria tren xam sang, ruot dac den tuyen (A_05)
/// - Back wall, Gian giao go chu X, Thap buc nhay, Cot gach co, Thang go 2 tang
/// - Parallax Background tu dong rai khap chieu rong va cao cua map
/// - Cinemachine Camera Confiner va di chuyen Player toi PlayerSpawn
/// </summary>
public static class Map3CaveMapBuilder
{
    const string Root = "CaveMap3_Generated";
    const string LayoutPath = "Assets/Editor/Map3CaveLayout.txt";
    const string TilesetPath = "Assets/Sprites/Tilesets/Map3/mainlev_build.png";
    const string DecorDir = "Assets/Sprites/Tilesets/Map3/Decors/props2_plants/";
    const string BgDir = "Assets/Sprites/Tilesets/Map3/Backgrounds/";
    const string TileOutDir = "Assets/Sprites/TileMaps/Map3/Generated";
    const int TilePx = 16;
    const int TilesetRows = 64;

    static readonly Dictionary<Vector2Int, Tile> tileCache = new Dictionary<Vector2Int, Tile>();
    static Dictionary<Vector2Int, Sprite> spriteByCell;

    [MenuItem("Tools/Musashi/Build Cave Map 3 (OakForest Style)")]
    public static void Build()
    {
        var layoutAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
        if (layoutAsset == null) { Debug.LogError("Missing " + LayoutPath); return; }
        string[] rows = layoutAsset.text.Replace("\r", "").Split('\n').Where(l => l.Length > 0).ToArray();
        int H = rows.Length, W = rows.Max(r => r.Length);
        char At(int x, int y) => (y < 0 || y >= H || x < 0 || x >= rows[y].Length) ? '.' : rows[y][x];

        LoadTilesetSprites();
        tileCache.Clear();

        var old = GameObject.Find(Root);
        if (old != null) Undo.DestroyObjectImmediate(old);
        HideOldMap();

        var root = new GameObject(Root);
        Undo.RegisterCreatedObjectUndo(root, "Build Cave Map 3");
        var grid = new GameObject("Grid", typeof(Grid));
        grid.transform.SetParent(root.transform);

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0) groundLayer = 0;

        Tilemap back = MakeTilemap(grid.transform, "CaveBackWall", -1, false, -1, sortingLayer: "Ground");
        Tilemap ground = MakeTilemap(grid.transform, "Ground", 0, true, groundLayer, sortingLayer: "Ground");
        Tilemap plats = MakeTilemap(grid.transform, "Platforms", 0, true, groundLayer, oneWay: true, sortingLayer: "Ground");

        bool Solid(int x, int y)
        {
            if (x < 0 || x >= W || y >= H) return true;
            if (y < 0) return false;
            return At(x, y) == '#';
        }

        var decorRoot = new GameObject("Decor").transform;
        decorRoot.SetParent(root.transform);
        int decorPlacedCount = 0;

        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            char c = At(x, y);
            var cell = new Vector3Int(x, H - 1 - y, 0);
            switch (c)
            {
                case '#': // Ground autotile: Ria xam vien sang, Ruot sau den dac A_05
                {
                    bool u = Solid(x, y - 1), d = Solid(x, y + 1), l = Solid(x - 1, y), r = Solid(x + 1, y);
                    Vector2Int t;
                    if (!u && !d) t = new Vector2Int(12 + (x % 2) * 2, 11);
                    else if (!l && !r) t = !u ? new Vector2Int(12 + (x % 2) * 2, 11) : !d ? new Vector2Int(17, 22) : new Vector2Int(2, 2);
                    else if (!u) t = !l ? new Vector2Int(16, 14) : !r ? new Vector2Int(26, 14) : new Vector2Int(12 + (x % 2) * 2, 11); // Ria xam mat tren A_50/51
                    else if (!d) t = !l ? new Vector2Int(16, 14) : !r ? new Vector2Int(26, 14) : new Vector2Int(17 + (x % 2) * 4, 22); // Tran nhu da A_86/87
                    else if (!l) t = new Vector2Int(16, 14); // Vach trai
                    else if (!r) t = new Vector2Int(26, 14); // Vach phai
                    else t = new Vector2Int(2, 2); // Ruot sau den dac A_05 (col 2, row 2)
                    ground.SetTile(cell, GetTile(t));
                    break;
                }
                case '=': // Buc go nhay (one-way platform)
                {
                    plats.SetTile(cell, GetTile(new Vector2Int(52 + (x % 2), 18)));
                    break;
                }
                case 'b': // Gian giao mo go nen phia sau
                    back.SetTile(cell, GetTile(new Vector2Int(48, 19)));
                    break;
                case 'X': // Gian giao go ben trai
                    back.SetTile(cell, GetTile(new Vector2Int(52, 21)));
                    break;
                case 'x': // Gian giao go ben phai
                    back.SetTile(cell, GetTile(new Vector2Int(53, 21)));
                    break;
                case 'B': // Tru gach da co ben trai
                    back.SetTile(cell, GetTile(new Vector2Int(56, 7)));
                    break;
                case 'k': // Tru gach da co ben phai
                    back.SetTile(cell, GetTile(new Vector2Int(57, 7)));
                    break;
                case '|': // Thang go dung
                    back.SetTile(cell, GetTile(new Vector2Int(47, 1)));
                    break;
                case 'S': // Bay chong nhon
                    back.SetTile(cell, GetTile(new Vector2Int(47, 18)));
                    break;
                case 'D': // Cua ham nguc
                    back.SetTile(cell, GetTile(new Vector2Int(62, 1)));
                    break;
                case 'P':
                {
                    var spawn = new GameObject("PlayerSpawn");
                    spawn.transform.SetParent(root.transform);
                    spawn.transform.position = new Vector3(x + 0.5f, H - 1 - y + 0.1f, 0);
                    var player = Object.FindObjectOfType<Player>();
                    if (player != null)
                    {
                        Undo.RecordObject(player.transform, "Move Player");
                        player.transform.position = spawn.transform.position + Vector3.up * 1f;
                    }
                    break;
                }
                case 'g': // Cay bui hang dong
                {
                    PlaceDecor(decorRoot, "plant_04_94x66", x, H - 1 - y, 2, "Ground");
                    decorPlacedCount++;
                    break;
                }
                case '.':
                    break;
            }
        }

        ground.CompressBounds(); plats.CompressBounds(); back.CompressBounds();
        BuildBackground(root.transform, W, H);
        SetupCamera(root.transform, W, H);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log($"Cave Map 3 built theo kien truc Map 2: {W}x{H} tiles with {decorPlacedCount} decors. Player moved to PlayerSpawn.");
    }

    static void HideOldMap()
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == Root || !go.activeSelf) continue;
            if (go.GetComponentInChildren<Player>(true) != null) continue;
            if (go.GetComponentInChildren<Camera>(true) != null) continue;
            if (go.GetComponentInChildren<Canvas>(true) != null) continue;
            if (go.GetComponentInChildren<EnemyBase>(true) != null) continue;
            string n = go.name.ToLower();
            bool isMap = go.GetComponentInChildren<Grid>(true) != null
                      || n.Contains("background") || n.Contains("foreground")
                      || n.StartsWith("bg") || n.Contains("decor") || n.Contains("tilemap") || n == "ground";
            if (!isMap) continue;
            Undo.RecordObject(go, "Hide old map");
            go.SetActive(false);
            Debug.Log("Da an map cu: " + go.name);
        }
    }

    static void SetupCamera(Transform root, int W, int H)
    {
        var go = new GameObject("CameraBounds_New");
        go.transform.SetParent(root);
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        var poly = go.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        poly.points = new[] { new Vector2(0, 0), new Vector2(W, 0), new Vector2(W, H + 6), new Vector2(0, H + 6) };

        int hooked = 0;
        foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            string typeName = mb.GetType().Name;

            if (typeName.Contains("VirtualCamera"))
            {
                var so = new SerializedObject(mb);
                var lensProp = so.FindProperty("m_Lens");
                if (lensProp != null)
                {
                    var orthoProp = lensProp.FindPropertyRelative("OrthographicSize");
                    if (orthoProp != null)
                    {
                        orthoProp.floatValue = 8.5f;
                        so.ApplyModifiedProperties();
                        Debug.Log("Da mo rong tam nhin Virtual Camera (OrthographicSize = 8.5).");
                    }
                }
            }

            if (typeName.Contains("Confiner"))
            {
                var so = new SerializedObject(mb);
                var prop = so.FindProperty("m_BoundingShape2D") ?? so.FindProperty("BoundingShape2D");
                if (prop != null)
                {
                    prop.objectReferenceValue = poly;
                    so.ApplyModifiedProperties();
                    hooked++;
                }
            }
        }

        foreach (var cam in Object.FindObjectsOfType<Camera>(true))
        {
            if (cam.orthographic)
            {
                Undo.RecordObject(cam, "Widen Camera");
                cam.orthographicSize = 8.5f;
            }
        }

        Debug.Log(hooked > 0 ? $"Da gan CameraBounds moi cho {hooked} confiner." : "Khong tim thay Cinemachine Confiner.");
    }

    static void LoadTilesetSprites()
    {
        spriteByCell = new Dictionary<Vector2Int, Sprite>();
        foreach (var s in AssetDatabase.LoadAllAssetsAtPath(TilesetPath).OfType<Sprite>())
        {
            int col = Mathf.RoundToInt(s.rect.x / TilePx);
            int rowFromTop = TilesetRows - 1 - Mathf.RoundToInt(s.rect.y / TilePx);
            spriteByCell[new Vector2Int(col, rowFromTop)] = s;
        }
    }

    static Tile GetTile(Vector2Int c)
    {
        if (tileCache.TryGetValue(c, out var t)) return t;
        if (!spriteByCell.TryGetValue(c, out var sprite))
        {
            Debug.LogWarning($"No sprite at tileset cell {c}");
            return null;
        }
        if (!AssetDatabase.IsValidFolder(TileOutDir))
        {
            Directory.CreateDirectory(TileOutDir);
            AssetDatabase.Refresh();
        }
        string path = $"{TileOutDir}/cave_{c.x}_{c.y}.asset";
        t = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (t == null)
        {
            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sprite;
            t.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(t, path);
        }
        tileCache[c] = t;
        return t;
    }

    static Tilemap MakeTilemap(Transform parent, string name, int order, bool collide, int layer, bool oneWay = false, string sortingLayer = "Ground")
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent);
        if (layer >= 0) go.layer = layer;
        var r = go.GetComponent<TilemapRenderer>();
        r.sortingLayerName = sortingLayer;
        r.sortingOrder = order;
        if (collide)
        {
            var tc = go.AddComponent<TilemapCollider2D>();
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            go.AddComponent<CompositeCollider2D>();
#if UNITY_2023_1_OR_NEWER
            tc.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
            tc.usedByComposite = true;
#endif
            if (oneWay)
            {
                go.GetComponent<CompositeCollider2D>().usedByEffector = true;
                var eff = go.AddComponent<PlatformEffector2D>();
                eff.useOneWay = true;
                eff.surfaceArc = 160f;
            }
        }
        return go.GetComponent<Tilemap>();
    }

    static void EnsureSpriteImported(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        bool dirty = false;
        if (ti.textureType != TextureImporterType.Sprite)
        {
            ti.textureType = TextureImporterType.Sprite;
            dirty = true;
        }
        if (ti.spriteImportMode == SpriteImportMode.Multiple && (ti.spritesheet == null || ti.spritesheet.Length == 0))
        {
            ti.spriteImportMode = SpriteImportMode.Single;
            dirty = true;
        }
        if (dirty)
        {
            ti.SaveAndReimport();
        }
    }

    static Sprite LoadFirstSprite(string path)
    {
        EnsureSpriteImported(path);
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(x => x.name).FirstOrDefault();
    }

    static void PlaceDecor(Transform parent, string name, int cellX, int cellY, int order, string sortingLayer = "Ground")
    {
        var sprite = LoadFirstSprite(DecorDir + name + ".png");
        if (sprite == null) { Debug.LogWarning("Missing decor " + name); return; }
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        float scale = sprite.pixelsPerUnit / (float)TilePx;
        go.transform.localScale = Vector3.one * scale;
        Bounds b = sprite.bounds;
        float px = cellX + 0.5f - b.center.x * scale;
        float py = cellY - b.min.y * scale;
        go.transform.position = new Vector3(px, py, 0);
    }

    static void BuildBackground(Transform root, int W, int H)
    {
        var bgRoot = new GameObject("Background").transform;
        bgRoot.SetParent(root);
        for (int i = 1; i <= 3; i++)
        {
            var sprite = LoadFirstSprite(BgDir + $"background{i}.png");
            if (sprite == null) continue;
            var layer = new GameObject("BG" + i).transform;
            layer.SetParent(bgRoot);
            float scale = H / sprite.bounds.size.y;
            float width = sprite.bounds.size.x * scale;
            for (float x = 0; x < W + width; x += width)
            {
                var go = new GameObject("part");
                go.transform.SetParent(layer);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = "Background";
                sr.sortingOrder = i - 1;
                go.transform.localScale = Vector3.one * scale;
                go.transform.position = new Vector3(
                    x + width / 2f - sprite.bounds.center.x * scale,
                    H / 2f - sprite.bounds.center.y * scale, 0);
            }
        }
    }
}
#endif
