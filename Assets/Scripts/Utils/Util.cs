using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

public static partial class Util
{
    public static bool TryGetComponentInChildren<T>(GameObject go, out T result) where T : Component
    {
        result = go.GetComponentInChildren<T>();
        return result != null;
    }

    public static T GetOrAddComponent<T>(GameObject go) where T : UnityEngine.Component
    {
        T component = go.GetComponent<T>();
		if (component == null)
            component = go.AddComponent<T>();
        return component;
	}

    public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
    {
        Transform transform = FindChild<Transform>(go, name, recursive);
        if (transform == null)
            return null;
        
        return transform.gameObject;
    }

    public static T FindChild<T>(GameObject go, string name = null, bool recursive = false) where T : UnityEngine.Object
    {
        if (go == null)
            return null;

        if (recursive == false)
        {
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform transform = go.transform.GetChild(i);
                if (string.IsNullOrEmpty(name) || transform.name == name)
                {
                    T component = transform.GetComponent<T>();
                    if (component != null)
                        return component;
                }
            }
		}
        else
        {
            foreach (T component in go.GetComponentsInChildren<T>())
            {
                if (string.IsNullOrEmpty(name) || component.name == name)
                    return component;
            }
        }

        return null;
    }

    public static Color HexToColor(string hex, byte alpha = 255)
    {
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

        return new Color32(r, g, b, alpha);
    }

    public static bool TryFirstOrDefault<T>(IEnumerable<T> source, out T value)
    {
        value = default(T);
        using (var iterator = source.GetEnumerator())
        {
            if (iterator.MoveNext())
            {
                value = iterator.Current;
                return true;
            }
            return false;
        }

    }

    public static GameObject FindOrCreateGameObject(string name) 
    {
        GameObject component = GameObject.Find(name);
        if (component == null)
            component = new GameObject { name = name };
        return component;

    }

    #region Random Pick

    // System.Random 인스턴스를 static으로 선언하여 스크립트 전체에서 공유하고 한 번만 초기화합니다.
    private static readonly System.Random Rng = new System.Random();

    /// <summary>
    /// 주어진 컬렉션에서 중복 없이 임의의 N개 요소를 추출합니다 (비복원 추출).
    /// </summary>
    /// <param name="source">원본 컬렉션</param>
    /// <param name="count">추출할 요소의 개수 (원본 크기보다 클 수 없습니다)</param>
    public static IEnumerable<T> GetRandomElements<T>(IEnumerable<T> source, int count)
    {
        // 1. 임의의 정렬 순서를 생성합니다.
        // OrderBy(item => Rng.Next())는 각 요소에 임의의 정수(Rng.Next())를 할당하고,
        // 이 임의의 정수를 기준으로 요소를 섞어줍니다.
        // Rng는 static 필드로 선언하여 한 번만 초기화하는 것이 좋습니다.

        // 2. Take(count)를 사용하여 섞인 리스트의 앞에서 count만큼 요소를 가져옵니다.

        // 3. ToList()로 최종 리스트를 반환합니다.

        return source
            .OrderBy(item => Rng.Next())
            .Take(count);
    }

    // IEnumerable 중 랜덤으로 하나 뽑기
    public static int RandomPickIndex<T>(this IEnumerable<T> source)
    {
        if (source == null)
            throw new System.ArgumentNullException(nameof(source));

        return Random.Range(0, source.Count()); // 0 이상, itemPrefabs.Length 미만의 정수 반환
    }

    // IEnumerable 중 랜덤으로 하나 뽑기
    public static T RandomPick<T>(this IEnumerable<T> source)
    {
        if (source == null || source.Count() == 0)
            throw new System.ArgumentNullException(nameof(source));

        var list = source as IList<T> ?? source.ToList(); // 캐싱
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    // IEnumerable 중 랜덤으로 하나 뽑고 제외하기
    public static T RandomPickWithExcept<T>(this IEnumerable<T> source, out IEnumerable<T> rest)
    {
        if (source == null) throw new System.ArgumentNullException(nameof(source));

        var list = source as IList<T> ?? source.ToList();
        int index = UnityEngine.Random.Range(0, list.Count);
        T pick = list[index];

        rest = list.Where((_, i) => i != index); // index만 제외한 새로운 시퀀스
        return pick;
    }

    #endregion


    /// <summary>
    /// min ~ max 범위에서 지정한 단위(step)만큼 간격을 두고 랜덤 값을 반환합니다.
    /// 예: (1.2, 1.5, 0.1) → 1.2, 1.3, 1.4, 1.5 중 하나
    /// 예: (20, 50, 10) → 20, 30, 40, 50 중 하나
    /// </summary>
    public static float GetRandomValue(float min, float max, float step)
    {
        if (step <= 0f)
        {
            Debug.LogWarning("Step must be greater than 0.");
            return min;
        }

        int stepCount = Mathf.FloorToInt((max - min) / step);
        if (stepCount < 0)
        {
            Debug.LogWarning("Invalid range: max must be greater than min.");
            return min;
        }

        int randomIndex = UnityEngine.Random.Range(0, stepCount + 1);
        float result = min + (randomIndex * step);

        // 부동소수점 오차 방지용 (예: 1.299999 → 1.3)
        result = (float)System.Math.Round(result, GetDecimalPlaces(step));

        return result;
    }

    /// <summary>
    /// step 값의 소수점 자릿수를 계산 (0.1 → 1, 0.01 → 2)
    /// </summary>
    private static int GetDecimalPlaces(float value)
    {
        int places = 0;
        while (value * Mathf.Pow(10, places) % 1 != 0)
        {
            places++;
            if (places > 5) break; // 안전장치
        }
        return places;
    }


    #region File Serach


    /// <summary>
    /// target 객체 내에서 특정 타입 T (혹은 하위 타입)를 가진 모든 필드를 재귀적으로 검색.
    /// 부모 클래스까지 탐색하며, 순환 참조나 동일 인스턴스 중복 탐색을 방지합니다.
    /// </summary>
    public static List<(FieldInfo field, object owner, T value)> FindAllFieldsOfType<T>(object target)
    {
        List<(FieldInfo field, object owner, T value)> results = new();
        HashSet<object> visited = new();
        ExploreObject(target, typeof(T), results, visited);
        return results;
    }

    private static void ExploreObject<T>(
        object obj,
        Type targetType,
        List<(FieldInfo field, object owner, T value)> results,
        HashSet<object> visited)
    {
        if (obj == null)
            return;

        // 순환 참조 방지
        if (visited.Contains(obj))
            return;

        visited.Add(obj);

        Type type = obj.GetType();

        while (type != null && type != typeof(object))
        {
            var fields = type.GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

            foreach (var field in fields)
            {
                object value = null;
                try { value = field.GetValue(obj); } catch { continue; }

                // 🔹 Unity 특유의 “null처럼 보이지만 실제 존재하는 오브젝트” 체크
                if (value == null)
                    continue;

                Type fieldType = field.FieldType;

                // 🔹 찾는 타입이면 바로 추가
                if (targetType.IsAssignableFrom(fieldType))
                {
                    if (value is T tValue)
                        results.Add((field, obj, tValue));
                    continue;
                }

                // 🔹 배열 / 리스트 내부 재귀 탐색
                if (value is IEnumerable enumerable && !(value is string))
                {
                    // Transform 은 자식 Transform을 열거하므로 제외
                    if (value is Transform)
                        continue;

                    foreach (var element in enumerable)
                        ExploreObject(element, targetType, results, visited);

                    continue;
                }

                // 🔹 순수 C# 직렬화 클래스 내부 탐색
                if (!fieldType.IsPrimitive && !fieldType.IsEnum && !fieldType.IsGenericTypeDefinition)
                {
                    ExploreObject(value, targetType, results, visited);
                }
            }

            type = type.BaseType;
        }
    }

    public static void ReplaceFieldValue(object owner, FieldInfo field, object newValue)
    {
        if (owner == null || field == null)
            return;

        try
        {
            field.SetValue(owner, newValue);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ {owner.GetType().Name}.{field.Name} 교체 실패: {e.Message}");
        }
    }

    #endregion

    #region Color
    /// <summary>
    /// 머티리얼의 메인 컬러를 HSV 기준으로 조정합니다.
    /// </summary>
    /// <param name="mat">대상 머티리얼</param>
    /// <param name="type">0=Hue, 1=Saturation, 2=Value</param>
    /// <param name="addValue">변화량 (정수, +면 증가, -면 감소)</param>
    /// <returns>조정된 머티리얼</returns>
    public static Material AdjustMaterialHSV(Material mat, int type, int addValue)
    {
        if (mat == null)
        {
            Debug.LogWarning("[Util.AdjustMaterialHSV] Material이 null입니다.");
            return null;
        }

        // RGB → HSV
        Color currentColor = mat.color;
        Color.RGBToHSV(currentColor, out float h, out float s, out float v);

        float delta = addValue / 100f;

        switch (type)
        {
            case 0: // Hue (색상)
                h = Mathf.Repeat(h + delta, 1f);
                break;

            case 1: // Saturation (채도)
                s = Mathf.Clamp01(s + delta);
                break;

            case 2: // Value (밝기)
                v = Mathf.Clamp01(v + delta);
                break;

            default:
                Debug.LogWarning($"[Util.AdjustMaterialHSV] 잘못된 type 값: {type} (0=H, 1=S, 2=V)");
                break;
        }

        // HSV → RGB 후 머티리얼에 적용
        mat.color = Color.HSVToRGB(h, s, v);

        return mat;
    }


    #endregion
    #region Screen Shot

    // 1. 스크린 샷 찍기 (UI를 다 띄운 것도 보여줌)
    public static Texture2D CaptureScreenshot()
    {
        int width = Screen.width;
        int height = Screen.height;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        return tex;
    }

    public static Texture2D CaptureCamera()
    {
        int width = Screen.width;
        int height = Screen.height;
        var cam = Camera.main;

        // 1. RenderTexture 생성
        RenderTexture rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;

        // 2. 카메라 렌더링
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        cam.Render();

        // 3. 픽셀 읽기
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // 4. 리소스 정리
        cam.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.Destroy(rt);

        return tex;
    }

    #endregion
}
