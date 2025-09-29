// Assets/Editor/HideDvcFiles.cs
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class HideDvcFiles
{
    static HideDvcFiles()
    {
        EditorApplication.projectWindowItemOnGUI += (guid, rect) =>
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".dvc"))
            {
                // Rect을 비워서 보이지 않게 함
                GUI.color = new Color(0, 0, 0, 0);
                GUI.Label(rect, "");
                GUI.color = Color.white;
            }
        };
    }
}
