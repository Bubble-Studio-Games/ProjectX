using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GridDebugObject : MonoBehaviour
{

    [SerializeField] private TextMeshPro textMeshPro;

    private object gridObject;

    public virtual void SetGridObject(object gridObject)
    {
        this.gridObject = gridObject;
        textMeshPro.text = $"{gridObject}";
    }

    public void UpdateGridObject()
    {
        var cellInfo = LevelGrid.Instance.GetGridPositionCellInfo(LevelGrid.Instance.GetGridPosition(transform.position));
        var unit = cellInfo.Entity;
        var gridType = cellInfo.gridType;

        if(gridType == Define.E_GridCheckType.GameEntity || gridType == Define.E_GridCheckType.Reserve)
        {
            textMeshPro.text = $"{gridObject} \n {gridType} \n {unit}";

        }
        else
        {
            textMeshPro.text = $"{gridObject} \n {gridType}";
        }

    }
}