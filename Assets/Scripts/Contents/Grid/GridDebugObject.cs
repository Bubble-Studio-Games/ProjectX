using UnityEngine;
using TMPro;

public class GridDebugObject : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMeshPro;

    private object gridObject;

    public virtual void SetGridObject(object gridObject)
    {
        this.gridObject = gridObject;
        UpdateGridObject();
    }

    public void UpdateGridObject()
    {
        var grid = Managers.Grid.GetGridPosition(transform.position);
        var unit = Managers.Grid.GetUnitAt(grid);
        var isFullOccupied = Managers.Grid.HasUnitAt(grid);

        string unitText = (unit != null) ? $"{unit.name}" : " ";

        if (isFullOccupied)
        {
            textMeshPro.text =
                $"{gridObject}\n" +
                $"Terrain : {Managers.Grid.GetTerrainType(grid)}\n" +
                $"Full Unit\n" +
                $"Units : {unitText}";
        }
        else
        {
            textMeshPro.text =
                $"{gridObject}\n" +
                $"Terrain : {Managers.Grid.GetTerrainType(grid)}\n" +
                $"Units : {unitText}";
        }
    }

}