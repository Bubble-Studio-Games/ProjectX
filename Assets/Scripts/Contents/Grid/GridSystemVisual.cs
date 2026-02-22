using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;


[Serializable]
public struct GridVisualTypeMaterial
{
    public E_GridVisualType_Color gridVisualType;
    public Material material;
    [Range(0, 100)] public int UpIntensity;
    [Range(0, 100)] public int DownIntensity;
}

[EditorShowInfo(@"
역할: 그리드 타일을 실제로 그려주는 렌더러(표시/숨김/색상/강도)
")]
public class GridSystemVisual : MonoBehaviour
{
    [Header("GridVisualColor")]
    [SerializeField] private Transform gridSystemVisualSinglePrefab;
    [SerializeField] public List<GridVisualTypeMaterial> gridVisualTypeMaterialList;

    public Dictionary<E_GridVisualType_Color, Dictionary<E_GridVisualType_Intensity, Material>> _materialCache;
    private readonly Dictionary<int, GridSystemVisualSingle[,]> _floorVisuals = new();

    // 현재 상태 스냅샷
    public int CurrentFloor { get; private set; }

    // ✅ 최적화: HashSet 재사용 (GC Alloc 방지)
    private HashSet<GridPosition> _occupiedCache = new();
    private HashSet<GridPosition> _obstacleCache = new();

    #region Unity Lifecycle & Init

    private void Awake()
    {
        InitializeMaterialCache();
        Managers.SceneServices.Register(this);
    }

    private void Start()
    {
        Init(Managers.Grid.Width, Managers.Grid.Height, Managers.Grid.CellSize, Managers.Grid.FloorAmount);

        // 초기화
        HideAll();
    }

    void OnEnable() 
    { 
        Managers.Grid.OnRequestVisualRefresh += Refresh;

        // 오브젝트가 선택 되었으면 다시 그려!
        Managers.Selection.OnSelectionChanged += Managers.Grid.RequestVisualRefresh;
    }
    void OnDisable()
    {
        Managers.Grid.OnRequestVisualRefresh -= Refresh;

        Managers.Selection.OnSelectionChanged -= Managers.Grid.RequestVisualRefresh;
    }

    private void Init(int width, int height, float cellSize, int floorAmount)
    {
        for (int floor = 0; floor < floorAmount; floor++)
        {
            var gridArray = new GridSystemVisualSingle[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    GridPosition gp = new GridPosition(x, z, floor);

                    Transform t = Instantiate(
                        gridSystemVisualSinglePrefab,
                        Managers.Grid.GetWorldPosition(gp),
                        Quaternion.identity,
                        transform);

                    gridArray[x, z] = t.GetComponent<GridSystemVisualSingle>();
                }
            }

            _floorVisuals[floor] = gridArray;
        }
    }

    #endregion

    // Material cache
    private void InitializeMaterialCache()
    {
        _materialCache = new Dictionary<E_GridVisualType_Color, Dictionary<E_GridVisualType_Intensity, Material>>();

        foreach (var item in gridVisualTypeMaterialList)
        {
            var colorType = item.gridVisualType;
            if (!_materialCache.ContainsKey(colorType))
                _materialCache[colorType] = new Dictionary<E_GridVisualType_Intensity, Material>();

            _materialCache[colorType][E_GridVisualType_Intensity.Medium] = item.material;

            Material lightMat = Util.ColorUtil.AdjustMaterialHSV(Instantiate(item.material), 2, -item.DownIntensity);
            Material strongMat = Util.ColorUtil.AdjustMaterialHSV(Instantiate(item.material), 2, item.UpIntensity);

            _materialCache[colorType][E_GridVisualType_Intensity.Light] = lightMat;
            _materialCache[colorType][E_GridVisualType_Intensity.Strong] = strongMat;
        }
    }

    private Material GetGridVisualTypeMaterial(E_GridVisualType_Color gridVisualType, E_GridVisualType_Intensity intensity)
    {
        if (_materialCache.TryGetValue(gridVisualType, out var intensityDict))
        {
            if (intensityDict.TryGetValue(intensity, out var mat))
                return mat;

            return intensityDict[E_GridVisualType_Intensity.Medium];
        }

        Debug.LogError($"❌ No material cache for {gridVisualType}");
        return null;
    }

    // Visualizer
    public void HideAll()
    {
        foreach (var floorPair in _floorVisuals)
        {
            var gridArray = floorPair.Value;
            int w = gridArray.GetLength(0);
            int h = gridArray.GetLength(1);

            for (int x = 0; x < w; x++)
                for (int z = 0; z < h; z++)
                    gridArray[x, z].Hide();
        }
    }

    public void Show(IEnumerable<GridPosition> grids, E_GridVisualType_Color color, E_GridVisualType_Intensity intensity)
    {
        if (grids == null) return;

        // IEnumerable multiple enumeration 방지
        var list = (grids as IList<GridPosition>) ?? grids.ToList();
        if (list.Count == 0) return;

        var mat = GetGridVisualTypeMaterial(color, intensity);

        foreach (var gp in list)
        {
            if (_floorVisuals.TryGetValue(gp.floor, out var gridArray))
                gridArray[gp.x, gp.z].Show(mat);
        }
    }

    #region Logic & Rendering

    public void Refresh()
    {
        // 배치 중이면 배치 모드를 보여준다.
        if (Managers.Building.IsSetuping)
            UpdateGridPositionPlace(CurrentFloor, Util.Mouse.GetMouseWorldGridPosition());
        // 오브젝트를 선택한 상태면 해당 오브젝트의 행동 반경(이동, 공격 범위 등)을 보여준다.
        else if (Managers.Selection.SelectedUnits.Count > 0)
            UpdateGridVisual();
        // 그 외에는 아무것도 보여주지 않는다.
        else
            HideAll();
    }

    #region [Update Logic] 일반 액션 범위 표시

    /// <summary>
    /// 선택된 유닛들의 공통 액션 범위를 계산하여 그리드에 표시합니다.
    /// </summary>
    /// <param name="actionType">BaseAction을 상속받은 클래스 타입</param>

    #endregion

    // 1. 일반 모드(오브젝트 선택 후 직접 명령 반경 그려주기) 렌더링
    private void UpdateGridVisual()
    {
        // 초기화
        HideAll();

        // 선택된 오브젝트들의 특정 Action(행동) 범위 그리기
        GetCommonAttackGridFromUnits<CommandMoveAction>();
        GetCommonAttackGridFromUnits<CombatAction>();

        void GetCommonAttackGridFromUnits<TAction>() where TAction : BaseAction
        {
            var filter = Util.FilterGameEntityHasAction<TAction, GameEntity>(Managers.Selection.SelectedUnitsT<GameEntity>());

            if (typeof(TAction) == typeof(CommandMoveAction))
            {
                HashSet<GridPosition> commonRange = null;

                foreach (var (unit, action) in filter)
                {
                    var validList = action.GetValidActionGridPositionList();
                    commonRange ??= validList.ToHashSet();
                    commonRange.IntersectWith(validList);
                }

                Show(FilterGridReservation(commonRange ?? Enumerable.Empty<GridPosition>()), E_GridVisualType_Color.White, E_GridVisualType_Intensity.Medium);
            }
            else if (typeof(TAction) == typeof(CombatAction))
            {
                HashSet<GridPosition> rangeList = null;
                HashSet<GridPosition> targetList = null;

                foreach (var (unit, action) in filter)
                {
                    var data = unit.GetAction<CombatAction>().m_ThisTimeAttack;

                    if (data == null)
                        continue;

                    var fg = Managers.Game.AttackPattern(data).GetAttackGridPositions(unit, unit.m_Target, data);

                    rangeList ??= fg.attackRangeGridList.ToHashSet();
                    rangeList.IntersectWith(fg.attackRangeGridList);

                    targetList ??= fg.targetGridList.ToHashSet();
                    targetList.IntersectWith(fg.targetGridList);
                }

                Show(FilterGridReservation(rangeList ?? Enumerable.Empty<GridPosition>()), E_GridVisualType_Color.Yellow, E_GridVisualType_Intensity.Light);
                Show(FilterGridReservation(targetList ?? Enumerable.Empty<GridPosition>()), E_GridVisualType_Color.Red, E_GridVisualType_Intensity.Medium);
            }
        }

        IEnumerable<GridPosition> FilterGridReservation(IEnumerable<GridPosition> list)
        {
            var reserved = list.Where(g => Managers.Grid.GetEntityCellType(g) == E_EntityCellType.Reserve).ToList();
            Show(reserved, E_GridVisualType_Color.Blue, E_GridVisualType_Intensity.Medium);
            return list.Except(reserved);
        }
    }

    // 2. 배치 모드 렌더링 (최적화 적용)
    private void UpdateGridPositionPlace(int floor, GridPosition pivotGrid)
    {
        var terrainGroups = Managers.Grid.LevelGrid.GetGroupedPositionsByFloor(floor);
        var entityGroups = Managers.Grid.EntityGrid.GetGroupedPositionsByFloor(floor);
        var placed = Managers.Building.PlacingObject;

        // 캐시 업데이트 (새 리스트 할당 대신 Clear 후 추가하여 GC 최소화)
        _occupiedCache.Clear();
        _obstacleCache.Clear();

        // Terrain
        foreach (var p in terrainGroups[E_TerrainCellType.Obstacle]) _obstacleCache.Add(p);
        foreach (var p in terrainGroups[E_TerrainCellType.Void]) _obstacleCache.Add(p);

        // Entity
        foreach (var p in entityGroups[E_EntityCellType.GameEntity]) _occupiedCache.Add(p);
        foreach (var p in entityGroups[E_EntityCellType.Reserve]) _occupiedCache.Add(p);

        // Place Object
        var preview = placed.GetGridPositionListAtSelectPosition(pivotGrid);

        HideAll();

        // 지형 가이드라인 
        Show(terrainGroups[E_TerrainCellType.Walkable], E_GridVisualType_Color.White, E_GridVisualType_Intensity.Light);

        bool isInvalid = false;
        bool isBlocked = false;

        foreach (var p in preview)
        {
            if (!Managers.Grid.IsValidGridPosition(p) || _obstacleCache.Contains(p)) { isInvalid = true; break; }
            if (_occupiedCache.Contains(p)) isBlocked = true;
        }

        // 프리뷰 색상 결정
        E_GridVisualType_Color color = isInvalid ? E_GridVisualType_Color.Red : (isBlocked ? E_GridVisualType_Color.Yellow : E_GridVisualType_Color.Green);
        Show(preview, color, E_GridVisualType_Intensity.Medium);

        // 현재 유닛 위치 강조
        Show(entityGroups[E_EntityCellType.GameEntity], E_GridVisualType_Color.Red, E_GridVisualType_Intensity.Medium);
    }

    #endregion
}
