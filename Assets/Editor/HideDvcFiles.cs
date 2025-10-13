using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class HideDvcFiles
{
    static HideDvcFiles()
    {
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowGUI;
    }

    private static void OnProjectWindowGUI(string guid, Rect rect)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (path.EndsWith(".dvc"))
        {

            // meta 파일이면 보이지 않도록
            // 위치를 강제로 화면 밖으로 밀어버림
            GUI.color = new Color(0, 0, 0, 0);
            GUI.Label(rect, "");
            GUI.color = Color.white;
        }
    }
}
