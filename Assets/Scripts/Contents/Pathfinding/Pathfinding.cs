using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using UnityEditor.TerrainTools;
using UnityEngine;
using static Define;

public class Pathfinding : MonoBehaviour
{

    public static Pathfinding Instance { get; private set; }


    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    [SerializeField] private Transform gridDebugObjectPrefab;
    [SerializeField] private Transform pathfindingLinkContainer;


    private int width;
    private int height;
    private float cellSize;
    private int floorAmount;
    private List<GridSystem<PathNode>> gridSystemList;
    private List<PathfindingLink> pathfindingLinkList;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one Pathfinding! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Setup(int width, int height, float cellSize, int floorAmount)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.floorAmount = floorAmount;

        gridSystemList = new List<GridSystem<PathNode>>();

        for (int floor = 0; floor < floorAmount; floor++)
        {
            GridSystem<PathNode> gridSystem = new GridSystem<PathNode>(width, height, cellSize, floor, LevelGrid.FLOOR_HEIGHT,
                (GridSystem<PathNode> g, GridPosition gridPosition) => new PathNode(gridPosition));

            //gridSystem.CreateDebugObjects(gridDebugObjectPrefab);

            gridSystemList.Add(gridSystem);
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                for (int floor = 0; floor < floorAmount; floor++)
                {
                    GridPosition gridPosition = new GridPosition(x, z, floor);
                    Vector3 worldPosition = LevelGrid.Instance.GetWorldPosition(gridPosition);
                    float raycastOffsetDistance = 1f;

                    LevelGrid.Instance.SetGridPositionCellInfo(gridPosition, E_GridCheckType.Void, null);

                    if (Physics.Raycast(
                        worldPosition + Vector3.up * raycastOffsetDistance,
                        Vector3.down,
                        raycastOffsetDistance * 2,
                        LayerManager.Instance.mousePlaneLayerMask))
                    {
                        LevelGrid.Instance.SetGridPositionCellInfo(gridPosition, E_GridCheckType.Walkable, null);
                    }

                    if (Physics.Raycast(
                        worldPosition + Vector3.down * raycastOffsetDistance,
                        Vector3.up,
                        raycastOffsetDistance * 2,
                        LayerManager.Instance.ObstaclesLayerMask))
                    {
                        LevelGrid.Instance.SetGridPositionCellInfo(gridPosition, E_GridCheckType.Obstacle, null);
                    }
                }
            }
        }


        pathfindingLinkList = new List<PathfindingLink>();
        foreach (Transform pathfindingLinkTransform in pathfindingLinkContainer)
        {
            if (pathfindingLinkTransform.TryGetComponent(out PathfindingLinkMonoBehaviour pathfindingLinkMonoBehaviour))
            {
                pathfindingLinkList.Add(pathfindingLinkMonoBehaviour.GetPathfindingLink());
            }
        }
    }

    public List<GridPosition> FindPath
        (GridPosition startGridPosition, 
        GridPosition endGridPosition, 
        out int pathLength, 

        bool CheckHasObject = true, 
        bool CheckReserve = true)
    {
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

                bool isStart = neighbourNode == startNode;
                bool isEnd = neighbourNode == endNode;
                var toCheckGridCellInfo = LevelGrid.Instance.GetGridPositionCellInfo(neighbourNode.GetGridPosition());
                
                // 1. Walkable이 아니면 기본적으로 제외
                if (!isStart && !isEnd)
                {
                    // 장애물, Void, Unit 등 막힌 칸
                    if (toCheckGridCellInfo.gridType != E_GridCheckType.Walkable)
                    {
                        bool isPassableReserve = false;

                        // LevelGrid.Instance.GetObjectAtGridPosition(startGridPosition) 는 
                        // 현재 길을 찾는 주체(Entity)를 가져오는 것으로 가정합니다.
                        if (toCheckGridCellInfo.gridType  == E_GridCheckType.GameEntity && CheckHasObject)
                        {
                            // 통과 가능: 현재 객체가 예약한 Reserve 칸
                            isPassableReserve = true;
                        }

                        // 예외 조건에 해당하지 않는다면, 길을 막습니다. (closedList에 추가하고 다음 노드로 넘어감)
                        if (toCheckGridCellInfo.gridType == E_GridCheckType.Reserve && CheckReserve)
                        {
                            if(toCheckGridCellInfo.Entity == LevelGrid.Instance.GetObjectAtGridPosition(startGridPosition))
                            {
                                // 통과 가능: 현재 객체가 예약한 Reserve 칸
                                isPassableReserve = true;
                            }
                        }

                        // 예외 조건에 해당하지 않는다면, 길을 막습니다. (closedList에 추가하고 다음 노드로 넘어감)
                        if (!isPassableReserve)
                        {
                            closedList.Add(neighbourNode);
                            continue;
                        }
                    }
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

    public int CalculateDistance(GridPosition gridPositionA, GridPosition gridPositionB)
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

    public PathNode GetNode(int x, int z, int floor)
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

    public bool HasPath(GridPosition startGridPosition, GridPosition endGridPosition)
    {
        return FindPath(startGridPosition, endGridPosition, out int pathLength) != null;
    }

    public int GetPathLength(GridPosition startGridPosition, GridPosition endGridPosition)
    {
        FindPath(startGridPosition, endGridPosition, out int pathLength);
        return pathLength;
    }

    // 반환: 출발점 → 선택된 목적지까지의 경로(목적지 포함). 실패 시 빈 리스트 반환.
    // allowApproachWhenUnreachable == true → 이동 불가능한 목표라도 근처까지 접근.
    // false → 도달 불가능하면 빈 리스트 반환.
    public List<GridPosition> FindNearestCandidatePath(
        GameEntity entity,
        GridPosition start,
        IEnumerable<GridPosition> attackablePositions,
        bool allowApproachWhenUnreachable = false)
    {
        // 1-1) 가장 빠르게 도달할 수 있는 후보 위치 찾기
        if (attackablePositions.Count() > 0)
        {
            int bestLength = int.MaxValue;
            List<GridPosition> bestPath = new();

            foreach (var tgt in attackablePositions)
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
            return new List<GridPosition>();

        // 근처까지 접근 시도 (fallback)
        var fallbackCandidates = new List<(List<GridPosition> pathToStop, int fullPathLength)>();

        foreach (var tgt in attackablePositions)
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

                bool valid =
                    LevelGrid.Instance.IsValidGridPosition(stopPos) &&
                    LevelGrid.Instance.IsGridPositionCheckType(stopPos, E_GridCheckType.Walkable);

                if (valid)
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
}
