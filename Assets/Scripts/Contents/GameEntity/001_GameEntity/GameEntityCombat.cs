using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

/// <summary>
/// 전투 전용 스크립트
/// 무기 장착 프로젝타일 소환 위치 등을 관리
/// </summary>
public class GameEntityCombat : MonoBehaviour
{
    public List<(Item obj, Transform spawnTransform)> m_AttackReadyItemObject = new();
    public HashSet<AttackPattern_Ready> m_ReadyAttackPattern = new HashSet<AttackPattern_Ready>();
    public GameEntity m_GameEntity { get; protected set; }

    [Header("Combat Flaged")]
    public bool isTwoHandingWeapon;
    public bool isUsingRightHand;
    public bool isUsingLeftHand;

    protected virtual void Awake()
    {
        m_GameEntity = GetComponent<GameEntity>();
        GetComponent<AttributeSystem>().OnDamaged += (s, e) => AttackReadyFailStart();

        LoadWeaponHolderSlots();

        m_ProjectileSpawnTransforms = GetComponentsInChildren<ProjectileTransform>();
    }


    public void Start()
    {
        LoadBothWeaponsOnSlots();
    }


    protected virtual void Update()
    {
        CheckAttackReady();
    }

    protected void CheckAttackReady()
    {
        var currentAttack = m_GameEntity.GetAction<CombatAction>()?.m_ThisTimeAttack;

        // 현재 준비 중인 공격이 없다면 패스

        if (currentAttack is not AttackPattern_Ready readyAttack)
            return;

        if (!m_ReadyAttackPattern.Contains(readyAttack))
            return;

        // 준비가 완료되었는지 체크
        if (readyAttack.m_ISAttackReadyFinished)
        {
            AttackReadyFailStart();  // 실패 처리
        }
    }

    // 공격 준비 실패에는 2가지가 있다.
    // 1. 공격 준비 중인데 공격을 받았을 때
    // 2. 공격 준비 후 다음 단계를 진행하지 못하고 일정 시간이 지났을 때

    public void AttackReadyFailStart()
    {
        var combatAction = m_GameEntity.GetAction<CombatAction>();

        if (combatAction == null)
            return;

        var attack = combatAction.m_ThisTimeAttack as AttackPattern_Ready;

        if (attack == null)
            return;

        if (m_ReadyAttackPattern.Contains(attack))
            m_ReadyAttackPattern.Remove(attack);

        attack.StartAttackFail(m_GameEntity, m_GameEntity.m_Target);

        // Animation
        m_GameEntity.GetAnimationsManager()[0].AttackReadyFail();

        // Sound
        m_GameEntity.GetSounderManager().AttackReadyFailSoundPlay();

        foreach (var (obj, _) in m_AttackReadyItemObject)
            obj.Destroy();

        // 리스트 초기화
        m_AttackReadyItemObject.Clear();

        // 이번 타임 공격 제거
        m_GameEntity.GetAction<CombatAction>().ChangeAttack(null);
    }

    public void AttackReadyFailEnd()
    {
        var combat = m_GameEntity.GetAction<CombatAction>();
        combat.m_ThisTimeAttack?.EndAttackFail();
        combat.m_ThisTimeAttack = null;
        combat.ActiveSet(false);
    }

    public virtual List<Transform> GetProjectileSpawnTransforms(bool isWantSpawnAtWeapon, int getCount = 0)
    {
        List<Transform> projectileSpawnTransforms = new();

        if (isWantSpawnAtWeapon)
        {
            var currentRightWeapon = m_RightHandSlot.currentWeapon;
            var currentLeftWeapon = m_LeftHandSlot.currentWeapon;

            // 1. 두 손 무기 착용 중이라면 → 반드시 오른손 기준
            if (isTwoHandingWeapon && currentRightWeapon != null)
            {
                if (currentRightWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
                {
                    // 두 손 활 → 왼손에 화살 소환
                    projectileSpawnTransforms.Add(m_LeftHandSlot.transform);
                }
                else if (currentRightWeapon.m_ProjectileSpawnTransform != null)
                {
                    // 일반 두 손 무기 → 발사 위치
                    projectileSpawnTransforms.Add(currentRightWeapon.m_ProjectileSpawnTransform.transform);
                }
            }
            // 2. 두 손 무기가 아닌 경우 → 우선 오른손 무기 체크
            else if (currentRightWeapon != null)
            {
                // 활은 반대 손에서 생성
                if (currentRightWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
                {
                    projectileSpawnTransforms.Add(m_LeftHandSlot.transform);
                }
                else if (currentRightWeapon.m_ProjectileSpawnTransform != null)
                {
                    projectileSpawnTransforms.Add(currentRightWeapon.m_ProjectileSpawnTransform.transform);
                }
            }
            // 3. 오른손 무기 없고, 왼손 무기가 있는 경우
            else if (currentLeftWeapon != null)
            {
                // 활은 반대 손에서 생성
                if (currentLeftWeapon.m_EWeaponItemType == E_WeaponItemType.Bow)
                {
                    projectileSpawnTransforms.Add(m_RightHandSlot.transform);
                }
                else if (currentLeftWeapon.m_ProjectileSpawnTransform != null)
                {
                    projectileSpawnTransforms.Add(currentLeftWeapon.transform);
                }
            }
            // 4. 무기 모두 없음 → 공격 손의 위치로
            else
            {
                projectileSpawnTransforms.Add(m_RightHandSlot.transform);
            }
        }
        else
        {
            if (m_ProjectileSpawnTransforms != null)
            {
                projectileSpawnTransforms.AddRange(m_ProjectileSpawnTransforms.Select(t => t.transform).ToList().Take(getCount));
            }
            else
            {
                projectileSpawnTransforms.Add(transform);
            }
        }

        return projectileSpawnTransforms;
    }


    #region WeaponSlot

    [Header("Weapon Slots")]
    public WeaponHolderSlot m_LeftHandSlot;
    public WeaponHolderSlot m_RightHandSlot;
    public WeaponHolderSlot backSlot;

    [Header("Hand IK Targets")]
    public RightHandIKTarget rightHandIKTarget;
    public LeftHandIKTarget leftHandIKTarget;

    [Header("Projectile Spawn Transform")]
    public ProjectileTransform[] m_ProjectileSpawnTransforms;

    [Header("Current Weapon")]
    public WeaponItem m_CurrentHandRightWeapon; // Equick Slot Right
    public WeaponItem m_CurrentHandLeftWeapon; // Equick Slot Left



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

        LoadWeaponOnSlot(m_CurrentHandLeftWeapon, true);
        LoadWeaponOnSlot(m_CurrentHandRightWeapon, false);
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
                if (isTwoHandingWeapon)
                {
                    backSlot?.LoadWeaponModel(m_LeftHandSlot.currentWeapon);
                    m_LeftHandSlot?.UnloadWeaponAndDestroy();
                    m_GameEntity.GetAnimationsManager()[0].PlayTargetAnimation("Left Arm Empty", false);
                }
                else
                {

                    backSlot?.UnloadWeaponAndDestroy();

                }

                m_RightHandSlot.currentWeapon = weaponItem;
                m_RightHandSlot?.LoadWeaponModel(weaponItem);
                LoadTwoHandIKTargtets(isTwoHandingWeapon);
            }

        }
        else
        {
            if (isLeft)
            {
                m_LeftHandSlot?.LoadWeaponModel(null);
            }
            else
            {
                m_RightHandSlot?.LoadWeaponModel(null);
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

        m_GameEntity.GetAnimationsManager()[0].SetHandIKForWeapon(rightHandIKTarget, leftHandIKTarget, isTwoHandingWeapon);
    }

    #endregion
}
