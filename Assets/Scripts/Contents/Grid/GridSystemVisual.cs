using RootMotion.FinalIK;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class GridSystemVisual : MonoBehaviour
{
    #region field

    public bool m_isShowReservationGrid;

    public static GridSystemVisual Instance { get; private set; }

    [Serializable]
    public struct GridVisualTypeMaterial
    {
        public E_GridVisualType_Color gridVisualType;
        public Material material;
        [Range(0, 100)] public int UpIntensity;
        [Range(0, 100)] public int DownIntensity;
    }

    [Header("GridVisualColor")]
    [SerializeField] private Transform gridSystemVisualSinglePrefab;
    [SerializeField] public List<GridVisualTypeMaterial> gridVisualTypeMaterialList;
    // 새로운 캐시 딕셔너리
    public Dictionary<E_GridVisualType_Color, Dictionary<E_GridVisualType_Intensity, Material>> _materialCache;


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

    #endregion

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one GridSystemVisual! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // ✅ 새 캐시 초기화
        InitializeMaterialCache();
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

        // Level Grid
        LevelGrid.Instance.OnChangeGrid += OnLevelGridChanged;

        UpdateGridVisual();
    }


    #region Color

    /// <summary>
    /// 컬러별, 강도별 머티리얼을 캐싱합니다.
    /// </summary>
    private void InitializeMaterialCache()
    {
        _materialCache = new Dictionary<E_GridVisualType_Color, Dictionary<E_GridVisualType_Intensity, Material>>();

        foreach (var item in gridVisualTypeMaterialList)
        {
            var colorType = item.gridVisualType;
            if (!_materialCache.ContainsKey(colorType))
                _materialCache[colorType] = new Dictionary<E_GridVisualType_Intensity, Material>();

            // 기본 머티리얼 (Medium)
            _materialCache[colorType][E_GridVisualType_Intensity.Medium] = item.material;

            // Light / Strong 버전 사전 생성
            Material lightMat = Util.AdjustMaterialHSV(
                Instantiate(item.material),
                2, // Value
                -item.DownIntensity);

            Material strongMat = Util.AdjustMaterialHSV(
                Instantiate(item.material),
                2,
                item.UpIntensity);

            _materialCache[colorType][E_GridVisualType_Intensity.Light] = lightMat;
            _materialCache[colorType][E_GridVisualType_Intensity.Strong] = strongMat;
        }
    }

    /// <summary>
    /// 캐시된 머티리얼을 즉시 반환.
    /// </summary>
    private Material GetGridVisualTypeMaterial(E_GridVisualType_Color gridVisualType, E_GridVisualType_Intensity intensity)
    {
        if (_materialCache.TryGetValue(gridVisualType, out var intensityDict))
        {
            if (intensityDict.TryGetValue(intensity, out var mat))
                return mat;

            // fallback
            return intensityDict[E_GridVisualType_Intensity.Medium];
        }

        Debug.LogError($"❌ No material cache for {gridVisualType}");
        return null;
    }

    #endregion


    /// <summary>
    /// 모든 층의 Grid Visual을 숨긴다.
    /// </summary>
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


    /// <summary>
    /// 지정한 층의 Grid Visual을 숨긴다.
    /// </summary>
    /// <param name="floor">층 인덱스</param>
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

    /// <summary>
    /// 전달된 GridPosition 리스트를 주어진 색(Material)으로 시각화한다.
    /// </summary>
    /// <param name="gridPositionList">표시할 그리드 목록</param>
    /// <param name="gridVisualType">적용할 시각 타입 (색상/머티리얼)</param>
    public void ShowGridPositionList
        (ICollection<GridPosition> gridPositionList, 
        E_GridVisualType_Color gridVisualType,
        E_GridVisualType_Intensity intensity)
    {
        if (gridPositionList == null || gridPositionList.Count == 0)
            return;

        var material = GetGridVisualTypeMaterial(gridVisualType, intensity);

        foreach (GridPosition gridPosition in gridPositionList)
        {
            if (_floorVisuals.TryGetValue(gridPosition.floor, out var gridArray))
            {
                gridArray[gridPosition.x, gridPosition.z].Show(material);
            }
        }
    }

    /// <summary>
    /// 이벤트 발생 시 Grid Visual을 갱신한다.
    /// </summary>
    public void UpdateGridVisual_Event(object sender, GameEntity e)
    {
        if (!UnitActionSystem.Instance.IsSelectedObject(e))
            return;

        UpdateGridVisual();
    }

    #region Select Setup Object

    /*
        📦 Grid Visual 업데이트 타이밍 요약

        1️⃣ 오브젝트 선택 변경 (OnSelectedChanged)
            - 플레이어가 새로운 건설 오브젝트(카드)를 선택하거나, 기존 것을 해제했을 때.
            - 선택된 오브젝트의 GridOffset(Min/Max, Floor)이 바뀌면 
              해당 층의 전체 배치 가능 영역을 다시 계산하고 시각화 갱신.

        2️⃣ 오브젝트 회전 (OnRotateObject)
            - 현재 손에 든 오브젝트의 회전(Dir)이 변경될 때.
            - 회전에 따라 차지하는 셀 범위(XZ Offset)가 바뀌므로
              배치 가능/불가 Grid를 다시 계산하여 해당 층의 시각 갱신.

        3️⃣ 오브젝트 배치 (OnObjectPlaced)
            - 실제 오브젝트가 그리드에 배치되면,
              LevelGrid 상태(Occupied, Reserved)가 변경되므로
              해당 GridPosition만 부분 갱신 (예약 상태 반영).

        4️⃣ 오브젝트 배치 취소 (OnObjectPlacedCancel)
            - 배치를 취소했거나 선택을 해제했을 때.
            - 시각화된 Grid와 캐시를 초기화하고, 모든 표시를 숨김.

        5️⃣ 유닛 이동 또는 Reserve 상태 변경 (OnLevelGridChanged)
            - LevelGrid 상에서 유닛이 이동하거나, 예약/점유 상태가 바뀌었을 때.
            - 변경된 GridPosition만 부분 갱신 (특정 셀만 업데이트).

        6️⃣ 다른 오브젝트 이동, 사망 등 상태 변화
            - 배치된 다른 오브젝트가 제거(사망, 해제)되면
              LevelGrid에서 해당 셀의 Occupied 플래그가 해제됨.
            - 해당 셀을 포함한 영역만 다시 갱신.

        7️⃣ 층 전환 (OnFloorCacheClear)
            - 카메라 또는 플레이어 시점이 다른 Floor로 이동할 때.
            - 해당 층 캐시를 초기화하고, 보이는 층의 Grid Visual만 갱신.

        ➕ 요약 정리:
           - 오브젝트 선택/회전/교체 → 전체 갱신 (해당 층)
           - 유닛/오브젝트 이동, 예약 변경 → 부분 갱신 (해당 셀)
           - 층 전환 → 캐시 초기화 + 해당 층 전체 갱신
    */


    /// <summary>
    /// 오브젝트 회전 시 호출됨.
    /// 배치 가능한 위치 Offset을 재계산하고 배치 가능/불가 영역을 갱신한다.
    /// </summary>
    private void OnObjectRotateUpdate(object sender, E_SetupObjectOffsetChange e)
    {
        if (e == E_SetupObjectOffsetChange.XZOffset)
            OnlyReCalulateOffsets();
        else if (e == E_SetupObjectOffsetChange.None)
            return;

        //m_IsFloorClearCache = true;
        UpdateCanPlacedGrid(sender, null);
    }

    /// <summary>
    /// 현재 배치 중인 오브젝트의 Offset만 재계산한다.
    /// (XZ 회전 등, 배치 가능한 셀 좌표 보정)
    /// </summary>
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

    /// <summary>
    /// 배치 중인 오브젝트가 변경되었을 때 호출됨.
    /// Offset 변화 유형에 따라 캐시 초기화 또는 재계산 수행.
    /// </summary>
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

    /// <summary>
    /// 카메라 층 전환 시 호출됨.
    /// 현재 바라보는 층의 캐시를 초기화하고 배치 가능 영역 재계산.
    /// </summary>
    private void OnFloorCacheClear(object sender, bool e)
    {
        m_IsFloorClearCache = e;
        UpdateCanPlacedGrid(sender, null);
    }

    /// <summary>
    /// LevelGrid 내부의 변화(유닛 이동, 배치 등) 감지 시 호출됨.
    /// 배치 가능/불가 Grid 상태를 갱신.
    /// </summary>
    private void OnLevelGridChanged(object sender, LevelGrid.OnChangeGridAgrs e)
    {
        UpdateCanPlacedGrid(sender, e);
    }

    /// <summary>
    /// 배치 취소 또는 완료 시 캐시와 표시된 Grid를 초기화.
    /// </summary>
    private void ClearPlace()
    {
        CacheClear();
        HideAllGridPosition();
    }

    /// <summary>
    /// 캐시 초기화.
    /// 각 층의 검사 여부를 false로 리셋하고 이전 상태를 백업 초기화.
    /// </summary>
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

    /// <summary>
    /// 현재 선택된 배치 오브젝트의 Grid 배치 가능 여부를 계산하고
    /// 배치 불가(빨강)/배치 가능(흰색) 영역을 시각화.
    /// </summary>
    /// <param name="sender">이벤트 호출자</param>
    /// <param name="e">그리드 변경 이벤트 정보</param>
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
        ShowGridPositionList(redList, E_GridVisualType_Color.Red, E_GridVisualType_Intensity.Light);
        ShowGridPositionList(whiteList, E_GridVisualType_Color.White, E_GridVisualType_Intensity.Medium);
    }

    #endregion

    /// <summary>
    /// 전체 Grid Visual을 갱신.
    /// 선택된 액션이나 유닛의 상태에 따라 이동/공격 범위를 갱신하고 표시.
    /// </summary>
    private void UpdateGridVisual()
    {
        // 전체 초기화
        HideAllGridPosition();
        
        BaseAction selectedAction = UnitActionSystem.Instance.GetSelectedAction();
        HashSet<GridPosition> commonGrid = new();
        HashSet<GridPosition> ActionGrid = new();

        // 현재 선택된 커맨드 그리드가 업을 때
        if (selectedAction == null)
        {
            // 선택한 유닛 중 일부가 전투 중이라면 공격 그리드 그리기 이는 이동 그리드 위에 덧 씌운다.
            commonGrid = UnitCommonGetValidactionGridPositionList<CommandMoveAction>
                (obj => obj.GetAction<CommandMoveAction>().GetValidActionGridPositionList());

            ActionGrid = UnitCommonGetValidactionGridPositionList<CombatAction>( 
                obj => Managers.Game.GetAttackPatternPosition(obj, obj.m_Target, obj.GetAction<CombatAction>().m_ThisTimeAttack),
                unit => unit.GetAction<CombatAction>().m_ThisTimeAttack != null);

        }

        if (commonGrid == null || commonGrid.Count == 0)
        {
            //Debug.Log("유효 그리드가 없습니다.");
            return;
        }

        commonGrid = Enumerable.ToHashSet(FilterGridReservation(commonGrid));

        ShowGridPositionList(commonGrid, E_GridVisualType_Color.White, E_GridVisualType_Intensity.Medium);
        ShowGridPositionList(ActionGrid, E_GridVisualType_Color.Red, E_GridVisualType_Intensity.Light);
    }

    /// <summary>
    /// 특정 액션(TAction)을 가진 유닛들의 공통된 유효 Grid 리스트를 추출.
    /// gridSelector로 가져올 Grid 계산 함수를, conditionUnit으로 유닛 필터 조건을 지정 가능.
    /// </summary>
    /// <typeparam name="TAction">액션 타입</typeparam>
    /// <param name="gridSelector">각 유닛에서 GridPosition을 가져오는 함수</param>
    /// <param name="conditionUnit">선택적 유닛 필터 조건</param>
    /// <returns>공통 유효 GridPosition 집합</returns>
    private HashSet<GridPosition> UnitCommonGetValidactionGridPositionList<TAction>(
        Func<ControllableObject, IEnumerable<GridPosition>> gridSelector, 
        Func<ControllableObject, bool> conditionUnit = null) 
        where TAction : BaseAction
    {
        // 제네렉 액션을 가진 유닛만 필터 거치기
        var filterList = UnitActionSystem.Instance
            .FilterUnitsWithAction<TAction>(UnitActionSystem.Instance.m_SelectedObjects.ToList());

        if (conditionUnit != null)
        {
            filterList = filterList
                .Where(pair => conditionUnit(pair.unit))
                .ToList();
        }

        // 유닛이 0명이면 공통 그리드 없음
        if (filterList.Count == 0)
            return default;

        HashSet<GridPosition> gridPositions = new();
        foreach (var obj in filterList)
        {
            var grids = gridSelector(obj.unit);
            gridPositions.AddRange(grids);
        }

        return gridPositions;
    }

    /// <summary>
    /// 예약된 Grid를 별도 색상(파란색)으로 표시하고,
    /// 예약되지 않은 Grid만 반환.
    /// </summary>
    /// <param name="list">입력 Grid 리스트</param>
    /// <returns>예약되지 않은 Grid 리스트</returns>
    private IEnumerable<GridPosition> FilterGridReservation(IEnumerable<GridPosition> list)
    {
        if (!m_isShowReservationGrid)
            return list;

        // 예약된 그리드를 flatten 해서 하나의 List<GridPosition>으로
        var reservedGrids = list
            .GroupBy(x => x.floor)
            .SelectMany(g => LevelGrid.Instance.GetReserveGridPositions(g.Key, true))
            .ToList();

        // 예약된 위치는 파란색으로 표시
        ShowGridPositionList(reservedGrids, E_GridVisualType_Color.Blue, E_GridVisualType_Intensity.Medium);

        // 예약되지 않은 위치만 반환
        return list.Except(reservedGrids).ToList();
    }


    /// <summary>
    /// 특정 좌표(x, z, floor)의 Grid Visual 객체 반환.
    /// </summary>
    public GridSystemVisualSingle GetVisual(int x, int z, int floor)
    {
        return _floorVisuals[floor][x, z];
    }

    /// <summary>
    /// 특정 층 전체 Grid Visual 2차원 배열 반환.
    /// </summary>
    public GridSystemVisualSingle[,] GetFloorVisuals(int floor)
    {
        return _floorVisuals[floor];
    }

    /// <summary>
    /// 특정 층의 Grid Visual을 1차원 리스트로 변환.
    /// (일괄 처리나 순회 시 유용)
    /// </summary>
    public List<GridSystemVisualSingle> GetFloorVisualsAsList(int floor)
    {
        var list = new List<GridSystemVisualSingle>();
        var grid = _floorVisuals[floor];
        foreach (var visual in grid)
            list.Add(visual);

        return list;
    }


}