using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public partial class CustomToolWindow 
{
    private int selectedPlayerIndex = 0; // 내부에서 선택된 인덱스 저장 (0 = 1명, 19 = 20명)
    private string[] playerCountOptions;

    private void Handle_PerformWin64Build()
    {
        GUILayout.Label("👥 플레이어 수 선택", EditorStyles.boldLabel);

        // 드롭다운 리스트로 1~20 중 선택
        selectedPlayerIndex = EditorGUILayout.Popup("플레이어 수", selectedPlayerIndex, playerCountOptions);

        // 선택된 값으로 빌드 실행
        if (GUILayout.Button("Win64 빌드 실행"))
        {
            int playerCount = selectedPlayerIndex + 1; // 인덱스가 0이면 실제 플레이어 수는 1
            PerformWin64Build(playerCount);
        }
    }

    private void PerformWin64Build(int playerCount)
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows);

        for (int i = 1; i <= playerCount; i++)
        {
            BuildPipeline.BuildPlayer(GetScenePaths(),
                "Builds/Win64/" + GetProjectName() + i.ToString() + "/" + GetProjectName() + i.ToString() + ".exe",
                BuildTarget.StandaloneWindows64, BuildOptions.AutoRunPlayer);
        }
    }

    private string GetProjectName()
    {
        string[] s = Application.dataPath.Split('/');
        return s[s.Length - 2];
    }

    private string[] GetScenePaths()
    {
        string[] scenes = new string[EditorBuildSettings.scenes.Length];

        for (int i = 0; i < scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }

        return scenes;
    }

}
