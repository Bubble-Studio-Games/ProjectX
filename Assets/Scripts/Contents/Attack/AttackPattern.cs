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
    public List<GridPosition> m_RangeOffset = new();   // 공격 범위 오프셋 (유닛 기준)
    public E_AttackType m_EAttackType;
    public E_RangeInclusionType m_ERangeInclusionType;
    public bool m_IsEnableSelfAttack; // 공격자가 대상자에 포함되는가?, 나도 공격/버프 당할 수 있는가?

    [Header("Attack Shape 보조, 필요시에만 기입")]
    // (Arc / Cone 파라미터 — 필요 시만 사용)
    public float m_ArcAngle = 90f;      // Arc 반각 예: 90 => ±45도
    public int m_RangeRadius = 1;       // 디자이너용 보조 정보
    public bool m_IncludeIntermediate = true;

    public AttackPattern[] m_iNextAttackPattern;
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
    #endregion
}

