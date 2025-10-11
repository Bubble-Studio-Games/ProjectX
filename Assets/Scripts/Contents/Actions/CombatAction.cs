using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public class CombatAction : BaseAction
{
    public event EventHandler<OnAttackBaseEventArgs> OnStartAttack;
    public event EventHandler OnEndAttack;
    public event EventHandler OnAttackCancel;
    public event EventHandler OnPhaseChange;

    public class OnAttackBaseEventArgs : EventArgs
    {
        public AttackPattern attackPattern;
    }

    Func<bool> conditionPase;
    bool isChaningPase;
    float m_fRotateTimer = 0;
    [SerializeField] float m_rotateTick = 0.1f;
    [SerializeField] float rotateSpeed = 70;

    BaseAction m_TODOChangeAction;

    public AttackPattern m_ThisTimeAttack;
    public AttackPattern m_PrevAttackPattern;
    [SerializeField] float attackReayTime = 0;

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();

        #region Condition

        if (m_BaseObject.m_CurrentAction != this)
            return;

        if (RotateTowardTarget() == false)
            return;

        // 애니메이션이 진행중이면 대기
        if (m_bIsActive)
            return;

        if (isChaningPase)
            return;

        #endregion

        // 1. 유닛이 특수 상태일 경우 페이즈 전환 (예: 2페이즈 보스)
        HandlePhaseTransition();

        // 2. 타겟 거리 확인 및 공격 방식 결정
        var target = m_BaseObject.m_Target;

        // 2.1 Live or Dead?
        if (target == null || target.m_AttributeSystem.m_IsDead)
        {
            target = null;
            return;
        }

        // 3. Attack Pattern
        AttackPattern todoAttack = null;
        List<AttackPattern> todoAttackList = new();

        // 직전 타임에 준비했던 공격 기술이 있다면
        // 다음 스텝으로
        if (m_ThisTimeAttack != null)
            todoAttackList = m_BaseObject.GetAttacksBaseByIDs(m_ThisTimeAttack.m_iNextAttackPattern);
        else
            todoAttackList = m_BaseObject.m_AttributeSystem.m_AttackPatterns;

        todoAttack = SelectAttackPattern(todoAttackList);

        if (todoAttack == null)
        {
            bool isStand = todoAttackList.Any(attack =>
                attack.CanExecute(m_BaseObject, m_BaseObject.m_Target) is
                    E_AttackCondition.Fail_CoolTime or E_AttackCondition.Fail_ManaCost);


            // 현재 모든 공격이 쿨타임이라면 대기
            if (isStand)
            {
                return;
            }
            else
            {
                // 현재 위치에서 공격할 수 있는 공격이 없음.
                m_TODOChangeAction = m_BaseObject.GetAction<ChaseAction>();
                //Debug.Log($"m_ThisAttackPattern is Nul!!!!");
                return;
            }
        }

        ChangeAttack(todoAttack);

        // Event (Animation, Sound) 실행
        OnStartAttackEventInvoke();
    }

    private bool RotateTowardTarget()
    {
        var target = m_BaseObject.m_Target;
        if (target == null)
            return false;

        // 타겟 방향 계산
        Vector3 moveDirection = (target.transform.position - m_BaseObject.transform.position).normalized;

        // 회전 완료 여부 판단
        float angleThreshold = 5f; // 허용 오차 각도 (예: 5도)
        float angle = Vector3.Angle(m_BaseObject.transform.forward, moveDirection);

        if (angle < angleThreshold)
        {
            return true;
        }
        else
        {
            m_fRotateTimer -= Time.deltaTime;
            if(m_fRotateTimer <= 0)
            {
                m_fRotateTimer = m_rotateTick;

                // 회전
                m_BaseObject.transform.forward = Vector3.Slerp(
                    m_BaseObject.transform.forward,
                    moveDirection,
                    Time.deltaTime * rotateSpeed
                );
            }

            return false;
        }
    }

    public override BaseAction TakeAction(GridPosition gridPosition = default, Action onActionComplete = null)
    {
       // if (m_BaseObject.m_TeamId == E_TeamId.Player)
        {
            if (m_BaseObject.m_Target == null || m_BaseObject.m_Target.m_AttributeSystem.m_IsDead)
            {
                // 커맨드 어택 수행 도중 적이 죽어 있다면 초기화
                m_BaseObject.m_isDetectionsurroundingsEnabled = true;
                //m_BaseObject.SetTarget(null);

                return m_BaseObject.GetAction<IdleAction>();
            }
        }
        //else if (m_BaseObject.m_TeamId == E_TeamId.Monster)
        {

        }

        // TODO 
        // 적과 싸우는 도중에 더 가까운 적이 나타나면 타겟 변경?

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

    public override string GetActionName()
    {
        return "Combat";
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
            OnPhaseChange?.Invoke(this, EventArgs.Empty);
        }
    }

    private AttackPattern SelectAttackPattern(List<AttackPattern> patterns)
    {
        E_Dir dir = LevelGrid.Instance.GetDirGridPosition(m_BaseObject.GetGridPosition(), m_BaseObject.m_Target.GetGridPosition());

        var validPatterns = patterns
            .Where(attack => attack.CanExecute(m_BaseObject, m_BaseObject.m_Target) == E_AttackCondition.Success)
            .ToList();
        
        validPatterns = validPatterns.Where(attack =>
        {
            if (attack.m_EAttackType == E_AttackType.Summon)
                return true;
            
            return attack.m_RangeOffset.Any(offset =>
                LevelGrid.Instance.ToGridPosition(offset, m_BaseObject.GetGridPosition(), dir) == m_BaseObject.m_Target.GetGridPosition());
        }).ToList();

        if (validPatterns.Count == 0)
            return null; // 공격 가능한 패턴이 없다면 null

        Console.WriteLine("가능한 공격들 : " + string.Join(" ", validPatterns));

        // 무작위로 하나 선택
        int index = UnityEngine.Random.Range(0, validPatterns.Count);
        return validPatterns[index];
    }

    public void OnStartAttackEventInvoke()
    {
        m_ThisTimeAttack.StartAttack(m_BaseObject, m_BaseObject.m_Target, m_PrevAttackPattern);

        OnStartAttack?.Invoke(this, new OnAttackBaseEventArgs()
        {
            attackPattern = m_ThisTimeAttack
        });

        m_bIsActive = true;
    }

    public void OnEndAttackEventInvoke()
    {
        m_ThisTimeAttack?.EndAttack(m_BaseObject, m_BaseObject.m_Target);

        OnEndAttack?.Invoke(this, EventArgs.Empty);

        m_bIsActive = false;

        m_TODOChangeAction = null;

        // 만약 다음 다음 어택이 있다면 교체
        if (m_ThisTimeAttack?.m_iNextAttackPattern.Length == 0)
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

    public void ChangeAttack(AttackPattern todoAttack)
    {
        m_PrevAttackPattern = m_ThisTimeAttack;
        m_ThisTimeAttack = todoAttack;
    }


}
