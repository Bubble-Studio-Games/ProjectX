using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

///  ProjectX에서만 사용되는 Util 함수
public static partial class Util
{
    /// <summary>
    /// 특정 GameEntity 리스트에서, TAction을 가진 유닛만 (유닛, 액션) 튜플로 필터링.
    /// </summary>
    public static IEnumerable<(TClass unit, TAction action)>
        FilterGameEntityHasAction<TAction, TClass>(IEnumerable<TClass> units)
        where TAction : BaseAction
        where TClass : GameEntity
    {
        return units
            .Select(u => (unit: u, action: u.GetAction<TAction>()))
            .Where(pair => pair.action != null);
    }


    public static (int MinX, int MaxX, int MinZ, int MaxZ, int MinFloor, int MaxFloor)
    GetRangeMinMaxFromOffsets(IEnumerable<GridPosition> m_RangeOffset)
    {
        if (m_RangeOffset == null || m_RangeOffset.Count() == 0)
            return (0, 0, 0, 0, 0, 0);

        int minX = 0, maxX = 0;
        int minZ = 0, maxZ = 0;
        int minF = 0, maxF = 0;

        foreach (var o in m_RangeOffset)
        {
            minX = Mathf.Min(minX, o.x);
            maxX = Mathf.Max(maxX, o.x);
            minZ = Mathf.Min(minZ, o.z);
            maxZ = Mathf.Max(maxZ, o.z);
            minF = Mathf.Min(minF, o.floor);
            maxF = Mathf.Max(maxF, o.floor);
        }

        return (minX, maxX, minZ, maxZ, minF, maxF);
    }

    #region Caculate


    // origin -> target의 방향
    public static E_Dir GetDirGridPosition(GridPosition origin, GridPosition target)
    {
        int dx = target.x - origin.x;
        int dz = target.z - origin.z;

        if (dx == 0 && dz == 0)
            return E_Dir.North; // 자기 자신 → 기본값 반환

        float angle = Mathf.Atan2(dz, dx) * Mathf.Rad2Deg;
        angle = (angle + 360f) % 360f; // 0~360도 정규화

        if (angle >= 337.5f || angle < 22.5f)
            return E_Dir.East;
        else if (angle >= 22.5f && angle < 67.5f)
            return E_Dir.NorthEast;
        else if (angle >= 67.5f && angle < 112.5f)
            return E_Dir.North;
        else if (angle >= 112.5f && angle < 157.5f)
            return E_Dir.NorthWest;
        else if (angle >= 157.5f && angle < 202.5f)
            return E_Dir.West;
        else if (angle >= 202.5f && angle < 247.5f)
            return E_Dir.SouthWest;
        else if (angle >= 247.5f && angle < 292.5f)
            return E_Dir.South;
        else // angle >= 292.5f && angle < 337.5f
            return E_Dir.SouthEast;
    }

    public static ICollection<GridPosition> ToGridPosition
        (IEnumerable<GridPosition> offset, GridPosition origin, E_Dir dir)
        => offset.Select(x => ToGridPosition(x, origin, dir)).ToList();

    /// <summary>
    /// origin 기준으로 dir 방향으로 offset 만큼 거리를 구한다.
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="origin"></param>
    /// <param name="dir"></param>
    /// <returns></returns>
    public static GridPosition ToGridPosition(GridPosition offset, GridPosition origin, E_Dir dir)
    {
        int x = offset.x;
        int z = offset.z;
        int rotatedX = 0;
        int rotatedZ = 0;

        switch (dir)
        {
            case E_Dir.North:
                rotatedX = x;
                rotatedZ = z;
                break;
            case E_Dir.East:
                rotatedX = z;
                rotatedZ = -x;
                break;
            case E_Dir.South:
                rotatedX = -x;
                rotatedZ = -z;
                break;
            case E_Dir.West:
                rotatedX = -z;
                rotatedZ = x;
                break;
            case E_Dir.NorthEast:
                rotatedX = Mathf.RoundToInt(x * 0.7071f + z * 0.7071f);
                rotatedZ = Mathf.RoundToInt(-x * 0.7071f + z * 0.7071f);
                break;
            case E_Dir.SouthEast:
                rotatedX = Mathf.RoundToInt(-x * 0.7071f + z * 0.7071f);
                rotatedZ = Mathf.RoundToInt(-x * 0.7071f - z * 0.7071f);
                break;
            case E_Dir.SouthWest:
                rotatedX = Mathf.RoundToInt(-x * 0.7071f - z * 0.7071f);
                rotatedZ = Mathf.RoundToInt(x * 0.7071f - z * 0.7071f);
                break;
            case E_Dir.NorthWest:
                rotatedX = Mathf.RoundToInt(x * 0.7071f - z * 0.7071f);
                rotatedZ = Mathf.RoundToInt(x * 0.7071f + z * 0.7071f);
                break;
        }

        return origin + new GridPosition(rotatedX, rotatedZ, offset.floor);
    }

    public static float GetObstacleMaxHeight(
    IPathfinder pathfinder,
    IGridQuery grid,
    GridPosition a,
    GridPosition b)
    {
        if (pathfinder == null || grid == null)
            return 0f;

        var path = pathfinder.FindPath(a, b, out _, E_GridCheckType.GameEntity);
        if (path == null || path.Count < 3)
            return 0f;

        float max = 0f;

        // 양 끝 제외 (RemoveAt 대신 for가 GC/안전 측면에서 더 좋음)
        for (int i = 1; i < path.Count - 1; i++)
        {
            var obj = grid.GetCellEntity(path[i]); // ← 여기 중요!
            if (obj == null || obj.m_HitCollider == null) continue;

            max = Mathf.Max(max, obj.m_HitCollider.bounds.max.y);
        }

        return max;
    }

    public static float GetObstacleMaxHeight(
    IPathfinder pathfinder,
    IGridQuery grid,
    Vector3 a,
    Vector3 b)
    {
        return GetObstacleMaxHeight(pathfinder, grid, grid.GetGridPosition(a), grid.GetGridPosition(b));
    }

    #endregion

    #region Path

    /// <summary>
    /// 탐색된 경로들을 거리 순으로 정렬
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    public static IEnumerable<GridPosition> GetGridPositionByOrderPathLength(IPathfinder pathfinder, GridPosition startPos, IEnumerable<GridPosition> list)
    {
        // 가까운 위치 순으로 정렬
        return list
            .OrderBy(pos =>
            {
                int length = pathfinder.GetPathLength(startPos, pos);
                return length == 0 ? int.MaxValue : length; // 경로 없으면 맨 뒤로
            });
    }


    /// <summary>
    /// 리스트 중에서 가장 가까운 그리드 반환
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    public static GridPosition GetGridPositionFindNearest(IPathfinder pathfinder, GridPosition startPos, IEnumerable<GridPosition> list)
    {
        return GetGridPositionByOrderPathLength(pathfinder, startPos, list).First();
    }

    #endregion

    #region Attack

    public static class ProjectileUtil
    {
        // 프로젝트마다 값 다를 수 있으니 Projectile에 있는 반지름을 기본으로 쓰는 걸 추천
        public static bool TryHitSweep(
            Vector3 from,
            Vector3 to,
            float radius,
            int layerMask,
            Transform ignoreRoot,
            Collider ignoreCollider,
            out RaycastHit hit)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist <= 0.0001f)
            {
                hit = default;
                return false;
            }

            dir /= dist;

            // 단발 cast는 "무시"가 어렵기 때문에, 여러 개를 받아서 거르는 방식이 안전함
            // (성능: 투사체 10~50개 수준이면 충분히 감당됨)
            var hits = Physics.SphereCastAll(from, radius, dir, dist, layerMask, QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0)
            {
                hit = default;
                return false;
            }

            // 거리순 정렬(가장 가까운 유효 히트를 선택)
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];

                // 1) 자기 자신의 콜라이더 무시
                if (ignoreCollider != null && h.collider == ignoreCollider)
                    continue;

                // 2) 발사자 루트(및 자식) 무시
                if (ignoreRoot != null && h.collider != null && h.collider.transform.IsChildOf(ignoreRoot))
                    continue;

                hit = h;
                return true;
            }

            hit = default;
            return false;
        }


        /// <summary>
        /// 누적 시간을 stepInterval(=1/stepFps) 단위로 잘라서,
        /// 이번 틱에서 실제로 "몇 step"을 처리할지 반환.
        /// </summary>
        public static int ConsumeSteps(ref float accum, float fixedDeltaTime, float stepFps)
        {
            if (stepFps <= 0f) stepFps = 1f;

            float interval = 1f / stepFps;
            accum += fixedDeltaTime;

            if (accum < interval) return 0;

            int steps = Mathf.FloorToInt(accum / interval);
            accum -= steps * interval;
            return steps;
        }

        /// <summary>
        /// Vector3를 snapUnit 간격으로 스냅(반올림).
        /// </summary>
        public static Vector3 Snap(Vector3 v, float snapUnit)
        {
            if (snapUnit <= 0f) return v;

            v.x = Mathf.Round(v.x / snapUnit) * snapUnit;
            v.y = Mathf.Round(v.y / snapUnit) * snapUnit;
            v.z = Mathf.Round(v.z / snapUnit) * snapUnit;
            return v;
        }

        // 각도 스냅(회전 툭툭 느낌 강화)
        public static Quaternion SnapRotation(Quaternion q, float snapDeg)
        {
            if (snapDeg <= 0f) return q;

            Vector3 e = q.eulerAngles;
            e.x = Mathf.Round(e.x / snapDeg) * snapDeg;
            e.y = Mathf.Round(e.y / snapDeg) * snapDeg;
            e.z = Mathf.Round(e.z / snapDeg) * snapDeg;
            return Quaternion.Euler(e);
        }
    }

    #endregion

    /// <summary>
    /// IEnumerble<GridPosition>을 받아서 디버그용 표시를 해준다.
    /// SceneView와 GameView에서 모두 확인 가능.
    /// </summary>
    public static void DrawDebugPositions(IEnumerable<GridPosition> positions,
                                          float duration = 5f,
                                          float size = 0.3f)
    {
        foreach (var pos in positions)
        {
            Vector3 wp = Managers.SceneServices.Grid.GetWorldPosition(pos);

            // Sphere-like marker (actually cube for simplicity)
            DebugDrawSphere(wp, size, duration);

            // Optional: vertical indicator line
            Debug.DrawLine(wp, wp + Vector3.up * 1.5f, Color.red, duration);
        }
    }

    private static void DebugDrawSphere(Vector3 position, float radius, float duration)
    {
        // 6 lines to fake a sphere
        Debug.DrawLine(position + Vector3.up * radius, position - Vector3.up * radius, Color.red, duration);
        Debug.DrawLine(position + Vector3.right * radius, position - Vector3.right * radius, Color.red, duration);
        Debug.DrawLine(position + Vector3.forward * radius, position - Vector3.forward * radius, Color.red, duration);
    }
}