using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class OakForestMapAutoTrigger
{
    static OakForestMapAutoTrigger()
    {
        EditorApplication.delayCall += RunOnce;
    }

    static void RunOnce()
    {
        string key = "OakForestMap_Built_Fix_V4";
        if (SessionState.GetBool(key, false)) return;
        SessionState.SetBool(key, true);

        string scenePath = "Assets/Scenes/Maps/Map2_OakForest.unity";
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != scenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        Debug.Log(">>> [OakForestMapAutoTrigger] Rebuilding Map 2 with updated layout, background wind sway, decor collision fixes and trap placement...");
        OakForestMapBuilder.Build();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log(">>> [OakForestMapAutoTrigger] Map 2 successfully rebuilt and saved!");
    }
}
