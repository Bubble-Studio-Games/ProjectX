using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public static partial class Util
{
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
}
