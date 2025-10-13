using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Define;

// 발사체 발사 전 소환
// 무기에 버프를 둘러서 강화하기
[CreateAssetMenu(menuName = "Attack Pattern/Ready")]
public class AttackPattern_Ready : AttackPattern<AttackPatternInfoClipWithReady>
{
    [Header("Clip")]
    public AudioClip ReadyFailAudioClip;

    [Header("Spawn Object")]
    public Item m_ReadyGameObjectPrefab;
    public GameObject m_FailPrefab;

    [Header("Ready")]
    public float m_AttackReadyTime = 2f;
    protected float lastAttackReadytime;
    public bool m_ISAttackReadyFinished => Time.time - lastAttackReadytime >= m_AttackReadyTime;

    public override void Init()
    {
        base.Init();
        lastAttackReadytime = -m_AttackReadyTime; // 준비시간 완료된 상태로 시작
    }

    public override void StartAttack(ControllableObject attacker, GameEntity target, AttackPattern prevAttackpatern)
    {
        base.StartAttack(attacker, target, prevAttackpatern);

        // 손 위치에 발사체 준비
        if(attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject != null)
        {
            // 2번째 준비 패턴이라면 패스 및 갱신

        }
        else
        {
            Transform weaponHandTransform = null;

            // TODO Building
            if(attacker is Unit unit)
                weaponHandTransform = GetProjectileSpawnTransformAtUnit(unit);

            var readyItem = Managers.Resource.Instantiate<Item>(m_ReadyGameObjectPrefab.gameObject, weaponHandTransform);
            readyItem.transform.localPosition = Vector3.zero;
            readyItem.transform.localRotation = Quaternion.identity;

            attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject = readyItem;
        }
    }

    public override void EndAttack(ControllableObject attacker, GameEntity target) // 종료
    {
        lastAttackReadytime = Time.time;
        attacker.m_ControllableObjectCombatManager.m_ReadyAttackPattern.Add(this);
    }

    public override void StartAttackFail(ControllableObject attacker, GameEntity target)
    {
        base.StartAttackFail(attacker, target);

        if(m_FailPrefab !=null)
        {
            var go = Managers.Resource.Instantiate(m_FailPrefab);
            attacker.StartCoroutine(ObjectDestroy(go, 3f));
        }
    }

    private Transform GetProjectileSpawnTransformAtUnit(Unit unit)
    {
        Transform weaponHandTransform = null;

        var equipManager = unit.m_UnitEquipEquipmentManager;
        var slot = unit.m_UnitWeaponSlotManager;
        var currentRightWeapon = slot.m_RightHandSlot.currentWeapon;
        var currentLeftWeapon = slot.m_LeftHandSlot.currentWeapon;
        var animator = unit.GetAnimationsManager()[0];

        // 1. 두 손 무기 착용 중이라면 → 반드시 오른손 기준
        if (unit.isTwoHandingWeapon && currentRightWeapon != null)
        {
            if (currentRightWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
            {
                // 두 손 활 → 왼손에 화살 소환
                weaponHandTransform = slot.m_LeftHandSlot.transform;
            }
            else if (currentRightWeapon.m_ProjectileSpawnTransform != null)
            {
                // 일반 두 손 무기 → 발사 위치
                weaponHandTransform = currentRightWeapon.m_ProjectileSpawnTransform;
            }
        }
        // 2. 두 손 무기가 아닌 경우 → 우선 오른손 무기 체크
        else if (currentRightWeapon != null)
        {
            // 활은 반대 손에서 생성
            if (currentRightWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
            {
                weaponHandTransform = slot.m_LeftHandSlot.transform;
            }
            else if (currentRightWeapon.m_ProjectileSpawnTransform != null)
            {
                weaponHandTransform = currentRightWeapon.m_ProjectileSpawnTransform;
            }
        }
        // 3. 오른손 무기 없고, 왼손 무기가 있는 경우
        else if (currentLeftWeapon != null)
        {
            // 활은 반대 손에서 생성
            if (currentLeftWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
            {
                weaponHandTransform = slot.m_RightHandSlot.transform;
            }
            else if (currentLeftWeapon.m_ProjectileSpawnTransform != null)
            {
                weaponHandTransform = currentLeftWeapon.transform;
            }
        }
        // 4. 무기 모두 없음 → 공격 손의 위치로
        else
        {
            weaponHandTransform = slot.m_RightHandSlot.transform;
        }

        return weaponHandTransform;
    }
}
