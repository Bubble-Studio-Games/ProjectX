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

            if (unit.m_StatSystem == null)
                return;

            var stat = unit.m_StatSystem.m_Stat;
            EditorGUILayout.LabelField("HP", $"{stat.m_iCurrentHp}/{stat.m_iMaxHP}");
            EditorGUILayout.LabelField("MP", $"{stat.m_iCurrentMP}/{stat.m_iMaxMP}");
            EditorGUILayout.LabelField("물리 방어력", stat.m_iPhysicalDefence.ToString());
            EditorGUILayout.LabelField("마법 방어력", stat.m_iMagicalDefence.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("⚔ 공격 패턴", EditorStyles.boldLabel);

            foreach (var pattern in stat.m_AttackPatterns)
            {
                EditorGUILayout.LabelField($"- {pattern.AttackName} | 쿨타임: {pattern.m_iCoolTime} | 현재 남은 쿨타임: {Time.time - pattern.lastCooltime}");
            }
        }
    }
}
#endif

