using UnityEngine;
using System;
using System.Collections.Generic;
using static Define;
using Data;
using System.Collections;

public static class NullServices
{
    public sealed class NullInputEvents : IInputEvents, INullServiceProxy<IInputEvents>
    {
        public static readonly NullInputEvents Instance = new();
        private NullInputEvents() { }


        public void Subscribe(E_InputEvent type, Action handler)
        {
        }

        public void Unsubscribe(E_InputEvent type, Action handler)
        {
        }

        public void Invoke(E_InputEvent type)
        {
        }
        public void TransferTo(IInputEvents real) { }

    }


    public sealed class NullInputActionMapController : IInputActionMapController, INullServiceProxy<IInputActionMapController>
    {
        public static readonly NullInputActionMapController Instance = new();
        private NullInputActionMapController() { }

        private Action<string> _OnActionMapChanged;

        public event Action<string> OnActionMapChanged
        {
            add    => _OnActionMapChanged += value;
            remove => _OnActionMapChanged -= value;
        }

        public string CurrentActionMapGroup => "";
        public Nullable<E_InputActionMap> CurrentActionMapType => default(Nullable<E_InputActionMap>);

        public void PushActionMapGroup(E_InputActionMap mapType)
        {
        }

        public void PushActionMapGroup(string groupName)
        {
        }

        public void PopActionMapGroup()
        {
        }
        public void TransferTo(IInputActionMapController real)
        {
            if (_OnActionMapChanged != null) real.OnActionMapChanged += _OnActionMapChanged;
            _OnActionMapChanged = null;
        }

    }


    public sealed class NullCoroutineRunner : ICoroutineRunner, INullServiceProxy<ICoroutineRunner>
    {
        public static readonly NullCoroutineRunner Instance = new();
        private NullCoroutineRunner() { }


        public Coroutine Run(IEnumerator routine) => null;

        public void Stop(Coroutine coroutine)
        {
        }
        public void TransferTo(ICoroutineRunner real) { }

    }


    public sealed class NullUnitActionTickService : IUnitActionTickService, INullServiceProxy<IUnitActionTickService>
    {
        public static readonly NullUnitActionTickService Instance = new();
        private NullUnitActionTickService() { }

        private Action _OnUpdateActionTick;

        public event Action OnUpdateActionTick
        {
            add    => _OnUpdateActionTick += value;
            remove => _OnUpdateActionTick -= value;
        }

        public void TransferTo(IUnitActionTickService real)
        {
            if (_OnUpdateActionTick != null) real.OnUpdateActionTick += _OnUpdateActionTick;
            _OnUpdateActionTick = null;
        }

    }


    public sealed class NullDungeonCoreRegistry : IDungeonCoreRegistry, INullServiceProxy<IDungeonCoreRegistry>
    {
        public static readonly NullDungeonCoreRegistry Instance = new();
        private NullDungeonCoreRegistry() { }

        private Action _OnReady;

        public event Action OnReady
        {
            add    => _OnReady += value;
            remove => _OnReady -= value;
        }

        public IReadOnlyList<IDungeonCore> Cores => System.Array.Empty<IDungeonCore>();
        public bool IsReady => default(bool);
        public bool IsStageFailed => default(bool);

        public void Register(IDungeonCore core)
        {
        }

        public void Unregister(IDungeonCore core)
        {
        }

        public bool IsCore(GameEntity entity) => default(bool);
        public void TransferTo(IDungeonCoreRegistry real)
        {
            if (_OnReady != null) real.OnReady += _OnReady;
            _OnReady = null;
        }

    }


    public sealed class NullInventoryRead : IInventoryRead, INullServiceProxy<IInventoryRead>
    {
        public static readonly NullInventoryRead Instance = new();
        private NullInventoryRead() { }

        private Action<int> _DownJamChanged;

        public event Action<int> DownJamChanged
        {
            add    => _DownJamChanged += value;
            remove => _DownJamChanged -= value;
        }

        public int DownJamAmount => default(int);
        public IReadOnlyList<GameEntity> EnabledCards => System.Array.Empty<GameEntity>();
        public void TransferTo(IInventoryRead real)
        {
            if (_DownJamChanged != null) real.DownJamChanged += _DownJamChanged;
            _DownJamChanged = null;
        }

    }


    public sealed class NullInventoryWrite : IInventoryWrite, INullServiceProxy<IInventoryWrite>
    {
        public static readonly NullInventoryWrite Instance = new();
        private NullInventoryWrite() { }


        public void AddDownJam(int amount)
        {
        }
        public void TransferTo(IInventoryWrite real) { }

    }


    public sealed class NullBuildingCardUI : IBuildingCardUI, INullServiceProxy<IBuildingCardUI>
    {
        public static readonly NullBuildingCardUI Instance = new();
        private NullBuildingCardUI() { }

        public bool IsDrawing => default(bool);
        public RectTransform RectTransform => null;
        public BuildingCard ActiveCard { get; set; }

        public void AddCard(GameEntity addUnit, Vector3 worldPosition, bool isInit)
        {
        }

        public void RestoreSaveDatas(IEnumerable<BaseData> datas)
        {
        }

        public List<BaseData> CaptureSaveData() => new System.Collections.Generic.List<BaseData>();

        public void TrySummonEntity(BuildingCard card, GameEntity entity, Vector2 originalPos)
        {
        }
        public void TransferTo(IBuildingCardUI real) { }

    }


    public sealed class NullCameraInfoProvider : ICameraInfoProvider, INullServiceProxy<ICameraInfoProvider>
    {
        public static readonly NullCameraInfoProvider Instance = new();
        private NullCameraInfoProvider() { }

        public Vector3 Position => Vector3.zero;
        public Quaternion Rotation => Quaternion.identity;

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
        }
        public void TransferTo(ICameraInfoProvider real) { }

    }


    public sealed class NullCameraRig : ICameraRig, INullServiceProxy<ICameraRig>
    {
        public static readonly NullCameraRig Instance = new();
        private NullCameraRig() { }

        private Action<int> _OnChangeLookFloor;

        public event Action<int> OnChangeLookFloor
        {
            add    => _OnChangeLookFloor += value;
            remove => _OnChangeLookFloor -= value;
        }

        public int CurrentLookFloor => default(int);

        public float GetCameraHeight() => default(float);
        public void TransferTo(ICameraRig real)
        {
            if (_OnChangeLookFloor != null) real.OnChangeLookFloor += _OnChangeLookFloor;
            _OnChangeLookFloor = null;
        }

    }


    public sealed class NullCameraShakeSettings : ICameraShakeSettings, INullServiceProxy<ICameraShakeSettings>
    {
        public static readonly NullCameraShakeSettings Instance = new();
        private NullCameraShakeSettings() { }


        public void SetImpulseReactionDuration(float duration)
        {
        }
        public void TransferTo(ICameraShakeSettings real) { }

    }


    public sealed class NullGridVisualUpdateSource : IGridVisualUpdateSource, INullServiceProxy<IGridVisualUpdateSource>
    {
        public static readonly NullGridVisualUpdateSource Instance = new();
        private NullGridVisualUpdateSource() { }

        private Action _OnDirty;

        public event Action OnDirty
        {
            add    => _OnDirty += value;
            remove => _OnDirty -= value;
        }

        public bool IsPlacing => default(bool);
        public int CurrentFloor => default(int);
        public GridPosition MouseGridPosition => default(GridPosition);
        public Type SelectedActionType => null;

        public void DrawGridVisual()
        {
        }
        public void TransferTo(IGridVisualUpdateSource real)
        {
            if (_OnDirty != null) real.OnDirty += _OnDirty;
            _OnDirty = null;
        }

    }


    public sealed class NullGridPlacementVisualizer : IGridPlacementVisualizer, INullServiceProxy<IGridPlacementVisualizer>
    {
        public static readonly NullGridPlacementVisualizer Instance = new();
        private NullGridPlacementVisualizer() { }


        public void HideAll()
        {
        }

        public void Show(IEnumerable<GridPosition> grids, E_GridVisualType_Color color, E_GridVisualType_Intensity intensity)
        {
        }
        public void TransferTo(IGridPlacementVisualizer real) { }

    }


    public sealed class NullBuildPlacementService : IBuildPlacementService, INullServiceProxy<IBuildPlacementService>
    {
        public static readonly NullBuildPlacementService Instance = new();
        private NullBuildPlacementService() { }

        private Action<E_SetupObjectOffsetChange> _OnSelectedChanged;
        private Action<BuildPlacedEventArgs> _OnPlaced;
        private Action _OnCanceled;
        private Action<E_SetupObjectOffsetChange> _OnRotated;

        public event Action<E_SetupObjectOffsetChange> OnSelectedChanged
        {
            add    => _OnSelectedChanged += value;
            remove => _OnSelectedChanged -= value;
        }
        public event Action<BuildPlacedEventArgs> OnPlaced
        {
            add    => _OnPlaced += value;
            remove => _OnPlaced -= value;
        }
        public event Action OnCanceled
        {
            add    => _OnCanceled += value;
            remove => _OnCanceled -= value;
        }
        public event Action<E_SetupObjectOffsetChange> OnRotated
        {
            add    => _OnRotated += value;
            remove => _OnRotated -= value;
        }

        public GameEntity Current => null;
        public Quaternion CurrentRotation => Quaternion.identity;

        public bool IsSetuping => false;

        public bool TryPlace() => default(bool);

        public void ChangeSelection(GameEntity toChangeObject, bool isInputNumberPad)
        {
        }
        public void TransferTo(IBuildPlacementService real)
        {
            if (_OnSelectedChanged != null) real.OnSelectedChanged += _OnSelectedChanged;
            _OnSelectedChanged = null;
            if (_OnPlaced != null) real.OnPlaced += _OnPlaced;
            _OnPlaced = null;
            if (_OnCanceled != null) real.OnCanceled += _OnCanceled;
            _OnCanceled = null;
            if (_OnRotated != null) real.OnRotated += _OnRotated;
            _OnRotated = null;
        }

        public void RotateSelectObject()
        {
        }
    }


    public sealed class NullCursor : ICursor, INullServiceProxy<ICursor>
    {
        public static readonly NullCursor Instance = new();
        private NullCursor() { }


        public GridPosition GetMouseWorldGridPosition() => default(GridPosition);

        public Vector3 GetMouseWorldPosition() => Vector3.zero;

        public Vector3 GetSnappedWorld(IGridQuery grid) => Vector3.zero;
        public void TransferTo(ICursor real) { }

    }


    public sealed class NullCursorEvents : ICursorEvents, INullServiceProxy<ICursorEvents>
    {
        public static readonly NullCursorEvents Instance = new();
        private NullCursorEvents() { }

        private Action<ValueTuple<GridPosition, GridPosition>> _OnMousePositionChanged;

        public event Action<ValueTuple<GridPosition, GridPosition>> OnMousePositionChanged
        {
            add    => _OnMousePositionChanged += value;
            remove => _OnMousePositionChanged -= value;
        }

        public void TransferTo(ICursorEvents real)
        {
            if (_OnMousePositionChanged != null) real.OnMousePositionChanged += _OnMousePositionChanged;
            _OnMousePositionChanged = null;
        }

    }


    public sealed class NullMouseClickHandler : IMouseClickHandler, INullServiceProxy<IMouseClickHandler>
    {
        public static readonly NullMouseClickHandler Instance = new();
        private NullMouseClickHandler() { }


        public void MouseDown(E_MouseClickType type)
        {
        }

        public void MouseUp(E_MouseClickType type)
        {
        }
        public void TransferTo(IMouseClickHandler real) { }

    }


    public sealed class NullCameraInput : ICameraInput, INullServiceProxy<ICameraInput>
    {
        public static readonly NullCameraInput Instance = new();
        private NullCameraInput() { }


        public Vector2 GetCameraMoveVector() => default(Vector2);
        public void TransferTo(ICameraInput real) { }

    }


    public sealed class NullInputQuery : IInputQuery, INullServiceProxy<IInputQuery>
    {
        public static readonly NullInputQuery Instance = new();
        private NullInputQuery() { }


        public bool IsActive(E_InputEvent evt) => default(bool);
        public void TransferTo(IInputQuery real) { }

    }


    public sealed class NullGridMutation : IGridMutation, INullServiceProxy<IGridMutation>
    {
        public static readonly NullGridMutation Instance = new();
        private NullGridMutation() { }


        public void SetCellType(GridPosition pos, E_GridCheckType type, GameEntity entity)
        {
        }

        public void SetCellType(IEnumerable<GridPosition> positions, E_GridCheckType type, GameEntity entity)
        {
        }
        public void TransferTo(IGridMutation real) { }

    }


    public sealed class NullGridQuery : IGridQuery, INullServiceProxy<IGridQuery>
    {
        public static readonly NullGridQuery Instance = new();
        private NullGridQuery() { }


        public int GetWidth() => default(int);

        public int GetHeight() => default(int);

        public int GetFloor(Vector3 worldPosition) => default(int);

        public float GetCellSize() => default(float);

        public int GetFloorAmount() => default(int);

        public List<GridPosition> GetFloorAndTypeGridPositions(int floor, E_GridCheckType type) => new System.Collections.Generic.List<GridPosition>();

        public Vector3 GetWorldPosition(GridPosition pos) => Vector3.zero;

        public GridPosition GetGridPosition(Vector3 worldPos) => default(GridPosition);

        public Vector3 GetWorldPositionNormalize(Vector3 worldPosition) => Vector3.zero;

        public float GetCurrentFloorHeight(Vector3 worldPosition) => default(float);

        public float GetCurrentFloorHeight(GridPosition gridPosition) => default(float);

        public float GetNextFloorHeight(GridPosition gridPosition) => default(float);

        public bool IsValidGridPosition(GridPosition gridPosition) => default(bool);

        public bool IsValidGridPosition(Vector3 worldPos) => default(bool);

        public E_GridCheckType GetCellType(GridPosition pos) => default(E_GridCheckType);

        public GameEntity GetCellEntity(GridPosition pos) => null;

        public bool IsGridPositionCheckType(GridPosition pos, params E_GridCheckType[] types) => default(bool);
        public void TransferTo(IGridQuery real) { }

    }


    public sealed class NullPathfinder : IPathfinder, INullServiceProxy<IPathfinder>
    {
        public static readonly NullPathfinder Instance = new();
        private NullPathfinder() { }


        public List<GridPosition> FindPath(GridPosition start, GridPosition end, out int len, params E_GridCheckType[] ignore)
        {
            len = default(int);
            return new System.Collections.Generic.List<GridPosition>();
        }

        public int GetPathLength(GridPosition startGridPosition, GridPosition endGridPosition, params E_GridCheckType[] ignoreGridtype) => default(int);

        public bool HasPath(GridPosition startGridPosition, GridPosition endGridPosition, params E_GridCheckType[] ignoreGridtype) => default(bool);

        public List<GridPosition> FindNearestCandidatePath(GridPosition start, IEnumerable<GridPosition> gridPositions, bool allowApproachWhenUnreachable) => new System.Collections.Generic.List<GridPosition>();
        public void TransferTo(IPathfinder real) { }

    }


    public sealed class NullUnitGridManager : IUnitGridManager, INullServiceProxy<IUnitGridManager>
    {
        public static readonly NullUnitGridManager Instance = new();
        private NullUnitGridManager() { }


        public void AddUnitAtGridPositions(IReadOnlyList<GridPosition> cells, GameEntity unit)
        {
        }

        public void RemoveUnitAtGridPositions(IReadOnlyList<GridPosition> cells, GameEntity unit)
        {
        }

        public void MoveUnit(GameEntity unit, IReadOnlyList<GridPosition> fromCells, IReadOnlyList<GridPosition> toCells)
        {
        }

        public IReadOnlyCollection<GameEntity> GetUnitsAt(GridPosition pos) => null;
        public void TransferTo(IUnitGridManager real) { }

    }


}
