using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitWeaponSlotManager : MonoBehaviour
{
    protected Unit m_Unit;
    UnitEquipEquipmentManager m_UnitEquipEquipmentManager;

    [Header("Unarmed Weapon")]
    //public WeaponItem unarmWeapon;

    [Header("Weapon Slots")]
    public WeaponHolderSlot m_LeftHandSlot;
    public WeaponHolderSlot m_RightHandSlot;
    public WeaponHolderSlot backSlot;

    [Header("Hand IK Targets")]
    public RightHandIKTarget rightHandIKTarget;
    public LeftHandIKTarget leftHandIKTarget;

    [Header("Projectile Spawn Transform")]
    public List<Transform> m_ProjectileSpawnTransforms;

    protected virtual void Awake()
    {
        m_Unit = GetComponent<Unit>();

        LoadWeaponHolderSlots();
        m_UnitEquipEquipmentManager = GetComponent<UnitEquipEquipmentManager>();
    }

    public void Start()
    {
        LoadBothWeaponsOnSlots();
    }

    // 슬롯 로드
    protected virtual void LoadWeaponHolderSlots()
    {
        WeaponHolderSlot[] weaponHolderSlots = GetComponentsInChildren<WeaponHolderSlot>();
        foreach (WeaponHolderSlot weaponSlot in weaponHolderSlots)
        {
            if (weaponSlot.isLeftHandSlot)
            {
                m_LeftHandSlot = weaponSlot;
            }
            else if (weaponSlot.isRightHandSlot)
            {
                m_RightHandSlot = weaponSlot;
            }
            else if (weaponSlot.isBackSlot)
            {
                backSlot = weaponSlot;
            }
        }
    }

    public virtual void LoadBothWeaponsOnSlots()
    {
        if (m_LeftHandSlot == null || m_RightHandSlot == null)
            return;

        LoadWeaponOnSlot(m_UnitEquipEquipmentManager.m_CurrentHandLeftWeapon, true);
        LoadWeaponOnSlot(m_UnitEquipEquipmentManager.m_CurrentHandRightWeapon, false);
    }

    public virtual void LoadWeaponOnSlot(WeaponItem weaponItem, bool isLeft)
    {
        if (weaponItem != null)
        {
            if (isLeft)
            {
                m_LeftHandSlot.currentWeapon = weaponItem;
                m_LeftHandSlot.LoadWeaponModel(weaponItem);
            }
            else
            {
                if (m_Unit.isTwoHandingWeapon)
                {
                    backSlot?.LoadWeaponModel(m_LeftHandSlot.currentWeapon);
                    m_LeftHandSlot?.UnloadWeaponAndDestroy();
                    m_Unit.GetAnimationManager().PlayTargetAnimation("Left Arm Empty", false);
                }
                else
                {

                    backSlot?.UnloadWeaponAndDestroy();

                }

                m_RightHandSlot.currentWeapon = weaponItem;
                m_RightHandSlot?.LoadWeaponModel(weaponItem);
                LoadTwoHandIKTargtets(m_Unit.isTwoHandingWeapon);
                //character.animator.runtimeAnimatorController = weaponItem.weaponController;
            }

        }
        else
        {
            if (isLeft)
            {
                //m_UnitEquipEquipmentManager.m_CurrentHandLeftWeapon = unarmWeapon;
                m_LeftHandSlot?.LoadWeaponModel(null);
                //character.characterAnimatorManager.PlayerTargetAnimation(weaponItem.offHandIdleAnimation, false, true);
            }
            else
            {
                //m_UnitEquipEquipmentManager.m_CurrentHandRightWeapon = unarmWeapon;
                m_RightHandSlot?.LoadWeaponModel(null);
                //character.animator.runtimeAnimatorController = weaponItem.weaponController;

            }
        }

    }

    public virtual void LoadTwoHandIKTargtets(bool isTwoHandingWeapon)
    {
        // 오른손 무기를 양손으로 잡기
        leftHandIKTarget = m_RightHandSlot.currentWeaponModel.GetComponentInChildren<LeftHandIKTarget>();
        rightHandIKTarget = m_RightHandSlot.currentWeaponModel.GetComponentInChildren<RightHandIKTarget>();

        if (leftHandIKTarget == null || rightHandIKTarget == null)
            return;

        m_Unit.GetAnimationManager().SetHandIKForWeapon(rightHandIKTarget, leftHandIKTarget, isTwoHandingWeapon);
    }
}
