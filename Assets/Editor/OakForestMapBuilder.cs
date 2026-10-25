using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Menu: Tools > Musashi > Build Oak Forest Map
/// Builds the map described in Assets/Editor/OakForestLayout.txt into the currently open scene,
/// using the Map2 (Oak Woods) assets. Re-running replaces the previously generated map.
///
/// Layout legend:  # ground | = one-way platform | b cave back wall | L lamp | F/f fence
///                 S sign | H shop | 1/2/3 rocks | g grass | P player spawn
/// </summary>
public static class OakForestMapBuilder
{
    const string Root = "OakForestMap_Generated";
    const string LayoutPath = "Assets/Editor/OakForestLayout.txt";
    const string TilesetPath = "Assets/Sprites/Tilesets/Map2/oak_woods_tileset.png";
    const string DecorDir = "Assets/Sprites/Tilesets/Map2/Decors/";
    const string BgDir = "Assets/Sprites/Tilesets/Map2/Backgrounds/";
    const string TileOutDir = "Assets/Sprites/TileMaps/Map2/Generated";
    const int TilePx = 24;
    const int TilesetRows = 15;

    static readonly Dictionary<Vector2Int, Tile> tileCache = new Dictionary<Vector2Int, Tile>();
    static Dictionary<Vector2Int, Sprite> spriteByCell;

    [MenuItem("Tools/Musashi/Build Oak Forest Map")]
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
        Undo.RegisterCreatedObjectUndo(root, "Build Oak Forest Map");
        var grid = new GameObject("Grid", typeof(Grid));
        grid.transform.SetParent(root.transform);

        int groundLayer = LayerMask.NameToLayer("Ground");
        Tilemap back = MakeTilemap(grid.transform, "CaveBackWall", -1, false, -1, sortingLayer: "Ground");
        Tilemap ground = MakeTilemap(grid.transform, "Ground", 0, true, groundLayer, sortingLayer: "Ground");
        Tilemap plats = MakeTilemap(grid.transform, "Platforms", 0, true, groundLayer, oneWay: false, sortingLayer: "Ground");

        bool Solid(int x, int y)
        {
            if (x < 0 || x >= W || y >= H) return true;
            if (y < 0) return false;
            return At(x, y) == '#';
        }

        var decorRoot = new GameObject("Decor").transform;
        decorRoot.SetParent(root.transform);
        var decorItems = new List<DecorItem>();
        var rng = new System.Random(7);

        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            char c = At(x, y);
            var cell = new Vector3Int(x, H - 1 - y, 0);
            switch (c)
            {
                case '#':
                {
                    bool u = Solid(x, y - 1), d = Solid(x, y + 1), l = Solid(x - 1, y), r = Solid(x + 1, y);
                    Vector2Int t;
                    if (!u && !d) t = (!l && r) ? new Vector2Int(5, 9) : (l && !r) ? new Vector2Int(7, 9) : new Vector2Int(6, 9);
                    else if (!l && !r) t = !u ? new Vector2Int(6, 2) : !d ? new Vector2Int(6, 5) : new Vector2Int(6, 4);
                    else if (!u) t = !l ? new Vector2Int(0, 0) : !r ? new Vector2Int(3, 0) : new Vector2Int(1 + x % 2, 0);
                    else if (!d) t = !l ? new Vector2Int(0, 3) : !r ? new Vector2Int(3, 3) : new Vector2Int(1 + x % 2, 3);
                    else if (!l) t = new Vector2Int(0, 1 + y % 2);
                    else if (!r) t = new Vector2Int(3, 1 + y % 2);
                    else t = new Vector2Int(1, 1);
                    ground.SetTile(cell, GetTile(t));
                    break;
                }
                case '=':
                {
                    bool l = At(x - 1, y) == '=', r = At(x + 1, y) == '=';
                    Vector2Int t = (!l && !r) ? new Vector2Int(10, 0) : !l ? new Vector2Int(5, 0) : !r ? new Vector2Int(8, 0) : new Vector2Int(6 + x % 2, 0);
                    plats.SetTile(cell, GetTile(t));
                    break;
                }
                case 'b':
                    back.SetTile(cell, GetTile(new Vector2Int(15, 1)));
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
                case '.':
                    break;
                default:
                {
                    var item = CreateDecorItem(c, x, H - 1 - y, rng);
                    if (item != null)
                    {
                        decorItems.Add(item);
                    }
                    break;
                }
            }
        }

        BuildOrganizedDecor(decorRoot, decorItems);
        int decorPlacedCount = decorItems.Count;

        ground.CompressBounds(); plats.CompressBounds(); back.CompressBounds();
        BuildBackground(root.transform, W, H);
        SetupCamera(root.transform, W, H);
        SetupTrapsAndPillars(root.transform);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log($"Oak Forest map built: {W}x{H} tiles with {decorPlacedCount} decors arranged into folders from left to right. Player moved to PlayerSpawn.");
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
                      || n.StartsWith("bg") || n.Contains("decor") || n.Contains("tilemap");
            if (!isMap) continue;
            Undo.RecordObject(go, "Hide old map");
            go.SetActive(false);
            Debug.Log("Đã ẩn map cũ: " + go.name);
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

            // 1. Mở rộng góc nhìn Camera ảo (Orthographic Size từ 6 lên 8.5)
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
                        Debug.Log("Đã mở rộng tầm nhìn Virtual Camera (OrthographicSize = 8.5).");
                    }
                }
            }

            // 2. Gán biên confiner mới bao phủ toàn bộ map
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

        // Camera chính
        foreach (var cam in Object.FindObjectsOfType<Camera>(true))
        {
            if (cam.orthographic)
            {
                Undo.RecordObject(cam, "Widen Camera");
                cam.orthographicSize = 8.5f;
            }
        }

        Debug.Log(hooked > 0 ? $"Đã gán CameraBounds mới cho {hooked} confiner." : "Không tìm thấy Cinemachine Confiner.");
    }

    class DecorItem
    {
        public char symbol;
        public string spriteName;
        public string categoryFolder; // "Lamp", "Rock", "Fence", "Grass", "Sign", "Shop"
        public string prefix;         // "lamp", "rock", "fence", "grass", "sign", "shop"
        public int cellX;
        public int cellY;
        public int order;
    }

    static DecorItem CreateDecorItem(char c, int cellX, int cellY, System.Random rng)
    {
        switch (c)
        {
            case 'L':
                return new DecorItem { symbol = c, spriteName = "lamp", categoryFolder = "Lamp", prefix = "lamp", cellX = cellX, cellY = cellY, order = 1 };
            case '1':
            case '2':
            case '3':
                return new DecorItem { symbol = c, spriteName = "rock_" + c, categoryFolder = "Rock", prefix = "rock", cellX = cellX, cellY = cellY, order = 1 };
            case 'F':
                return new DecorItem { symbol = c, spriteName = "fence_1", categoryFolder = "Fence", prefix = "fence", cellX = cellX, cellY = cellY, order = 1 };
            case 'f':
                return new DecorItem { symbol = c, spriteName = "fence_2", categoryFolder = "Fence", prefix = "fence", cellX = cellX, cellY = cellY, order = 1 };
            case 'g':
                return new DecorItem { symbol = c, spriteName = "grass_" + rng.Next(1, 4), categoryFolder = "Grass", prefix = "grass", cellX = cellX, cellY = cellY, order = 2 };
            case 'S':
                return new DecorItem { symbol = c, spriteName = "sign", categoryFolder = "Sign", prefix = "sign", cellX = cellX, cellY = cellY, order = 1 };
            case 'H':
                return new DecorItem { symbol = c, spriteName = "shop", categoryFolder = "Shop", prefix = "shop", cellX = cellX, cellY = cellY, order = 1 };
        }
        return null;
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
        string path = $"{TileOutDir}/oak_{c.x}_{c.y}.asset";
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
        if (path.Contains("Background"))
        {
            if (ti.wrapMode != TextureWrapMode.Repeat)
            {
                ti.wrapMode = TextureWrapMode.Repeat;
                dirty = true;
            }
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                settings.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(settings);
                dirty = true;
            }
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

    static void BuildOrganizedDecor(Transform decorRoot, List<DecorItem> decorItems)
    {
        var categories = new[] { "Lamp", "Rock", "Fence", "Grass", "Sign", "Shop" };
        foreach (var cat in categories)
        {
            var itemsInCat = decorItems
                .Where(d => d.categoryFolder == cat)
                .OrderBy(d => d.cellX)
                .ThenBy(d => d.cellY)
                .ToList();

            if (itemsInCat.Count == 0) continue;

            var folderGo = new GameObject(cat);
            folderGo.transform.SetParent(decorRoot);

            for (int i = 0; i < itemsInCat.Count; i++)
            {
                var item = itemsInCat[i];
                string objectName = $"{item.prefix} {i + 1}";
                PlaceDecor(folderGo.transform, item.spriteName, objectName, item.cellX, item.cellY, item.order, "Ground");
            }
        }
    }

    static void PlaceDecor(Transform parent, string spriteName, string objectName, int cellX, int cellY, int order, string sortingLayer = "Ground")
    {
        var sprite = LoadFirstSprite(DecorDir + spriteName + ".png");
        if (sprite == null) { Debug.LogWarning("Missing decor sprite " + spriteName); return; }
        var go = new GameObject(objectName);
        go.transform.SetParent(parent);
        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycast >= 0) go.layer = ignoreRaycast;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;

        // Nếu là shop, gắn Animator để phát animation rèm cửa/đèn buôn bán
        if (spriteName == "shop")
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Enemies/Shop.controller");
            if (controller != null)
            {
                var anim = go.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;
            }
        }

        // Decor is imported at 32 PPU, tiles at 24 PPU -> scale so pixels match.
        float scale = sprite.pixelsPerUnit / (float)TilePx;
        go.transform.localScale = Vector3.one * scale;
        Bounds b = sprite.bounds;
        float px = cellX + 0.5f - b.center.x * scale;
        float py = cellY - b.min.y * scale;      // bottom of sprite sits on cell bottom
        go.transform.position = new Vector3(px, py, 0);
    }

    static void BuildBackground(Transform root, int W, int H)
    {
        var bgRoot = new GameObject("Background").transform;
        bgRoot.SetParent(root);
        bgRoot.localPosition = Vector3.zero;

        // Chiều cao map là H (50) và camera confiner lên tới H + 6 (56)
        // Sprite background gốc 320x180 px ở 16 PPU có chiều cao 11.25 units
        // Để phủ trọn vẹn từ Y=0 đến Y=56: scale = 5.2f (11.25 * 5.2 = 58.5 units)
        // Tọa độ Y đặt tại 58.5f / 2f = 29.25f để đáy sprite vừa vặn chạm mốc Y=0
        float scale = 5.2f;
        float targetY = (11.25f * scale) / 2f; // 29.25f
        float tiledWidth = 500f; // Trải dài 500 units theo chiều ngang (drawMode = Tiled)

        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = Object.FindFirstObjectByType<Camera>();
        GameObject camGo = mainCam != null ? mainCam.gameObject : null;

        // Cấu hình giống hệt Map 1: 3 layer BG1, BG2, BG3
        // Mỗi layer là 1 GameObject duy nhất (không chia ra từng part con), trải dài toàn map với DrawMode = Tiled
        // Gắn BackgroundController để parallax mượt mà theo camera
        var layers = new[]
        {
            new { name = "BG1", spriteFile = "background_layer_1.png", order = 0, parallax = 1.0f },
            new { name = "BG2", spriteFile = "background_layer_2.png", order = 1, parallax = 0.6f },
            new { name = "BG3", spriteFile = "background_layer_3.png", order = 2, parallax = 0.3f }
        };

        foreach (var l in layers)
        {
            var sprite = LoadFirstSprite(BgDir + l.spriteFile);
            if (sprite == null) continue;

            var go = new GameObject(l.name);
            go.transform.SetParent(bgRoot);
            go.transform.position = new Vector3(0f, targetY, 0f);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(tiledWidth, sprite.rect.height / sprite.pixelsPerUnit);
            sr.sortingLayerName = "Background";
            sr.sortingOrder = l.order;

            var controller = go.AddComponent<BackgroundController>();
            controller.cam = camGo;
            controller.parallaxEffect = l.parallax;
        }
    }

    static void SetupTrapsAndPillars(Transform root)
    {
        // 1. Đặt các bẫy gai (Wood_Trap) nằm gọn trong hố đất, đầu chông ngang mặt đất theo hình 4
        var trapPositions = new Dictionary<string, Vector3>
        {
            { "Wood_Trap (6)", new Vector3(145.0f, 37.0f, 0f) },
            { "Wood_Trap (7)", new Vector3(164.0f, 37.0f, 0f) },
            { "Wood_Trap (4)", new Vector3(63.0f, 16.0f, 0f) },
            { "Wood_Trap (5)", new Vector3(71.0f, 8.0f, 0f) },
            { "Wood_Trap (8)", new Vector3(237.0f, 14.0f, 0f) },
            { "Wood_Trap (3)", new Vector3(275.0f, 29.0f, 0f) },
            { "Wood_Trap (1)", new Vector3(287.0f, 6.0f, 0f) },
            { "Wood_Trap (2)", new Vector3(292.0f, 6.0f, 0f) },
            { "Wood_Trap (9)", new Vector3(319.0f, 28.0f, 0f) },
            { "Wood_Trap (10)", new Vector3(352.0f, 14.0f, 0f) },
            { "Wood_Trap (11)", new Vector3(360.0f, 14.0f, 0f) }
        };

        foreach (var trap in Object.FindObjectsOfType<Trap>(true))
        {
            if (trapPositions.TryGetValue(trap.gameObject.name, out var pos))
            {
                Undo.RecordObject(trap.transform, "Position Trap");
                trap.transform.position = pos;
            }
        }

        // 2. Bố trí 3 trụ đá phun lửa bậc thang theo hình 5
        var cratePositions = new Dictionary<string, Vector3>
        {
            { "Wooden_Crate_Box (4)", new Vector3(227.5f, 14.5f, 0f) }, // Trụ 1 (Cao 1 block)
            { "Wooden_Crate_Box (3)", new Vector3(230.5f, 15.5f, 0f) }, // Trụ 2 (Cao 2 block)
            { "Wooden_Crate_Box (1)", new Vector3(233.5f, 16.5f, 0f) }, // Trụ 3 (Cao 3 block)
            { "Wooden_Crate_Box (2)", new Vector3(58.5f, 16.5f, 0f) },
            { "Wooden_Crate_Box (5)", new Vector3(314.5f, 28.5f, 0f) }
        };

        foreach (var ft in Object.FindObjectsOfType<FlameTrap>(true))
        {
            if (cratePositions.TryGetValue(ft.gameObject.name, out var pos))
            {
                Undo.RecordObject(ft.transform, "Position FlameTrap");
                ft.transform.position = pos;
            }
        }
    }
}
