using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unit.Dependencies;

public sealed class GridService : MonoBehaviour, IGridService
{
    [SerializeField] private float cellSize = 1f;

    public bool TryWorldToGrid(Vector3 worldPos, out GridPosition grid)
    {
        int x = Mathf.RoundToInt(worldPos.x / cellSize);
        int y = Mathf.RoundToInt(worldPos.z / cellSize);

        grid = new GridPosition(x, y, 0);
        return true;
    }
}
