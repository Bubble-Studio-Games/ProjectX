using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassiveObjectAnimator : GameEntityAnimator
{
    private PassiveObject m_PassiveObject;

    protected override void Awake()
    {
        base.Awake();
        m_PassiveObject = GetComponentInParent<PassiveObject>();
    }

    // 애니메이션 이벤트에서 호출한다.
    public override void AttackPoint()
    {
        base.AttackPoint();

        if (!_attackValid)
            return; // 부모에서 실패하면 즉시 중단

        // 어택
        var combatAction = m_PassiveObject.GetAction<CombatAction>();
        if(combatAction != null )
        {
            combatAction.m_ThisTimeAttack.Attack(m_PassiveObject, null);
        }
        else
            _attackValid = false;
    }
}
