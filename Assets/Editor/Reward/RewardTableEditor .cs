#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using static Define;



[CustomEditor(typeof(RewardTable))]
public class RewardTableEditor : Editor
{
    private const int SIMULATION_COUNT = 1000;

    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 먼저 그림
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Drop Simulator", EditorStyles.boldLabel);

        if (GUILayout.Button($"Simulate {SIMULATION_COUNT} Drops"))
        {
            RunSimulation((RewardTable)target);
        }
    }

    private void RunSimulation(RewardTable table)
    {
        var result = table.SimulateRandomDrop(SIMULATION_COUNT);

        if (result.Count == 0)
        {
            Debug.Log("⚠ 드롭 가능한 랜덤 보상이 없습니다.");
            return;
        }

        Debug.Log($"===== Drop Simulation ({SIMULATION_COUNT}회) =====");

        int total = result.Values.Sum();

        foreach (var pair in result.OrderByDescending(p => p.Value))
        {
            float percent = (float)pair.Value / SIMULATION_COUNT * 100f;

            Debug.Log(
                $"{pair.Key.Name,-20} : {pair.Value,4}회  ({percent:F2}%)"
            );
        }

        Debug.Log("===================================");
    }
}
#endif
