using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

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
    

    // 1. 스크린 샷 찍기 (UI를 다 띄운 것도 보여줌)
    public static Texture2D CaptureScreenshot()
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
}
