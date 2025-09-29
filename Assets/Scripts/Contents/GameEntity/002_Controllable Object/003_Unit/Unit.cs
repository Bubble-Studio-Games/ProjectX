using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Define;

[RequireChildComponent(typeof(UnitAnimationManager), typeof(GameEntitySounder))]
[RequireComponent(typeof(UnitWeaponSlotManager), typeof(UnitEquipEquipmentManager), typeof(UnitWeaponSlotManager))]
public class Unit : ControllableObject, IAccessories<UnitAnimationManager, ControllableObjectSounder>
{
    [Header("Combat Flaged")]
    public bool isTwoHandingWeapon;
    public bool isUsingRightHand;
    public bool isUsingLeftHand;

    [Header("Ref")]
    protected UnitAnimationManager m_UnitAnimationManager;
    public UnitEquipEquipmentManager m_UnitEquipEquipmentManager;
    public UnitWeaponSlotManager m_UnitWeaponSlotManager;

    protected override void Awake()
    {
        base.Awake();

        m_UnitAnimationManager = GetComponentInChildren<UnitAnimationManager>();
        m_UnitEquipEquipmentManager = GetComponent<UnitEquipEquipmentManager>();
        m_UnitWeaponSlotManager = GetComponent<UnitWeaponSlotManager>();
    }

    public Unit()
    {
        m_ObjectType = E_ObjectType.Unit;
    }

    public new UnitAnimationManager GetAnimationManager()
    {
        return m_UnitAnimationManager;
    }

    public new ControllableObjectSounder GetSounderManager()
    {
        return m_ControllableObjectSounder;
    }
}