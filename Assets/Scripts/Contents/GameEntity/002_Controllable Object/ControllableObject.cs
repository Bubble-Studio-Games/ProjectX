using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Define;

[RequireComponent(typeof(ControllableObjectCombatManager), typeof(SetupAnimation), typeof(Poolable))]
public class ControllableObject : GameEntity, IAccessories<ControllableObjectAnimator, ControllableObjectSounder>
{
    //[Header("Event")]
    public static event EventHandler OnAnyActionPointsChanged;

    public ControllableObjectCombatManager m_ControllableObjectCombatManager { get; private set; }

    public E_MoveType m_EMoveType { get; private set; }

    [Header("Action")]
    private Dictionary<Type, BaseAction> baseActionDict = new Dictionary<Type, BaseAction>();
    [SerializeField] private BaseAction currentAction;
    public BaseAction m_CurrentAction
    {
        get => currentAction;
        protected set => currentAction = value;
    }

    [SerializeField] private BaseAction m_NextAction;
    [SerializeField] private BaseAction m_BeforeAction;
    [SerializeField] public BaseAction m_CommandAction;

    public GameEntity m_Target { get; protected set; }

    protected ControllableObjectAnimator m_ControllableObjectAnimator;
    protected ControllableObjectSounder m_ControllableObjectSounder;

    [Header("Flag")]
    public bool IsAttackStand;
    public bool m_isDetectionsurroundingsEnabled = true; // 주위 적 탐색이 가능한가?

    protected override void Awake()
    {
        base.Awake();
        foreach (var action in GetComponentsInChildren<BaseAction>())
              baseActionDict[action.GetType()] = action;

        m_ControllableObjectCombatManager = GetComponent<ControllableObjectCombatManager>();

        m_StatSystem.OnDead += ClearAction;
        m_StatSystem.OnDead += (s, e) => Death();

        m_ControllableObjectAnimator = GetComponentInChildren<ControllableObjectAnimator>();
        m_ControllableObjectSounder = GetComponent<ControllableObjectSounder>();
    }

    public override void SpawnComplete()
    {
        base.SpawnComplete();

        // Base Action
        SwitchToNextStateAction(GetAction<IdleAction>());

        // UnitActionSystem
        UnitActionSystem.Instance.OnUpdateActionTick += ExecuteAction;
    }

    protected override void Update()
    {
        base.Update();

        if (m_IsSetuping)
            return;

        UpdateGridPosition();
    }

    #region Action

    // UnitActionSystem에서 관리
    private void ExecuteAction(object sender, GridPosition args)
    {
        if (m_StatSystem.m_IsDead)
            return;

        // 커맨드 명령이 들어왔을 때 최초 1회 실행 이후로는 else 문에서 반복 실행.
        if (m_CommandAction != null)
        {
            m_CurrentAction.ClearAction();
            SwitchToNextStateAction(m_CommandAction);
            m_CommandAction = null;

            m_CurrentAction?.TakeAction(args);
        }
        else
        {
            m_NextAction = m_CurrentAction?.TakeAction();

            if (m_NextAction is not null && m_NextAction != m_CurrentAction)
            {
                m_CurrentAction.ClearAction();
                SwitchToNextStateAction(m_NextAction);
            }
        }

    }

    public void SwitchToNextStateAction(BaseAction nextAction)
    {
        m_CurrentAction = nextAction;

        UpdateMoveState();
    }

    public BaseAction GetBackStateAction()
    {
        if(m_BeforeAction == null)
        {
            return GetAction<IdleAction>();
        }
        else
        {
            return m_BeforeAction;
        }
    }

    private void UpdateMoveState()
    {
        m_EMoveType = E_MoveType.Idle;

        if (m_CurrentAction is ChaseAction || m_CurrentAction is CommandMoveAction)
        {
            if (m_TeamId == E_TeamId.Monster)
            {
                if (m_Target == DungeonCore.instance)
                    m_EMoveType = E_MoveType.Walk;
                else
                    m_EMoveType = E_MoveType.Run;
            }
            else
                m_EMoveType = E_MoveType.Run;
        }
    }

    public float GetMoveSpeed()
    {
        switch (m_EMoveType)
        {
            case E_MoveType.Idle:
                return 0;
            case E_MoveType.Walk:
                return m_StatSystem.m_Stat.m_fWalkSpeed;
            case E_MoveType.Run:
                return m_StatSystem.m_Stat.m_fChaseSpeed;
            default:
                return 0;
        }
    }


    public void ClearAction(object sender, EventArgs e)
    {
        m_CurrentAction = null;
    }

    public IEnumerable<BaseAction> GetActions()
    {
        return baseActionDict.Values;
    }

    public T GetAction<T>() where T : BaseAction
    {
        if (baseActionDict.TryGetValue(typeof(T), out var action))
            return action as T;
        return null;
    }

    public void DirectCommand<TAction>(BaseAction action, Action<ControllableObject, TAction> onActionComplete) where TAction : BaseAction
    {
        m_BeforeAction = m_CurrentAction;
        m_CommandAction = action;

        if (action is TAction typedAction)
        {
            action.SetActionComlete(() => onActionComplete?.Invoke(this, typedAction));
        }
    }

    #endregion

    #region Battle

    public AttackPattern GetAttackBaseByID(int id)
    {
        return m_StatSystem.m_Stat.m_AttackPatterns.Where(x => x.ID == id).FirstOrDefault();
    }

    public List<AttackPattern> GetAttacksBaseByIDs(int[] ids)
    {
        // LINQ 쿼리 한 줄로 끝!
        // m_StatSystem.m_Stat.attackPatterns 중에서
        // attack의 ID가 ids 배열에 포함되어 있는 것들만 골라서 리스트로 만들어줘!
        return m_StatSystem.m_Stat.m_AttackPatterns
            .Where(attack => ids.Contains(attack.ID))
            .ToList();
    }

    public List<AttackPattern> GetAttacksBaseByIDs(AttackPattern[] patterns)
    {
        // 비교용 ID 배열 추출
        var ids = patterns.Select(p => p.ID).ToArray();

        return GetAttacksBaseByIDs(ids);
    }

    public virtual void SetTarget(GameEntity target)
    {
        m_Target = target;
    }


    public new ControllableObjectAnimator GetAnimationManager()
    {
        return m_ControllableObjectAnimator;
    }

    public new ControllableObjectSounder GetSounderManager()
    {
        return m_ControllableObjectSounder;
    }

    #endregion

    public void Death()
    {
        if(UnitActionSystem.Instance.m_SelectedObjects.Contains(this))
        {
            UnitActionSystem.Instance.m_SelectedObjects.Remove(this);
        }
    }

}
