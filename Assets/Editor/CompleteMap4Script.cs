using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Reflection;

[InitializeOnLoad]
public class CompleteMap4Script
{
    static CompleteMap4Script()
    {
        if (!SessionState.GetBool("CompleteMap4Script_Run", false))
        {
            SessionState.SetBool("CompleteMap4Script_Run", true);
            EditorApplication.delayCall += CompleteSetup;
        }
    }

    [MenuItem("Tools/Complete Map 4 Setup")]
    public static void CompleteSetup()
    {
        string map1Path = "Assets/Scenes/Maps/Map1_AutumnForest.unity";
        string map4Path = "Assets/Scenes/Maps/Map4_Desert.unity";

        // 1. Open Map 1 and extract Canvas & EventSystem
        Scene map1 = EditorSceneManager.OpenScene(map1Path);
        GameObject canvasMap1 = GameObject.Find("Canvas");
        GameObject eventSystemMap1 = GameObject.Find("EventSystem");
        GameObject boundsMap1 = GameObject.Find("CameraBounds"); // Sometimes used

        if (canvasMap1 != null) PrefabUtility.SaveAsPrefabAsset(canvasMap1, "Assets/TempCanvas.prefab");
        if (eventSystemMap1 != null) PrefabUtility.SaveAsPrefabAsset(eventSystemMap1, "Assets/TempEventSystem.prefab");
        if (boundsMap1 != null) PrefabUtility.SaveAsPrefabAsset(boundsMap1, "Assets/TempBounds.prefab");

        // 2. Open Map 4
        Scene map4 = EditorSceneManager.OpenScene(map4Path);

        // 3. Instantiate Canvas, EventSystem, Bounds
        GameObject tempCanvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TempCanvas.prefab");
        GameObject tempEventPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TempEventSystem.prefab");
        GameObject tempBoundsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TempBounds.prefab");

        if (tempCanvasPrefab != null && GameObject.Find("Canvas") == null) 
        {
            GameObject canvas = (GameObject)PrefabUtility.InstantiatePrefab(tempCanvasPrefab);
            canvas.name = "Canvas";
        }
        if (tempEventPrefab != null && GameObject.Find("EventSystem") == null) 
        {
            GameObject evt = (GameObject)PrefabUtility.InstantiatePrefab(tempEventPrefab);
            evt.name = "EventSystem";
        }
        if (tempBoundsPrefab != null && GameObject.Find("CameraBounds") == null) 
        {
            GameObject bnd = (GameObject)PrefabUtility.InstantiatePrefab(tempBoundsPrefab);
            bnd.name = "CameraBounds";
            // Scale or adapt bounds logic here if needed
        }

        // 4. Place Player (Musashi)
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Musashi.prefab");
        GameObject playerInstance = GameObject.Find("Musashi");
        if (playerInstance == null && playerPrefab != null)
        {
            playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.name = "Musashi";
            playerInstance.transform.position = new Vector3(-2, 0, 0); // Default safe start
        }

        // 5. Place Enemies
        PlaceEnemy("Assets/Prefabs/Enemies/Map4/EvilWizard3.prefab", "EvilWizard3", new Vector3(4, 0, 0));
        PlaceEnemy("Assets/Prefabs/Enemies/Map4/HeroKnight1.prefab", "HeroKnight1", new Vector3(8, 0, 0));
        PlaceEnemy("Assets/Prefabs/Enemies/Map4/MartialHero1.prefab", "MartialHero1", new Vector3(12, 0, 0));
        // You can add more enemies if there are any others

        // 6. Set Virtual Camera Follow to Player
        GameObject vcamObj = GameObject.Find("Virtual Camera");
        if (vcamObj != null && playerInstance != null)
        {
            Component vcam = vcamObj.GetComponent("CinemachineVirtualCamera");
            if (vcam != null)
            {
                PropertyInfo followProp = vcam.GetType().GetProperty("Follow");
                if (followProp != null)
                {
                    followProp.SetValue(vcam, playerInstance.transform);
                    Debug.Log("Set Virtual Camera Follow to Player.");
                }
            }
        }

        // 7. Cleanup temp prefabs
        AssetDatabase.DeleteAsset("Assets/TempCanvas.prefab");
        AssetDatabase.DeleteAsset("Assets/TempEventSystem.prefab");
        AssetDatabase.DeleteAsset("Assets/TempBounds.prefab");

        EditorSceneManager.SaveScene(map4);
        Debug.Log("Map 4 Fully Completed (Player, UI, Camera, Enemies)!");
    }

    private static void PlaceEnemy(string prefabPath, string name, Vector3 position)
    {
        if (GameObject.Find(name) != null) return; // Already exists
        
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.position = position;
        }
    }
}
