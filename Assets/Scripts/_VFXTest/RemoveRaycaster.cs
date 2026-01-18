using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveRaycaster : MonoBehaviour
{
    [SerializeField] private bool _isDebug = false;
    private DungeonTiles _dungeonTiles;
    public DungeonTiles DungeonTiles
    {
        get
        {
            if (_dungeonTiles == null)
                _dungeonTiles = FindAnyObjectByType<DungeonTiles>();
            return _dungeonTiles;
        }
    }
    private SwampBubbleVFX _vfx;
    public SwampBubbleVFX VFX
    {
        get
        {
            if (_vfx == null)
                _vfx = FindAnyObjectByType<SwampBubbleVFX>();
            return _vfx;
        }
    }

    private Camera _mainCamera => Camera.main;

    private void OnDestroy()
    {
        _dungeonTiles = null;
        _vfx = null;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) == false)
            return;

        CastRayToRemoveTile();
    }

    private void CastRayToRemoveTile()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (_isDebug)
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);

        var ret = Physics.RaycastAll(ray);

        foreach (var hit in ret)
        {
            if (hit.collider.gameObject.TryGetComponent(out SpikeTrap spikeTrap))
            {
                spikeTrap.Shoot();
                return;
            }
        }

        foreach (var hit in ret)
        {
            if (DungeonTiles.TryGetCell(hit.point, out GameObject tileGameObject))
            {
                Vector2Int gridPos = DungeonTiles.GetGridPosition(hit.point);
                DungeonTiles.RemoveTile(tileGameObject);
                VFX.ActiveSwampTile(gridPos.x, gridPos.y);
            }
        }



    }
}
