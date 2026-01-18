using Assets.Scripts.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class GameConfig
{
    private const string baseFolder = "Data/GameConfig";

    // 타입별 설정 캐시
    private static readonly Dictionary<Type, ScriptableObject> _configCache = new();

    #region 공통 로더

    private static T LoadConfig<T>(string assetName = null) where T : ScriptableObject
    {
        var type = typeof(T);

        // 1차: 캐시에서 가져오기
        if (_configCache.TryGetValue(type, out var cached) && cached != null)
            return (T)cached;

        // 2차: Resources에서 로드
        string name = string.IsNullOrEmpty(assetName) ? type.Name : assetName;
        string path = $"{baseFolder}/{name}";

        var loaded = Resources.Load<T>(path);
        _configCache[type] = loaded;

#if UNITY_EDITOR
        if (loaded == null)
        {
            Debug.LogError($"[GameConfig] {type.Name} not found at Resources/{path}.asset");
        }
#endif

        return loaded;
    }

    /// <summary>
    /// 필요하면 전체 캐시를 비우고 다시 로딩하고 싶을 때 사용.
    /// (에디터에서만 쓸 일 많을 듯)
    /// </summary>
    public static void ClearCache()
    {
        _configCache.Clear();
    }

    #endregion

    public static LayerSettings Layer => LoadConfig<LayerSettings>();
    public static SoundSettings Sound => LoadConfig<SoundSettings>();
    public static MouseSettings Mouse => LoadConfig<MouseSettings>();
    public static NPCSettings NPC => LoadConfig<NPCSettings>();
    public static DialogueSettings Dialogue => LoadConfig<DialogueSettings>();
    public static SceneSettings Scene => LoadConfig<SceneSettings>();
    public static InventorySettings Inventory => LoadConfig<InventorySettings>();
    public static RuntimeSettings RuntimeSettings => LoadConfig<RuntimeSettings>();
    public static TeamRelationSettings TeamRelation => LoadConfig<TeamRelationSettings>();

}
