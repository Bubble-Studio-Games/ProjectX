using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Define;

[RequireComponent(typeof(ControllableObjectCombatManager))]
[RequireChildComponent(typeof(UnitAnimationManager), typeof(GameEntitySounder))]
public class Building : ControllableObject
{
    public E_BuildingType m_EBuildingType;

    public Building()
    {
        m_ObjectType = E_ObjectType.Building;
    }
}
