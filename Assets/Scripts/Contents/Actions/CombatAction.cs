using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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

        OnStartAttack += (s, e) =>  GridSystemVisual.Instance.UpdateGridVisual_Event(s, m_BaseObject);
        OnEndAttack += (s, e) =>  GridSystemVisual.Instance.UpdateGridVisual_Event(s, m_BaseObject);
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

        if (m_BaseObject is PassiveObject pobj)
        {
            // 현재 공격 후보들 추리기
            // cooltime, mana의 경우 대기 할지 말지 용도로 뽑기
            var usablePatterns = Managers.Game.EvaluateAttackPatternsByCondition
                                (pobj,
                                 null,
                                 E_AttackCondition.Success);


            if (usablePatterns == null || usablePatterns.Count == 0)
            {
                Debug.Log($"{m_BaseObject}가 현재 공격할 수 있는 공격 패턴이 없습니다. 대기합니다.");
                return;
            }

            var toAttack = usablePatterns.RandomPick();
            //Debug.Log($"{pobj}가 현재 선택한 공격 {toAttack.pattern}");
            ChangeAttack(toAttack.pattern);

            // Event (Animation, Sound) 실행
            OnStartAttackEventInvoke();
        }
        else if (m_BaseObject is ControllableObject cobj)
        {
            // 2. 타겟 거리 확인 및 공격 방식 결정
            var target = cobj.m_Target;

            // 2.1 Live or Dead?
            if (target == null || target.m_AttributeSystem.m_IsDead)
            {
                target = null;
                return;
            }


            // 현재 공격 후보들 추리기
            // cooltime, mana의 경우 대기 할지 말지 용도로 뽑기
            var usablePatterns = Managers.Game.EvaluateAttackPatternsByCondition
                                (cobj,
                                 cobj.m_Target,
                                 E_AttackCondition.Success,
                                 E_AttackCondition.Fail_CoolTime,
                                 E_AttackCondition.Fail_ManaCost);


            if (usablePatterns == null || usablePatterns.Count == 0)
            {
                //Debug.Log($"{m_BaseObject}가 현재 공격할 수 있는 공격 패턴이 없습니다. 추격으로 돌아갑니다.");
                m_TODOChangeAction = m_BaseObject.GetAction<ChaseAction>();
                return;
            }

            var rightUseAttack = usablePatterns.Where(p => p.condition == E_AttackCondition.Success);

            // 당장 공격할 수 있는게 있다면
            if (rightUseAttack.Count() > 0)
            {
                var toAttack = rightUseAttack.RandomPick();
                //Debug.Log($"{m_BaseObject}가 현재 선택한 공격 {toAttack.pattern}");
                ChangeAttack(toAttack.pattern);
            }
            // 현재 마나랑 쿨타임으로 인해 공격하지 못하고 있다면 대기
            else
            {
                //Debug.Log($"{m_BaseObject}의 가지고 있는 공격들이 쿨타임과 마나 때문에 대기하고 있음");
                return;
            }

            // Event (Animation, Sound) 실행
            OnStartAttackEventInvoke();
        }
        else
        {

        }




    }

    private bool RotateTowardTarget()
    {
        GameEntity target = null;

        if (m_BaseObject is PassiveObject pobj)
        {
            return true;
        }
        else if (m_BaseObject is ControllableObject cobj)
        {
            target = cobj.m_Target;
        }
        else
        {
            return true;
        }


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
        if (m_BaseObject is PassiveObject pobj)
        {
            return this;
        }
        else if (m_BaseObject is ControllableObject cobj)
        {
            if (cobj.m_Target == null || cobj.m_Target.m_AttributeSystem.m_IsDead)
            {
                // 커맨드 어택 수행 도중 적이 죽어 있다면 초기화
                cobj.m_isDetectionsurroundingsEnabled = true;

                return cobj.GetAction<IdleAction>();
            }

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

    public void OnStartAttackEventInvoke()
    {
        GameEntity target = null;

        if (m_BaseObject is PassiveObject pobj)
        {

        }
        else if (m_BaseObject is ControllableObject cobj)
        {
            target = cobj.m_Target;
        }
        else
        {

        }

        m_ThisTimeAttack.StartAttack(m_BaseObject, target, m_PrevAttackPattern);

        OnStartAttack?.Invoke(this, new OnAttackBaseEventArgs()
        {
            attackPattern = m_ThisTimeAttack
        });

        m_bIsActive = true;
    }

    public void OnEndAttackEventInvoke()
    {
        GameEntity target = null;

        if (m_BaseObject is PassiveObject pobj)
        {

        }
        else if (m_BaseObject is ControllableObject cobj)
        {
            target = cobj.m_Target;
        }
        else
        {

        }

        m_ThisTimeAttack?.EndAttack(m_BaseObject, target);

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
