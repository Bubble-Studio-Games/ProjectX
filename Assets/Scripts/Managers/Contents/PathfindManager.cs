using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;


public readonly struct IgnoreCellType
{
    public readonly E_TerrainCellType Terrain;
    public readonly E_EntityCellType[] Entities;

    public IgnoreCellType(
        E_TerrainCellType terrain = E_TerrainCellType.Walkable,
        params E_EntityCellType[] entities)
    {
        Terrain = terrain;
        Entities = entities ?? Array.Empty<E_EntityCellType>();
    }

    // 선택: 명시적으로 기본값 쓰고 싶을 때
    public static IgnoreCellType Default => new IgnoreCellType(E_TerrainCellType.Walkable);
}

public class PathfindManager : IManager
{
    private static readonly List<GridPosition> s_EmptyGridPositionList = new(0);


    #region Field

    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    private int width => GameConfig.Grid.Width;
    private int height => GameConfig.Grid.Height;
    private float cellSize => GameConfig.Grid.CellSize;
    private int floorAmount => GameConfig.Grid.FloorAmount;
    private List<GridSystem<PathNode>> gridSystemList;
    private List<PathfindingLink> pathfindingLinkList = new();

    #endregion

    #region Init

    public void Clear()
    {
        gridSystemList.Clear();
        pathfindingLinkList.Clear();
    }

    public void Init()
    {
        gridSystemList = new List<GridSystem<PathNode>>();

        for (int floor = 0; floor < floorAmount; floor++)
        {
            GridSystem<PathNode> gridSystem = new GridSystem<PathNode>(width, height, cellSize, floor, 
                FLOOR_HEIGHT,
                (GridSystem<PathNode> g, GridPosition gridPosition) => new PathNode(gridPosition));

            gridSystemList.Add(gridSystem);
        }
    }

    public void PathFindingLinkRegister(PathfindingLink link) => pathfindingLinkList.Add(link);

    #endregion

    #region A* 계산

    private int CalculateDistance(GridPosition gridPositionA, GridPosition gridPositionB)
    {
        GridPosition gridPositionDistance = gridPositionA - gridPositionB;
        int xDistance = Mathf.Abs(gridPositionDistance.x);
        int zDistance = Mathf.Abs(gridPositionDistance.z);
        int remaining = Mathf.Abs(xDistance - zDistance);
        return MOVE_DIAGONAL_COST * Mathf.Min(xDistance, zDistance) + MOVE_STRAIGHT_COST * remaining;
    }

    private PathNode GetLowestFCostPathNode(List<PathNode> pathNodeList)
    {
        PathNode lowestFCostPathNode = pathNodeList[0];
        for (int i = 0; i < pathNodeList.Count; i++)
        {
            if (pathNodeList[i].GetFCost() < lowestFCostPathNode.GetFCost())
            {
                lowestFCostPathNode = pathNodeList[i];
            }
        }
        return lowestFCostPathNode;
    }

    private GridSystem<PathNode> GetGridSystem(int floor)
    {
        return gridSystemList[floor];
    }

    private PathNode GetNode(int x, int z, int floor)
    {
        return GetGridSystem(floor).GetGridObject(new GridPosition(x, z, floor));
    }

    private List<PathNode> GetNeighbourList(PathNode currentNode)
    {
        List<PathNode> neighbourList = new List<PathNode>();

        GridPosition gridPosition = currentNode.GetGridPosition();

        if (gridPosition.x - 1 >= 0)
        {
            // Left
            neighbourList.Add(GetNode(gridPosition.x - 1, gridPosition.z + 0, gridPosition.floor));
            if (gridPosition.z - 1 >= 0)
            {
                // Left Down
                neighbourList.Add(GetNode(gridPosition.x - 1, gridPosition.z - 1, gridPosition.floor));
            }

            if (gridPosition.z + 1 < height)
            {
                // Left Up
                neighbourList.Add(GetNode(gridPosition.x - 1, gridPosition.z + 1, gridPosition.floor));
            }
        }

        if (gridPosition.x + 1 < width)
        {
            // Right
            neighbourList.Add(GetNode(gridPosition.x + 1, gridPosition.z + 0, gridPosition.floor));
            if (gridPosition.z - 1 >= 0)
            {
                // Right Down
                neighbourList.Add(GetNode(gridPosition.x + 1, gridPosition.z - 1, gridPosition.floor));
            }
            if (gridPosition.z + 1 < height)
            {
                // Right Up
                neighbourList.Add(GetNode(gridPosition.x + 1, gridPosition.z + 1, gridPosition.floor));
            }
        }

        if (gridPosition.z - 1 >= 0)
        {
            // Down
            neighbourList.Add(GetNode(gridPosition.x + 0, gridPosition.z - 1, gridPosition.floor));
        }
        if (gridPosition.z + 1 < height)
        {
            // Up
            neighbourList.Add(GetNode(gridPosition.x + 0, gridPosition.z + 1, gridPosition.floor));
        }

        List<PathNode> totalNeighbourList = new List<PathNode>();
        totalNeighbourList.AddRange(neighbourList);

        List<GridPosition> pathfindingLinkGridPositionList = GetPathfindingLinkConnectedGridPositionList(gridPosition);

        foreach (GridPosition pathfindingLinkGridPosition in pathfindingLinkGridPositionList)
        {
            totalNeighbourList.Add(
                GetNode(
                    pathfindingLinkGridPosition.x, 
                    pathfindingLinkGridPosition.z, 
                    pathfindingLinkGridPosition.floor
                )
            );
        }

        return totalNeighbourList;
    }

    private List<GridPosition> GetPathfindingLinkConnectedGridPositionList(GridPosition gridPosition)
    {
        List<GridPosition> gridPositionList = new List<GridPosition>();

        foreach (PathfindingLink pathfindingLink in pathfindingLinkList)
        {
            if (pathfindingLink.gridPositionA == gridPosition)
            {
                gridPositionList.Add(pathfindingLink.gridPositionB);
            }
            if (pathfindingLink.gridPositionB == gridPosition)
            {
                gridPositionList.Add(pathfindingLink.gridPositionA);
            }
        }

        return gridPositionList;
    }

    private List<GridPosition> CalculatePath(PathNode endNode)
    {
        List<PathNode> pathNodeList = new List<PathNode>();
        pathNodeList.Add(endNode);
        PathNode currentNode = endNode;
        while (currentNode.GetCameFromPathNode() != null)
        {
            pathNodeList.Add(currentNode.GetCameFromPathNode());
            currentNode = currentNode.GetCameFromPathNode();
        }

        pathNodeList.Reverse();

        List<GridPosition> gridPositionList = new List<GridPosition>();
        foreach (PathNode pathNode in pathNodeList)
        {
            gridPositionList.Add(pathNode.GetGridPosition());
        }

        return gridPositionList;
    }

    #endregion

    #region FindPath

    public List<GridPosition> FindPath
        (GridPosition startGridPosition, GridPosition endGridPosition,
        out int pathLength,
        IgnoreCellType ignore = default)
    {

        // ✅ default(struct)로 들어오면 Entities가 null일 수 있으니 보정
        var ignoreTerrainCellType = ignore.Terrain;
        var ignoreEntityCellType = ignore.Entities ?? Array.Empty<E_EntityCellType>();

        List<PathNode> openList = new List<PathNode>();
        List<PathNode> closedList = new List<PathNode>();

        PathNode startNode = GetGridSystem(startGridPosition.floor).GetGridObject(startGridPosition);
        PathNode endNode = GetGridSystem(endGridPosition.floor).GetGridObject(endGridPosition);
        openList.Add(startNode);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                for (int floor = 0; floor < floorAmount; floor++)
                {
                    GridPosition gridPosition = new GridPosition(x, z, floor);
                    PathNode pathNode = GetGridSystem(floor).GetGridObject(gridPosition);

                    pathNode.SetGCost(int.MaxValue);
                    pathNode.SetHCost(0);
                    pathNode.CalculateFCost();
                    pathNode.ResetCameFromPathNode();
                }
            }
        }

        startNode.SetGCost(0);
        startNode.SetHCost(CalculateDistance(startGridPosition, endGridPosition));
        startNode.CalculateFCost();

        while (openList.Count > 0)
        {
            PathNode currentNode = GetLowestFCostPathNode(openList);

            if (currentNode == endNode)
            {
                // Reached final node
                pathLength = endNode.GetFCost();
                return CalculatePath(endNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (PathNode neighbourNode in GetNeighbourList(currentNode))
            {
                if (closedList.Contains(neighbourNode))
                {
                    continue;
                }

                if (!Managers.Grid.CanMoveTo(neighbourNode.GetGridPosition(), startGridPosition, ignoreTerrainCellType, ignoreEntityCellType))
                {
                    closedList.Add(neighbourNode);
                    continue;
                }

                int tentativeGCost =
                    currentNode.GetGCost() + CalculateDistance(currentNode.GetGridPosition(), neighbourNode.GetGridPosition());

                if (tentativeGCost < neighbourNode.GetGCost())
                {
                    neighbourNode.SetCameFromPathNode(currentNode);
                    neighbourNode.SetGCost(tentativeGCost);
                    neighbourNode.SetHCost(CalculateDistance(neighbourNode.GetGridPosition(), endGridPosition));
                    neighbourNode.CalculateFCost();

                    if (!openList.Contains(neighbourNode))
                    {
                        openList.Add(neighbourNode);
                    }
                }
            }
        }

        // No path found
        pathLength = 0;
        return null;
    }

    public bool HasPath(
    GridPosition startGridPosition,
    GridPosition endGridPosition,
        IgnoreCellType ignore = default)
    {
        return FindPath(startGridPosition, endGridPosition, out int pathLength, ignore) != null;
    }

    public int GetPathLength(
        GridPosition startGridPosition, 
        GridPosition endGridPosition,
        IgnoreCellType ignore = default)
    {
        FindPath(startGridPosition, endGridPosition, out int pathLength, ignore);
        return pathLength;
    }

    public List<GridPosition> FindNearestCandidatePath(
        GridPosition start,
        IEnumerable<GridPosition> gridPositions,
        bool allowApproachWhenUnreachable = false)
    {
        if (gridPositions == null)
            return s_EmptyGridPositionList;

        if (gridPositions is not ICollection<GridPosition> cached)
            cached = gridPositions as IList<GridPosition> ?? gridPositions.ToList();

        if (cached.Count == 0)
            return s_EmptyGridPositionList;

        // 1) 가장 빠르게 도달 가능한 후보 찾기
        int bestLength = int.MaxValue;
        List<GridPosition> bestPath = null;

        foreach (var tgt in cached)
        {
            var path = FindPath(start, tgt, out int length);
            if (path == null || path.Count == 0) continue;

            if (length < bestLength)
            {
                bestLength = length;
                bestPath = path;
            }
        }

        // 도달 가능한 최선의 경로가 있으면 즉시 반환 (fallback 불필요)
        if (bestPath != null)
            return bestPath;

        // 2) 직접 도달 불가능 + fallback 비활성
        if (!allowApproachWhenUnreachable)
            return s_EmptyGridPositionList;

        // 3) fallback: 목표에 도달 못 해도 근처에서 멈추기
        //     임시 후보 리스트(fallbackCandidates)를 만들지 않고,
        //     "현재까지의 최선"만 추적해서 메모리 할당을 줄인다.
        int bestFullLen = int.MaxValue;
        List<GridPosition> bestFallbackPath = null;

        foreach (var tgt in cached)
        {
            var fullPath = FindPath(start, tgt, out int fullLen);
            if (fullPath == null || fullPath.Count == 0)
                continue;

            // 목표 셀에서 Remove_MOVE_GRID 만큼 앞에서 멈추기 위한 인덱스
            int stopIndex = fullPath.Count - 1 - Remove_MOVE_GRID;
            if (stopIndex < 0)
                continue;  // 경로가 너무 짧아서 멈출 지점이 없음

            // stopIndex가 막혀 있으면 한 칸씩 앞(경로 시작쪽)으로 물러나 유효 지점 탐색
            while (stopIndex >= 0)
            {
                var stopPos = fullPath[stopIndex];

                if (Managers.Grid.CanMoveTo(stopPos, start))
                {
                    if (fullLen < bestFullLen)
                    {
                        bestFullLen = fullLen;

                        // bestFallbackPath는 새로 만들어서 교체 (기존 best는 GC 대상)
                        // LINQ Take/ToList 대신 직접 복사
                        var pathToStop = new List<GridPosition>(stopIndex + 1);
                        for (int i = 0; i <= stopIndex; i++)
                            pathToStop.Add(fullPath[i]);

                        bestFallbackPath = pathToStop;
                    }
                    break;
                }

                stopIndex--;
            }
        }

        return bestFallbackPath ?? s_EmptyGridPositionList;
    }


    // 반환: 출발점 → 선택된 목적지까지의 경로(목적지 포함). 실패 시 빈 리스트 반환.
    // allowApproachWhenUnreachable == true → 이동 불가능한 목표라도 근처까지 접근.
    // false → 도달 불가능하면 빈 리스트 반환.
    public List<GridPosition> FindNearestCandidatePath2(
        GridPosition start,
        IEnumerable<GridPosition> gridPositions,
        bool allowApproachWhenUnreachable = false)
    {
        // 1-1) 가장 빠르게 도달할 수 있는 후보 위치 찾기
        if (gridPositions.Count() > 0)
        {
            int bestLength = int.MaxValue;
            List<GridPosition> bestPath = new();

            foreach (var tgt in gridPositions)
            {
                var path = FindPath(start, tgt, out int length);
                if (path == null || path.Count == 0) continue;

                if (length < bestLength)
                {
                    bestLength = length;
                    bestPath = path;
                }
            }

            if (bestPath.Count > 0)
                return bestPath;
        }

        // 2) 직접 도달 불가능한 경우
        // allowApproachWhenUnreachable이 false면 여기서 바로 중단
        if (!allowApproachWhenUnreachable)
            return s_EmptyGridPositionList;

        // 근처까지 접근 시도 (fallback)
        var fallbackCandidates = new List<(List<GridPosition> pathToStop, int fullPathLength)>();

        foreach (var tgt in gridPositions)
        {
            var fullPath = FindPath(start, tgt, out int fullLength);
            if (fullPath == null || fullPath.Count == 0)
                continue;

            // 목표 셀로부터 Remove_MOVE_GRID 만큼 앞에서 멈추기
            int stopIndex = fullPath.Count - 1 - Remove_MOVE_GRID;
            if (stopIndex < 0)
                continue; // 경로가 너무 짧아 멈출 지점이 없음

            // stopIndex 지점이 막혀 있으면 한 칸씩 앞으로(경로 시작 쪽) 물러나며 유효 지점 찾기
            while (stopIndex >= 0)
            {
                var stopPos = fullPath[stopIndex];

                if (Managers.Grid.GetTerrainType(stopPos) == E_TerrainCellType.Walkable)
                {
                    // stopIndex까지 포함한 경로를 후보로 추가
                    var pathToStop = fullPath.Take(stopIndex + 1).ToList();
                    fallbackCandidates.Add((pathToStop, fullLength));
                    break;
                }

                stopIndex--; // 한 칸 앞(더 이전 지점)으로 물러남
            }
        }

        // fallback 후보들 중에서 전체 경로 길이가 가장 짧은 것 선택
        if (fallbackCandidates.Count > 0)
        {
            int minFullLen = int.MaxValue;
            List<GridPosition> bestFallback = new();

            foreach (var (pathToStop, fullLen) in fallbackCandidates)
            {
                if (fullLen < minFullLen)
                {
                    minFullLen = fullLen;
                    bestFallback = pathToStop;
                }
            }

            if (bestFallback.Count > 0)
                return bestFallback;
        }

        return new List<GridPosition>();
    }
    #endregion

    #region Util

    /// <summary>
    /// 탐색된 경로들을 거리 순으로 정렬
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    public List<GridPosition> SortByPathDistance(
        GridPosition startPos,
        IEnumerable<GridPosition> list)
    {
        return list
            .Select(pos => new
            {
                Pos = pos,
                Length = GetPathLength(startPos, pos)
            })
            .OrderBy(x => x.Length == 0 ? int.MaxValue : x.Length)
            .Select(x => x.Pos)
            .ToList();
    }


    /// <summary>
    /// 리스트 중에서 가장 가까운 그리드 반환
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    public GridPosition FindNearestByPathDistance(GridPosition startPos, IEnumerable<GridPosition> list)
    {
        return SortByPathDistance(startPos, list).First();
    }

    public float GetObstacleMaxHeight(GridPosition a, GridPosition b,
        IgnoreCellType ignore = default)
    {
        var path = FindPath(a, b, out _, ignore);
        if (path == null || path.Count < 3)
            return 0f;

        float max = 0f;

        // 양 끝 제외 (RemoveAt 대신 for가 GC/안전 측면에서 더 좋음)
        for (int i = 1; i < path.Count - 1; i++)
        {
            var obj = Managers.Grid.GetUnitAt(path[i]); // ← 여기 중요!
            if (obj == null || obj.m_HitCollider == null) continue;

            max = Mathf.Max(max, obj.m_HitCollider.bounds.max.y);
        }

        return max;
    }

    #endregion
}


