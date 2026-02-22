using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class CombatAction : BaseAction
{
    CombatAction()
    {
        m_actionName = "Combat";
    }

    public event Action<AttackData> OnStartAttack;
    public event Action OnEndAttack;
    //public event Action OnAttackCancel;
    public event Action OnPhaseChange;

    Func<bool> conditionPase;
    bool isChaningPase;

    BaseAction m_TODOChangeAction;

    public AttackData m_ThisTimeAttack;
    public AttackData m_PrevAttackPattern;

    private void OnEnable()
    {
        OnStartAttack += DrawGridVisual;
        OnEndAttack += DrawGridVisual;
    }

    private void OnDisable()
    {
        OnStartAttack -= DrawGridVisual;
        OnEndAttack -= DrawGridVisual;
    }

    protected override void Update()
    {
        base.Update();

        #region Check List

        if (m_GameEntity.m_CurrentAction != this)
            return;

        // 적을 향해 마주보고 있지 않으면 회전
        if (!m_GameEntity.RotateTowardTarget())
            return;

        // 애니메이션이 진행중이면 대기
        if (m_bIsActive)
            return;

        if (isChaningPase)
            return;

        #endregion

        // 1. 유닛이 특수 상태일 경우 페이즈 전환 (예: 2페이즈 보스)
        HandlePhaseTransition();

        // 페이즈 대기

        // 현재 공격 후보들 추리기
        // cooltime, mana의 경우 대기 할지 말지 용도로 뽑기
        var usablePatterns = Managers.Game.EvaluateAttackPatternsByCondition
                            (m_GameEntity,
                             m_GameEntity.m_Target,
                             E_AttackCondition.Success,
                             E_AttackCondition.Fail_CoolTime,
                             E_AttackCondition.Fail_Distance,
                             E_AttackCondition.Fail_ManaCost);


        if (!usablePatterns.Any())
        {
            //Debug.Log($"{m_GameEntity}가 현재 공격할 수 있는 공격 패턴이 없습니다. 추적 모드로 돌아갑니다.");
            m_TODOChangeAction = m_GameEntity.GetAction<ChaseAction>();

            return;
        }

        // 조건별 그룹화
        var grouped = usablePatterns
        .GroupBy(r => r.condition)
        .ToDictionary(g => g.Key, g => g.ToList());

        // 1. Success가 하나라도 있는가?
        if (grouped.TryGetValue(E_AttackCondition.Success, out var successList))
        {
            // 등급을 매겨서 우선순위 정하기
            var toAttack = successList.OrderBy(list => list.pattern.m_iPriority).First();
            
            //Debug.Log($"{m_GameEntity}가 현재 선택한 공격 {toAttack.pattern}");
            ChangeAttack(toAttack.pattern);

            // Event (Animation, Sound) 실행
            OnStartAttackEventInvoke();
        }

        // 2. Fail_Distance가 있는가?
        else if (grouped.TryGetValue(E_AttackCondition.Fail_Distance, out var distList))
        {
            if(m_GameEntity.m_AttributeSystem.m_CanMoveableGameEntity)
                m_TODOChangeAction = m_GameEntity.GetAction<ChaseAction>();
        }

        // 3. Fail_CoolTime / Fail_ManaCost
        else if (grouped.TryGetValue(E_AttackCondition.Fail_CoolTime, out var coolList)
                || grouped.TryGetValue(E_AttackCondition.Fail_ManaCost, out var manaList))
        {
            // 대기
        }
    }

    public override BaseAction TakeAction(GridPosition gridPosition = default)
    {
        if (m_bIsActive)
            return this;

        if (m_TODOChangeAction != null)
        {
            BaseAction ac = m_TODOChangeAction;
            m_TODOChangeAction = null;
            return ac;
        }
        else
            return this;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        throw new NotImplementedException();
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        throw new NotImplementedException();
    }

    public void HandlePhaseTransition()
    {
        if(conditionPase != null && conditionPase.Invoke())
        {
            OnPhaseChange?.Invoke();
        }
    }

    public void OnStartAttackEventInvoke()
    {
        var pattern = Managers.Game.AttackPattern(m_ThisTimeAttack);
        pattern.StartAttack(m_GameEntity, m_GameEntity.m_Target, m_ThisTimeAttack, m_PrevAttackPattern);
        //m_ThisTimeAttack.StartAttack(m_GameEntity, m_GameEntity.m_Target, m_PrevAttackPattern);

        OnStartAttack?.Invoke(m_ThisTimeAttack);

        m_bIsActive = true;
    }

    public void OnEndAttackEventInvoke()
    {
        if (m_ThisTimeAttack != null)
        {
            var pattern = Managers.Game.AttackPattern(m_ThisTimeAttack);
            pattern.EndAttack(m_GameEntity, m_GameEntity.m_Target, m_ThisTimeAttack);
        }
        //m_ThisTimeAttack?.EndAttack(m_GameEntity, m_GameEntity.m_Target);

        OnEndAttack?.Invoke();

        m_bIsActive = false;

        m_TODOChangeAction = null;

        // 만약 다음 다음 어택이 있다면 교체
        if (m_ThisTimeAttack?.NextAttacks.Length == 0)
        {
            m_ThisTimeAttack = null;
        }
    }

    public override void ClearAction()
    {
        base.ClearAction();

        m_bIsActive = false;
    }

    public void ActiveSet(bool isFalse)
    {
        m_bIsActive = isFalse;
    }

    public void ChangeAttack(AttackData todoAttack)
    {
        m_PrevAttackPattern = m_ThisTimeAttack;
        m_ThisTimeAttack = todoAttack;
    }


}
