using static Define;
using UnityEngine;
using System;


/// <summary>
/// 건물 배치 프리뷰(고스트) 표시 + 확정 시 Reserve 처리
/// 현재 선택된 배치 대상(Current) 을 보고, 프리뷰 오브젝트를 생성/파괴한다.
/// 마우스 월드 위치/스냅 위치를 받아서 프리뷰를 부드럽게 따라가게(Lerp) 만든다.
/// 배치 확정 이벤트(OnPlaced)가 오면:
/// 프리뷰를 실제 오브젝트로 “확정”하고(부모 해제, 레이어 변경)
/// IGridMutation.SetCellType(... Reserve ...)로 그 footprint를 예약 처리한다. 
/// BuildingGhost
/// 즉, 건설 로직을 판단하지 않고 “보여주기 + 확정 후 그리드 예약 반영”만 담당.
// </summary>
public class BuildingGhost2 : MonoBehaviour
{
    private GameEntity visual;
    [SerializeField] private float floatingHeight = 1f;
    public Vector3 m_PivotPosition { get; private set; }

    private GridBuildingManager _build;

    private void Start()
    {
        _build = Managers.Building;//.BuildPlacementService;

        RefreshVisual();
    }

    private void OnDestroy()
    {
        if (_build == null) return;
       //_build.OnCanceled -= HandleCanceled;
       //_build.OnSelected -= HandleSelectedChanged;
       //_build.OnPlaced -= ObjectPlaced;
    }

    private void HandleCanceled() => RefreshVisual();
    private void HandleSelectedChanged(E_SetupObjectOffsetChange e) => RefreshVisual();

    private void LateUpdate()
    {
        if (visual == null) return;

        Vector3 target = Util.Mouse.GetSnappedWorld();
        target.y += floatingHeight;

        visual.transform.position = Vector3.Lerp(visual.transform.position, target, Time.deltaTime * 15f);
        //visual.transform.rotation = Quaternion.Lerp(visual.transform.rotation, _build.CurrentRotation, Time.deltaTime * 15f);
    }

    private void RefreshVisual()
    {
        if (visual != null)
        {
            Managers.Game.GameEntityModelsSetLayer(visual, LayerMask.NameToLayer("Default"));
            Managers.Resource.Destroy(visual.gameObject);
            visual = null;
        }

        var placedObject = new GameEntity();// _build.Current;
        if (placedObject == null) return;

        var mouseWorld = Util.Mouse.GetMouseWorldPosition();
        if (!Managers.Grid.IsValidGridPosition(mouseWorld)) return;

        // Normalized
        m_PivotPosition = Managers.Grid.GetWorldPosition(Managers.Grid.GetGridPosition(mouseWorld));

        visual = Managers.Resource.Instantiate<GameEntity>(placedObject.gameObject, Vector3.zero, Quaternion.identity);
        visual.transform.SetParent(transform);
        visual.transform.localPosition = m_PivotPosition;
        visual.transform.rotation = Quaternion.Euler(0, placedObject.GetRotationAngle(), 0);
        visual.m_CurrentEDir = placedObject.m_CurrentEDir;

        visual.SelectSpawnObject();
        Managers.Game.GameEntityModelsSetLayer(visual, LayerMask.NameToLayer("Ghost"));
    }

    // 오브젝트 배치 완료
    private void ObjectPlaced(GridPosition pivotGridPosition)
    {
        if (visual == null) return;

        visual.transform.SetParent(null);
        Managers.Game.GameEntityModelsSetLayer(visual, LayerMask.NameToLayer("Default"));

        // Reserve 처리(그리드 쓰기)
        Managers.Grid.RequestCell(
            visual.GetGridPositionListAtSelectPosition(pivotGridPosition),
            visual,
            E_EntityCellType.Reserve
            );

        visual.PlayPlacedSpawnAnimation();
        visual = null;
    }
}