using Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Define;
using static Util;


[RequireComponent(typeof(ControllableObjectCombatManager), typeof(SetupAnimation), typeof(Poolable))]
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
    public E_ObjectGrade m_originalEObjectGrade; //원래 등급
    public E_ObjectGrade m_EObjectGrade; //조정된 등급
    public OnChangeGradeEventArgs m_OnChangeGradeEventArgs; // 조정 수치
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

        if(m_originalEObjectGrade != m_EObjectGrade)
        {
            OnChangeGrade?.Invoke(this, m_OnChangeGradeEventArgs);
        }
    }

    public override void SpawnComplete()
    {
        base.SpawnComplete();

        // Base Action
        if(m_CurrentAction == null)
            SwitchToNextStateAction(GetAction<IdleAction>());

        // UnitActionSystem
        UnitActionSystem.Instance.OnUpdateActionTick += ExecuteAction;
    }

<<<<<<< HEAD

=======
>>>>>>> develop
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

        // 강화 성공
        if (value < m_fEnhanceChance)
        {
            m_EObjectGrade = E_ObjectGrade.Elite;
        }
        // 원래 등급으로
        else
        {
            m_EObjectGrade = m_originalEObjectGrade;
        }

        m_OnChangeGradeEventArgs = new OnChangeGradeEventArgs()
        {
            objGrade = m_EObjectGrade,
            enhanceValue = GetRandomValue(1.2f, 1.5f, 0.1f),
            gradeEnhanceType = n_EnhanceTypeList.RandomPick(),
            isSuccessGrade = m_EObjectGrade != m_originalEObjectGrade
        };

        // 업그레이드 실행
        OnChangeGrade?.Invoke(this, m_OnChangeGradeEventArgs);
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

    #region Data Save & Load

    public override BaseData CaptureSaveData()
    {
        var baseData =  base.CaptureSaveData() as GameEntityData;

        return new ControllableObjectData()
        {
            // 공통 필드 복사
            prefabName = baseData.prefabName,
            position = baseData.position,
            rotation = baseData.rotation,
            guid = baseData.guid,
            attributeSystemData = baseData.attributeSystemData,
            gradeArgs = m_OnChangeGradeEventArgs,

            // 하위 클래스 고유 데이터 추가
            attackReadyItemData =
                m_ControllableObjectCombatManager?.m_AttackReadyItemObject.Select(item => item.obj.CaptureSaveData()).ToList(),

            readyAttackPatternData =
               m_ControllableObjectCombatManager?.m_ReadyAttackPattern != null
                   ? m_ControllableObjectCombatManager.m_ReadyAttackPattern
                       .Select(attack => attack?.CaptureSaveData())
                       .Where(data => data != null)
                       .ToHashSet()
                   : new HashSet<AttackPatternData>(),

            targetGuid = m_Target?.guid
        };
    }

    public override void RestoreSaveData(BaseData data)
    {
        base.RestoreSaveData(data);

        ControllableObjectData cData = data as ControllableObjectData;

        // readyAttackPatternData가 null이 아니고, 비어있지 않을 때만 복원
        if (cData.readyAttackPatternData != null && cData.readyAttackPatternData.Count > 0)
        {
            m_ControllableObjectCombatManager.m_ReadyAttackPattern =
                m_AttributeSystem.m_AttackPatterns
                    .Where(a => cData.readyAttackPatternData.Any(b => a.ID == b.id))
                    .OfType<AttackPattern_Ready>() // 타입 안전 변환
                    .ToHashSet();
        }

        m_OnChangeGradeEventArgs = cData.gradeArgs;
        m_EObjectGrade = m_OnChangeGradeEventArgs.objGrade;

        SetTarget(Managers.Object.FindByGuidObject<GameEntity>(cData.targetGuid));
    }

    #endregion
}
