#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Menu: Tools > Musashi > Apply Grey Cave Styling To Ground (Keep Background)
/// 
/// Chuyen doi toan bo Tilemap Ground hien tai trong Scene sang phong cach A_grey_cave:
/// - Lop sau ben trong (ruot vach da, long dat): Chuyen thanh den dac A_05 (mainlev_build_21)
/// - Ria player di chuyen (mat san phang): Da xam vien sang A_50/A_51 (mainlev_build_201/202)
/// - Mep bo trai / phai: A_56 (mainlev_build_242), A_59 (mainlev_build_245)
/// - Tran hang thach nhu: A_86 / A_87 (mainlev_build_326/327)
/// 
/// KHONG CHAM VAO LOP BACKGROUND (Giu nguyen 100% Background do ban da dung).
/// </summary>
public static class Map3GroundStyling
{
    const string TilePath_Black = "Assets/Sprites/TileMaps/Map3/mainlev_build_21.asset";       // A_05
    const string TilePath_Top1 = "Assets/Sprites/TileMaps/Map3/mainlev_build_201.asset";      // A_50
    const string TilePath_Top2 = "Assets/Sprites/TileMaps/Map3/mainlev_build_202.asset";      // A_51
    const string TilePath_RimL = "Assets/Sprites/TileMaps/Map3/mainlev_build_242.asset";      // A_56
    const string TilePath_RimR = "Assets/Sprites/TileMaps/Map3/mainlev_build_245.asset";      // A_59
    const string TilePath_Ceil1 = "Assets/Sprites/TileMaps/Map3/mainlev_build_326.asset";     // A_86
    const string TilePath_Ceil2 = "Assets/Sprites/TileMaps/Map3/mainlev_build_327.asset";     // A_87

    [MenuItem("Tools/Musashi/Apply Grey Cave Styling To Ground (Keep Background)")]
    public static void ApplyStyling()
    {
        var groundGO = GameObject.Find("Ground");
        if (groundGO == null)
        {
            Debug.LogError("Map3GroundStyling: Khong tim thay GameObject 'Ground' trong Scene!");
            return;
        }

        var ground = groundGO.GetComponent<Tilemap>();
        if (ground == null)
        {
            Debug.LogError("Map3GroundStyling: GameObject 'Ground' khong co component Tilemap!");
            return;
        }

        var tileBlack = AssetDatabase.LoadAssetAtPath<TileBase>(TilePath_Black);
        var tileTop1 = AssetDatabase.LoadAssetAtPath<TileBase>(TilePath_Top1);
        var tileTop2 = AssetDatabase.LoadAssetAtPath<TileBase>(TilePath_Top2);
        var tileRimL = AssetDatabase.LoadAssetAtPath<TileBase>(TilePath_RimL);
        var tileRimR = AssetDatabase.LoadAssetAtPath<TileBase>(TilePath_RimR);
        var tileCeil1 = AssetDatabase.LoadAssetAtPath<TileBase>(TilePath_Ceil1);
        var tileCeil2 = AssetDatabase.LoadAssetAtPath<TileBase>(TilePath_Ceil2);

        if (tileBlack == null || tileTop1 == null)
        {
            Debug.LogError("Map3GroundStyling: Khong tim thay du cac Tile asset trong Assets/Sprites/TileMaps/Map3/!");
            return;
        }

        // Thu thap tat ca toa do cell dang co tile
        var bounds = ground.cellBounds;
        var solidCoords = new HashSet<Vector3Int>();

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (ground.HasTile(pos))
            {
                solidCoords.Add(pos);
            }
        }

        Undo.RecordObject(ground, "Apply Grey Cave Styling To Ground");

        var posList = new List<Vector3Int>();
        var tileList = new List<TileBase>();

        int topCount = 0;
        int ceilCount = 0;
        int wallCount = 0;
        int blackCount = 0;

        foreach (var pos in solidCoords)
        {
            bool u = solidCoords.Contains(new Vector3Int(pos.x, pos.y + 1, 0));
            bool d = solidCoords.Contains(new Vector3Int(pos.x, pos.y - 1, 0));
            bool l = solidCoords.Contains(new Vector3Int(pos.x - 1, pos.y, 0));
            bool r = solidCoords.Contains(new Vector3Int(pos.x + 1, pos.y, 0));

            TileBase targetTile;

            if (!u && !d)
            {
                targetTile = (Mathf.Abs(pos.x) % 2 == 0) ? tileTop1 : tileTop2;
                topCount++;
            }
            else if (!u)
            {
                if (!l) { targetTile = tileRimL; wallCount++; }
                else if (!r) { targetTile = tileRimR; wallCount++; }
                else
                {
                    targetTile = (Mathf.Abs(pos.x) % 2 == 0) ? tileTop1 : tileTop2;
                    topCount++;
                }
            }
            else if (!d)
            {
                if (!l) { targetTile = tileRimL; wallCount++; }
                else if (!r) { targetTile = tileRimR; wallCount++; }
                else
                {
                    targetTile = (Mathf.Abs(pos.x) % 2 == 0) ? tileCeil1 : tileCeil2;
                    ceilCount++;
                }
            }
            else if (!l)
            {
                targetTile = tileRimL;
                wallCount++;
            }
            else if (!r)
            {
                targetTile = tileRimR;
                wallCount++;
            }
            else
            {
                targetTile = tileBlack;
                blackCount++;
            }

            posList.Add(pos);
            tileList.Add(targetTile);
        }

        ground.SetTiles(posList.ToArray(), tileList.ToArray());
        ground.CompressBounds();

        EditorSceneManager.MarkSceneDirty(ground.gameObject.scene);
        Debug.Log($"Map3GroundStyling: Hoan thanh restyle Ground voi {posList.Count} o! (Mat san xam di chuyen: {topCount}, Tran nhu da: {ceilCount}, Vach dung: {wallCount}, Ruot den dac A_05: {blackCount}). Background duoc giu nguyen 100%.");
    }
}
#endif
