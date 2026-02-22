using static Define;
using System.Collections.Generic;
using UnityEngine;

public class GridBuildingSystem : MonoBehaviour
{
    private GameEntity _template; // 선택된 원본(데이터/계산용)
    private GameEntity _ghost;    // 움직이는 프리뷰(인스턴스)

    [SerializeField] private float floatingHeight = 1f;
    [SerializeField] private float followSpeed = 15f;

    private Dictionary<Transform, int> _layerSnapshot;

    #region Unity Life Cycle

    private void OnEnable()
    {
        Managers.Building.OnRequestApply += Apply;
    }

    private void OnDisable()
    {
        Managers.Building.OnRequestApply -= Apply;
    }

    private void LateUpdate()
    {
        if (_ghost == null) return;

        Vector3 target = Util.Mouse.GetSnappedWorld();
        target.y += floatingHeight;

        _ghost.transform.position = Vector3.Lerp(_ghost.transform.position, target, Time.deltaTime * followSpeed);

        var rot = (_template != null)
            ? Quaternion.Euler(0, _template.GetRotationAngle(), 0)
            : Quaternion.identity;

        _ghost.transform.rotation = Quaternion.Lerp(_ghost.transform.rotation, rot, Time.deltaTime * followSpeed);
    }

    #endregion

    private void Apply(BuildContext req)
    {
        switch (req.Type)
        {
            case BuildRequestType.Select: ApplySelect(req.Target); break;
            case BuildRequestType.Rotate: ApplyRotate(); break;
            case BuildRequestType.Place: ApplyPlace(); break;
            case BuildRequestType.Cancel: ApplyCancel(); break;
        }

        // GridVisual은 GridManager 이벤트만 듣고 갱신
        Managers.Grid.RequestVisualRefresh();
    }

    // 인자로 받은 오브젝트의 모습을 보여주기 시작.
    private void ApplySelect(GameEntity toChangeObject)
    {
        Debug.Log("배치 시작");
        var before = _template;
        _template = toChangeObject;

        // 이전 고스트 정리
        DestroyGhost();

        if (_template == null)
        {
            Managers.Building.RaiseCanceled();
            return;
        }

        // 상태 계산(기존 로직 유지)
        var state = E_SetupObjectOffsetChange.All;

        if (before != null)
        {
            if (before.GetGridPositionListAtCurrentDir() != _template.GetGridPositionListAtCurrentDir())
            {
                state = (before.GetGridPositionYOffset() != _template.GetGridPositionYOffset())
                    ? E_SetupObjectOffsetChange.All
                    : E_SetupObjectOffsetChange.XZOffset;
            }
            else state = E_SetupObjectOffsetChange.None;
        }

        // 고스트 생성
        _ghost = Managers.Resource.Instantiate<GameEntity>(_template.gameObject, Vector3.zero, Quaternion.identity);

        // 레이어 스냅샷 + Ghost 레이어 적용(유틸 사용)
        _layerSnapshot = LayerUtil.SnapshotAndSetLayerRecursive(_ghost.gameObject, LayerMask.NameToLayer("Ghost"));

        _ghost.m_CurrentEDir = _template.m_CurrentEDir;
        _ghost.SelectSpawnObject();
    }

    private void ApplyRotate()
    {
        if (_template == null) return;

        _template.m_CurrentEDir = _template.GetNextDir();
        if (_ghost != null) _ghost.m_CurrentEDir = _template.m_CurrentEDir;

        var e = _template.m_IsRotateSymmetry ? E_SetupObjectOffsetChange.None : E_SetupObjectOffsetChange.XZOffset;
        Managers.Building.RaiseRotated();
    }

    private void ApplyPlace()
    {
        if (_template == null || _ghost == null) return;

        GridPosition pivot = Util.Mouse.GetMouseWorldGridPosition();
        var cells = _template.GetGridPositionListAtSelectPosition(pivot);

        foreach (var gp in cells)
        {
            if (!Managers.Grid.IsWalkableTerrain(gp) || Managers.Grid.HasUnitAt(gp))
            {
                Managers.Building.Reqeust(new BuildContext(BuildRequestType.Fail, _template));
                Debug.Log("실패");
                return;
            }
        }

        // 레이어 원복
        LayerUtil.RestoreLayers(_layerSnapshot);

        // Reserve 처리(확정은 고스트 인스턴스 기준)
        Managers.Grid.RequestCell(
            _ghost.GetGridPositionListAtSelectPosition(pivot),
            _ghost,
            E_EntityCellType.Reserve
        );

        _ghost.PlayPlacedSpawnAnimation();

        Managers.Grid.RequestVisualRefresh();

        Managers.Building.RaisePlaced(pivot);

        _ghost = null;
        _template = null;
    }

    private void ApplyCancel()
    {
        if (_template == null) return;

        DestroyGhost();
        Managers.Building.RaiseCanceled();

        _template = null;
    }

    private void DestroyGhost()
    {
        LayerUtil.RestoreLayers(_layerSnapshot);

        if (_ghost != null)
        {
            Managers.Resource.Destroy(_ghost.gameObject);
            _ghost = null;
        }
    }
}
