using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

public partial class CustomToolWindow : EditorWindow
{
    private enum MainCategory { Animation, ProjectFile, Hierarchy, Audio, Build }
    private MainCategory selectedMain = MainCategory.Animation;
    private Vector2 scrollPos;

    private Dictionary<MainCategory, string[]> subCategoryMap = new Dictionary<MainCategory, string[]> {
        { MainCategory.Animation, new string[] { "Change Tag Selected Object  ", "GameEntityAnimationTester" } },
        { MainCategory.ProjectFile, new string[] { "FBX Animation Batch Tool", "Add Collider To Selected And Children", "Convert material To Propixelizer", "Ragoll Auto Wizard", "Handle_DeleteFile" } },
        { MainCategory.Hierarchy, new string[] { "Find Audio Listeners", "Add Collider To Selected And Children", "Adjust Skinned Mesh Bounds", "Replace Font" } },
        { MainCategory.Audio, new string[] { "TODO", "TODO" } },
        { MainCategory.Build, new string[] { "Multy Player", "TODO" } }
    };


    // 👉 서브툴 함수 맵핑
    private Dictionary<(MainCategory, int), Action> toolDrawMap;

    private int selectedSub = 0;

    [MenuItem("Tools/Custom Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<CustomToolWindow>("My Tool");
        window.minSize = new Vector2(700, 500); // 최소 크기 설정
    }

    private void OnEnable()
    {
        // 📌 함수 연결 테이블
        toolDrawMap = new Dictionary<(MainCategory, int), Action>
        {
            { (MainCategory.Animation, 0), Handle_SelectObjectChangeTag },
            { (MainCategory.Animation, 1), Handle_DrawGameEntityAnimation },

            { (MainCategory.ProjectFile, 0), Handle_FBXAnimationBatchTool },
            { (MainCategory.ProjectFile, 1), HandleAddColliderToSelectedAndChildren },
            { (MainCategory.ProjectFile, 2), Handle_ConvertMaterialsToProPixelizer },
            { (MainCategory.ProjectFile, 3), Handle_RagollAutoWizard },
            { (MainCategory.ProjectFile, 4), Handle_DeleteFile },

            { (MainCategory.Hierarchy, 0), Handle_FindAudioListeners },
            { (MainCategory.Hierarchy, 1), HandleAddColliderToSelectedAndChildren },
            { (MainCategory.Hierarchy, 2), Handle_AdjustSkinnedMeshBounds },
            { (MainCategory.Hierarchy, 3), Handle_FontReplace },

            { (MainCategory.Audio, 0), DrawVolumeTool },

            { (MainCategory.Build, 0), Handle_PerformWin64Build }

            // 필요한 만큼 계속 추가 가능
        };

        // 나머지 세팅

        // 1~20 플레이어 옵션을 문자열 배열로 생성
        playerCountOptions = new string[20];
        for (int i = 0; i < 20; i++)
        {
            playerCountOptions[i] = (i + 1).ToString();
        }

        minSize = new Vector2(700, 500);
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.BeginHorizontal();

        DrawMainTabs();

        EditorGUILayout.BeginVertical();

        DrawSubTabs();

        EditorGUILayout.Space(10);

        DrawContentArea();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    private void DrawMainTabs()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(100));

        foreach (MainCategory category in System.Enum.GetValues(typeof(MainCategory)))
        {
            if (GUILayout.Toggle(selectedMain == category, category.ToString(), "Button"))
            {
                if (selectedMain != category)
                {
                    selectedMain = category;
                    selectedSub = 0;
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSubTabs()
    {
        string[] subTabs = subCategoryMap[selectedMain];

        float totalWidth = position.width - 120; // 대분류 탭 100 + 여유
        float buttonMinWidth = 150f;
        float spacing = 4f;

        int buttonsPerRow = Mathf.Max(1, Mathf.FloorToInt((totalWidth + spacing) / (buttonMinWidth + spacing)));

        for (int row = 0; row < Mathf.CeilToInt((float)subTabs.Length / buttonsPerRow); row++)
        {
            EditorGUILayout.BeginHorizontal();

            for (int col = 0; col < buttonsPerRow; col++)
            {
                int index = row * buttonsPerRow + col;
                if (index >= subTabs.Length)
                    break;

                if (GUILayout.Toggle(selectedSub == index, new GUIContent(subTabs[index], subTabs[index]), "Button", GUILayout.MinWidth(buttonMinWidth)))
                {
                    selectedSub = index;
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawContentArea()
    {
        // 🔹 현재 선택된 탭 기준으로 GameEntityAnimationTester 활성/비활성 관리
        UpdateGameEntityAnimationTesterActivation();

        var key = (selectedMain, selectedSub);
        if (toolDrawMap.TryGetValue(key, out var drawFunc))
        {
            drawFunc.Invoke();
        }
        else
        {
            GUILayout.Label("No tool assigned to this tab.");
        }
    }


    // 📌 예시용 추가 함수들
    private void DrawRenameTool() => GUILayout.Label("Rename Tool Placeholder");
    private void DrawGravityTool() => GUILayout.Label("Gravity Tool Placeholder");
    private void DrawVolumeTool() => GUILayout.Label("Volume Tool Placeholder");
}
