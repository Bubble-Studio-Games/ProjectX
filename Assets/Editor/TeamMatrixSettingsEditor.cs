#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using static Define;

[CustomEditor(typeof(TeamRelationSettings))]
public class TeamMatrixSettingsEditor : Editor
{
    private string[] _teamNames;
    private int _teamCount;

    private void OnEnable()
    {
        var names = Enum.GetNames(typeof(E_TeamId));
        // 마지막이 Count 라는 전제
        _teamCount = names.Length - 1;
        _teamNames = new string[_teamCount];

        for (int i = 0; i < _teamCount; i++)
            _teamNames[i] = names[i];
    }

    public override void OnInspectorGUI()
    {
        var settings = (TeamRelationSettings)target;

        EnsureRows(settings);

        EditorGUILayout.HelpBox(
            "행: 기준 팀(A)\n열: 대상 팀(B)\n체크: A 기준으로 B가 Ally/Enemy 인지 여부",
            MessageType.Info
        );

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Enemy Matrix", EditorStyles.boldLabel);
        DrawMatrix(settings, isEnemy: true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ally Matrix", EditorStyles.boldLabel);
        DrawMatrix(settings, isEnemy: false);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(settings);
        }
    }

    private void EnsureRows(TeamRelationSettings settings)
    {
        // E_TeamId 순서대로 Rows 맞추기
        while (settings.Rows.Count < _teamCount)
        {
            settings.Rows.Add(new TeamRelationSettings.TeamRow
            {
                Team = (E_TeamId)settings.Rows.Count
            });
        }

        for (int i = 0; i < _teamCount; i++)
        {
            settings.Rows[i].Team = (E_TeamId)i;
        }
    }

    private const float LeftLabelWidth = 80f;
    // 헤더/셀 공통 폭
    private const float CellWidth = 30f; // 25~35 정도면 충분

    private void DrawMatrix(TeamRelationSettings settings, bool isEnemy)
    {
        var rows = settings.Rows;

        // 헤더 행
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(LeftLabelWidth);
            for (int col = 0; col < _teamCount; col++)
            {
                GUILayout.Label(_teamNames[col], GUILayout.Width(CellWidth));
            }
        }

        // 각 행
        for (int row = 0; row < _teamCount; row++)
        {
            var rowData = rows[row];

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(_teamNames[row], GUILayout.Width(LeftLabelWidth));

                int mask = isEnemy ? rowData.EnemyMask : rowData.AllyMask;

                for (int col = 0; col < _teamCount; col++)
                {
                    bool current = (mask & (1 << col)) != 0;

                    bool next = GUILayout.Toggle(
                        current,
                        GUIContent.none,
                        GUILayout.Width(CellWidth)
                    );

                    if (next != current)
                    {
                        if (next) mask |= (1 << col);
                        else mask &= ~(1 << col);
                    }
                }

                if (isEnemy) rowData.EnemyMask = mask;
                else rowData.AllyMask = mask;
            }
        }
    }
}
#endif
