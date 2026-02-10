#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SteppedAnimationBaker
{
    // cache는 Resources에 두면 런타임 로드가 편함
    private const string DefaultCacheAssetPath = "Assets/Resources/SteppedClipCache.asset";
    private const string DefaultOutputFolder = "Assets/Resources/SteppedClips";

    [MenuItem("Tools/ProjectX/Animation/Bake Stepped Clips (Selected)")]
    public static void BakeSelectedClips()
    {
        var clips = AnimationClipCollector.CollectFromSelection(verboseLog: true);

        if (clips.Length == 0)
        {
            Debug.LogWarning("[SteppedBaker] No AnimationClips found. Check FBX import settings (Import Animation).");
            return;
        }
        // 프로젝트 설정에서 가져오고 싶으면 여길 바꾸면 됨
        int fps = Mathf.Max(1, GameConfig.RuntimeSettings.animationStepFps);
        var mode = GameConfig.RuntimeSettings.mode;

        var cache = LoadOrCreateCache(DefaultCacheAssetPath);
        EnsureFolder(DefaultOutputFolder);

        int baked = 0;
        foreach (var src in clips)
        {
            // key는 기본적으로 "원본 클립 이름" (Idle/Run/Attack...)
            var key = NormalizeKey(src.name);

            var stepped = CreateSteppedClipAsset(src, fps, mode, DefaultOutputFolder);
            if (stepped == null) continue;

            UpsertCache(cache, key, stepped);
            baked++;
        }

        EditorUtility.SetDirty(cache);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SteppedBaker] Done. baked={baked}/{clips.Length}, fps={fps}, mode={mode}");
    }

    // --------------------------------------------
    // Cache handling
    // --------------------------------------------

    private static SteppedClipCache LoadOrCreateCache(string assetPath)
    {
        var cache = AssetDatabase.LoadAssetAtPath<SteppedClipCache>(assetPath);
        if (cache != null) return cache;

        cache = ScriptableObject.CreateInstance<SteppedClipCache>();
        EnsureFolder(Path.GetDirectoryName(assetPath)?.Replace("\\", "/"));
        AssetDatabase.CreateAsset(cache, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SteppedBaker] Created cache: {assetPath}");
        return cache;
    }

    private static void UpsertCache(SteppedClipCache cache, string key, AnimationClip steppedClip)
    {
        // SteppedClipCache는 entries가 private라면,
        // 아래처럼 editor 전용 API를 하나 만들어서 쓰는 게 깔끔함.
        // (이번 답변에서는 cache에 editor 전용 메서드가 있다고 가정하지 않고 reflection 없이 처리하려고
        // SteppedClipCache에 아래 메서드를 추가하는 것을 권장한다.)

        // SteppedClipCache에 아래 메서드 추가 필요:
        // #if UNITY_EDITOR
        // public void EditorUpsert(string key, AnimationClip clip) { ... }
        // #endif

        cache.EditorUpsert(key, steppedClip);
    }

    // --------------------------------------------
    // Stepped clip creation
    // --------------------------------------------

    private static AnimationClip CreateSteppedClipAsset(
        AnimationClip source,
        int fps,
        ProPixelizer.Tools.SteppedAnimation.StepMode mode,
        string outputFolder)
    {
        if (source == null) return null;

        var steppedTemp = new AnimationClip
        {
            frameRate = fps
        };

        // Source -> Temp 복사 (타입 mismatch 에러 방지: CopySerialized는 같은 타입이어야 함)
        EditorUtility.CopySerialized(source, steppedTemp);

        var times = GetSampleTimes(source.length, fps, mode);
        if (times.Count == 0)
        {
            Debug.LogWarning($"[SteppedBaker] No sample times: {source.name}");
            return null;
        }

        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            var srcCurve = AnimationUtility.GetEditorCurve(source, binding);
            if (srcCurve == null || srcCurve.length == 0) continue;

            var keys = new Keyframe[times.Count];
            for (int i = 0; i < times.Count; i++)
            {
                float t = times[i];
                float v = srcCurve.Evaluate(t);
                keys[i] = new Keyframe(t, v);
            }

            var steppedCurve = new AnimationCurve(keys);

            // Constant tangent (stepped 느낌)
            for (int i = 0; i < steppedCurve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(steppedCurve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(steppedCurve, i, AnimationUtility.TangentMode.Constant);
            }

            AnimationUtility.SetEditorCurve(steppedTemp, binding, steppedCurve);
        }

        // Animation Events 복사
        AnimationUtility.SetAnimationEvents(steppedTemp, AnimationUtility.GetAnimationEvents(source));

        var safeName = SanitizeFileName(source.name);
        var assetPath = $"{outputFolder}/{safeName}_stepped_{fps}fps_{mode}.anim";

        // 이미 있으면 덮어쓰기
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(steppedTemp, existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.ImportAsset(assetPath);
            return existing;
        }

        AssetDatabase.CreateAsset(steppedTemp, assetPath);
        AssetDatabase.ImportAsset(assetPath);

        return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }

    private static List<float> GetSampleTimes(float length, int fps, ProPixelizer.Tools.SteppedAnimation.StepMode mode)
    {
        var times = new List<float>(256);
        if (fps <= 0) return times;

        switch (mode)
        {
            case ProPixelizer.Tools.SteppedAnimation.StepMode.FixedRate:
                {
                    int frameCount = Mathf.CeilToInt(length * fps);
                    for (int i = 0; i <= frameCount; i++)
                        times.Add(i / (float)fps);
                    break;
                }
            case ProPixelizer.Tools.SteppedAnimation.StepMode.FixedTimeDelay:
                {
                    float delay = 1f / fps;
                    int count = Mathf.CeilToInt(length / delay);
                    for (int i = 0; i <= count; i++)
                        times.Add(i * delay);
                    break;
                }
            case ProPixelizer.Tools.SteppedAnimation.StepMode.Manual:
                Debug.LogWarning("[SteppedBaker] Manual mode not supported.");
                break;
        }

        // clamp/sort/unique
        for (int i = 0; i < times.Count; i++)
            times[i] = Mathf.Clamp(times[i], 0f, length);

        times.Sort();

        const float eps = 0.000001f;
        for (int i = times.Count - 2; i >= 0; i--)
            if (Mathf.Abs(times[i + 1] - times[i]) <= eps)
                times.RemoveAt(i + 1);

        if (times.Count == 0 || Mathf.Abs(times[^1] - length) > eps)
            times.Add(length);

        return times;
    }

    // --------------------------------------------
    // Utilities
    // --------------------------------------------

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return;

        folder = folder.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(folder)) return;

        var parts = folder.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private static string NormalizeKey(string name)
    {
        // 필요하면 여기서 접두사/접미사 정리 룰 넣으면 됨
        // 예: "Idle@mixamo.com" -> "Idle"
        return name.Trim();
    }

}
public static class SteppedAnimationUtility
{
    public static AnimationClip CreateSteppedClip(AnimationClip source, int fps = 12)
    {
        if (source == null) return null;
        if (fps <= 0) fps = 12;

        // 새 stepped clip 생성 (CopySerialized 금지)
        var stepped = new AnimationClip
        {
            frameRate = fps,
            name = $"{source.name}_stepped_{fps}fps"
        };

        // 샘플 타임 생성
        var times = GetSampleTimes(source.length, fps);

        // Curve 복사 + 스텝화
        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            var srcCurve = AnimationUtility.GetEditorCurve(source, binding);
            if (srcCurve == null || srcCurve.length == 0) continue;

            var keys = new Keyframe[times.Count];
            for (int i = 0; i < times.Count; i++)
            {
                float t = times[i];
                float v = srcCurve.Evaluate(t);
                keys[i] = new Keyframe(t, v);
            }

            var steppedCurve = new AnimationCurve(keys);

            // 인덱스 방식 Constant tangent (버전 호환)
            for (int i = 0; i < steppedCurve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(steppedCurve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(steppedCurve, i, AnimationUtility.TangentMode.Constant);
            }

            AnimationUtility.SetEditorCurve(stepped, binding, steppedCurve);
        }

        // (선택) Animation Events 복사??
        AnimationUtility.SetAnimationEvents(stepped, AnimationUtility.GetAnimationEvents(source));

        // 저장
        var path = GetOutputPath(source, fps);
        EnsureFolder(Path.GetDirectoryName(path));

        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null)
        {
            // 기존 에셋이 있으면 덮어쓰기(참조 안정)
            EditorUtility.CopySerialized(stepped, existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.ImportAsset(path);
            return existing;
        }

        AssetDatabase.CreateAsset(stepped, path);
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
    }

    private static List<float> GetSampleTimes(float length, int fps)
    {
        var times = new List<float>(256);
        int frameCount = Mathf.CeilToInt(length * fps);

        for (int i = 0; i <= frameCount; i++)
            times.Add(i / (float)fps);

        const float eps = 0.000001f;

        // clamp + dedupe + ensure length
        for (int i = 0; i < times.Count; i++)
            times[i] = Mathf.Clamp(times[i], 0f, length);

        times.Sort();

        for (int i = times.Count - 2; i >= 0; i--)
            if (Mathf.Abs(times[i + 1] - times[i]) <= eps)
                times.RemoveAt(i + 1);

        if (times.Count == 0 || Mathf.Abs(times[^1] - length) > eps)
            times.Add(length);

        return times;
    }

    private static string GetOutputPath(AnimationClip source, int fps)
    {
        const string root = "Assets/Resources/Stepped";
        var safe = SanitizeFileName(source.name);
        return $"{root}/{safe}_stepped_{fps}fps.anim";
    }

    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath)) return;
        folderPath = folderPath.Replace("\\", "/");

        if (AssetDatabase.IsValidFolder(folderPath)) return;

        var parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
public static class AnimationClipCollector
{
    public static AnimationClip[] CollectFromSelection(bool verboseLog = true)
    {
        var clips = new List<AnimationClip>();

        var selection = Selection.objects;
        if (selection == null || selection.Length == 0)
        {
            if (verboseLog) Debug.LogWarning("[Collector] Selection is empty.");
            return Array.Empty<AnimationClip>();
        }

        var paths = new HashSet<string>();
        foreach (var obj in selection)
        {
            if (obj == null) continue;

            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
            {
                if (verboseLog)
                    Debug.LogWarning($"[Collector] No asset path: {obj.name} ({obj.GetType().Name}). Select FBX in Project window.");
                continue;
            }

            paths.Add(path);
        }

        foreach (var path in paths)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            if (verboseLog)
            {
                Debug.Log($"[Collector] Path: {path}");
                Debug.Log($"[Collector] SubAssets count: {all.Length}");
                // 어떤 타입들이 들어있는지 확인
                foreach (var a in all)
                    Debug.Log($"  - {a.GetType().Name} : {a.name} (hideFlags={a.hideFlags})");
            }

            foreach (var a in all)
            {
                if (a is not AnimationClip c) continue;

                // preview 제외 (mixamo에서 자주 나옴)
                if (c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                    continue;

                // 빈/에디터용 클립 제외(선택)
                if (c.name.Equals("Take 001", StringComparison.OrdinalIgnoreCase) == false &&
                    c.length <= 0f)
                {
                    // 길이 0인 건 보통 필요 없음. (원하면 이 조건도 제거 가능)
                    // continue;
                }

                clips.Add(c);
            }

            // 그래도 0이면 Import 설정 문제일 확률이 큼 → 진단 로그
            if (verboseLog && clips.Count == 0)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null)
                {
                    Debug.LogWarning(
                        $"[Collector] No AnimationClips extracted from: {path}\n" +
                        $"- importAnimation: {importer.importAnimation}\n" +
                        $"- animationType: {importer.animationType}\n" +
                        $"- clipAnimations: {(importer.clipAnimations != null ? importer.clipAnimations.Length : 0)}\n" +
                        $"- defaultClipAnimations: {(importer.defaultClipAnimations != null ? importer.defaultClipAnimations.Length : 0)}\n" +
                        $"FBX Inspector > Animation 탭에서 'Import Animation' 체크 후 Apply 필요",
                        importer);
                }
            }
        }

        return clips.Distinct().ToArray();
    }
}
#endif
