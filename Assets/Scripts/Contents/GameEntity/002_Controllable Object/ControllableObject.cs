using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;
using static Util;   // 👈 추가!

[RequireComponent(typeof(ControllableObjectCombatManager), typeof(SetupAnimation) /*,typeof(Poolable)*/)]
public class ControllableObject : GameEntity, IAccessories<ControllableObjectAnimator, ControllableObjectSounder>
{
    public event EventHandler<OnChangeGradeEventArgs> OnChangeGrade;
    public class OnChangeGradeEventArgs: EventArgs
    {
        public E_ObjectGrade objGrade;
        public E_ObjectEnhanceType gradeEnhanceType;
        public float enhanceValue;
        public bool isSuccessGrade;
    }

    [Header("Ref")]
    protected List<ControllableObjectAnimator> m_ControllableObjectAnimator;
    protected ControllableObjectSounder m_ControllableObjectSounder;
    public ControllableObjectCombatManager m_ControllableObjectCombatManager { get; private set; }

    [Header("Action")]
    [SerializeField] public BaseAction m_CommandAction;

    public GameEntity m_Target { get; protected set; }
    protected StatBarUI m_StatBarUI;

    [Header("Flag")]
    public bool IsAttackStand;
    public bool m_isDetectionsurroundingsEnabled = true; // 주위 적 탐색이 가능한가?
    public bool m_isChaseCore = true; // 몬스터는 항상 코어를 찾는가? 임시

    public E_MoveType m_EMoveType { get; private set; }

    [Header("Grade")]
    public E_ObjectGrade m_originalGrade;
    [SerializeField] [Range(0, 100)] private float m_fEnhanceChance;
    [SerializeField] private List<E_ObjectEnhanceType> n_EnhanceTypeList;
    

    protected override void Awake()
    {
        base.Awake();

        m_ControllableObjectCombatManager = GetComponent<ControllableObjectCombatManager>();

        m_AttributeSystem.OnDead += ClearAction;
        m_AttributeSystem.OnDead += (s, e) => Death();

        m_ControllableObjectAnimator = GetComponentsInChildren<ControllableObjectAnimator>().ToList();
        m_ControllableObjectSounder = GetComponent<ControllableObjectSounder>();

        m_StatBarUI = GetComponentInChildren<StatBarUI>();

        // Event
        OnChangeGrade += ChangeMaterialOfGrade;
    }

    protected override void Start()
    {
        base.Start();
    }

    /// <summary>
    /// 초기 상태 액션 설정
    /// </summary>
    protected virtual void InitStateAction()
    {
        // Base Action
        SwitchToNextStateAction(GetAction<IdleAction>());
    }

    public override void SpawnComplete()
    {
        base.SpawnComplete();

        InitStateAction();

        // UnitActionSystem
        UnitActionSystem.Instance.OnUpdateActionTick += ExecuteAction;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (UnitActionSystem.Instance != null)
            UnitActionSystem.Instance.OnUpdateActionTick -= ExecuteAction;
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
    protected override void ExecuteAction(object sender, GridPosition args)
    {
        if (m_AttributeSystem.m_IsDead)
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

    public override void SwitchToNextStateAction(BaseAction nextAction)
    {
        base.SwitchToNextStateAction(nextAction);

        UpdateMoveState();
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
                return m_AttributeSystem.m_Stat.m_fWalkSpeed;
            case E_MoveType.Run:
                return m_AttributeSystem.m_Stat.m_fChaseSpeed;
            default:
                return 0;
        }
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
        return m_AttributeSystem.m_AttackPatterns.Where(x => x.ID == id).FirstOrDefault();
    }

    public List<AttackPattern> GetAttacksBaseByIDs(int[] ids)
    {
        // LINQ 쿼리 한 줄로 끝!
        // m_StatSystem.m_Stat.attackPatterns 중에서
        // attack의 ID가 ids 배열에 포함되어 있는 것들만 골라서 리스트로 만들어줘!
        return m_AttributeSystem.m_AttackPatterns
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


    public new List<ControllableObjectAnimator> GetAnimationsManager()
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

    #region Grade

    // 등급 강화 시도
    public void TryEnhanceGrade()
    {
        if(n_EnhanceTypeList.Count == 0)
        {
            Debug.Log($"강화 타입이 없습니다. {name}");
            return;
        }

        float value = Mathf.Round(UnityEngine.Random.Range(0f, 100f) * 100f) / 100f;
        E_ObjectGrade toGrade;

        // 강화 성공
        if (value < m_fEnhanceChance)
        {
            toGrade = E_ObjectGrade.Elite;
        }
        // 원래 등급으로
        else
        {
            toGrade = m_originalGrade;
        }

        OnChangeGrade?.Invoke(this, new OnChangeGradeEventArgs
        {
            objGrade = toGrade,
            enhanceValue = GetRandomValue(1.2f, 1.5f, 0.1f),
            gradeEnhanceType = n_EnhanceTypeList.RandomPick(),
            isSuccessGrade = toGrade != m_originalGrade
        });
    }

    // 등급 변화에 따른 변화
    private void ChangeMaterialOfGrade(object sender, OnChangeGradeEventArgs args)
    {
        switch (args.objGrade)
        {
            case E_ObjectGrade.Normal:
                ChangeMaterialOutlineColor(GetModelsMaterial(), Color.white);   // 아웃라인 효과
                break;
            case E_ObjectGrade.Elite:
                ChangeMaterialOutlineColor(GetModelsMaterial(), Color.yellow);   // 아웃라인 효과
                break;
            case E_ObjectGrade.Boss:
                ChangeMaterialOutlineColor(GetModelsMaterial(), Color.red);    // 아웃라인 효과 
                break;
            default:
                break;
        }
    }

    private void ChangeMaterialOutlineColor(IEnumerable<(Material, GameObject obj)> materials, Color color)
    {
        foreach (var material in materials)
        {
            if(material.Item1.HasProperty("_OutlineColor"))
            {
                material.Item1.SetColor("_OutlineColor", color);
            }
        }
    }

    #endregion
}
