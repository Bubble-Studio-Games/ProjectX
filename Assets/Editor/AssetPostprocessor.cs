// Assets/Editor/IgnoreDvcFiles.cs
using UnityEditor;

public class IgnoreDvcFiles : AssetPostprocessor
{
    static string[] ignoredExtensions = { ".dvc" };

    static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (var asset in importedAssets)
        {
            foreach (var ext in ignoredExtensions)
            {
                if (asset.EndsWith(ext))
                {
                    AssetDatabase.DeleteAsset(asset); // Unity DB에서 제거
                    return;
                }
            }
        }
    }
}
