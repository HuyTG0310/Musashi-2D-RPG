#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Menu: Tools > Musashi > Polish Hand-Drawn Cave Map 3
/// 
/// Tinh chinh CHUAN XAC cho NEN GROUND DI CHUYEN:
/// - GIU NGUYEN 100% TRAN HANG (Y >= 7): Toan bo thach nhu do ban tu tay ve duoc giu nguyen.
/// - GIU NGUYEN 100% CAC BUC NHAY (Jump Platforms):
///   + Cac phien da lo lung (Stepping stones) 258, 259...
///   + Cac buc go nhay 237, 238, 239...
///   + Cac buc go ham nguc 248, 249, 250...
///   + Cau go cau go than cay 51..60...
/// - GIU NGUYEN 100% CAC CON DOC (Slopes):
///   + Cac doan doc nghieng 180, 181, 186..192...
///   + Cac bac thang chuyen do cao giua cac tang.
/// - CHI NANG CAP DUY NHAT:
///   + Mat san di chuyen phang o Y <= 6: Vien da xam pha le phat sang A_50/51 (mainlev_build_201/202).
///   + Mep bo vuc thang dung cua san phang: Vien dung A_56/59 (mainlev_build_242/245).
///   + Ruot sau ben trong long dat: Mau den dac A_05 (mainlev_build_21).
/// </summary>
public static class Map3CaveAutoPolisher
{
    const string TilePath_Black   = "Assets/Sprites/TileMaps/Map3/mainlev_build_21.asset";       // A_05 (Solid Black #151015)
    const string TilePath_Top1    = "Assets/Sprites/TileMaps/Map3/mainlev_build_201.asset";      // A_50 (Walkable Flat 1)
    const string TilePath_Top2    = "Assets/Sprites/TileMaps/Map3/mainlev_build_202.asset";      // A_51 (Walkable Flat 2)
    const string TilePath_RimL    = "Assets/Sprites/TileMaps/Map3/mainlev_build_242.asset";      // A_56 (Left Rim / Wall)
    const string TilePath_RimR    = "Assets/Sprites/TileMaps/Map3/mainlev_build_245.asset";      // A_59 (Right Rim / Wall)

    static readonly HashSet<string> ProtectedTileNames = new HashSet<string>
    {
        // Slopes:
        "mainlev_build_180", "mainlev_build_181", "mainlev_build_186", "mainlev_build_187",
        "mainlev_build_188", "mainlev_build_189", "mainlev_build_190", "mainlev_build_191",
        "mainlev_build_192", "mainlev_build_306",
        // Jump platforms (wood, stone, dungeon):
        "mainlev_build_237", "mainlev_build_238", "mainlev_build_239",
        "mainlev_build_248", "mainlev_build_249", "mainlev_build_250",
        "mainlev_build_254", "mainlev_build_255", "mainlev_build_258", "mainlev_build_259",
        "mainlev_build_260", "mainlev_build_261", "mainlev_build_262", "mainlev_build_263",
        "mainlev_build_150",
        // Log bridge:
        "mainlev_build_51", "mainlev_build_52", "mainlev_build_53", "mainlev_build_54",
        "mainlev_build_55", "mainlev_build_56", "mainlev_build_57", "mainlev_build_58",
        "mainlev_build_59", "mainlev_build_60"
    };

    public static void PolishMapBatchMode()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Maps/Map3_Cave.unity");
        PolishMap();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Map3CaveAutoPolisher: Saved scene after batchmode polish.");
    }

    [MenuItem("Tools/Musashi/Polish Hand-Drawn Cave Map 3")]
    public static void PolishMap()
    {
        var groundGO = GameObject.Find("Ground");
        if (groundGO == null)
        {
            Debug.LogError("Map3CaveAutoPolisher: Khong tim thay GameObject 'Ground' trong Scene!");
            return;
        }

        var ground = groundGO.GetComponent<Tilemap>();
        if (ground == null)
        {
            Debug.LogError("Map3CaveAutoPolisher: GameObject 'Ground' khong co component Tilemap!");
            return;
        }

        var tileBlack = AssetDatabase.LoadAssetAtPath<TileBase>(TilePath_Black);
        var tileTop1 = AssetDatabase.LoadAssetAtPath<TileBase>(TilePath_Top1);
        var tileTop2 = AssetDatabase.LoadAssetAtPath<TileBase>(TilePath_Top2);
        var tileRimL = AssetDatabase.LoadAssetAtPath<TileBase>(TilePath_RimL);
        var tileRimR = AssetDatabase.LoadAssetAtPath<TileBase>(TilePath_RimR);

        if (tileBlack == null || tileTop1 == null || tileRimL == null || tileRimR == null)
        {
            Debug.LogError("Map3CaveAutoPolisher: Khong the load day du cac asset tile goc trong Assets/Sprites/TileMaps/Map3/!");
            return;
        }

        Undo.RegisterCompleteObjectUndo(ground, "Polish Movement Ground (Keep Slopes & Platforms)");

        // 1. Thu thap tat ca cac toa do cell hien co trong Ground
        var bounds = ground.cellBounds;
        var solidCoords = new HashSet<Vector3Int>();
        var tileMap = new Dictionary<Vector3Int, TileBase>();

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (ground.HasTile(pos))
            {
                solidCoords.Add(pos);
                tileMap[pos] = ground.GetTile(pos);
            }
        }

        if (solidCoords.Count == 0)
        {
            Debug.LogWarning("Map3CaveAutoPolisher: Tilemap Ground khong co tile nao!");
            return;
        }

        // 2. Xac dinh cac be mat (Surface points) o do cao Y < 7
        var surfacePoints = new HashSet<Vector3Int>();
        foreach (var pos in solidCoords)
        {
            if (pos.y < 7 && !solidCoords.Contains(new Vector3Int(pos.x, pos.y + 1, 0)))
            {
                surfacePoints.Add(pos);
            }
        }

        // 3. Xac dinh cac o thuoc Doc (Slopes)
        var slopeCoords = new HashSet<Vector3Int>();
        foreach (var pos in surfacePoints)
        {
            var currentTile = tileMap[pos];
            if (currentTile != null && ProtectedTileNames.Contains(currentTile.name))
            {
                slopeCoords.Add(pos);
                continue;
            }

            // Kiem tra do cao cua cac o be mat lien ke trai va phai
            bool hasDiffL = false, hasDiffR = false;
            foreach (var sp in surfacePoints)
            {
                if (sp.x == pos.x - 1 && sp.y != pos.y) hasDiffL = true;
                if (sp.x == pos.x + 1 && sp.y != pos.y) hasDiffR = true;
            }

            if (hasDiffL || hasDiffR)
            {
                slopeCoords.Add(pos);
            }
        }

        // 4. Xac dinh cac o thuoc Buc nhay (Jump Platforms / Stepping stones)
        var jumpPlatformCoords = new HashSet<Vector3Int>();
        foreach (var pos in solidCoords)
        {
            if (pos.y < 7)
            {
                var currentTile = tileMap[pos];
                if (currentTile != null && ProtectedTileNames.Contains(currentTile.name))
                {
                    jumpPlatformCoords.Add(pos);
                }
                else
                {
                    bool airAbove = !solidCoords.Contains(new Vector3Int(pos.x, pos.y + 1, 0));
                    bool airBelow = !solidCoords.Contains(new Vector3Int(pos.x, pos.y - 1, 0));
                    if (airAbove && airBelow)
                    {
                        jumpPlatformCoords.Add(pos);
                    }
                }
            }
        }

        // 5. Tien hanh ap dung: Chi doi cac san phang va ruot den dac, bao toan tat ca nhung thu khac!
        var posList = new List<Vector3Int>();
        var tileList = new List<TileBase>();

        int keptOriginalCount = 0;
        int styledFlatCount = 0;
        int styledBlackMassCount = 0;

        foreach (var pos in solidCoords)
        {
            // NGUYEN TAC: GIU NGUYEN TRAN HANG, BUC NHAY VA DOC
            if (pos.y >= 7 || jumpPlatformCoords.Contains(pos) || slopeCoords.Contains(pos))
            {
                posList.Add(pos);
                tileList.Add(tileMap[pos]);
                keptOriginalCount++;
                continue;
            }

            bool u = solidCoords.Contains(new Vector3Int(pos.x, pos.y + 1, 0));
            bool d = solidCoords.Contains(new Vector3Int(pos.x, pos.y - 1, 0));
            bool l = solidCoords.Contains(new Vector3Int(pos.x - 1, pos.y, 0));
            bool r = solidCoords.Contains(new Vector3Int(pos.x + 1, pos.y, 0));

            TileBase chosenTile;

            if (!u)
            {
                // Mat san di chuyen phang: Vien da xam pha le
                if (!l)
                {
                    chosenTile = tileRimL;
                }
                else if (!r)
                {
                    chosenTile = tileRimR;
                }
                else
                {
                    chosenTile = (Mathf.Abs(pos.x) % 2 == 0) ? tileTop1 : tileTop2;
                }
                styledFlatCount++;
            }
            else if (!l)
            {
                chosenTile = tileRimL;
                styledFlatCount++;
            }
            else if (!r)
            {
                chosenTile = tileRimR;
                styledFlatCount++;
            }
            else if (!d)
            {
                // Mat duoi vom da: Giu nguyen tile goc cua ban
                chosenTile = tileMap[pos];
                keptOriginalCount++;
            }
            else
            {
                // Ruot sau ben trong long dat: Mau den dac A_05 (100% Solid Black)
                chosenTile = tileBlack;
                styledBlackMassCount++;
            }

            posList.Add(pos);
            tileList.Add(chosenTile);
        }

        // 6. Ap dung vao Ground
        ground.SetTiles(posList.ToArray(), tileList.ToArray());
        ground.CompressBounds();

        EditorSceneManager.MarkSceneDirty(ground.gameObject.scene);

        Debug.Log($"<color=#55FF55><b>Map3CaveAutoPolisher:</b></color> Da tinh chinh san di chuyen hoan tat!\n" +
                  $"- Tran hang, Buc nhay & Con doc GIU NGUYEN 100%: {keptOriginalCount} o\n" +
                  $"- San di chuyen phang vien sang A_50/51: {styledFlatCount} o\n" +
                  $"- Ruot sau long dat den dac A_05: {styledBlackMassCount} o\n" +
                  $"Toan bo hinh dang doc, buc nhay, Foreground va Background deu duoc giu nguyen ven.");
    }
}
#endif