using System;
using System.Collections.Generic;
using static Define;

public class EntityGrid
{
    public int width { get; private set; }
    public int height { get; private set; }
    public float cellSize { get; private set; }
    public int floorAmount { get; private set; }

    // 각 격자 칸에 들어갈 엔티티 정보를 담는 클래스
    public class EntityCell
    {

        //// 한 칸에 여러 엔티티가 있을 수 있으므로 HashSet으로 관리 (중복 방지 및 빠른 삭제)
        //private readonly HashSet<GameEntity> _entities = new();
        public GameEntity _entitie { get; private set; }
        public E_EntityCellType CellState { get; set; } = E_EntityCellType.None;
        public GridPosition gridPosition { get; private set; }

        public EntityCell(GridPosition pos) => gridPosition = pos;

        public void Add(GameEntity unit, E_EntityCellType state)
        {
            _entitie = unit;
            // 유닛이 추가될 때 상태 업데이트 (예: 하나라도 있으면 Occupied 또는 Reserve)
            CellState = state;
        }

        public void Remove(GameEntity unit)
        {
            _entitie = null;
            CellState = E_EntityCellType.None; // 아무도 없으면 다시 기본 상태로
        }

        public GameEntity GetEntities() => _entitie;
        public bool HasAny() => _entitie != null;
    }

    private readonly List<GridSystem<EntityCell>> _gridSystemList;
    private readonly int _floorAmount;

    public EntityGrid(int width, int height, float cellSize, int floorAmount, float floorHeight)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.floorAmount = floorAmount;

        _floorAmount = floorAmount;
        _gridSystemList = new List<GridSystem<EntityCell>>();

        for (int floor = 0; floor < floorAmount; floor++)
        {
            // GridSystem의 TGridObject로 EntityCell을 사용
            var system = new GridSystem<EntityCell>(width, height, cellSize, floor, floorHeight,
                (g, pos) => new EntityCell(pos));
            _gridSystemList.Add(system);
        }
    }

    public void Clear()
    {
        _gridSystemList.Clear();
    }

    #region Entity Management Functions (기존 IUnitGridManager의 기능들)

    // 특정 좌표의 현재 상태(예약/점유 등)를 반환
    public E_EntityCellType GetCellType(GridPosition pos)
    {
        if (!IsValid(pos)) return E_EntityCellType.None;
        return _gridSystemList[pos.floor].GetGridObject(pos).CellState;
    }

    // 유닛을 특정 좌표들에 추가
    public void AddUnit(IEnumerable<GridPosition> cells, GameEntity unit, E_EntityCellType state = E_EntityCellType.GameEntity)
    {
        foreach (var pos in cells)
            AddUnit(pos, unit, state);
    }

    // ✅ 추가된 메서드: 상태와 함께 유닛 추가
    public void AddUnit(GridPosition pos, GameEntity unit, E_EntityCellType state)
    {
        if (!IsValid(pos)) return;

        _gridSystemList[pos.floor].GetGridObject(pos).Add(unit, state);
    }

    // 특정 좌표에 특정 유닛이 포함되어 있는지 확인
    public bool ContainsUnit(GridPosition pos, GameEntity unit)
    {
        if (!IsValid(pos)) return false;
        //return _gridSystemList[pos.floor].GetGridObject(pos).GetEntities().Contains(unit);
        return _gridSystemList[pos.floor].GetGridObject(pos)._entitie == unit;
    }

    /// <summary>
    /// 여러 좌표에서 유닛 제거 (복수 버전)
    /// </summary>
    public void RemoveUnit(IEnumerable<GridPosition> cells, GameEntity unit)
    {
        foreach (var pos in cells)
            RemoveUnit(pos, unit);
    }
    /// <summary>
    /// 여러 좌표에서 유닛 제거 (복수 버전)
    /// </summary>
    public void RemoveUnit(GridPosition cell, GameEntity unit)
    {
        if (IsValid(cell))
            _gridSystemList[cell.floor].GetGridObject(cell).Remove(unit);
    }

    /// <summary>
    /// 유닛 이동 및 상태 갱신
    /// </summary>
    public void MoveUnit(GameEntity unit, IEnumerable<GridPosition> fromCells, IEnumerable<GridPosition> toCells, E_EntityCellType newState = E_EntityCellType.GameEntity)
    {
        RemoveUnit(fromCells, unit);
        AddUnit(toCells, unit, newState);
    }

    public void MoveUnit(GameEntity unit, GridPosition fromCell, GridPosition toCell, E_EntityCellType newState = E_EntityCellType.GameEntity)
    {
        RemoveUnit(fromCell, unit);
        AddUnit(toCell, unit, newState);
    }

    // 특정 좌표에 있는 모든 유닛 조회
    public IReadOnlyCollection<GameEntity> GetUnitsAt(GridPosition pos)
    {
        if (!IsValid(pos)) return System.Array.Empty<GameEntity>();
        //return _gridSystemList[pos.floor].GetGridObject(pos).GetEntities();
        return new List<GameEntity>() { _gridSystemList[pos.floor].GetGridObject(pos)._entitie };
    }

    // 특정 좌표에 있는 대표 유닛 조회
    public GameEntity GetUnitAt(GridPosition pos)
    {
        if (!IsValid(pos)) return null;
        return _gridSystemList[pos.floor].GetGridObject(pos)._entitie;
    }

    #endregion

    private bool IsValid(GridPosition pos)
    {
        return pos.floor >= 0 && pos.floor < _floorAmount && _gridSystemList[pos.floor].IsValidGridPosition(pos);
    }

    public Dictionary<E_EntityCellType, List<GridPosition>> GetGroupedPositionsByFloor(int floor)
    {
        var groups = new Dictionary<E_EntityCellType, List<GridPosition>>();

        // 1. 모든 Enum 타입에 대해 리스트 초기화
        foreach (E_EntityCellType type in Enum.GetValues(typeof(E_EntityCellType)))
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
}