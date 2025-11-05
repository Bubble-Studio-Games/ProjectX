#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Unit))]
public class ObjectInfoInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameEntity unit = (GameEntity)target;
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🔥 Runtime Stat Info", EditorStyles.boldLabel);

            if (unit.m_AttributeSystem == null)
                return;

            var stat = unit.m_AttributeSystem.m_Stat;
            EditorGUILayout.LabelField("HP", $"{stat.m_iCurrentHp.Value}/{stat.m_iMaxHP.Value}");
            EditorGUILayout.LabelField("MP", $"{stat.m_iCurrentMP.Value}/{stat.m_iMaxMP.Value}");
            EditorGUILayout.LabelField("물리 방어력", stat.m_iPhysicalDefence.Value.ToString());
            EditorGUILayout.LabelField("마법 방어력", stat.m_iMagicalDefence.Value.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("⚔ 공격 패턴", EditorStyles.boldLabel);

            foreach (var pattern in unit.m_AttributeSystem.m_AttackPatterns)
            {
                EditorGUILayout.LabelField($"- {pattern.AttackName} | 쿨타임: {pattern.m_iCoolTime} | 현재 남은 쿨타임: {Time.time - pattern.m_fLastCooltime}");
            }
        }
    }
}
#endif

