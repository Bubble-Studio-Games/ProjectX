using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridObject
{
    private GridSystem<GridObject> gridSystem;
    private GridPosition gridPosition;
    private GameEntity unit;
    private IInteractable interactable;

    public GridObject(GridSystem<GridObject> gridSystem, GridPosition gridPosition)
    {
        this.gridSystem = gridSystem;
        this.gridPosition = gridPosition;
    }

    public override string ToString()
    {
        return gridPosition.ToString() + "\n" + unit?.name;
    }

    public void AddUnit(GameEntity unit)
    {
        this.unit = unit;
    }

    public void RemoveUnit(GameEntity unit)
    {
        this.unit = null;
    }

    public bool HasAnyUnit()
    {
        return unit != null;
    }

    public GameEntity GetObject()
    {
        return unit;
    }

    public IInteractable GetInteractable()
    {
        return interactable;
    }

    public void SetInteractable(IInteractable interactable)
    {
        this.interactable = interactable;
    }

    public void ClearInteractable()
    {
        this.interactable = null;
    }


}