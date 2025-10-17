using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Define;

[Serializable]
public class AttackPatternInfoClip
{
    public AnimationClip AttackAnimationClip;
}

[Serializable]
public class AttackPatternInfoClipWithReady : AttackPatternInfoClip
{
    public AnimationClip ReadyFailAnimationClip;
}

public class AttackPattern<TClip> : AttackPattern
    where TClip : AttackPatternInfoClip
{

    public TClip[] m_Clips;
    public override AttackPatternInfoClip[] GetBaseClip() => m_Clips;
}

// 데이터
public  class AttackPattern : ScriptableObject
{
    #region 공격 데이터

    [Header("Base Info")]
    public int ID;
    public string AttackName;                    // 예: "전방3칸", "부채꼴" 등
    public E_AttackType m_EAttackType;
    public bool m_IsEnableSelfAttack; // 공격자가 대상자에 포함되는가?, 나도 공격/버프 당할 수 있는가?

    [Header("Range & Shape")]
    public E_RangeFillType m_ERangeFillType;
    public E_RangeShapeType m_ERangeShapeType;
    public List<GridPosition> m_RangeOffset = new(); 
    public float m_ArcAngle = 90f;

    [Header("Attack Start Pos")]
    // true이면 시전자 위치를 기준으로, false이면 타겟을 기준으로
    // 타겟이 없으면 해당 공격은 canexecute에서 제외함.
    public bool m_IsAttackStartPositionAtAttacker = true; 
    public GridPosition m_StartOffset = new(0, 0, 0);  // 공격 시작 기준 위치 (예: 전방 1칸 등)

    [Header("Condition")]
    public List<E_GridCheckType> m_GridCheckTypes = new List<E_GridCheckType>();
    public E_TeamId m_ApplyTargetTeampId;

    [Header("Combo / Chain Links")]
    public AttackPattern[] m_iNextAttackPattern;

    [Tooltip("선행 패턴 조건 (null이면 조건 없음)")] 
    public AttackPattern m_iConditionPrevAttackPattern; // 

    [Header("Condition")]
    public StatValue m_iCoolTime = 2f;
    public StatValue lastCooltime { get; private set; }
    public bool m_bCoolTimeIsFinishied => Time.time - lastCooltime >= m_iCoolTime;
    public StatValue m_iManaCost;
    public bool m_IsTwoHandAttack; // 두 손 행동인가?

    [Header("Damage Info")]
    public StatValue m_iPhysicalAttackDamage = new StatValue(0, false);     // 물리 공격 데미지
    public StatValue m_iMagicAttackDamage = new StatValue(0, false);        // 미밥 공격 데미지
    public StatValue m_iPhysicalFixedDamage = new StatValue(0, false);      // 물리 고정 데미지
    public StatValue m_iMagicFixedDamage = new StatValue(0, false);         // 마법 고정 데미지
    public StatValue m_fPhysicalArmorPenetraion = new StatValue(0, false);    // 물리 방어구 관통력
    public StatValue m_fMagicalArmorPenetraion = new StatValue(0, false);     // 마법 방어구 관통력

    [Header("Battle Attack Chance")]
    public StatValue m_iCriticalChance = new StatValue(0, false);     // 치명타율
    public StatValue m_fCriticalDamageUp = new StatValue(1, false);   // 치명타 데미지 증가율
    public StatValue m_fAccuracy = new StatValue(0, false);           // 명중률
    public StatValue m_fAttackSpeed = new StatValue(1, false);        // 공격 속도
    public StatValue m_iKnockbackChance = new StatValue(0, false);    // 넉백 확률

    [Header("Clip")]
    public AudioClip AttackAudioClip;

    public virtual AttackPatternInfoClip[] GetBaseClip() { return null; }

    #endregion

    #region 공격 로직

    public virtual void Init()
    {
        lastCooltime = -m_iCoolTime;              // 쿨타임 끝난 상태로 시작
    }

    public virtual E_AttackCondition CanExecute(ControllableObject attacker, GameEntity target)
    {
        // Mana
        if (attacker.m_AttributeSystem.m_Stat.m_iCurrentMP < m_iManaCost)
            return E_AttackCondition.Fail_ManaCost;

        // CoolTime
        if (!m_bCoolTimeIsFinishied)
            return E_AttackCondition.Fail_CoolTime;

        //// 공격 패턴이 없는지 확인
        //if (attacker.GetAction<CombatAction>().m_ThisTimeAttack == null)
        //    return E_AttackCondition.Fail_IndividualCondition;

        // 전 공격 준비 단계가 있어야 하는지 확인
        if (m_iConditionPrevAttackPattern != null)
        {
            AttackPattern attack = attacker.GetAction<CombatAction>().m_ThisTimeAttack;
            if (attack == null || attack.ID != m_iConditionPrevAttackPattern.ID)
            {
                return E_AttackCondition.Fail_NotHasPrevAttack;
            }
        }

        // 🔹 4. 거리(공격 범위) 검사
        if (target != null)
        {
            // 실제 공격 가능한 모든 그리드 계산
            HashSet<GridPosition> attackablePositions = Managers.Game.GetAttackPatternPosition(attacker, target, this);
            GridPosition targetPos = target.GetGridPosition();

            // 타겟이 범위 내에 없으면 실패
            if (!attackablePositions.Contains(targetPos))
                return E_AttackCondition.Fail_Distance;
        }

        return E_AttackCondition.Success;
    }

    public virtual void StartAttack(ControllableObject attacker, GameEntity target, AttackPattern prevAttackpatern) // 실행
    {
        // 쿨타임 갱신
        lastCooltime = Time.time;

        // 전 준비 단계가 있다면 해시에서 제거
        if (prevAttackpatern != null && prevAttackpatern.m_iNextAttackPattern.Select(p => p.ID).ToArray().Contains(ID))
        {
            attacker.m_ControllableObjectCombatManager.m_ReadyAttackPattern.Remove(prevAttackpatern as AttackPattern_Ready);
        }
    }

    public virtual void Attack(ControllableObject attacker, GameEntity target) { } // 종료

    public virtual void EndAttack(ControllableObject attacker, GameEntity target) { } // 종료

    public virtual void StartAttackFail(ControllableObject attacker, GameEntity target)
    {
        //Debug.Log($"{attacker.name}의 {AttackName} 공격 실패");
    }

    public void EndAttackFail()
    {
        // 쿨타임 갱신
        lastCooltime = Time.time;
    }

    protected IEnumerator ObjectDestroy(GameObject go, float time)
    {
        yield return new WaitForSeconds(time);
        Managers.Resource.Destroy(go);
    }

    public GridPosition GetStartOrigin(ControllableObject owner, GameEntity target)
    {
        GridPosition origin = owner.GetGridPosition();
        E_Dir dir = LevelGrid.Instance.GetDirGridPosition(owner.GetGridPosition(), target.GetGridPosition());

        if (!m_IsAttackStartPositionAtAttacker)
        {
            if (target == null)
                return origin; // 타겟 없으면 기본값
            origin = target.GetGridPosition();
            dir = LevelGrid.Instance.GetDirGridPosition(target.GetGridPosition(), owner.GetGridPosition());
        }

        return LevelGrid.Instance.ToGridPosition(m_StartOffset, origin, dir);
    }

    #endregion
}

