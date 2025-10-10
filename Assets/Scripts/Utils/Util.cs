using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Util
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


    // IEnumerable 중 랜덤으로 하나 뽑기
    public static T RandomPick<T>(this IEnumerable<T> source)
    {
        if (source == null)
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
}
