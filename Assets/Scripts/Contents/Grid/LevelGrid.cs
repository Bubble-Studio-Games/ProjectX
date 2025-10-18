using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using static Define;
using static UnityEngine.EventSystems.EventTrigger;



public class LevelGrid : MonoBehaviour
{
    public static LevelGrid Instance { get; private set; }

    public const float FLOOR_HEIGHT = 20f;

    public event EventHandler<OnAnyUnitMovedGridPositionEventArgs> OnAnyUnitMovedGridPosition;
    public class OnAnyUnitMovedGridPositionEventArgs : EventArgs
    {
        public GameEntity unit;
        public List<GridPosition> fromGridPositions;
        public List<GridPosition> toGridPositions;
    }


    [SerializeField] private Transform gridDebugObjectPrefab;
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private float cellSize;
    [SerializeField] private int floorAmount;
    
    public List<GridSystem<GridObject>> GridSystemList {  get; private set; }

    [Header("DeBug")]
    [SerializeField] private bool m_isShowCreateDebugObjects;

    /// <summary>
    /// 층별 그리드 상태 캐시
    /// Floor → CheckType → (GridPosition → bool)
    /// 
    /// - Valid: 해당 좌표가 유효한 그리드인지
    /// - HasUnit: 유닛이 있는지
    /// - Reserved: 예약(건설 예정/점유 예약) 상태인지
    /// 
    /// bool 값 의미:
    ///   true  = 상태가 충족됨 (예: 예약됨, 유닛 있음)
    ///   false = 상태가 충족되지 않음
    /// </summary>

    private Dictionary<int, Dictionary<GridPosition, GridCellInfo>> _floorGridCache
        = new Dictionary<int, Dictionary<GridPosition, GridCellInfo>>();  

    public class GridCellInfo
    {
        public bool IsBlocked;            // 기존 bool
        public GameEntity Entity;         // 해당 칸에 있는 엔티티
        public E_GridCheckType CheckType; // 필요하다면 체크 타입도 저장

        public GridCellInfo(bool isBlocked, GameEntity entity, E_GridCheckType checkType)
        {
            IsBlocked = isBlocked;
            Entity = entity;
            CheckType = checkType;
        }

        public GridCellInfo()
        {

        }
    }

    public event EventHandler<OnChangeGridAgrs> OnChangeGrid;

    public class OnChangeGridAgrs : EventArgs
    {
        public E_GridCheckType type;
        public List<GridPosition> ListGridPosition;
        public bool isNotGrid;
    }


    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one LevelGrid! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        GridSystemList = new List<GridSystem<GridObject>>();

        for (int floor = 0; floor < floorAmount; floor++)
        {
            GridSystem<GridObject> gridSystem = new GridSystem<GridObject>(width, height, cellSize, floor, FLOOR_HEIGHT,
                    (GridSystem<GridObject> g, GridPosition gridPosition) => new GridObject(g, gridPosition));
            if(m_isShowCreateDebugObjects)
                gridSystem.CreateDebugObjects(gridDebugObjectPrefab);
            
            GridSystemList.Add(gridSystem);

            /// 층별 캐시 초기화 보장
            // 층별 캐시 초기화 보장
            if (!_floorGridCache.ContainsKey(floor))
            {
                _floorGridCache[floor] = new Dictionary<GridPosition, GridCellInfo>();
            }
        }
    }

    private void Start()
    {
        Pathfinding.Instance.Setup(width, height, cellSize, floorAmount);
    }

    #region Grid Object Add/Remove/Move

    public void AddUnitAtGridPosition(List<GridPosition> gridPositions, GameEntity unit)
    {
        foreach (GridPosition gridPosition in gridPositions)
        {
            GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition);
            gridObject.AddUnit(unit);

            SetFloorCheckCache(gridPosition, E_GridCheckType.HasUnit, true, unit);
        }

        OnChangeGrid?.Invoke(this, new OnChangeGridAgrs
        {
            type = E_GridCheckType.HasUnit,
            ListGridPosition = gridPositions,
            isNotGrid = true
        });

        OnChangeGrid?.Invoke(this, new OnChangeGridAgrs
        {
            type = E_GridCheckType.HasUnit,
            ListGridPosition = gridPositions,
            isNotGrid = true
        });
    }

    public void RemoveUnitAtGridPosition(List<GridPosition> gridPositions, GameEntity unit)
    {
        foreach (GridPosition gridPosition in gridPositions)
        {
            GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition);
            gridObject.RemoveUnit(unit);

            // 캐시 삭제
            _floorGridCache[gridPosition.floor].Remove(gridPosition);
        }

        OnChangeGrid?.Invoke(this, new OnChangeGridAgrs
        {
            type = E_GridCheckType.Empty,
            ListGridPosition = gridPositions,
            isNotGrid = false
        });
    }

    public void UnitMovedGridPosition(GameEntity unit, List<GridPosition> fromGridPositions, List<GridPosition> toGridPositions)
    {
        RemoveUnitAtGridPosition(fromGridPositions, unit);
        AddUnitAtGridPosition(toGridPositions, unit);

        OnAnyUnitMovedGridPosition?.Invoke(this, new OnAnyUnitMovedGridPositionEventArgs {
            unit = unit,
            fromGridPositions = fromGridPositions,
            toGridPositions = toGridPositions,
        });
    }

    #endregion

    #region Get Grid Info for Object

    public List<GameEntity> GetUnitListAtGridPosition(GridPosition gridPosition)
    {
        // ?? 어디에 쓰는 물건이고
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition);
        return gridObject.GetUnitList();
    }

    public T GetObjectAtGridPosition<T>(GridPosition gridPosition) where T : GameEntity
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition);
        return gridObject.GetObject() as T;
    }

    public GameEntity GetObjectAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition);
        return gridObject.GetObject();
    }

    public List<GameEntity> GetObjectsAtGridPositions(List<GridPosition> gridPositions)
    {
        return gridPositions.Select(pos => GetObjectAtGridPosition(pos)).ToList();
    }



    public void SetInteractableAtGridPosition(GridPosition gridPosition, IInteractable interactable)
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition);
        gridObject.SetInteractable(interactable);
    }

    public IInteractable GetInteractableAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition);
        return gridObject.GetInteractable();
    }

    public (ControllableObject, GridPosition) GetClosestTargetGridInfo(GridPosition gridPosition, List<GridPosition> positions)
    {
        if (positions.Count == 0)
            return (null, default);

        GridPosition closest = default;
        ControllableObject obj = null;
        int minPathLength = int.MaxValue;

        foreach (GridPosition pos in positions)
        {
            if (!Pathfinding.Instance.HasPath(gridPosition, pos))
                continue;

            // 직접 조작 유닛 외 제외
            if (GetObjectAtGridPosition(pos) is not ControllableObject serch)
                continue;

            // 죽은 놈패스
            if (serch == null || serch.m_AttributeSystem.m_IsDead)
                continue;

            // (옵션) 자동 탐지가 아닌 것들 Spanwer 같은 것들
            if (serch is Building building)
            {
                if (building.m_EBuildingType == E_BuildingType.Spawner)
                    continue;
            }


            int pathLength = Pathfinding.Instance.GetPathLength(gridPosition, pos);
            if (pathLength < minPathLength)
            {
                minPathLength = pathLength;
                closest = pos;
                obj = serch;
            }
        }

        return (obj, closest);
    }

    public GridPosition GetClosestGridPositionSpecificCondition(GridPosition gridPosition, List<GridPosition> positions, Func<GridPosition, bool> condition = null)
    {
        if (positions.Count == 0)
            return default;

        GridPosition closest = default;
        int minPathLength = int.MaxValue;

        foreach (GridPosition pos in positions)
        {
            if (!Pathfinding.Instance.HasPath(gridPosition, pos))
                continue;

            if (condition != null && !condition(pos))
                continue;

            int pathLength = Pathfinding.Instance.GetPathLength(gridPosition, pos);
            if (pathLength < minPathLength)
            {
                minPathLength = pathLength;
                closest = pos;
            }
        }

        return closest;
    }

    public bool IsValidGridPosition(GridPosition gridPosition)
    {
        if (gridPosition.floor < 0 || gridPosition.floor >= floorAmount)
        {
            return false;
        }
        else
        {
            return GetGridSystem(gridPosition.floor).IsValidGridPosition(gridPosition);
        }
    }

    public bool IsValidGridPosition(Vector3 worldPos)
    {
        var gridPosition = GetGridPosition(worldPos);

        return IsValidGridPosition(gridPosition);
    }

    public bool HasAnyUnitOnGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition);
        return gridObject.HasAnyUnit();
    }

    public bool HasEnemyAtGridPosition(GridPosition gridPosition, GridPosition targetPosition)
    {
        GameEntity searcherObject = GetObjectAtGridPosition<GameEntity>(gridPosition);
        if (searcherObject == null)
            return false;

        GameEntity targetObject = GetObjectAtGridPosition<GameEntity>(targetPosition);
        if (targetObject == null)
            return false;

        return searcherObject.IsEnemy(targetObject);
    }

    public bool IsTargeSoFarAtChase(GridPosition gridPosition, GridPosition targetPosition)
    {
        ControllableObject attacker = GetObjectAtGridPosition<ControllableObject>(gridPosition);

        int pos = gridPosition.x - targetPosition.x + gridPosition.z - targetPosition.z;
        if (attacker.m_AttributeSystem.m_Stat.m_iChaseRange <= pos)
            return true;

        return false;
    }

    #endregion

    #region Get Grid System Info

    private GridSystem<GridObject> GetGridSystem(int floor)
    {
        return GridSystemList[floor];
    }


    public int GetFloor(Vector3 worldPosition)
    {
        return Mathf.RoundToInt(worldPosition.y / FLOOR_HEIGHT);
    }

    public GridPosition GetGridPosition(Vector3 worldPosition)
    {
        int floor = GetFloor(worldPosition);
        if (floor >= floorAmount)
            floor = floorAmount - 1;
        return GetGridSystem(floor).GetGridPosition(worldPosition);
    }

    public Vector3 GetWorldPositionNormalize(Vector3 worldPosition)
    {
        return GetWorldPosition(GetGridPosition(worldPosition));
    }

    public Vector3 GetWorldPosition(GridPosition gridPosition) => GetGridSystem(gridPosition.floor).GetWorldPosition(gridPosition);
    
    public int GetWidth() => GetGridSystem(0).GetWidth();
    
    public int GetHeight() => GetGridSystem(0).GetHeight();

    public int GetCellSize() => GetGridSystem(0).GetCellSize();

    public int GetFloorAmount() => floorAmount;


    public E_Dir GetDirGridPosition(GridPosition origin, GridPosition target)
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

    
    public (T result, GridPosition position) GetClosestGridPositionWithData<T>(
    GridPosition origin,
    List<GridPosition> positions,
    Func<GridPosition, bool> condition,
    Func<GridPosition, T> selector)
    {
        if (positions.Count == 0)
            return (default, default);

        GridPosition closest = default;
        T result = default;
        int minPathLength = int.MaxValue;

        foreach (var pos in positions)
        {
            if (!Pathfinding.Instance.HasPath(origin, pos))
                continue;

            if (condition != null && !condition(pos))
                continue;

            int pathLength = Pathfinding.Instance.GetPathLength(origin, pos);
            if (pathLength < minPathLength)
            {
                minPathLength = pathLength;
                closest = pos;
                result = selector(pos);
            }
        }

        return (result, closest);
    }


    #endregion

    #region Caculate

    public List<GridPosition> ToGridPosition(GameEntity entity, E_Dir dir)
    {
        return entity.m_GridPositionOffsets
            .Select(x => ToGridPosition(x, entity.m_GridPosition, dir)).ToList();
    }

    public List<GridPosition> ToGridPosition(GameEntity entity)
    {
        return entity.m_GridPositionOffsets
            .Select(x => ToGridPosition(x, entity.m_GridPosition, entity.m_CurrentEDir)).ToList();
    }

    public List<GridPosition> ToGridPosition(GameEntity entity, GridPosition origin)
    {
        return entity.m_GridPositionOffsets
            .Select(x => ToGridPosition(x, origin, entity.m_CurrentEDir)).ToList();
    }

    public GridPosition ToGridPosition(GridPosition offset, GridPosition origin, E_Dir dir)
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

    public float GetObstacleMaxHeight(GridPosition gridPosition, GridPosition targetPosition)
    {
        var posList = Pathfinding.Instance.FindPath(gridPosition, targetPosition, out int len, false);
        float maxHegiht = 0;

        // 공격자와 피격자 사이의 1칸 이상의 거리가 있다면
        // 특정 사이즈 아래의 오브젝트가 있는가?
        if (posList != null && posList.Count >= 3)
        {
            posList.RemoveAt(posList.Count - 1);
            posList.RemoveAt(0);

            foreach (var pos in posList)
            {
                var obj = GetObjectAtGridPosition(pos);
                if(obj != null)
                {
                    maxHegiht = Math.Max(maxHegiht, obj.m_HitCollider.bounds.max.y);
                }
            }
        }

        return maxHegiht;
    }

    public float GetObstacleMaxHeight(Vector3 gridPosition, Vector3 targetPosition)
    {
        return GetObstacleMaxHeight(GetGridPosition(gridPosition), GetGridPosition(targetPosition));
    }

    #endregion

    #region Order

    public void ClearInteractableAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition);
        gridObject.ClearInteractable();
    }

    #endregion

    #region Cache

    /// <summary>
    /// 특정 층, 특정 타입에서 state 상태인 그리드만 반환
    /// </summary>
    public List<GridPosition> GetFloorGridPositions(int floor, E_GridCheckType type, bool state = true)
    {
        if (!_floorGridCache.TryGetValue(floor, out var floorDict))
            return new List<GridPosition>();

        return floorDict
            .Where(kvp => kvp.Value.CheckType == type && kvp.Value.IsBlocked == state)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    private bool CachedCheck(int floor, E_GridCheckType type, GridPosition pos)
    {
        if (!_floorGridCache.TryGetValue(floor, out var floorDict))
            return false;

        if (!floorDict.TryGetValue(pos, out var cell))
        {
            cell = new GridCellInfo();
            floorDict[pos] = cell;
        }

        // 타입에 맞게 값 갱신
        bool result;
        switch (type)
        {
            case E_GridCheckType.Walkable:
                result = IsValidGridPosition(pos);
                break;
            case E_GridCheckType.HasUnit:
                result = HasAnyUnitOnGridPosition(pos);
                break;
            case E_GridCheckType.Reserved:
                result = IsReservedGridPosition(pos);
                break;
            default:
                result = false;
                break;
        }

        cell.CheckType = type;
        cell.IsBlocked = result;
        return result;
    }

    private void ClearFloorCache(int floor)
    {
        if (_floorGridCache.ContainsKey(floor))
            _floorGridCache[floor].Clear();
    }

    private void ClearAllFloorCache()
    {
        _floorGridCache.Clear();
    }


    #endregion



    #region ===== Reserved 전용 메서드 =====

    /// <summary>
    /// 단일 좌표 예약 상태 설정
    /// </summary>
    public void SetReserveGridPosition(GridPosition gridPosition, bool isReserve, GameEntity reserveGameEntity)
    {
        SetReserveGridPosition(new List<GridPosition>() { gridPosition }, isReserve, reserveGameEntity);
    }

    /// <summary>
    /// 여러 좌표 예약 상태 설정 (층이 섞여 있어도 처리)
    /// </summary>
    public void SetReserveGridPosition(List<GridPosition> gridPositions, bool isReserve, GameEntity reserveGameEntity)
    {
        if (gridPositions == null || gridPositions.Count == 0)
            return;

        foreach (var pos in gridPositions)
        {
            _floorGridCache[pos.floor][pos] = new GridCellInfo(isReserve, reserveGameEntity, E_GridCheckType.Reserved);
        }

        OnChangeGrid?.Invoke(this, new OnChangeGridAgrs
        {
            type = E_GridCheckType.Reserved,
            ListGridPosition = gridPositions,
            isNotGrid = isReserve
        });
    }


    /// <summary>
    /// 특정 좌표가 예약 상태인지 확인
    /// </summary>
    public bool IsReservedGridPosition(GridPosition gridPosition)
    {
        if ( _floorGridCache[gridPosition.floor].TryGetValue(gridPosition, out var info))
        {
            if (info.CheckType == E_GridCheckType.Reserved)
                return info.IsBlocked;
        }

        return false;
    }

    // 예약하려는 오브젝트가 인자 오브젝트와 같은지 체크
    public bool IsDifferentGameEntityAtReservedGridPosition(GridPosition gridPosition, GameEntity gameEntity)
    {
        return IsReservedGridPosition(gridPosition) && _floorGridCache[gridPosition.floor][gridPosition].Entity != gameEntity;
    }

    /// <summary>
    /// 예약된 그리드 좌표 목록 가져오기
    /// isReserve = true → 예약된 좌표들
    /// isReserve = false → 예약 해제된 좌표들
    /// </summary>
    public List<GridPosition> GetReserveGridPositions(int floor, bool isReserve = true)
    {
        var dict = _floorGridCache[floor];
        var list = new List<GridPosition>();

        foreach (var kvp in dict)
        {
            if (kvp.Value.IsBlocked == isReserve)
                list.Add(kvp.Key);
        }

        return list;
    }

    #endregion

    #region ===== 장애물 체크 전용 메서드 =====

    public void SetFloorCheckCache(GridPosition gridPosition, E_GridCheckType type, bool isWalkable, GameEntity entity)
    {
        _floorGridCache[gridPosition.floor][gridPosition] = new GridCellInfo(isWalkable, entity, type);
    }

    #endregion
}