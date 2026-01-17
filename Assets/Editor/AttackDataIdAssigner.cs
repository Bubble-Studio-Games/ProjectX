#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

public static class AttackDataIdAssigner
{
    private const string ROOT_PATH = "Assets/Resources/Data/Attack Data";

    // 네 AttackData의 ID 필드 이름만 맞춰줘
    // (예: "Id" / "m_Id" / "m_iId" 등)
    private const string ID_PROPERTY_NAME = "Id";

    [MenuItem("Tools/AttackData/Auto Assign IDs (ID Only)")]
    public static void AssignIds_ID_Only()
    {
        var guids = AssetDatabase.FindAssets("t:AttackData", new[] { ROOT_PATH });
        if (guids == null || guids.Length == 0)
        {
            Debug.LogWarning($"[AttackDataIdAssigner] No AttackData found under: {ROOT_PATH}");
            return;
        }

        var paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        int id = 1;
        foreach (var path in paths)
        {
            var data = AssetDatabase.LoadAssetAtPath<AttackData>(path);
            if (data == null) continue;

            // Undo 지원 (ID 변경만 되돌리기 가능)
            Undo.RecordObject(data, "Assign AttackData IDs");

            var so = new SerializedObject(data);
            var idProp = so.FindProperty(ID_PROPERTY_NAME);

            if (idProp == null)
            {
                Debug.LogError($"[AttackDataIdAssigner] Cannot find '{ID_PROPERTY_NAME}' in: {path}");
                continue;
            }

            // ✅ 오직 이 한 줄만 바꿈: ID
            idProp.intValue = id;

            // ✅ 변경사항 적용 (ID 외에는 건드린 게 없으니 다른 값은 그대로)
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(data);
            id++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AttackDataIdAssigner] Assigned IDs 1 ~ {id - 1} (ID only)");
    }
}
#endif
