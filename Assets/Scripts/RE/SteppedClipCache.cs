#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ProjectX/Animation/Stepped Clip Cache")]
public sealed class SteppedClipCache : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string key;
        public AnimationClip steppedClip;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, AnimationClip> map;

    private void OnEnable()
    {
        map = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            if (string.IsNullOrWhiteSpace(e.key) || e.steppedClip == null) continue;
            map[e.key.Trim()] = e.steppedClip;
        }
    }

    public bool TryGet(string key, out AnimationClip clip)
    {
        if (map == null) OnEnable();
        return map.TryGetValue(key.Trim(), out clip) && clip != null;
    }

#if UNITY_EDITOR
    public void EditorUpsert(string key, AnimationClip clip)
    {
        key = key.Trim();
        for (int i = 0; i < entries.Count; i++)
        {
            if (string.Equals(entries[i].key, key, StringComparison.OrdinalIgnoreCase))
            {
                entries[i] = new Entry { key = key, steppedClip = clip };
                EditorUtility.SetDirty(this);
                return;
            }
        }

        entries.Add(new Entry { key = key, steppedClip = clip });
        EditorUtility.SetDirty(this);
    }
#endif
}
