using RootMotion.FinalIK;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Define;
using static UnityEngine.UI.CanvasScaler;

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


    #endregion

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"There's more than one {name!}");
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


            _floorVisuals[floor] = gridArray;
        }

        Managers.Command.OnSelectedActionChanged += (s, e) => UpdateGridVisual();
        Managers.Selection.OnSelectionChanged += (s, e) => UpdateGridVisual();

        //// Building Place
        //if(GridBuildingSystem.Instance != null)
        //{
        //    GridBuildingSystem.Instance.OnObjectPlacedCancel += (s, e) => HideAllGridPosition();
        //    GridBuildingSystem.Instance.OnObjectPlaced += (s, e) => HideAllGridPosition();

        //    // 최적화 필요
        //    GridBuildingSystem.Instance.OnSelectedChanged += (s, e) => UpdateGridPositionPlace();
        //    GridBuildingSystem.Instance.OnRotateObject += (s, e) => UpdateGridPositionPlace();
        //}
        if(CameraController.Instance != null)
            CameraController.Instance.OnChangeLookFloor += (s, e) => UpdateGridPositionPlace();

        MouseWorld.Instance.OnMousePositionChanged += (s, e) => UpdateGridPositionPlace();

        UpdateGridVisual();
    }

    #region Grid Color

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
            .Where(grid => LevelGrid.Instance.IsGridPositionCheckType(grid, E_GridCheckType.Reserve));

        // 예약된 위치는 파란색으로 표시
        ShowGridPositionList(reservedGrids, E_GridVisualType_Color.Blue, E_GridVisualType_Intensity.Medium);

        // 예약되지 않은 위치만 반환
        return list.Except(reservedGrids).ToList();
    }


    #endregion

    #region Hide And Show Grid
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
        (IEnumerable<GridPosition> gridPositionList, 
        E_GridVisualType_Color gridVisualType,
        E_GridVisualType_Intensity intensity)
    {
        if (gridPositionList == null || gridPositionList.Count() == 0)
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

    #endregion

    /// <summary>
    /// 이벤트 발생 시 Grid Visual을 갱신한다.
    /// </summary>
    public void UpdateGridVisual_Event(object sender, GameEntity e)
    {
        //if (!UnitActionSystem.Instance.IsSelectedObject(e))
        //    return;

        UpdateGridVisual();
    }

    /// <summary>
    /// 전체 Grid Visual을 갱신.
    /// 
    /// - 오브젝트 배치 상태 체크
    /// - 기존 시각화 초기화
    /// - 선택된 액션 여부에 따라 이동/공격 그리드 구성
    /// - 예약된 그리드 필터링
    /// - 실제 그리드 색상 적용
    /// </summary>
    private void UpdateGridVisual()
    {
        // 현재 배치중인 오브젝트가 없으면 건너 띔
        if (GridBuildingSystem.Instance?.m_PlacedObject != null)
            return;

        // 전체 초기화
        HideAllGridPosition();

        BaseAction selectedAction = Managers.Command.m_SelectAction;

        // 액션 선택이 없을 때 = "유닛 공통 이동/공격 범위" 표시 모드
        if (selectedAction == null)
        {
            GetCommonAttackGridFromUnits<CommandMoveAction>();
            GetCommonAttackGridFromUnits<CombatAction>();
        }
    }

    /// <summary>
    /// 특정 액션(TAction)을 가진 유닛들 중에서,
    /// condition(선택) 조건을 만족하는 유닛들의
    /// 공통된 GridPosition 집합을 반환.
    /// 
    /// gridSelector는 각 유닛의 Grid 리스트를 가져오는 함수.
    /// </summary>
    private void GetCommonAttackGridFromUnits<TAction>() 
        where TAction : BaseAction
    {
        // 특정 TAction만 가진 객체 필터링
        var filter =
            Managers.Command.FilterUnitsWithAction<TAction, GameEntity>();


        if (typeof(TAction) == typeof(CommandMoveAction))
        {
            HashSet<GridPosition> commonRange = null;

            // Move 전용 처리
            foreach (var (unit, action) in filter)
            {
                var validList = action.GetValidActionGridPositionList();

                // 첫 번째 유닛 초기화
                if (commonRange == null)
                    commonRange = Enumerable.ToHashSet(validList);
                // 교집합
                else
                    commonRange.IntersectWith(validList);
            }

            // 시각화
            ShowGridPositionList(commonRange, E_GridVisualType_Color.White, E_GridVisualType_Intensity.Medium);
        }
        else if (typeof(TAction) == typeof(CombatAction))
        {
            HashSet<GridPosition> rangeList = null;
            HashSet<GridPosition> targetList = null;

            // Move 전용 처리
            foreach (var (unit, action) in filter)
            {
                // 필터링
                if (unit.GetAction<CombatAction>().m_ThisTimeAttack == null)
                    continue;

                var filterGrid = unit.GetAction<CombatAction>().m_ThisTimeAttack.GetAttackGridPositions(unit, unit.m_Target);

                // 첫 번째 유닛 초기화
                if (rangeList == null)
                    rangeList = Enumerable.ToHashSet(filterGrid.attackRangeGridList);
                // 교집합
                else
                    rangeList.IntersectWith(filterGrid.attackRangeGridList);

                // 첫 번째 유닛 초기화
                if (targetList == null)
                    targetList = Enumerable.ToHashSet(filterGrid.targetGridList);
                // 교집합
                else
                    targetList.IntersectWith(filterGrid.targetGridList);
            }

            // 시각화
            ShowGridPositionList(rangeList, E_GridVisualType_Color.Yellow, E_GridVisualType_Intensity.Light);
            ShowGridPositionList(targetList, E_GridVisualType_Color.Red, E_GridVisualType_Intensity.Medium);
        }
    }



    #region Place GameEntity (그리드 배치)

    // 건물 배치할 때 보여주는 용도
    public void UpdateGridPositionPlace()
    {
        // 현재 배치중인 오브젝트가 없으면 건너 띔
        if (GridBuildingSystem.Instance.m_PlacedObject == null)
            return;

        var buildingSystem = GridBuildingSystem.Instance;
        var levelGrid = LevelGrid.Instance;
        var camera = CameraController.Instance;
        int currentFloor = camera.m_CurrentLookFloor;

        // 2️ 현재 층의 모든 Grid 상태 가져오기
        var walkableGrids = levelGrid.GetFloorAndTypeGridPositions(currentFloor, E_GridCheckType.Walkable);
        var obstacleGrids = levelGrid.GetFloorAndTypeGridPositions(currentFloor, E_GridCheckType.Obstacle);
        var reservedGrids = levelGrid.GetFloorAndTypeGridPositions(currentFloor, E_GridCheckType.Reserve);
        var unitGrids = levelGrid.GetFloorAndTypeGridPositions(currentFloor, E_GridCheckType.GameEntity);
        var voidGrids = levelGrid.GetFloorAndTypeGridPositions(currentFloor, E_GridCheckType.Void);

        // 3️ 오브젝트의 현재 방향 기준 오프셋 좌표
        var objectOffsets = GridBuildingSystem.Instance.GetPlacedObject().GetGridPositionListAtCurrentDir();

        // 4️ 설치 불가능 지역(장애물, 예약, 유닛)
        HashSet<GridPosition> blockedGrids = new();
        blockedGrids.UnionWith(obstacleGrids);
        blockedGrids.UnionWith(reservedGrids);
        blockedGrids.UnionWith(unitGrids);

        // 5️ 충돌 예측 (설치 불가능 지역 주변 계산)
        foreach (var npos in blockedGrids.ToList())
        {
            var affected = objectOffsets
                .Select(offset => npos + offset.ReverseSign())
                .Where(p => levelGrid.IsValidGridPosition(p));

            blockedGrids.UnionWith(affected);
        }


        // 6️ 장애물 및 비활성(GridType.Void) 위치는 시각화 제외
        blockedGrids.ExceptWith(obstacleGrids);
        blockedGrids.ExceptWith(voidGrids);

        // 7️ 설치 가능한 영역 계산
        HashSet<GridPosition> placeableGrids = Enumerable.ToHashSet(walkableGrids.Where(p => !blockedGrids.Contains(p)));

        // 8️⃣ 마우스 위치 기준 오브젝트 배치 영역
        var mousePosition = MouseWorld.Instance.GetGridPosition();
        var previewGrids = buildingSystem.GetPlacedObject().GetGridPositionListAtSelectPosition(mousePosition);

        if (previewGrids.All(p => levelGrid.IsValidGridPosition(p) && !obstacleGrids.Contains(p)))
        {
            // 8-1️ 배치 가능한 경우: 초록색
            ShowGridPositionList(previewGrids, E_GridVisualType_Color.Green, E_GridVisualType_Intensity.Medium);

            // 8-2️ 배치 불가능 겹침(경고) 영역: 노란색
            var warningGrids = previewGrids
                .Where(p => blockedGrids.Contains(p))
                .ToList();
            if (warningGrids.Count > 0)
                ShowGridPositionList(warningGrids, E_GridVisualType_Color.Yellow, E_GridVisualType_Intensity.Medium);

            // 배치된 오브젝트 영역은 일반 흰색/빨강 영역에서 제외
            placeableGrids.ExceptWith(previewGrids);
            blockedGrids.ExceptWith(previewGrids);
        }

        // 9️ 시각화 표시
        ShowGridPositionList(blockedGrids, E_GridVisualType_Color.Red, E_GridVisualType_Intensity.Medium);
        ShowGridPositionList(placeableGrids, E_GridVisualType_Color.White, E_GridVisualType_Intensity.Medium);
    }

    #endregion

    #region Get

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

    #endregion
}