using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[DefaultExecutionOrder(-100)]
public class DungeonTiles : MonoBehaviour
{
    private Dictionary<Vector2Int, GameObject> _floorsTiles = new();
    [SerializeField] private LayerMask _floorLayer;
    private Grid _grid;

    private void Awake()
    {
        _grid = GetComponent<Grid>();
        InitFloorTiles();
    }

    private void OnDestroy()
    {
        _floorsTiles.Clear();
        _grid = null;
    }

    private void InitFloorTiles()
    {
        _floorsTiles.Clear();

        var floorTiles = GetComponentsInChildren<Transform>();
        foreach (Transform child in floorTiles)
        {
            if (child == transform)
                continue;

            if (((_floorLayer.value >> child.gameObject.layer) & 1) == 0)
                continue;

            var gridCell = _grid.LocalToCell(child.position);
            var gridPosition = new Vector2Int(gridCell.x, gridCell.y);

            _floorsTiles[gridPosition] = child.gameObject;
        }
    }

    public void RemoveTile(GameObject tileGameObject)
    {
        var gridPosition = _floorsTiles
            .FirstOrDefault(x => x.Value == tileGameObject)
            .Key;

        _floorsTiles.Remove(gridPosition);
        Destroy(tileGameObject);
    }

    public GameObject GetCell(Vector3 worldPosition)
    {
        var gridCell = _grid.LocalToCell(worldPosition);
        var gridPosition = new Vector2Int(gridCell.x, gridCell.y);

        _floorsTiles.TryGetValue(gridPosition, out GameObject ret);
        return ret;
    }

    public bool TryGetCell(Vector3 worldPosition, out GameObject tileGameObject)
    {
        var gridCell = _grid.LocalToCell(worldPosition);
        var gridPosition = new Vector2Int(gridCell.x, gridCell.y);

        return _floorsTiles.TryGetValue(gridPosition, out tileGameObject);
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        if (_grid == null)
            return Vector2Int.zero;

        var gridCell = _grid.LocalToCell(worldPosition);
        var ret = new Vector2Int(gridCell.x, gridCell.y);
        return ret;
    }
}
