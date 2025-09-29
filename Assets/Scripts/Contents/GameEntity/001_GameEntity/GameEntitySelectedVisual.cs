using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class GameEntitySelectedVisual : MonoBehaviour
{

    private GameEntity unit;

    private MeshRenderer meshRenderer;
    string m_AllyMaterial = "UnitSelectedVisual";
    string m_EnemyMaterial = "EnemyUnitSelectedVisual";
    string m_NoneMaterial = "NoneSelectedVisual";

    private void Awake()
    {
        unit = GetComponentInParent<GameEntity>();
        meshRenderer = GetComponent<MeshRenderer>();

    }

    private void Start()
    {
        UnitActionSystem.Instance.OnSelectedUnitChanged += UnitActionSystem_OnSelectedUnitChanged;

        UpdateVisual();

        string common = "Art/Materials/Base/Select Game Entity/";

        if(unit.m_TeamId == E_TeamId.Player)
            common += m_AllyMaterial;
        else if (unit.m_TeamId == E_TeamId.Monster)
            common += m_EnemyMaterial;
        else if (unit.m_TeamId == E_TeamId.None)
            common += m_NoneMaterial;

        meshRenderer.material =  Managers.Resource.Load<Material>(common);
    }

    private void UnitActionSystem_OnSelectedUnitChanged(object sender, EventArgs empty)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if(unit is GameEntity contObj)
        {
            if (UnitActionSystem.Instance.IsSelectedObject(contObj))
            {
                meshRenderer.enabled = true;
            }
            else
            {
                meshRenderer.enabled = false;
            }
        }
    }

    private void OnDestroy()
    {
        UnitActionSystem.Instance.OnSelectedUnitChanged -= UnitActionSystem_OnSelectedUnitChanged;
    }
}