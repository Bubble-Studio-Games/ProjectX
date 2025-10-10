using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuffRegistry
{
    [MenuItem("Tools/Buffs/Buff Registry")]
    public static void Execute()
    {
        // Buffs 폴더안의 BuffConfig 로드
        var buffs = new List<BuffConfig>();
        string[] guids = AssetDatabase.FindAssets("t:BuffConfig", new[] { "Assets/Resources/Data/Buff" });

        // path 얻어서 list에 저장
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var buff = AssetDatabase.LoadAssetAtPath<BuffConfig>(path);
            if (buff != null)
                buffs.Add(buff);
        }

        // Collectiong 할 데이터 없으면 리턴
        if (buffs.Count == 0)
        {
            Debug.LogWarning("Collection을 생성할 BuffConfig 없음");
            return;
        }

        // SO에 넣어줄 데이터 리스트 생성
        var entries = new List<BuffPathCollection.Entry>();

        // 찾은 Config 의 path 얻어서 기타 경로 지우는 과정
        foreach (var buff in buffs)
        {
            string path = AssetDatabase.GetAssetPath(buff);
            int startIndex = path.IndexOf("Resources") + "Resources/".Length;   // Resources 시작 인덱스 + Resources/ 까지의 길이
            path = path.Substring(startIndex); //문자열에서 지정한 위치(startIndex)부터 끝까지 잘라내는 함수
            path = Path.ChangeExtension(path, null);    //파일의 확장자(.asset, .png, .txt 등)을 제거하는 함수

            entries.Add(new BuffPathCollection.Entry { id = buff.id, path = path });
        }

        // 생성할 에셋의 경로
        const string assetPath = "Assets/Resources/Data/Buff/BuffPathRegistry.asset";
        BuffPathCollection registry = AssetDatabase.LoadAssetAtPath<BuffPathCollection>(assetPath);

        // 처음 생성이면?
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<BuffPathCollection>();
            AssetDatabase.CreateAsset(registry, assetPath);
            Debug.Log("BuffPath Collection 생성 완료!");
        }

        // 갱신처리
        registry.SetEntries(entries);
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"{entries.Count}개 버프 등록");
    }
}
