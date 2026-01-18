#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class GameEntityVariantMenu
{
    enum E_GameEntityType
    {
        Unit,
        Building,
        Obstacle,
        Interact,
        Trap
    }

    const string path = "Assets/Resources/Prefabs/GameEntity/";

    // ---------- 메뉴 항목들 ----------

    [MenuItem("Tools/Create Game Entity/Unit Variant")]
    public static void CreateUnitVariant()
        => CreateVariant(E_GameEntityType.Unit);

    [MenuItem("Tools/Create Game Entity/Building Variant")]
    public static void CreateBuildingVariant()
        => CreateVariant(E_GameEntityType.Building);

    [MenuItem("Tools/Create Game Entity/Obstacle Variant")]
    public static void CreateObstacleVariant()
        => CreateVariant(E_GameEntityType.Obstacle);

    [MenuItem("Tools/Create Game Entity/Interact Variant")]
    public static void CreateInteractVariant()
        => CreateVariant(E_GameEntityType.Obstacle);

    [MenuItem("Tools/Create Game Entity/Trap Variant")]
    public static void CreateTrapVariant()
        => CreateVariant(E_GameEntityType.Trap);

    // ---------- 공통 처리 로직 ----------

    private static void CreateVariant(E_GameEntityType type)
    {
        string objstPath = $"{path}/Base Game Entity ({type}).prefab";


        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(objstPath);
        if (basePrefab == null)
        {
            Debug.LogError($"Base prefab not found at path: {objstPath}");
            return;
        }

        // 1) 베이스 프리팹 인스턴스 생성
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        if (instance == null)
        {
            Debug.LogError("Failed to instantiate base prefab.");
            return;
        }

        // 2) 저장할 폴더 결정 (Project 창에서 선택한 폴더 기준)
        string folder = "Assets";
        var active = Selection.activeObject;
        if (active != null)
        {
            var path = AssetDatabase.GetAssetPath(active);
            if (AssetDatabase.IsValidFolder(path))
                folder = path;
            else
                folder = Path.GetDirectoryName(path);
        }

        string defaultFileName = $"New {type}.prefab";

        // 3) 새 프리팹 경로 만들기
        string newPath = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(folder, defaultFileName));

        // 4) Prefab Variant로 저장
        bool success;
        PrefabUtility.SaveAsPrefabAsset(instance, newPath, out success);

        // 5) 임시 인스턴스 제거
        Object.DestroyImmediate(instance);

        if (!success)
        {
            Debug.LogError("Failed to create prefab variant.");
            return;
        }

        // 6) 방금 만든 Variant 선택
        var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(newPath);
        Selection.activeObject = newPrefab;

        Debug.Log($"Created prefab variant: {newPath}");
    }
}
#endif
