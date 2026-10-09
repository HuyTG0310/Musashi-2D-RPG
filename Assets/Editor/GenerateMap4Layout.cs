using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;

public class GenerateMap4Layout
{
    [MenuItem("Tools/Generate Map 4 Layout")]
    public static void GenerateLayout()
    {
        Scene map4 = EditorSceneManager.OpenScene("Assets/Scenes/Maps/Map4_Desert.unity");
        GameObject groundObj = GameObject.Find("Ground");
        if (groundObj == null) {
            Debug.LogError("Could not find 'Ground' Tilemap!");
            return;
        }

        Tilemap groundTm = groundObj.GetComponent<Tilemap>();
        if (groundTm != null)
        {
            groundTm.ClearAllTiles();

            // Load Tile
            TileBase groundTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Sprites/TileMaps/Map4/00Atlas_1.asset");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Sprites/TileMaps/Map4/00Atlas_14.asset");
            if (wallTile == null) wallTile = groundTile;
            if (groundTile == null) {
                Debug.LogError("Could not find ground tile '00Atlas_1.asset'!");
                return;
            }

            int roomWidth = 30;
            int wallHeight = 6;
            int numRooms = 5;

            // Generate 5 rooms (tầng) separated by walls
            for (int r = 0; r < numRooms; r++)
            {
                int startX = r * (roomWidth + 2);
                
                // Floor
                for (int x = startX; x < startX + roomWidth; x++)
                {
                    for (int y = -5; y <= 0; y++)
                    {
                        groundTm.SetTile(new Vector3Int(x, y, 0), y == 0 ? groundTile : wallTile);
                    }
                }

                // Wall at the end of the room (except the last room)
                if (r < numRooms - 1)
                {
                    int wallX = startX + roomWidth;
                    for (int w = 0; w < 2; w++) // 2 tiles thick wall
                    {
                        for (int y = -5; y <= wallHeight; y++)
                        {
                            groundTm.SetTile(new Vector3Int(wallX + w, y, 0), wallTile);
                        }
                    }
                    
                    // Add some stairs to jump over the wall
                    groundTm.SetTile(new Vector3Int(wallX - 2, 1, 0), groundTile);
                    groundTm.SetTile(new Vector3Int(wallX - 1, 2, 0), groundTile);
                    groundTm.SetTile(new Vector3Int(wallX - 1, 3, 0), groundTile);
                    
                    groundTm.SetTile(new Vector3Int(wallX + 2, 3, 0), groundTile);
                    groundTm.SetTile(new Vector3Int(wallX + 2, 2, 0), groundTile);
                    groundTm.SetTile(new Vector3Int(wallX + 3, 1, 0), groundTile);
                }
            }

            Debug.Log("Generated 5 rooms separated by walls.");
        }

        // Clear old enemies
        string[] oldEnemies = { "EvilWizard", "HeroKnight", "MartialHero" };
        GameObject[] objs = GameObject.FindObjectsOfType<GameObject>();
        foreach (var o in objs)
        {
            foreach (string e in oldEnemies)
            {
                if (o.name.Contains(e))
                {
                    GameObject.DestroyImmediate(o);
                    break;
                }
            }
        }

        // Place new enemies (2 per room)
        PlaceEnemies("Assets/Prefabs/Enemies/Map4/EvilWizard1.prefab", "EvilWizard1", 0, 10, 20);
        PlaceEnemies("Assets/Prefabs/Enemies/Map4/EvilWizard2.prefab", "EvilWizard2", 1, 10, 20);
        PlaceEnemies("Assets/Prefabs/Enemies/Map4/EvilWizard3.prefab", "EvilWizard3", 2, 10, 20);
        PlaceEnemies("Assets/Prefabs/Enemies/Map4/HeroKnight1.prefab", "HeroKnight1", 3, 10, 20);
        PlaceEnemies("Assets/Prefabs/Enemies/Map4/MartialHero1.prefab", "MartialHero1", 4, 10, 20);

        // Adjust Camera Bounds
        GameObject boundsObj = GameObject.Find("CameraBounds");
        if (boundsObj != null)
        {
            PolygonCollider2D poly = boundsObj.GetComponent<PolygonCollider2D>();
            if (poly != null)
            {
                poly.points = new Vector2[] {
                    new Vector2(-5, 15),
                    new Vector2(165, 15),
                    new Vector2(165, -10),
                    new Vector2(-5, -10)
                };
            }
        }

        // Place Player at the beginning
        GameObject player = GameObject.Find("Musashi");
        if (player != null)
        {
            player.transform.position = new Vector3(2, 2, 0);
        }

        EditorSceneManager.SaveScene(map4);
        Debug.Log("Map 4 layout and enemies updated!");
    }

    private static void PlaceEnemies(string prefabPath, string name, int roomIndex, float offset1, float offset2)
    {
        int startX = roomIndex * 32;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            GameObject e1 = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            e1.name = name + "_1";
            e1.transform.position = new Vector3(startX + offset1, 2, 0);

            GameObject e2 = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            e2.name = name + "_2";
            e2.transform.position = new Vector3(startX + offset2, 2, 0);
        }
    }
}
