using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSystem<TGridObject>
{
    private int width;
    private int height;
    private float cellSize;
    private int floor;
    private float floorHeight;
    private Vector3 _originOffset;
    private TGridObject[,] gridObjectArray;

    public GridSystem(int width, int height, float cellSize, int floor, float floorHeight, Vector3 originOffset)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.floor = floor;
        this.floorHeight = floorHeight;
        this._originOffset = originOffset;

        gridObjectArray = new TGridObject[width, height];
    }

    public GridSystem(int width, int height, float cellSize, int floor, float floorHeight, Vector3 originOffset, Func<GridSystem<TGridObject>, GridPosition, TGridObject> createGridObject)
        : this(width, height, cellSize, floor, floorHeight, originOffset)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z, floor);
                gridObjectArray[x, z] = createGridObject(this, gridPosition);
            }
        }
    }

    public void RegisterGridObject(GridPosition gridPosition, TGridObject gridObject)
    {
        if (IsValidGridPosition(gridPosition) == false)
        {
            Debug.LogWarning($"유효하지 않은 GridPosition에 등록 시도: {gridPosition}");
            return;
        }

        gridObjectArray[gridPosition.x, gridPosition.z] = gridObject;
    }

    public Vector3 GetWorldPosition(GridPosition gridPosition)
    {
        var ret = new Vector3(gridPosition.x + 0.5f, 0, gridPosition.z + 0.5f) * cellSize +
                  new Vector3(0, gridPosition.floor, 0) * floorHeight +
                  _originOffset;
        return ret;
    }


    public GridPosition GetGridPosition(Vector3 worldPosition)
    {
        var localPos = worldPosition - _originOffset;
        var ret = new GridPosition(
            Mathf.RoundToInt(localPos.x / cellSize),
            Mathf.RoundToInt(localPos.z / cellSize),
            floor
        );
        return ret;
    }

    public Dictionary<GridPosition, GridDebugObject > CreateDebugObjects(Transform debugPrefab, Transform parent)
    {
        Dictionary<GridPosition, GridDebugObject> gridDebugObjects = new();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z, floor);

                Transform debugTransform = GameObject.Instantiate(debugPrefab, GetWorldPosition(gridPosition), Quaternion.identity);
                GridDebugObject gridDebugObject = debugTransform.GetComponent<GridDebugObject>();
                gridDebugObject.SetGridObject(gridPosition);

                debugTransform.SetParent(parent);

                gridDebugObjects[gridPosition] = gridDebugObject;
            }
        }

        return gridDebugObjects;
    }

    public TGridObject GetGridObject(GridPosition gridPosition)
    {
        return gridObjectArray[gridPosition.x, gridPosition.z];
    }

    public bool IsValidGridPosition(GridPosition gridPosition)
    {
        return  gridPosition.x >= 0 && 
                gridPosition.z >= 0 && 
                gridPosition.x < width && 
                gridPosition.z < height &&
                gridPosition.floor == floor;
    }

    public int GetWidth()
    {
        return width;
    }

    public int GetHeight()
    {
        return height;
    }

    public float GetCellSize()
    {
        return cellSize;
    }



}