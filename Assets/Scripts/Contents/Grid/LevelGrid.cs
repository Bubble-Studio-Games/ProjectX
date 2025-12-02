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
    Dictionary<GridPosition, GridDebugObject> m_griddebug = new();
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

    public Dictionary<int, Dictionary<GridPosition, GridCellInfo>> m_DicFloorGridCache { get; private set; } = new();

    public class GridCellInfo
    {
        public GameEntity Entity;         // 해당 칸에 있는 엔티티
        public E_GridCheckType gridType; // 필요하다면 체크 타입도 저장

        public GridCellInfo(GameEntity entity, E_GridCheckType checkType)
        {
            Entity = entity;
            gridType = checkType;
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
        
        // 초기화
        GridSystemList = new List<GridSystem<GridObject>>();
        m_DicFloorGridCache.Clear();

        for (int floor = 0; floor < floorAmount; floor++)
        {
            GridSystem<GridObject> gridSystem = new GridSystem<GridObject>(width, height, cellSize, floor, FLOOR_HEIGHT,
                    (GridSystem<GridObject> g, GridPosition gridPosition) => new GridObject(g, gridPosition));
            if(m_isShowCreateDebugObjects)
                m_griddebug.AddRange(gridSystem.CreateDebugObjects(gridDebugObjectPrefab));
            
            GridSystemList.Add(gridSystem);

            // ✅ 층 캐시 초기화
            var dict = new Dictionary<GridPosition, GridCellInfo>();
            for (int x = 0; x < width; x++)
                for (int z = 0; z < height; z++)
                    dict[new GridPosition(x, z, floor)] = new GridCellInfo(null, E_GridCheckType.Void);

            m_DicFloorGridCache[floor] = dict;
        }

        OnChangeGrid += GridDebugObjectUpdate;
    }

    #region Grid Object Add/Remove/Move

    public void AddUnitAtGridPosition(List<GridPosition> gridPositions, GameEntity unit)
    {
        foreach (GridPosition gridPosition in gridPositions)
        {
            GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition);
            gridObject.AddUnit(unit);

        }

        E_GridCheckType type = E_GridCheckType.Walkable;

        switch (unit.m_ObjectType)
        {
            case E_ObjectType.None:
                type = E_GridCheckType.Obstacle;
                break;
            case E_ObjectType.Unit:
            case E_ObjectType.Building:
            case E_ObjectType.Interact:
            case E_ObjectType.AutoTrigger:
            case E_ObjectType.PassiveObject:
                type = E_GridCheckType.GameEntity;
                break;
            case E_ObjectType.Obstacle:
                type = E_GridCheckType.Obstacle;
                break;
        }

        SetGridPositionCellInfo(gridPositions, type, unit);
    }

    public void RemoveUnitAtGridPosition(List<GridPosition> gridPositions, GameEntity unit)
    {
        foreach (GridPosition gridPosition in gridPositions)
        {
            GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition);
            gridObject.RemoveUnit(unit);
        }

        SetGridPositionCellInfo(gridPositions, E_GridCheckType.Walkable, unit);
    }

    public void UnitMovedGridPosition(GameEntity unit, List<GridPosition> fromGridPositions, List<GridPosition> toGridPositions)
    {
        //Debug.Log($"unit {unit} from {string.Join(" ", fromGridPositions)} to {string.Join(" ", toGridPositions) }");
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

    // 인자로 받아온 포지션에서 모든 그리드 오브젝트 반환
    public List<GameEntity> GetObjectsAtGridPositions(List<GridPosition> gridPositions)
    {
        if (gridPositions == null || gridPositions.Count == 0)
            return new List<GameEntity>();

        // 각 GridPosition에 해당하는 GameEntity를 가져오고 null이 아닌 것만 필터링
        return gridPositions
            .Select(pos => GetObjectAtGridPosition(pos))
            .Where(obj => obj != null)
            .ToList();
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

    // origin -> target의 방향
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

    public float GetCurrentFloorHeight(Vector3 worldPosition) 
    { 
        return GetFloor(worldPosition) * FLOOR_HEIGHT; 
    }

    public float GetCurrentFloorHeight(GridPosition gridPosition) 
    { 
        return gridPosition.floor * FLOOR_HEIGHT; 
    }

    public float GetNextFloorHeight(GridPosition gridPosition) 
    { 
        return (gridPosition.floor + 1) * FLOOR_HEIGHT; 
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

    #region Todo Delete
    public IInteractable GetInteractableAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition);
        return gridObject.GetInteractable();
    }


    public void SetInteractableAtGridPosition(GridPosition gridPosition, IInteractable interactable)
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition);
        gridObject.SetInteractable(interactable);
    }

    public void ClearInteractableAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition);
        gridObject.ClearInteractable();
    }

    #endregion

    #region ===== 그리드 Cache와 관련된 함수들 =====

    public void SetGridPositionCellInfo(ICollection<GridPosition> gridPositions, E_GridCheckType type, GameEntity entity = null)
    {
        if (gridPositions == null || gridPositions.Count == 0)
            return;

        foreach (var gridPosition in gridPositions)
            m_DicFloorGridCache[gridPosition.floor][gridPosition] = new GridCellInfo(entity, type);

        // 3이벤트 호출
        OnChangeGrid?.Invoke(this, new OnChangeGridAgrs
        {
            type = type,
            ListGridPosition = gridPositions.ToList(),
        });
    }

    public void SetGridPositionCellInfo(GridPosition gridPosition, E_GridCheckType type, GameEntity entity = null)
    {
        // 1층 딕셔너리가 없으면 생성
        m_DicFloorGridCache[gridPosition.floor][gridPosition] = new GridCellInfo(entity, type);

        // 3이벤트 호출
        OnChangeGrid?.Invoke(this, new OnChangeGridAgrs
        {
            type = type,
            ListGridPosition = new List<GridPosition> { gridPosition },
        });
    }


    public GridCellInfo GetGridPositionCellInfo(GridPosition gridPosition)
    {
        return m_DicFloorGridCache[gridPosition.floor][gridPosition];
    }

    public IEnumerable<(GridPosition, GridCellInfo)> GetFloorGridPositionCellInfo(int floor)
    {
        if (!m_DicFloorGridCache.ContainsKey(floor))
            return Enumerable.Empty<(GridPosition, GridCellInfo)>();

        return m_DicFloorGridCache[floor].Select(pair => (pair.Key, pair.Value));
    }

    public E_GridCheckType GetGridPositionType(GridPosition gridPosition)
    {
        return m_DicFloorGridCache[gridPosition.floor][gridPosition].gridType;
    }

    public List<(GridPosition, E_GridCheckType)> GetFloorGridPositionAndType(int floor)
    {
        // 1. floor가 유효한지 확인
        if (!m_DicFloorGridCache.ContainsKey(floor))
            return new List<(GridPosition, E_GridCheckType)>();

        // 2. 해당 층의 Dictionary<GridPosition, GridCellInfo> 꺼냄
        var floorData = m_DicFloorGridCache[floor];

        // 3. (GridPosition, E_GridCheckType) 튜플 리스트로 변환해서 반환
        return floorData
            .Select(pair => (pair.Key, pair.Value.gridType)) // ← 여기서 GridType이 enum E_GridCheckType 타입이라고 가정
            .ToList();
    }

    /// <summary>
    /// 특정 층, 특정 타입에서 state 상태인 그리드만 반환
    /// </summary>
    public List<GridPosition> GetFloorAndTypeGridPositions(int floor, E_GridCheckType type)
    {
        if (!m_DicFloorGridCache.TryGetValue(floor, out var floorDict))
            return new List<GridPosition>();

        return floorDict
            .Where(kvp => kvp.Value.gridType == type)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public bool IsGridPositionCheckType(GridPosition gridPosition, IEnumerable<E_GridCheckType> types)
    {
        return types.Any(type => IsGridPositionCheckType(gridPosition, type));
    }

    public bool IsGridPositionCheckType(GridPosition gridPosition, params E_GridCheckType[] types)
    {
        if (IsValidGridPosition(gridPosition) == false)
            return false;

        // 타입이 최소 1개 이상 전달되어야 함
        if (types == null || types.Length == 0)
        {
            Debug.LogWarning($"⚠️ IsGridCheckType 호출 오류: 최소 1개 이상의 E_GridCheckType을 전달해야 합니다. (pos: {gridPosition})");
            return false;
        }

        if (m_DicFloorGridCache.TryGetValue(gridPosition.floor, out var floorDict))
        {
            if (floorDict.TryGetValue(gridPosition, out var info))
            {
                // 여러 타입 중 하나라도 일치하면 true
                return types.Contains(info.gridType);
            }
        }

        return false;
    }

    public bool IsGridPositionCheckType(ICollection<GridPosition> gridPositions, params E_GridCheckType[] types)
    {
        // 모든 gridPosition이 지정된 타입들 중 하나에 속해야 true
        return gridPositions.All(pos => IsGridPositionCheckType(pos, types));
    }

    private void ClearFloorCache(int floor)
    {
        if (m_DicFloorGridCache.ContainsKey(floor))
            m_DicFloorGridCache[floor].Clear();
    }

    private void ClearAllFloorCache()
    {
        m_DicFloorGridCache.Clear();
    }
    #endregion

    private void GridDebugObjectUpdate(object sender, OnChangeGridAgrs info)
    {
        if (!m_isShowCreateDebugObjects)
            return;

        foreach (var pos in info.ListGridPosition)
            m_griddebug[pos].UpdateGridObject();
    }
}