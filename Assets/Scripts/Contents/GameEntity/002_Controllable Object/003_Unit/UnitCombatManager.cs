using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;
using static AttributeSystem;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using UnityEditor.Experimental.GraphView;

public class UnitCombatManager : ControllableObjectCombatManager
{
    [HideInInspector] public Unit m_Unit;

    protected override void Awake()
    {
        base.Awake();

        m_Unit = GetComponent<Unit>();
    }

    public override List<Transform> GetProjectileSpawnTransforms(bool isWantSpawnAtWeapon, int getCount = 0)
    {
        List<Transform> projectileSpawnTransforms = new();

        var slot = m_Unit.m_UnitWeaponSlotManager;

        if (isWantSpawnAtWeapon)
        {
            var currentRightWeapon = slot.m_RightHandSlot.currentWeapon;
            var currentLeftWeapon = slot.m_LeftHandSlot.currentWeapon;

            // 1. 두 손 무기 착용 중이라면 → 반드시 오른손 기준
            if (m_Unit.isTwoHandingWeapon && currentRightWeapon != null)
            {
                if (currentRightWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
                {
                    // 두 손 활 → 왼손에 화살 소환
                    projectileSpawnTransforms.Add(slot.m_LeftHandSlot.transform);
                }
                else if (currentRightWeapon.m_ProjectileSpawnTransform != null)
                {
                    // 일반 두 손 무기 → 발사 위치
                    projectileSpawnTransforms.Add(currentRightWeapon.m_ProjectileSpawnTransform);
                }
            }
            // 2. 두 손 무기가 아닌 경우 → 우선 오른손 무기 체크
            else if (currentRightWeapon != null)
            {
                // 활은 반대 손에서 생성
                if (currentRightWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
                {
                    projectileSpawnTransforms.Add(slot.m_LeftHandSlot.transform);
                }
                else if (currentRightWeapon.m_ProjectileSpawnTransform != null)
                {
                    projectileSpawnTransforms.Add(currentRightWeapon.m_ProjectileSpawnTransform);
                }
            }
            // 3. 오른손 무기 없고, 왼손 무기가 있는 경우
            else if (currentLeftWeapon != null)
            {
                // 활은 반대 손에서 생성
                if (currentLeftWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
                {
                    projectileSpawnTransforms.Add(slot.m_RightHandSlot.transform);
                }
                else if (currentLeftWeapon.m_ProjectileSpawnTransform != null)
                {
                    projectileSpawnTransforms.Add(currentLeftWeapon.transform);
                }
            }
            // 4. 무기 모두 없음 → 공격 손의 위치로
            else
            {
                projectileSpawnTransforms.Add(slot.m_RightHandSlot.transform);
            }
        }
        else
        {
            if (slot != null && slot.m_ProjectileSpawnTransforms != null)
            {
                projectileSpawnTransforms.AddRange(slot.m_ProjectileSpawnTransforms.Take(getCount).ToList());
            }
            else
            {
                projectileSpawnTransforms.Add(m_Unit.transform);
            }
        }

        return projectileSpawnTransforms;
    }
}
