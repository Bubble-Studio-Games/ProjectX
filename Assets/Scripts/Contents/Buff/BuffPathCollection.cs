using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 외부에서 생성이 불가능한 Data
/// </summary>
public class BuffPathCollection : ScriptableObject
{
#if UNITY_EDITOR
    private List<Entry> entries = new();
#endif
    private Dictionary<int, string> pathMap;

#if UNITY_EDITOR
    [System.Serializable]
    public struct Entry
    {
        public int id;
        public string path;
    }
#endif

#if UNITY_EDITOR
    public void SetEntries(List<Entry> newEntries)
    {
        entries = newEntries;
    }
    public void Initialize()
    {
        pathMap = new Dictionary<int, string>();
        foreach (var e in entries)
            pathMap[e.id] = e.path;
    }
#endif

    public string GetPath(int id)
    {
        if (pathMap == null)
            Initialize();
        return pathMap.TryGetValue(id, out var path) ? path : null;
    }
}
