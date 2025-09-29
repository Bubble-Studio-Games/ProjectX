using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;
using static StatSystem;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class ControllableObjectCombatManager : MonoBehaviour
{
    public Item m_AttackReadyItemObject;

    [HideInInspector] public ControllableObject m_ControllableObject;

    public HashSet<AttackPattern_Ready> m_ReadyAttackPattern = new HashSet<AttackPattern_Ready>();

    private void Awake()
    {
        m_ControllableObject = GetComponent<ControllableObject>();
        GetComponent<StatSystem>().OnDamaged +=(s, e) => AttackReadyFailStart();
    }

    protected virtual void Update()
    {
        CheckAttackReady();
    }

    private void CheckAttackReady()
    {
        var currentAttack = m_ControllableObject.GetAction<CombatAction>()?.m_ThisTimeAttack;

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
        var combatAction = m_ControllableObject.GetAction<CombatAction>();

        if (combatAction == null)
            return;

        var attack = combatAction.m_ThisTimeAttack as AttackPattern_Ready;

        if (attack == null)
            return;

        if (m_ReadyAttackPattern.Contains(attack))
            m_ReadyAttackPattern.Remove(attack);

        attack.StartAttackFail(m_ControllableObject, m_ControllableObject.m_Target);

        // Animation
        m_ControllableObject.GetAnimationManager().AttackReadyFail();

        // Sound
        m_ControllableObject.GetSounderManager().AttackReadyFailPlay();

        if (m_AttackReadyItemObject != null)
        {
            m_AttackReadyItemObject.Destroy();
            m_AttackReadyItemObject = null;
        }

        // 이번 타임 공격 제거
        m_ControllableObject.GetAction<CombatAction>().ChangeAttack(null);
    }

    public void AttackReadyFailEnd()
    {
        var combat = m_ControllableObject.GetAction<CombatAction>();
        combat.m_ThisTimeAttack?.EndAttackFail();
        combat.m_ThisTimeAttack = null;
        combat.ActiveSet(false);
    }
}
