using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 🔹 Assets/Resources/Data/Save 폴더에서 우클릭 시 나타나는 "모든 세이브 파일 삭제" 메뉴
/// </summary>
public static class DeleteSaveFilesMenu
{
    private const string targetPath = "Assets/Resources/Data/Save";

    [MenuItem("Assets/Delete All Save Files", validate = true)]
    private static bool ValidateDeleteSaveFiles()
    {
        // ✅ 현재 선택된 경로가 Assets/Resources/Data/Save일 때만 메뉴 활성화
        string selectedPath = GetSelectedFolderPath();
        return selectedPath != null && selectedPath.Replace("\\", "/").StartsWith(targetPath);
    }

    [MenuItem("Assets/Delete All Save Files", priority = 0)]
    private static void DeleteAllSaveFiles()
    {
        string selectedPath = GetSelectedFolderPath();
        if (selectedPath == null)
        {
            EditorUtility.DisplayDialog("경로 오류", "폴더를 찾을 수 없습니다.", "확인");
            return;
        }

        string fullPath = Path.GetFullPath(selectedPath);
        if (!Directory.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("삭제 실패", $"폴더가 존재하지 않습니다:\n{fullPath}", "확인");
            return;
        }

        // 확인 팝업
        if (!EditorUtility.DisplayDialog("⚠️ 모든 세이브 파일 삭제",
            $"이 폴더 안의 파일을 전부 삭제하시겠습니까?\n\n{selectedPath}",
            "삭제", "취소"))
        {
            return;
        }

        // 🔥 파일 삭제
        var files = Directory.GetFiles(fullPath);
        foreach (var file in files)
        {
            try
            {
                File.Delete(file);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"파일 삭제 실패: {file}\n{e.Message}");
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("✅ 완료", "모든 세이브 파일이 삭제되었습니다.", "확인");
        Debug.Log($"🧹 Save 폴더 정리 완료 → {selectedPath}");
    }

    // 선택된 폴더 경로 가져오기
    private static string GetSelectedFolderPath()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path))
            return null;

        if (Directory.Exists(path))
            return path;

        return Path.GetDirectoryName(path);
    }
}
