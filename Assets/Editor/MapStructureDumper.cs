using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Text;
using System.IO;

public class MapStructureDumper
{
    [MenuItem("Tools/Dump Map 1 Structure")]
    public static void Dump()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Maps/Map1_AutumnForest.unity");
        StringBuilder sb = new StringBuilder();
        
        foreach (GameObject rootObj in scene.GetRootGameObjects())
        {
            DumpGameObject(rootObj, sb, 0);
        }
        
        File.WriteAllText("Map1_Structure.txt", sb.ToString());
        Debug.Log("Dumped Map 1 structure to Map1_Structure.txt");
    }

    private static void DumpGameObject(GameObject go, StringBuilder sb, int indentLevel)
    {
        sb.Append(new string('-', indentLevel * 2));
        sb.Append(go.name);
        
        // Append components
        Component[] components = go.GetComponents<Component>();
        sb.Append(" [");
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null)
            {
                sb.Append(components[i].GetType().Name);
                if (i < components.Length - 1) sb.Append(", ");
            }
        }
        sb.Append("]\n");

        foreach (Transform child in go.transform)
        {
            DumpGameObject(child.gameObject, sb, indentLevel + 1);
        }
    }
}
