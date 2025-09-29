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
    }

    protected virtual void Update()
    {
        bool isReserve = LevelGrid.Instance.IsReservedGridPosition(LevelGrid.Instance.GetGridPosition(transform.position));
        if(isReserve)
        {
            textMeshPro.text = gridObject.ToString() + " isReserve";
        }
        else
            textMeshPro.text = gridObject.ToString();



    }

}