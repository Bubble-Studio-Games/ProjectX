#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // GUI.enabled를 false로 설정하여 편집 불가능하게 만듦
        GUI.enabled = false;
        // 기존 필드 그리기 로직 호출 (값을 보여줌)
        EditorGUI.PropertyField(position, property, label, true);
        // 다시 true로 복원 (다른 필드에 영향 주지 않도록)
        GUI.enabled = true;
    }
}
#endif
