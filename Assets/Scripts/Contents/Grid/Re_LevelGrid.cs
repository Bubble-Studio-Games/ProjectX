using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

[EditorShowInfo("그리드 지형 지물의 정보를 가진다.")]
public class Re_LevelGrid
{
    public int width { get; private set; }
    public int height { get; private set; }
    public float cellSize { get; private set; }
    public int floorAmount { get; private set; }

    // 지형 시스템 데이터
    public List<GridSystem<GridCellInfo>> GridSystemList { get; private set; }

    public class GridCellInfo
    {
        public E_TerrainCellType gridType;
        public GridPosition gridPosition; // 자기 위치 정보를 들고 있으면 편리합니다.

        // 지형 레이어에서 판단하는 '통과 가능 여부' (엔티티 제외)
        public bool IsWalkableTerrain => gridType == E_TerrainCellType.Walkable;

        public GridCellInfo(GridPosition pos, E_TerrainCellType type)
        {
            gridPosition = pos;
            gridType = type;
        }
    }

    public Re_LevelGrid(int width, int height, float cellSize, int floorAmount, float floorHegiht)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.floorAmount = floorAmount;

        GridSystemList = new List<GridSystem<GridCellInfo>>();

        for (int floor = 0; floor < floorAmount; floor++)
        {
            // 생성자에서 바로 GridCellInfo를 생성하여 할당합니다.
            var gridSystem = new GridSystem<GridCellInfo>(width, height, cellSize, floor, floorHegiht,
                (g, pos) => new GridCellInfo(pos, E_TerrainCellType.Void));

            GridSystemList.Add(gridSystem);
        }
    }

    public void Clear()
    {
        GridSystemList.Clear();
    }

    #region IGridQuery

    public bool IsValidGridPosition(GridPosition gridPosition)
    {
        if (gridPosition.floor < 0 || gridPosition.floor >= floorAmount)
            return false;
        else
            return GridSystemList[gridPosition.floor].IsValidGridPosition(gridPosition);
    }

    public bool IsValidGridPosition(Vector3 worldPos)
        => IsValidGridPosition(GetGridPosition(worldPos));

    public int GetFloor(Vector3 worldPosition)
        => Mathf.RoundToInt(worldPosition.y / FLOOR_HEIGHT);

    public GridPosition GetGridPosition(Vector3 worldPosition)
    {
        int floor = GetFloor(worldPosition);
        if (floor >= floorAmount)
            floor = floorAmount - 1;
        return GridSystemList[floor].GetGridPosition(worldPosition);
    }

    public Vector3 GetWorldPositionNormalize(Vector3 worldPosition)
        => GetWorldPosition(GetGridPosition(worldPosition));

    public Vector3 GetWorldPosition(GridPosition gridPosition) 
        => GridSystemList[gridPosition.floor].GetWorldPosition(gridPosition);

    public float GetCurrentFloorHeight(Vector3 worldPosition)
        => GetFloor(worldPosition) * FLOOR_HEIGHT;

    public float GetCurrentFloorHeight(GridPosition gridPosition)
        => gridPosition.floor * FLOOR_HEIGHT;

    public float GetNextFloorHeight(GridPosition gridPosition)
        => (gridPosition.floor + 1) * FLOOR_HEIGHT;

    public E_TerrainCellType GetCellType(GridPosition pos)
    {
        if (!IsValidGridPosition(pos)) return E_TerrainCellType.Void;
        return GridSystemList[pos.floor].GetGridObject(pos).gridType;
    }

    // 1. 특정 좌표의 전체 정보를 가져오기
    public GridCellInfo GetGridCellInfo(GridPosition pos)
    {
        if (!IsValidGridPosition(pos)) return null;
        return GridSystemList[pos.floor].GetGridObject(pos);
    }

    public bool IsWalkable(GridPosition pos)
    {
        var info = GetGridCellInfo(pos);
        return info != null && info.IsWalkableTerrain;
    }

    public void UpdateTerrainType(GridPosition pos, E_TerrainCellType newType)
    {
        var info = GetGridCellInfo(pos);
        if (info != null)
        {
            info.gridType = newType;
        }
    }

    public Dictionary<E_TerrainCellType, List<GridPosition>> GetGroupedPositionsByFloor(int floor)
    {
        var groups = new Dictionary<E_TerrainCellType, List<GridPosition>>();

        // 1. 모든 Enum 타입에 대해 리스트 초기화
        foreach (E_TerrainCellType type in Enum.GetValues(typeof(E_TerrainCellType)))
        {
            groups[type] = new List<GridPosition>();
        }

        // 2. 현재 층의 가로/세로 범위를 순회 (O(N) 단일 루프)
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridPosition pos = new GridPosition(x, z, floor);

                // 지형 지물 체크
                var girdType = GetCellType(pos);

                groups[girdType].Add(pos);
            }
        }

        return groups;
    }
    #endregion
}