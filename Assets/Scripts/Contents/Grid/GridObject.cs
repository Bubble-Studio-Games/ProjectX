using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridObject
{
    private GridSystem<GridObject> gridSystem;
    private GridPosition gridPosition;
    private List<GameEntity> unitList;
    private IInteractable interactable;

    public GridObject(GridSystem<GridObject> gridSystem, GridPosition gridPosition)
    {
        this.gridSystem = gridSystem;
        this.gridPosition = gridPosition;
        unitList = new List<GameEntity>();
    }

    public override string ToString()
    {
        string unitString = "";
        foreach (GameEntity unit in unitList)
        {
            unitString += unit + "\n";
        }

        return gridPosition.ToString() + "\n" + unitString;
    }

    public void AddUnit(GameEntity unit)
    {
        unitList.Add(unit);
    }

    public void RemoveUnit(GameEntity unit)
    {
        unitList.Remove(unit);
    }

    public List<GameEntity> GetUnitList()
    {
        return unitList;
    }

    public bool HasAnyUnit()
    {
        return unitList.Count > 0;
    }

    public GameEntity GetObject()
    {
        if (HasAnyUnit())
        {
            return unitList[0];
        } 

        return null;
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