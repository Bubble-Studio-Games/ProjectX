#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ProPixelizer.Tools;
using System.IO;
using System.Collections;
using System.Reflection;
using UnityEngine.EventSystems;
using System;
using Unity.VisualScripting;

[ExecuteInEditMode]
public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }

    [Header("UI")]
    public AudioClip m_UIButtonClickAudioClip;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        // 폴더 준비
        EnsureSaveFolderExists();

        LoadCache();
    }

    private void Start()
    {
        SaveData();
    }

    #region FPS

    public event EventHandler OnEventApplicationQuit;

    [Range(0.1f, 120f)]
    public float fps = 12f;
    public SteppedAnimation.StepMode mode = SteppedAnimation.StepMode.FixedRate;

    [Tooltip("Stepped 애니메이션이 저장될 폴더 경로 (예: Assets/SteppedClips)")]
    public string saveFolder = "Assets/Resources/Data/Animation/SteppedClips";

    private const string CACHE_FILE_NAME = "SteppedCache.json";
    private SteppedCacheData steppedCacheData = new();

    private void SaveData()
    {
        SaveCache();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void OnApplicationQuit()
    {
        Debug.Log("💾 애니메이션 캐시 저장 중...");

        OnEventApplicationQuit?.Invoke(this, EventArgs.Empty);

        SaveData();
    }

    /// <summary>
    /// 주어진 AnimationClip을 Stepped 방식으로 변환하거나, 캐시에 존재하면 로드하여 반환.
    /// Attack Pattern에서 써먹음
    /// </summary>
    public AnimationClip ReplaceOrLoadSteppedClip(AnimationClip clip)
    {
        if (clip == null) return null;

        if (TryGetCachedClip(clip, out string cachedPath))
        {
            var cachedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(cachedPath);
            if (cachedClip != null) return cachedClip;

            Debug.LogWarning($"⚠️ 캐시 경로에 애셋이 없습니다. 재생성합니다: {cachedPath}");
        }

        string steppedName = GetSteppedClipFileName(clip.name);
        string outputPath = Path.Combine(saveFolder, steppedName);

        AnimationClip stepped = CreateSteppedClip(clip, outputPath);
        if (stepped == null)
        {
            Debug.LogError($"❌ Stepped 애니메이션 생성 실패: {clip.name}");
            return clip;
        }

        AddCacheEntry(clip, outputPath);
        return stepped;
    }



    public void ReplaceAllAnimationClipArraysInObject(MonoBehaviour gameEntity)
    {
        Type type = gameEntity.GetType();

        while (type != null && type != typeof(object))
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            foreach (var field in fields)
            {
                bool isModified = false;

                // 배열 처리
                if (field.FieldType == typeof(AnimationClip[]))
                {
                    var clips = field.GetValue(gameEntity) as AnimationClip[];
                    if (clips == null) continue;

                    for (int i = 0; i < clips.Length; i++)
                    {
                        var replaced = ReplaceOrLoadSteppedClip(clips[i]);
                        if (replaced != null && replaced != clips[i])
                        {
                            clips[i] = replaced;
                            isModified = true;
                        }
                    }

                    if (isModified)
                    {
                        field.SetValue(gameEntity, clips);
                    }
                }

                // 단일 AnimationClip 처리
                else if (field.FieldType == typeof(AnimationClip))
                {
                    var clip = field.GetValue(gameEntity) as AnimationClip;
                    var replaced = ReplaceOrLoadSteppedClip(clip);
                    if (replaced != null && replaced != clip)
                    {
                        field.SetValue(gameEntity, replaced);
                        isModified = true;
                    }
                }

                if (isModified)
                {
                    EditorUtility.SetDirty(gameEntity);
                }
            }

            type = type.BaseType;
        }
    }

    private AnimationClip CreateSteppedClip(AnimationClip sourceClip, string outputPath)
    {
        AnimationClip steppedClip = new AnimationClip();
        EditorUtility.CopySerialized(sourceClip, steppedClip);

        var sampleTimes = GetKeyframeTimes(sourceClip);

        foreach (var binding in AnimationUtility.GetCurveBindings(sourceClip))
        {
            var sourceCurve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            if (sourceCurve == null || sourceCurve.length == 0)
                continue; // 빈 커브는 무시

            // Keyframe 배열을 한 번에 생성 (AddKey 반복 대신)
            Keyframe[] keys = new Keyframe[sampleTimes.Count];
            for (int i = 0; i < sampleTimes.Count; i++)
            {
                float t = Mathf.Clamp(sampleTimes[i], 0, sourceClip.length);
                float value = sourceCurve.Evaluate(t);

                keys[i] = new Keyframe(
                    t,
                    value,
                    float.NegativeInfinity,
                    float.PositiveInfinity,
                    0f,
                    0f
                );
            }

            var newCurve = new AnimationCurve(keys);
            AnimationUtility.SetEditorCurve(steppedClip, binding, newCurve);
        }

        // Asset 생성/갱신
        AssetDatabase.CreateAsset(steppedClip, outputPath);
        AssetDatabase.ImportAsset(outputPath);

        return AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
    }


    private List<float> GetKeyframeTimes(AnimationClip clip)
    {
        List<float> times = new();

        switch (mode)
        {
            case SteppedAnimation.StepMode.FixedRate:
                int frameCount = Mathf.CeilToInt(clip.length * fps);
                for (int i = 0; i <= frameCount; i++)
                    times.Add(i / fps);
                break;

            case SteppedAnimation.StepMode.FixedTimeDelay:
                float delay = 1f / fps;
                int count = Mathf.CeilToInt(clip.length / delay);
                for (int i = 0; i <= count; i++)
                    times.Add(i * delay);
                break;

            case SteppedAnimation.StepMode.Manual:
                Debug.LogWarning("Manual 모드는 현재 지원되지 않습니다.");
                break;
        }

        return times;
    }

    private string GetSteppedClipFileName(string clipName)
    {
        return $"{clipName}_stepped_{fps}fps_{mode}.anim";
    }

    private void EnsureSaveFolderExists()
    {
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            string[] split = saveFolder.Split('/');
            string path = split[0];
            for (int i = 1; i < split.Length; i++)
            {
                string next = split[i];
                if (!AssetDatabase.IsValidFolder($"{path}/{next}"))
                {
                    AssetDatabase.CreateFolder(path, next);
                }
                path += $"/{next}";
            }
        }
    }

    private void LoadCache()
    {
        string fullPath = Path.Combine(saveFolder, CACHE_FILE_NAME);
        if (File.Exists(fullPath))
        {
            string json = File.ReadAllText(fullPath);
            steppedCacheData = JsonUtility.FromJson<SteppedCacheData>(json);
        }
        else
        {
            steppedCacheData = new SteppedCacheData();
        }
    }

    private void SaveCache()
    {
        string json = JsonUtility.ToJson(steppedCacheData, true);
        File.WriteAllText(Path.Combine(saveFolder, CACHE_FILE_NAME), json);
    }

    private bool TryGetCachedClip(AnimationClip clip, out string cachedPath)
    {
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip));

        foreach (var entry in steppedCacheData.stepped_cache)
        {
            if (entry.clipGUID == guid && Mathf.Approximately(entry.fps, fps) && entry.mode == mode.ToString())
            {
                cachedPath = entry.path;
                return true;
            }
        }

        cachedPath = null;
        return false;
    }

    private void AddCacheEntry(AnimationClip clip, string steppedPath)
    {
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip));
        var entry = new SteppedCacheEntry
        {
            clipGUID = guid,
            fps = fps,
            mode = mode.ToString(),
            path = steppedPath
        };
        steppedCacheData.stepped_cache.Add(entry);
    }

    public void ClearSteppedCache()
    {
        //steppedCache.Clear();
        //if (File.Exists(cachePath))
        //    File.Delete(cachePath);
    }

    #endregion
}

[System.Serializable]
public class SteppedCacheEntry
{
    public string clipGUID;
    public float fps;
    public string mode;
    public string path;
}

[System.Serializable]
public class SteppedCacheData
{
    public List<SteppedCacheEntry> stepped_cache = new();
}
#endif
