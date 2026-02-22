using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 그리드 상에서 지형(Re_LevelGrid)과 엔티티(EntityGrid)를 소유하고 관리하는 매니저 클래스.
/// </summary>
public class GridManager : IManager
{
    public int Width => GameConfig.Grid.Width;
    public int Height => GameConfig.Grid.Height;
    public float CellSize => GameConfig.Grid.CellSize;
    public int FloorAmount => GameConfig.Grid.FloorAmount;

    // 실무 로직을 담당하는 순수 C# 객체들
    public Re_LevelGrid LevelGrid { get; private set; }
    public EntityGrid EntityGrid { get; private set; }

    // 그리드의 변화를 알리는 이벤트 (최적화를 위해 위치 정보 포함)
    public event Action<GridPosition> OnGridChanged;

    /// <summary>
    /// 분리된 두 그리드 시스템을 초기화합니다.
    /// </summary>
    public void Init()
    {
        // 1. 지형 그리드 생성 (Re_LevelGrid)
        LevelGrid = new Re_LevelGrid(Width, Height, CellSize, FloorAmount, FLOOR_HEIGHT);

        // 2. 엔티티 그리드 생성 (EntityGrid)
        // FLOOR_HEIGHT는 Define_Const.cs에 정의된 값을 사용합니다.
        EntityGrid = new EntityGrid(Width, Height, CellSize, FloorAmount, FLOOR_HEIGHT);

        Debug.Log($"GridController: Grids Initialized ({Width}x{Height}, Floors: {FloorAmount})");
    }

    public void Clear()
    {
        LevelGrid.Clear();
        EntityGrid.Clear();
    }

    // ⭐ Init 이후, 외부에서 호출
    // 2. 물리 스캔을 통한 초기 지형 데이터 설정 (기존 Pathfinding의 Setup 로직)
    public void SetupTerrain(IGridTerrainScanner scanner)
    {
        ScanTerrain(scanner);
    }

    private void ScanTerrain(IGridTerrainScanner scanner)
    {
        for (int f = 0; f < FloorAmount; f++)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Height; z++)
                {
                    var gp = new GridPosition(x, z, f);
                    var worldPos = LevelGrid.GetWorldPosition(gp);

                    var type = scanner.Scan(gp, worldPos);
                    LevelGrid.UpdateTerrainType(gp, type);
                }
            }
        }
    }

    #region IGridService - Terrain (Re_LevelGrid Delegation)

    public E_TerrainCellType GetTerrainType(GridPosition pos) => LevelGrid.GetCellType(pos);
    public bool IsWalkableTerrain(GridPosition pos) => LevelGrid.IsWalkable(pos);
    public Vector3 GetWorldPosition(GridPosition pos) => LevelGrid.GetWorldPosition(pos);
    public GridPosition GetGridPosition(Vector3 worldPos) => LevelGrid.GetGridPosition(worldPos);
    public bool IsValidGridPosition(Vector3 worldPos) => LevelGrid.IsValidGridPosition(worldPos);
    public bool IsValidGridPosition(GridPosition Pos) => LevelGrid.IsValidGridPosition(Pos);

    #endregion

    #region IGridService - Entity (EntityGrid Delegation)

    public bool HasUnitAt(GridPosition pos) => EntityGrid.GetUnitAt(pos) != null;
        
    public GameEntity GetUnitAt(GridPosition pos) => EntityGrid.GetUnitAt(pos);
    public E_EntityCellType GetEntityCellType(GridPosition pos) => EntityGrid.GetCellType(pos);

    public void UpdateUnitPosition(GameEntity unit, IReadOnlyList<GridPosition> oldPos, IReadOnlyList<GridPosition> newPos)
    {
        EntityGrid.MoveUnit(unit, oldPos, newPos);
        foreach(var pos in oldPos)
            OnGridChanged?.Invoke(pos);
        foreach(var pos in newPos)
            OnGridChanged?.Invoke(pos);
    }

    #endregion

    #region IGridService - Combined Logic & Request System

    // --- 단일 처리 ---
    public bool RequestCell(GridPosition pos, GameEntity unit, E_EntityCellType requestType)
    {
        //if (GetTerrainType(pos) == E_GridCheckType.Void) return false;

        EntityGrid.AddUnit(pos, unit, requestType);

        OnGridChanged?.Invoke(pos);
        return true;
    }

    public void ReleaseCell(GridPosition pos, GameEntity unit)
    {
        EntityGrid.RemoveUnit(pos, unit);

        OnGridChanged?.Invoke(pos);
    }

    // --- 복수 처리 (Overloading) ---
    public bool RequestCell(IEnumerable<GridPosition> positions, GameEntity unit, E_EntityCellType requestType)
    {
        bool ok = true;
        foreach (var pos in positions)
            ok &= RequestCell(pos, unit, requestType);
        return ok;
    }

    public void ReleaseCell(IEnumerable<GridPosition> positions, GameEntity unit)
    {
        // 복수 위치 알림
        foreach (var pos in positions)
            ReleaseCell(pos, unit);
    }

    #endregion

    #region Grid Visual 전용

    public event Action OnRequestVisualRefresh;
    private bool _visualDirty = false;
    public void RequestVisualRefresh() => _visualDirty = true;
    public void LateTick()
    {
        if (!_visualDirty) return;
        _visualDirty = false;
        OnRequestVisualRefresh?.Invoke();
    }

    #endregion

    #region Rule

    // destpos가 이동 가능한 지점인가?
    public bool CanMoveTo(GridPosition destpos, GridPosition startPos, E_TerrainCellType terrainType = E_TerrainCellType.Walkable, params E_EntityCellType[] cell)
    {
        // 1) 지형 체크
        var terrain = GetTerrainType(destpos);
        if (terrain != E_TerrainCellType.Walkable)
            return false;

        // 2) 유닛 체크
        // 해당 타일에 다른 유닛이 있으면 불가
        var entityHere = GetUnitAt(destpos);
        var startEntity = GetUnitAt(startPos);
        if (entityHere != null)
        {
            if(entityHere != startEntity)
                return false;
        }

        return true;
    }
    #endregion
}