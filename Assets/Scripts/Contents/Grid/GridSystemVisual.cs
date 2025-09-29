using RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class GridSystemVisual : MonoBehaviour
{
    public bool m_isShowReservationGrid;

    public static GridSystemVisual Instance { get; private set; }

    [Serializable]
    public struct GridVisualTypeMaterial
    {
        public GridVisualType gridVisualType;
        public Material material;
    }

    [SerializeField] private Transform gridSystemVisualSinglePrefab;
    [SerializeField] private List<GridVisualTypeMaterial> gridVisualTypeMaterialList;

    // 기존: GridSystemVisualSingle[,,] gridSystemVisualSingleArray;
    // 개선: 층별 관리용 Dictionary
    private Dictionary<int, GridSystemVisualSingle[,]> _floorVisuals = new Dictionary<int, GridSystemVisualSingle[,]>();

    [Header("Select Object")]
    private bool m_IsFloorClearCache = false;
    Dictionary<int, HashSet<GridPosition>> m_CacheWalkableFloor = new();

    // 몇 층 부터 몇 층까지 검사 했는지 담아두기
    private Dictionary<int, bool> m_CacheCheckFloor = new();
    // 이전 프레임의 층 상태 저장
    private Dictionary<int, bool> m_PreviousCacheCheckFloor = new();

    // 층별 유닛/예약 그리드와 영향 관계 캐시
    Dictionary<int, Dictionary<E_GridCheckType, HashSet<GridPosition>>> notplaceGrid = new();
    Dictionary<E_GridCheckType, Dictionary<GridPosition, HashSet<GridPosition>>> notPlaceGridOffset = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one GridSystemVisual! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        for (int floor = 0; floor < LevelGrid.Instance.GetFloorAmount(); floor++)
        {
            var gridArray = new GridSystemVisualSingle[
                LevelGrid.Instance.GetWidth(),
                LevelGrid.Instance.GetHeight()
            ];

            for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
            {
                for (int z = 0; z < LevelGrid.Instance.GetHeight(); z++)
                {
                    GridPosition gridPosition = new GridPosition(x, z, floor);

                    Transform gridSystemVisualSingleTransform =
                        Instantiate(gridSystemVisualSinglePrefab,
                                    LevelGrid.Instance.GetWorldPosition(gridPosition),
                                    Quaternion.identity);

                    gridSystemVisualSingleTransform.transform.parent = transform;

                    gridArray[x, z] = gridSystemVisualSingleTransform.GetComponent<GridSystemVisualSingle>();
                }
            }

            // 초기화 및 데이터 집어 넣기
            m_CacheWalkableFloor.Add(floor, Enumerable.ToHashSet( LevelGrid.Instance.GetFloorGridPositions(floor, E_GridCheckType.Walkable)));
            m_CacheCheckFloor.Add(floor, false);

            notplaceGrid[floor] = new();
            notplaceGrid[floor][E_GridCheckType.Obstacle] = new();
            notplaceGrid[floor][E_GridCheckType.HasUnit] = new();
            notplaceGrid[floor][E_GridCheckType.Reserved] = new();


            _floorVisuals[floor] = gridArray;
        }

        // 각 타입별 그리드 수집
        foreach (E_GridCheckType type in Enum.GetValues(typeof(E_GridCheckType)))
            notPlaceGridOffset[type] = new();

        UnitActionSystem.Instance.OnSelectedActionChanged += (s, e) => UpdateGridVisual();
        UnitActionSystem.Instance.OnSelectedUnitChanged += (s, e) => UpdateGridVisual();
        //UnitActionSystem.Instance.OnUpdateActionTick += (s, e) => UpdateGridVisual();

        // Building Place
        GridBuildingSystem.Instance.OnObjectPlacedCancel += (s, e) => ClearPlace();
        GridBuildingSystem.Instance.OnObjectPlaced += (s, e) => ClearPlace();

        GridBuildingSystem.Instance.OnSelectedChanged += OnObjectSelectChangeUpdate;
        GridBuildingSystem.Instance.OnRotateObject += OnObjectRotateUpdate;
        CameraController.Instance.OnChangeLookFloor += OnFloorCacheClear;
        LevelGrid.Instance.OnChangeGrid += OnLevelGridChanged;

        UpdateGridVisual();
    }

    public void HideAllGridPosition()
    {
        foreach (var floorPair in _floorVisuals) // floor → 2D 배열
        {
            var gridArray = floorPair.Value;
            int width = gridArray.GetLength(0);
            int height = gridArray.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    gridArray[x, z].Hide();
                }
            }
        }
    }

    public void HideGridPositionByFloor(int floor)
    {
        if (!_floorVisuals.ContainsKey(floor)) return;

        var gridArray = _floorVisuals[floor];
        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                gridArray[x, z].Hide();
            }
        }
    }

    private void ShowGridPositionRange(GridPosition gridPosition, int range, GridVisualType gridVisualType)
    {
        List<GridPosition> gridPositionList = new List<GridPosition>();

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                GridPosition testGridPosition = gridPosition + new GridPosition(x, z, 0);

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                {
                    continue;
                }

                int testDistance = Mathf.Abs(x) + Mathf.Abs(z);
                if (testDistance > range)
                {
                    continue;
                }

                gridPositionList.Add(testGridPosition);
            }
        }

        ShowGridPositionList(gridPositionList, gridVisualType);
    }

    private void ShowGridPositionRangeSquare(GridPosition gridPosition, int range, GridVisualType gridVisualType)
    {
        List<GridPosition> gridPositionList = new List<GridPosition>();

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                GridPosition testGridPosition = gridPosition + new GridPosition(x, z, 0);

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                {
                    continue;
                }

                gridPositionList.Add(testGridPosition);
            }
        }

        ShowGridPositionList(gridPositionList, gridVisualType);
    }

    public void ShowGridPositionList(ICollection<GridPosition> gridPositionList, GridVisualType gridVisualType)
    {
        var material = GetGridVisualTypeMaterial(gridVisualType);

        foreach (GridPosition gridPosition in gridPositionList)
        {
            if (_floorVisuals.TryGetValue(gridPosition.floor, out var gridArray))
            {
                gridArray[gridPosition.x, gridPosition.z].Show(material);
            }
        }
    }


    public void UpdateGridVisual_Event(object sender, EventArgs e)
    {
        MoveAction action = sender as MoveAction;
        if(action != null)
        {
            ControllableObject obj = action.m_BaseObject;
            if (!UnitActionSystem.Instance.IsSelectedObject(obj))
                return;

            UpdateGridVisual();
        }
    }

    #region Select Setup Object

    /*
        그리드 업데이트는 언제?
        1. 손에 든 카드(오브젝트)가 바뀌어 GridOffset Min/Max가 달라졌을 때 → 해당 층의 전체 갱신
        2. 유닛이 이동하거나 Reserve 상태가 바뀌었을 때 → 해당 그리드만 갱신
    */
    private void OnObjectRotateUpdate(object sender, E_SetupObjectOffsetChange e)
    {
        if (e == E_SetupObjectOffsetChange.XZOffset)
            OnlyReCalulateOffsets();
        else if (e == E_SetupObjectOffsetChange.None)
            return;

        //m_IsFloorClearCache = true;
        UpdateCanPlacedGrid(sender, null);
    }

    private void OnlyReCalulateOffsets()
    {
        if (GridBuildingSystem.Instance.m_PlacedObject == null)
            return;

        var objectOffsets = LevelGrid.Instance.ToGridPosition(GridBuildingSystem.Instance.GetPlacedObject());

        foreach (var floor in m_CacheCheckFloor.Where(i => i.Value == true).Select(k => k.Key))
        {
            // Offset 다시 계산
            foreach (var kv in notplaceGrid[floor])
            {
                foreach (var pos in kv.Value)
                {
                    // 초기화
                    notPlaceGridOffset[kv.Key][pos].Clear();

                    var ng = objectOffsets
                        .Select(o => pos + o.ReverseSign())
                        .Where(p => LevelGrid.Instance.IsValidGridPosition(p))
                        .Except(notplaceGrid[pos.floor][E_GridCheckType.Obstacle])
                        .Where(p => m_CacheWalkableFloor[pos.floor].Contains(p));

                    notPlaceGridOffset[kv.Key][pos] = Enumerable.ToHashSet(ng);
                }
            }
        }
    }

    private void OnObjectSelectChangeUpdate(object sender, E_SetupObjectOffsetChange e)
    {
        switch (e)
        {
            case E_SetupObjectOffsetChange.None:
                return;
            case E_SetupObjectOffsetChange.YOffset:
                m_IsFloorClearCache = true;
                break;
            case E_SetupObjectOffsetChange.XZOffset:
                OnlyReCalulateOffsets();
                break;
            case E_SetupObjectOffsetChange.All:
                m_IsFloorClearCache = true;
                break;
            default:
                break;
        }

        UpdateCanPlacedGrid(sender, null);
    }

    private void OnFloorCacheClear(object sender, bool e)
    {
        m_IsFloorClearCache = e;
        UpdateCanPlacedGrid(sender, null);
    }

    private void OnLevelGridChanged(object sender, LevelGrid.OnChangeGridAgrs e)
    {
        UpdateCanPlacedGrid(sender, e);
    }

    private void ClearPlace()
    {
        CacheClear();
        HideAllGridPosition();
    }

    private void CacheClear()
    {
        m_CacheCheckFloor = m_CacheCheckFloor
            .Select(x => new KeyValuePair<int, bool>(x.Key, x.Value ? false : x.Value)) // Value가 true이면 false로, false이면 그대로 false로 유지
                                                                                        // 또는 그냥 new KeyValuePair<int, bool>(x.Key, false) 라고 해도 동일한 결과를 얻을 수 있어요.
            .ToDictionary(x => x.Key, x => x.Value);

        m_PreviousCacheCheckFloor = m_PreviousCacheCheckFloor
            .Select(x => new KeyValuePair<int, bool>(x.Key, x.Value ? false : x.Value))
            .ToDictionary(x => x.Key, x => x.Value);
    }

    /// 그리드 배치 가능 여부를 업데이트하고 색상으로 표시한다.
    public void UpdateCanPlacedGrid(object sender, LevelGrid.OnChangeGridAgrs e)
    {
        if (GridBuildingSystem.Instance.m_PlacedObject == null)
            return;

        if (m_CacheWalkableFloor.Count() == 0)
        {
            Debug.Log("설치 가능한 위치의 그리드가 없습니다.");
            return;
        }

        // 상대 오프셋
        var objectOffsets = LevelGrid.Instance.ToGridPosition(GridBuildingSystem.Instance.GetPlacedObject());

        // 1. 캐시 초기화 (처음 카드 선택 or 층 변경 시)
        if (m_IsFloorClearCache)
        {
            // 오브젝트의 Y 오프셋 (예: 2층까지 차지하는 건물일 경우 Min=0, Max=1)
            var yOffset = GridBuildingSystem.Instance.GetPlacedObject().GetGridPositionYOffset();
            int currentLookFloor = CameraController.Instance.m_CurrentLookFloor;

            // 오브젝트가 차지하는 실제 층 범위 계산
            int minFloorIndex = currentLookFloor + yOffset.Min;
            int maxFloorIndex = currentLookFloor + yOffset.Max;

            // 유효한 층만 검사 (1 이상, FloorAmount 이하)
            for (int floorIndex = Mathf.Max(0, minFloorIndex);
                 floorIndex <= Mathf.Min(LevelGrid.Instance.GetFloorAmount() - 1, maxFloorIndex);
                 floorIndex++)
            {
                m_CacheCheckFloor[floorIndex] = true; // 이번에도 검사
            }

            // 빠진 층은 false 처리
            var activeFloors = Enumerable.Range(minFloorIndex, maxFloorIndex - minFloorIndex + 1);
            foreach (var key in m_CacheCheckFloor.Keys.ToList())
                if (!activeFloors.Contains(key))
                    m_CacheCheckFloor[key] = false;

            #region Caculate new Floor

            // 새롭게 탐색할 층
            var newSearchFloor = m_CacheCheckFloor
                .Where(kv => kv.Value == true &&
                             (!m_PreviousCacheCheckFloor.TryGetValue(kv.Key, out var prev) || prev == false))
                .Select(kv => kv.Key)
                .ToList();

            // 새로운 층의 변동 정보들 가져오기
            foreach (int floor in newSearchFloor)
            {
                var obs =  Enumerable.ToHashSet(LevelGrid.Instance.GetFloorGridPositions(floor, E_GridCheckType.Obstacle));
                var unis = Enumerable.ToHashSet(LevelGrid.Instance.GetFloorGridPositions(floor, E_GridCheckType.HasUnit));
                var res = Enumerable.ToHashSet(LevelGrid.Instance.GetFloorGridPositions(floor, E_GridCheckType.Reserved));

                notplaceGrid[floor][E_GridCheckType.Obstacle].AddRange(obs);
                notplaceGrid[floor][E_GridCheckType.HasUnit].AddRange(unis);
                notplaceGrid[floor][E_GridCheckType.Reserved].AddRange(res);

                // 각 타입별 offset 처리
                foreach (var kv in notplaceGrid[floor])
                {
                    var walkableFloor = m_CacheWalkableFloor[floor]; 

                    var type = kv.Key;
                    var positions = kv.Value;

                    foreach (var pos in positions)
                    {
                        var ng = objectOffsets
                            .Select(o => pos + o.ReverseSign())
                            .Where(p => LevelGrid.Instance.IsValidGridPosition(p))
                            .Except(obs) // 장애물에 그리지 않게
                            .Where(p => walkableFloor.Contains(p)); // 발판 없는 곳에 그리지 않게

                        notPlaceGridOffset[type][pos] = Enumerable.ToHashSet(ng);
                    }
                }
            }

            #endregion

            #region Disable Not Use Floor

            // 이번에 false 로 갱신된 층 찾기
            var newlyDisabledFloors = m_CacheCheckFloor
                .Where(kv => kv.Value == false &&
                             m_PreviousCacheCheckFloor.TryGetValue(kv.Key, out var prev) && prev == true)
                .Select(kv => kv.Key)
                .ToList();

            // 그 층만 초기화
            foreach (var floor in newlyDisabledFloors)
            {
                if (notplaceGrid.TryGetValue(floor, out var grids))
                {
                    // Offset 제거
                    foreach (var kv in grids)
                    {
                        notPlaceGridOffset[kv.Key].Clear();
                    }

                    grids.Clear(); // key는 유지
                }
            }

            #endregion

            // 마지막에 현재 상태를 백업
            m_PreviousCacheCheckFloor = new Dictionary<int, bool>(m_CacheCheckFloor);
            m_IsFloorClearCache = false;
        }

        if (m_CacheCheckFloor.All(x => x.Value == false))
        {
            //Debug.Log("설치 가능한 위치의 그리드가 없습니다.");
            return;
        }


        // 2. 그리드 재 검사
        if(e != null)
        {
            // 그리드 배치가 불가능하게!
            if (e.isNotGrid)
            {
                foreach (var pos in e.ListGridPosition)
                {
                    notplaceGrid[pos.floor][e.type].Add(pos);

                    // OFFset 계산해서 넣기
                    var cangrid = objectOffsets
                            .Select(o => pos + o.ReverseSign())
                            .Where(p => LevelGrid.Instance.IsValidGridPosition(p))
                            .Except(notplaceGrid[pos.floor][E_GridCheckType.Obstacle])
                            .Where(p => m_CacheWalkableFloor[pos.floor].Contains(p));
            
                    notPlaceGridOffset[e.type][pos] = Enumerable.ToHashSet(cangrid);
                }
            
            }
            // 그리드 배치가 가능해졌다!
            else
            {
                foreach (var pos in e.ListGridPosition)
                {
                    notplaceGrid[pos.floor][e.type].Remove(pos);

                    if(notPlaceGridOffset[e.type].ContainsKey(pos))
                        notPlaceGridOffset[e.type][pos].Clear();
                }
            }
        }


        // 3. 결과 리스트 (Red: 불가능 / White: 가능)
        HashSet<GridPosition> redList = new();
        HashSet<GridPosition> whiteList = new();

        // 4. 그리드 최종 결과
        foreach (var floor in m_CacheCheckFloor.Where(info => info.Value == true).Select(i => i.Key))
        {
            redList.AddRange(notplaceGrid[floor][E_GridCheckType.HasUnit]);
            redList.AddRange(notplaceGrid[floor][E_GridCheckType.Reserved]);
        
            redList.AddRange(notPlaceGridOffset[E_GridCheckType.HasUnit].SelectMany(x => x.Value));
            redList.AddRange(notPlaceGridOffset[E_GridCheckType.Reserved].SelectMany(x => x.Value));
            redList.AddRange(notPlaceGridOffset[E_GridCheckType.Obstacle].SelectMany(x => x.Value));

            var white = m_CacheWalkableFloor[floor]
                        .Except(redList);

            whiteList.AddRange(white);
        }


        // 5. 시각화 갱신
        ShowGridPositionList(redList, GridVisualType.Red);
        ShowGridPositionList(whiteList, GridVisualType.White);
    }

    #endregion

    private void UpdateGridVisual()
    {
        HideAllGridPosition();
        
        BaseAction selectedAction = UnitActionSystem.Instance.GetSelectedAction();
        GridVisualType gridVisualType = GridVisualType.White;
        List<GridPosition> commonGrid = new List<GridPosition>();

        if (selectedAction == null)
        {
            commonGrid = UnitCommonGetValidactionGridPositionList<CommandMoveAction>();
            gridVisualType = GridVisualType.White;
        }

        if (commonGrid.Count == 0)
        {
            //Debug.Log("유효 그리드가 없습니다.");
            return;
        }

        commonGrid = FilterGridReservation(commonGrid);

        ShowGridPositionList(commonGrid, gridVisualType);
    }

    // 특정 액션을 가진 유닛들의 공통된 그리드 리스트 추출하기
    private List<GridPosition> UnitCommonGetValidactionGridPositionList<TAction>() where TAction : BaseAction
    {
        // 제네렉 액션을 가진 유닛만 필터 거치기
        var filterList = UnitActionSystem.Instance
            .FilterUnitsWithAction<TAction>(UnitActionSystem.Instance.m_SelectedObjects.ToList());

        // 유닛이 0명이면 공통 그리드 없음
        if (filterList.Count == 0)
            return new List<GridPosition>();

        // 모든 유닛의 Grid 리스트를 교집합으로 축소
        var common = filterList
            .Select(pair => pair.action.GetValidActionGridPositionList())
            .Aggregate((prev, next) => prev.Intersect(next).ToList());

        return common;
    }

    // 예약 그리드를 파란 색으로 변경
    private List<GridPosition> FilterGridReservation(List<GridPosition> list)
    {
        if (!m_isShowReservationGrid)
            return list;

        // 예약된 그리드를 flatten 해서 하나의 List<GridPosition>으로
        var reservedGrids = list
            .GroupBy(x => x.floor)
            .SelectMany(g => LevelGrid.Instance.GetReserveGridPositions(g.Key, true))
            .ToList();

        // 예약된 위치는 파란색으로 표시
        ShowGridPositionList(reservedGrids, GridVisualType.Blue);

        // 예약되지 않은 위치만 반환
        return list.Except(reservedGrids).ToList();
    }


    private Material GetGridVisualTypeMaterial(GridVisualType gridVisualType)
    {
        foreach (GridVisualTypeMaterial gridVisualTypeMaterial in gridVisualTypeMaterialList)
        {


            if (gridVisualTypeMaterial.gridVisualType == gridVisualType)
            {
                return gridVisualTypeMaterial.material;
            }
        }

        Debug.LogError("Could not find GridVisualTypeMaterial for GridVisualType " + gridVisualType);
        return null;
    }

    // 좌표별 visual 가져오기
    public GridSystemVisualSingle GetVisual(int x, int z, int floor)
    {
        return _floorVisuals[floor][x, z];
    }

    // 특정 층 전체 visual 가져오기
    public GridSystemVisualSingle[,] GetFloorVisuals(int floor)
    {
        return _floorVisuals[floor];
    }

    // 특정 층을 1차원 리스트로 변환해서 쓰고 싶으면
    public List<GridSystemVisualSingle> GetFloorVisualsAsList(int floor)
    {
        var list = new List<GridSystemVisualSingle>();
        var grid = _floorVisuals[floor];
        foreach (var visual in grid)
            list.Add(visual);

        return list;
    }


}