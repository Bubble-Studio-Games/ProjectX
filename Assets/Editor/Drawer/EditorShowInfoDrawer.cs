#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

[CustomEditor(typeof(MonoBehaviour), true)]
public class EditorShowInfoDrawer : Editor
{
    public override void OnInspectorGUI()
    {
        DrawFoldableInfoIfExists();
        DrawDefaultInspector();
    }

    private void DrawFoldableInfoIfExists()
    {
        var targetType = target.GetType();
        var attr = targetType.GetCustomAttribute<EditorShowInfoAttribute>();
        if (attr == null) return;

        // 오브젝트/컴포넌트별로 접힘 상태 기억
        string key = $"EditorShowInfo.Foldout.{targetType.FullName}.{target.GetInstanceID()}";
        bool isOpen = EditorPrefs.GetBool(key, true);

        // Foldout 헤더
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            // Foldout 라벨은 원하는대로
            isOpen = EditorGUILayout.Foldout(isOpen, "Info", true);
            EditorPrefs.SetBool(key, isOpen);

            if (isOpen)
            {
                EditorGUILayout.HelpBox(attr.Message, attr.MessageType);
            }
        }
    }
}
#endif
