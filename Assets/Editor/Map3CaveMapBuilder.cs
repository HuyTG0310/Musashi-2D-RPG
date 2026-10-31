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
/// XAY DUNG TOAN DIEN MAP 3 CAVE (DIA HINH + ENEMIES + TRAPS + CHESTS + COINS + EXIT):
/// - Doc file layout Assets/Editor/Map3CaveLayout.txt (350x50)
/// - Ground autotile: ria tren da xam sang (A_50/51), tran nhu da (A_86/87), vach trai/phai, ruot den dac (A_05)
/// - Back wall mo co ('b'), Gian giao chu X ('X','x'), Buc go nhay 1 chieu ('='), Tru gach co ('B','k'), Thang go ('|')
/// - Bay chong nhon Tilemap ('S') tu dong gop thanh cac Trap_Zone co BoxCollider2D + script Trap (damage + slow/knockback)
/// - Tu dong sinh Enemies Map 3:
///     'W' = FireWorm       | 'H' = Huntress1      | 'M' = MartialHero2
///     'N' = yellowNinja    | 'K' = HeroKnight2
/// - Tu dong sinh Traps & Props:
///     'T' = Wood_Trap      | 'F' = Wooden_Crate_Box (FlameTrap)
///     'C' = Chest (ruong)  | 'o' = Coin (xu)      | 'g' = Cay bui | 'r' = Da ngam
/// - Tu dong sinh Cua qua man ('D') + MapExit (MapTransition -> Map4_Desert) + LevelManager (diet het quai moi mo cua)
/// - Tu dong an/xoa cac Enemy, Trap_Zone, Chest cu bi lech toa do am trong scene cu
/// - Parallax Background 5 lop (BG1..BG5) va Cinemachine Camera Confiner
/// </summary>
public static class Map3CaveMapBuilder
{
    const string Root = "CaveMap3_Generated";
    const string LayoutPath = "Assets/Editor/Map3CaveLayout.txt";
    const string TilesetPath = "Assets/Sprites/Tilesets/Map3/mainlev_build.png";
    const string PlantDecorDir = "Assets/Sprites/Tilesets/Map3/Decors/props2_plants/";
    const string RockDecorDir = "Assets/Sprites/Tilesets/Map3/Decors/props2_rocks/";
    const string BgDir = "Assets/Sprites/Tilesets/Map3/Backgrounds/";
    const string TileOutDir = "Assets/Sprites/TileMaps/Map3/Generated";
    const int TilePx = 16;
    const int TilesetRows = 64;

    // Prefab paths
    const string Prefab_FireWorm     = "Assets/Prefabs/Enemies/Map3/FireWorm.prefab";
    const string Prefab_Huntress1    = "Assets/Prefabs/Enemies/Map3/Huntress1.prefab";
    const string Prefab_MartialHero2 = "Assets/Prefabs/Enemies/Map3/MartialHero2.prefab";
    const string Prefab_YellowNinja  = "Assets/Prefabs/Enemies/Map3/yellowNinja.prefab";
    const string Prefab_HeroKnight2  = "Assets/Prefabs/Enemies/Map3/HeroKnight2.prefab";

    const string Prefab_WoodTrap     = "Assets/Prefabs/Traps/Wood_Trap.prefab";
    const string Prefab_FlameBox     = "Assets/Prefabs/Traps/Wooden_Crate_Box.prefab";
    const string Prefab_Chest        = "Assets/Prefabs/Chest.prefab";
    const string Prefab_Coin         = "Assets/Prefabs/Coin.prefab";

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
        HideOldMapAndLegacyEntities();

        var root = new GameObject(Root);
        Undo.RegisterCreatedObjectUndo(root, "Build Cave Map 3");
        var grid = new GameObject("Grid", typeof(Grid));
        grid.transform.SetParent(root.transform);

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0) groundLayer = 0;
        int playerLayerMask = 1 << (LayerMask.NameToLayer("Player") >= 0 ? LayerMask.NameToLayer("Player") : 3);
        int trapLayer = LayerMask.NameToLayer("Trap");
        if (trapLayer < 0) trapLayer = 0;

        Tilemap back = MakeTilemap(grid.transform, "CaveBackWall", -1, false, -1, sortingLayer: "Ground");
        Tilemap ground = MakeTilemap(grid.transform, "Ground", 0, true, groundLayer, sortingLayer: "Ground");
        Tilemap plats = MakeTilemap(grid.transform, "Platforms", 0, true, groundLayer, oneWay: true, sortingLayer: "Ground");

        bool Solid(int x, int y)
        {
            if (x < 0 || x >= W || y >= H) return true;
            if (y < 0) return false;
            return At(x, y) == '#';
        }

        // Helper: kiem tra cell xung quanh co nam trong khu vuc mo co ('b') de tu dong ve nen mo phia sau Enemy/Trap/Item
        bool HasMineBackWallNearby(int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                char nb = At(x + dx, y + dy);
                if (nb == 'b' || nb == '|') return true;
            }
            return false;
        }

        var decorRoot   = new GameObject("Decor").transform;   decorRoot.SetParent(root.transform);
        var enemiesRoot = new GameObject("Enemies").transform; enemiesRoot.SetParent(root.transform);
        var trapsRoot   = new GameObject("Traps").transform;   trapsRoot.SetParent(root.transform);
        var itemsRoot   = new GameObject("Items").transform;   itemsRoot.SetParent(root.transform);

        int decorCount = 0, enemyCount = 0, trapCount = 0, chestCount = 0, coinCount = 0;
        Vector3 exitDoorPos = new Vector3(W - 14f, 16f, 0f);
        bool hasExitDoor = false;

        var rng = new System.Random(42);

        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            char c = At(x, y);
            int wy = H - 1 - y;
            var cell = new Vector3Int(x, wy, 0);

            // Neu la ky tu dat tren nen mo co, van ve lop nen mo phia sau
            if (c != '.' && c != '#' && c != 'b' && HasMineBackWallNearby(x, y))
            {
                back.SetTile(cell, GetTile(new Vector2Int(48, 19)));
            }

            switch (c)
            {
                case '#': // Ground autotile: Ria xam vien sang, Ruot sau den dac A_05
                {
                    bool u = Solid(x, y - 1), d = Solid(x, y + 1), l = Solid(x - 1, y), r = Solid(x + 1, y);
                    Vector2Int t;
                    if (!u && !d) t = new Vector2Int(12 + (x % 2) * 2, 11);
                    else if (!l && !r) t = !u ? new Vector2Int(12 + (x % 2) * 2, 11) : !d ? new Vector2Int(17, 22) : new Vector2Int(2, 2);
                    else if (!u) t = !l ? new Vector2Int(16, 14) : !r ? new Vector2Int(26, 14) : new Vector2Int(12 + (x % 2) * 2, 11);
                    else if (!d) t = !l ? new Vector2Int(16, 14) : !r ? new Vector2Int(26, 14) : new Vector2Int(17 + (x % 2) * 4, 22);
                    else if (!l) t = new Vector2Int(16, 14);
                    else if (!r) t = new Vector2Int(26, 14);
                    else t = new Vector2Int(2, 2);
                    ground.SetTile(cell, GetTile(t));
                    break;
                }
                case '=': // Buc go nhay (one-way platform)
                    plats.SetTile(cell, GetTile(new Vector2Int(52 + (x % 2), 18)));
                    break;
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
                case 'S': // Bay chong nhon (hinh anh tren Tilemap + gop thanh Trap_Zone ben duoi)
                    back.SetTile(cell, GetTile(new Vector2Int(47, 18)));
                    break;
                case 'D': // Cua ham nguc (Exit Door)
                    back.SetTile(cell, GetTile(new Vector2Int(62, 1)));
                    if (!hasExitDoor)
                    {
                        exitDoorPos = new Vector3(x + 0.5f, wy, 0f);
                        hasExitDoor = true;
                    }
                    break;
                case 'P': // Vi tri xuat phat cua Musashi
                {
                    var spawn = new GameObject("PlayerSpawn");
                    spawn.transform.SetParent(root.transform);
                    spawn.transform.position = new Vector3(x + 0.5f, wy + 0.2f, 0f);
                    spawn.AddComponent<PlayerSpawn>();
                    var player = Object.FindObjectOfType<Player>();
                    if (player != null)
                    {
                        Undo.RecordObject(player.transform, "Move Player");
                        player.transform.position = spawn.transform.position + Vector3.up * 1.0f;
                    }
                    break;
                }
                case 'g': // Cay bui hang dong
                {
                    string[] plants = { "plant_03_133x75", "plant_04_94x66", "plant_07_94x66", "plant_08_133x75" };
                    PlaceDecor(decorRoot, PlantDecorDir, plants[rng.Next(plants.Length)], x, wy, 2, "Ground", 0.35f);
                    decorCount++;
                    break;
                }
                case 'r': // Tang da hang dong
                {
                    string[] rocks = { "rock_02_302x183", "rock_04_353x144" };
                    PlaceDecor(decorRoot, RockDecorDir, rocks[rng.Next(rocks.Length)], x, wy, 1, "Ground", 0.28f);
                    decorCount++;
                    break;
                }
                // ======================== ENEMIES ========================
                case 'W': // FireWorm (tâm cách mặt đất ~1.0f)
                    if (SpawnPrefab(Prefab_FireWorm, enemiesRoot, $"FireWorm_{enemyCount + 1}", new Vector3(x + 0.5f, wy + 1.0f, 0f)) != null)
                        enemyCount++;
                    break;
                case 'H': // Huntress1 (tâm cách mặt đất ~1.4f)
                    if (SpawnPrefab(Prefab_Huntress1, enemiesRoot, $"Huntress1_{enemyCount + 1}", new Vector3(x + 0.5f, wy + 1.4f, 0f)) != null)
                        enemyCount++;
                    break;
                case 'M': // MartialHero2 (tâm cách mặt đất ~1.2f)
                    if (SpawnPrefab(Prefab_MartialHero2, enemiesRoot, $"MartialHero2_{enemyCount + 1}", new Vector3(x + 0.5f, wy + 1.2f, 0f)) != null)
                        enemyCount++;
                    break;
                case 'N': // yellowNinja (tâm cách mặt đất ~2.1f do collider offset y = -0.9)
                    if (SpawnPrefab(Prefab_YellowNinja, enemiesRoot, $"yellowNinja_{enemyCount + 1}", new Vector3(x + 0.5f, wy + 2.1f, 0f)) != null)
                        enemyCount++;
                    break;
                case 'K': // HeroKnight2 (tâm cách mặt đất ~1.0f)
                    if (SpawnPrefab(Prefab_HeroKnight2, enemiesRoot, $"HeroKnight2_{enemyCount + 1}", new Vector3(x + 0.5f, wy + 1.0f, 0f)) != null)
                        enemyCount++;
                    break;
                // ======================== TRAPS ========================
                case 'T': // Wood_Trap (bẫy chông đẩy lùi, đặt tại wy + 1.0f giống Map 2)
                    if (SpawnPrefab(Prefab_WoodTrap, trapsRoot, $"Wood_Trap_{trapCount + 1}", new Vector3(x + 0.5f, wy + 1.0f, 0f)) != null)
                        trapCount++;
                    break;
                case 'F': // Wooden_Crate_Box (FlameTrap phun lửa định kỳ, đặt tại wy + 1.5f giống Map 2)
                    if (SpawnPrefab(Prefab_FlameBox, trapsRoot, $"FlameTrap_Box_{trapCount + 1}", new Vector3(x + 0.5f, wy + 1.5f, 0f)) != null)
                        trapCount++;
                    break;
                // ======================== ITEMS ========================
                case 'C': // Rương kho báu (Chest, đặt tại wy + 0.5f)
                    if (SpawnPrefab(Prefab_Chest, itemsRoot, $"Chest_{chestCount + 1}", new Vector3(x + 0.5f, wy + 0.5f, 0f)) != null)
                        chestCount++;
                    break;
                case 'o': // Đồng xu (Coin, đặt tại wy + 0.5f)
                    if (SpawnPrefab(Prefab_Coin, itemsRoot, $"Coin_{coinCount + 1}", new Vector3(x + 0.5f, wy + 0.5f, 0f)) != null)
                        coinCount++;
                    break;
            }
        }

        // Tu dong tao cac vung sat thuong Trap_Zone cho tat ca cac dai chong nhon 'S' tren Tilemap
        int spikeZones = BuildSpikeTrapZones(rows, W, H, trapsRoot, trapLayer, playerLayerMask);
        trapCount += spikeZones;

        // Tao MapExit (chuyen sang Map4_Desert) va LevelManager (diet het Enemy moi mo cua)
        BuildLevelExit(root.transform, exitDoorPos);

        ground.CompressBounds(); plats.CompressBounds(); back.CompressBounds();
        BuildBackground(root.transform, W, H);
        SetupCamera(root.transform, W, H);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log($"[Map3CaveMapBuilder] Hoan tat Map 3 ({W}x{H}): {enemyCount} Enemies, {trapCount} Traps (gom {spikeZones} ho chong), {chestCount} Chests, {coinCount} Coins, {decorCount} Decors.");
    }

    static GameObject SpawnPrefab(string prefabPath, Transform parent, string instanceName, Vector3 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("Missing prefab at " + prefabPath);
            return null;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = instanceName;
        go.transform.position = position;
        Undo.RegisterCreatedObjectUndo(go, "Spawn " + instanceName);
        return go;
    }

    /// <summary>
    /// Gom cac o 'S' (chong nhon) lien tiep tren cung 1 hang thanh 1 GameObject Trap_Zone
    /// co BoxCollider2D (isTrigger = true) va component Trap (gay sat thuong + day lui).
    /// </summary>
    static int BuildSpikeTrapZones(string[] rows, int W, int H, Transform trapsRoot, int trapLayer, int playerLayerMask)
    {
        int zoneCount = 0;
        for (int y = 0; y < H; y++)
        {
            int x = 0;
            while (x < W)
            {
                if (x < rows[y].Length && rows[y][x] == 'S')
                {
                    int startX = x;
                    while (x < rows[y].Length && rows[y][x] == 'S') x++;
                    int endX = x - 1;
                    int length = endX - startX + 1;
                    int wy = H - 1 - y;

                    zoneCount++;
                    var zoneGO = new GameObject($"SpikePit_TrapZone_{zoneCount}");
                    zoneGO.transform.SetParent(trapsRoot);
                    zoneGO.layer = trapLayer;
                    zoneGO.transform.position = new Vector3(startX + length * 0.5f, wy + 0.45f, 0f);

                    var box = zoneGO.AddComponent<BoxCollider2D>();
                    box.isTrigger = true;
                    box.size = new Vector2(length, 0.9f);

                    var trap = zoneGO.AddComponent<Trap>();
                    trap.damage = 15f;
                    trap.damageOverTime = false;
                    trap.damageInterval = 0.8f;
                    trap.playerLayer = playerLayerMask;
                    trap.knockbackEnabled = true;
                    trap.knockbackForceX = 10f;
                    trap.knockbackForceY = 11f;
                    trap.slowEnabled = true;
                    trap.slowMultiplier = 0.5f;
                }
                else
                {
                    x++;
                }
            }
        }
        return zoneCount;
    }

    static void BuildLevelExit(Transform root, Vector3 doorPos)
    {
        var exitGO = new GameObject("MapExit");
        exitGO.transform.SetParent(root);
        exitGO.transform.position = doorPos;

        var box = exitGO.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.offset = new Vector2(0f, 1.5f);
        box.size = new Vector2(2.5f, 4.0f);

        var transition = exitGO.AddComponent<MapTransition>();
        transition.nextMapName = "Map4_Desert";

        var lmGO = new GameObject("LevelManager");
        lmGO.transform.SetParent(root);
        lmGO.transform.position = doorPos;
        var lm = lmGO.AddComponent<LevelManager>();
        lm.mapExitTrigger = exitGO;
    }

    static void HideOldMapAndLegacyEntities()
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == Root || !go.activeSelf) continue;
            if (go.GetComponentInChildren<Player>(true) != null) continue;
            if (go.GetComponentInChildren<Camera>(true) != null) continue;

            // Giu lai Canvas UI, EventSystem, Audio Manager cua Scene
            string n = go.name.ToLower();
            if (n.Contains("canvas") || n.Contains("eventsystem") || n.Contains("audio")) continue;

            // An tat ca Grid, Background, Trap_Zone, Enemy, Chest cu o ngoai Root de tranh trung lap hoac lech toa do am
            bool isLegacyMapOrEntity =
                   go.GetComponentInChildren<Grid>(true) != null
                || go.GetComponentInChildren<EnemyBase>(true) != null
                || go.GetComponentInChildren<Trap>(true) != null
                || go.GetComponentInChildren<FlameTrap>(true) != null
                || go.GetComponentInChildren<Chest>(true) != null
                || go.GetComponentInChildren<LevelManager>(true) != null
                || go.GetComponentInChildren<MapTransition>(true) != null
                || n.Contains("background") || n.Contains("foreground")
                || n.StartsWith("bg") || n.Contains("decor") || n.Contains("tilemap")
                || n == "ground" || n.Contains("trap");

            if (!isLegacyMapOrEntity) continue;
            Undo.RecordObject(go, "Hide old map or entity");
            go.SetActive(false);
        }
    }

    static void SetupCamera(Transform root, int W, int H)
    {
        var go = new GameObject("CameraBounds_New");
        go.transform.SetParent(root);
        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycast >= 0) go.layer = ignoreRaycast;
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
        bool hasSprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Any();
        if (ti.spriteImportMode == SpriteImportMode.Multiple && !hasSprites)
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

    static void PlaceDecor(Transform parent, string dir, string name, int cellX, int cellY, int order, string sortingLayer = "Ground", float scaleMultiplier = 1f)
    {
        var sprite = LoadFirstSprite(dir + name + ".png");
        if (sprite == null) { Debug.LogWarning("Missing decor " + dir + name); return; }
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        float scale = (sprite.pixelsPerUnit / (float)TilePx) * scaleMultiplier;
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
        string[] bgFiles = { "background1.png", "background2.png", "background3.png", "background4a.png", "background4b.png" };
        for (int i = 0; i < bgFiles.Length; i++)
        {
            var sprite = LoadFirstSprite(BgDir + bgFiles[i]);
            if (sprite == null) continue;
            var layer = new GameObject("BG" + (i + 1)).transform;
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
                sr.sortingOrder = i;
                go.transform.localScale = Vector3.one * scale;
                go.transform.position = new Vector3(
                    x + width / 2f - sprite.bounds.center.x * scale,
                    H / 2f - sprite.bounds.center.y * scale, 0);
            }
        }
    }
}
#endif