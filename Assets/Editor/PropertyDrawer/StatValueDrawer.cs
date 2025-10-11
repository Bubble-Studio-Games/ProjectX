#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;

[CustomPropertyDrawer(typeof(StatValue))]
public class StatValueDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 내부 필드 찾기
        var valueProp = property.FindPropertyRelative("_value");
        var placesProp = property.FindPropertyRelative("_decimalPlaces");
        var roundProp = property.FindPropertyRelative("_autoRound");

        // 프리팹 오버라이드/Undo 지원 래핑
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();

        // 한 줄 float 필드 (Delayed로 깜빡임/부분입력 방지)
        float newValue = EditorGUI.DelayedFloatField(position, label, valueProp.floatValue);

        if (EditorGUI.EndChangeCheck())
        {
            // 반올림 정책 적용
            bool autoRound = roundProp != null && roundProp.boolValue;
            int places = (placesProp != null) ? placesProp.intValue : 0;

            if (autoRound)
                newValue = (float)Math.Round(newValue, Mathf.Clamp(places, 0, 6));

            valueProp.floatValue = newValue;

            // 변경 반영
            property.serializedObject.ApplyModifiedProperties();
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight; // 한 줄 높이
    }
}
#endif
