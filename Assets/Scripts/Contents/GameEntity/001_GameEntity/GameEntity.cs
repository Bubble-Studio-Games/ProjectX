using Data;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Define;
using Material = UnityEngine.Material;

[RequireComponent(typeof(AttributeSystem))]
public class GameEntity : MonoBehaviour,
    ISaveable, 
    IGuidObject,
    ISelectable
{
    #region Field

    // Spawn & DeSpawn
    public event Action OnSpawnObjectSelected; // 오브젝트를 배치하려고 할 때
    public event Action OnObjectSpawnStart; // 씬에 생성되거나 활성화될 때
    public event Action OnSpawnCompleted; // 씬에 생성되거나 활성화될 때
    public event Action OnObjectDespawned; // 파괴되거나 비활성화될 때

    // Select
    public event Action OnSelectedEvent;
    public event Action OnDeselectedEvent;

    // Attribute
    public event Action<OnAttackInfoEventArgs> OnDamaged;
    public event Action<OnAttackInfoEventArgs> OnDead;
    public event Action OnRevived;

    // Action
    public event Action<Type> OnActionChanged;

    // Battle
    public event Action<AttackData> OnStartAttack;
    public event Action<AttackData> OnAttackPoint;
    public event Action<AttackData> OnAttackReadyFailed;
    public event Action OnPhaseChange;

    // Move
    public event Action OnStartMoving;
    public event Action OnStopMoving;
    public event Action OnStep;
    public event Action<OnChangeFloorsStartedEventArgs> OnChangedFloorsStarted;

    private string _guid;
    public string guid => _guid;

    public GameEntityAnimator m_GameEntityAnimator { get; private set; }
    public AttributeSystem m_AttributeSystem { get; private set; }
    public Collider m_HitCollider { get; protected set; }
    private SetupAnimation m_SetupAnimation;
    public GameEntityCombat m_CombatManager { get; private set; }
    public ProjectileSpawnProvider m_ProjectileSpawnProvider { get; private set; }

    private HashSet<(Material mat, GameObject obj)> m_ModelMaterials = new();

    [Header("Info")]
    public GridPosition[] m_GridPositionOffsets;
    private GridPosition m_GridPosition;
    public E_ObjectType m_EObjectType;
    public E_Dir m_CurrentEDir = E_Dir.South;
    public E_TeamId m_TeamId;

    [Header("Action")]
    [SerializeField] protected BaseAction currentAction;
    public BaseAction m_CurrentAction
    {
        get => currentAction;
        protected set => currentAction = value;
    }
    protected BaseAction m_NextAction;
    protected BaseAction m_BeforeAction;

    private CombatAction _combatAction;
    private MoveAction[] _moveActions;

    protected Dictionary<Type, BaseAction> baseActionDict = new Dictionary<Type, BaseAction>();

    [Tooltip("현재 객체에 할당된 Action 큐")]
    protected Queue<(BaseAction action, GridPosition grid)> m_ActionQueue = new();
    
    // 지정 명령 예약
    // 1, 2 번의 Action이 들어왔을 때 1번 Action이 완전히 끝나면 2번 Action이 실행되게 만든다.
    protected bool m_IsCommandAction = false;

    [Header("Flag")]
    public bool m_IsDirectDesawnAtDeath = true; // 사망 후 바로 디스폰 처리 하는가?
    public bool m_IsSpawning { get; protected set; } = false; // 카드에서 뽑아 소환 중인가?
    private bool _isSpawnRegistered = false;

    [HideInInspector] public bool m_DisableDespawnFlowForAnimTest = false;

    [Tooltip("체크 되어 있을 경우 무조건 던전 코어를 향해 이동 (몬스터 전용)")]
    [HideInInspector] public bool m_IsTowardDungeonCore = false;

    // 전역 캐시 (Prefab 단위)
    private static Dictionary<string, bool> s_RotateSymmetryCache = new();
    public bool m_IsRotateSymmetry { get; private set; }

    // Getter
    public E_MoveType CurrentMoveType => m_AttributeSystem.m_EMoveType;
    public string AnimKeyName => m_AttributeSystem.m_Stat.Name;

    #endregion

    #region Unity Life Cycle

    protected virtual void Awake()
    {
        m_AttributeSystem = GetComponent<AttributeSystem>();
        m_GameEntityAnimator = GetComponentInChildren<GameEntityAnimator>();
        m_CombatManager = GetComponent<GameEntityCombat>();
        m_SetupAnimation = GetComponent<SetupAnimation>();
        m_ProjectileSpawnProvider = GetComponent<ProjectileSpawnProvider>();

        foreach (var action in GetComponentsInChildren<BaseAction>())
            baseActionDict[action.GetType()] = action;

        m_HitCollider = GetChildColliders().FirstOrDefault();
    }

    protected virtual void Start()
    {
        if (string.IsNullOrEmpty(_guid))
            _guid = GUID.Generate().ToString();

        // 해당 조건은 몬스터 전용이다.
        if (m_TeamId != E_TeamId.Monster && m_IsTowardDungeonCore)
        {
            m_IsTowardDungeonCore = false;
            Debug.Log($"{name}의 팀 타입이 변경됩니다. 해당 조건은 팀 타입이 몬스터일 때에만 허용됩니다.");
        }

        // 현재 Command Action에만 지정 명령 종료를 지정함.
        GetActions()
            .Where(action => action.GetComponent<ICommandAction>() != null)
            .ToList()
            .ForEach(a =>
            {
                a.OnActionCompleted += () =>
                {
                    m_IsCommandAction = false;
                    //Debug.Log($"{a.m_actionName}의 실행 종료");
                };
            });

        CheckRotateSymmetry();

        if (m_IsSpawning)
            return;

        // 맵에 그냥 배치되어 있을 경우
        SpawnRegister();   
        SpawnComplete();
    }

    protected virtual void OnEnable()
    {
        if (baseActionDict.Count > 0)
            Managers.Tick.OnUpdateActionTick += ExecuteAction;

        if (m_GameEntityAnimator != null)
        {
            m_GameEntityAnimator.OnSpawnAnimFinished += SpawnComplete;
            m_GameEntityAnimator.OnDespawnAnimFinished += DeSpawnComplete;
            m_GameEntityAnimator.OnStep += HandleStep;
            m_GameEntityAnimator.OnAttackPoint += HandleAttackPoint;
        }

        //m_AttributeSystem.OnDead += ClearAction;
        m_AttributeSystem.OnDamaged += HandleDamaged;
        m_AttributeSystem.OnDead += HandleDead;
        m_AttributeSystem.OnRevived += HandleRevived;

        // CombatAction
        _combatAction = GetAction<CombatAction>();
        if (_combatAction != null)
        {
            _combatAction.OnStartAttack += HandleStartAttack;
            _combatAction.OnPhaseChange += HandlePhaseChange;
        }

        // MoveAction
        _moveActions = GetComponentsInChildren<MoveAction>(true);
        foreach (var move in _moveActions)
        {
            move.OnStartMoving += HandleStartMoving;
            move.OnStopMoving += HandleStopMoving;
            move.OnChangedFloorsStarted += HandleChangedFloorsStarted;
        }
    }

    protected virtual void OnDisable()
    {
        if (baseActionDict.Count > 0)
            Managers.Tick.OnUpdateActionTick -= ExecuteAction;

        if (m_GameEntityAnimator != null)
        {
            m_GameEntityAnimator.OnSpawnAnimFinished -= SpawnComplete;
            m_GameEntityAnimator.OnDespawnAnimFinished -= DeSpawnComplete;
            m_GameEntityAnimator.OnStep -= HandleStep;
            m_GameEntityAnimator.OnAttackPoint -= HandleAttackPoint;
        }

        // ✅ Forwarding 해제
        //m_AttributeSystem.OnDead -= ClearAction;
        m_AttributeSystem.OnDamaged -= HandleDamaged;
        m_AttributeSystem.OnDead -= HandleDead;
        m_AttributeSystem.OnRevived -= HandleRevived;

        if (_combatAction != null)
        {
            _combatAction.OnStartAttack -= HandleStartAttack;
            _combatAction.OnPhaseChange -= HandlePhaseChange;
        }

        if (_moveActions != null)
        {
            foreach (var move in _moveActions)
            {
                move.OnStartMoving -= HandleStartMoving;
                move.OnStopMoving -= HandleStopMoving;
                move.OnChangedFloorsStarted -= HandleChangedFloorsStarted;
            }
        }
    }

    protected virtual void OnDestroy()
    {
    }

    #endregion

    #region Grid

    public List<Collider> GetChildColliders()
    {
        List<Collider> colliders = new List<Collider>();

        // root 포함 모든 자식 탐색
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (((1 << child.gameObject.layer) & GameConfig.Layer.HitColLayerMask) != 0)
            {
                Collider col = child.GetComponent<Collider>();
                if (col != null)
                {
                    colliders.Add(col);
                }
            }
        }

        return colliders;
    }

    public void UpdateGridPosition()
    {
        GridPosition newGridPosition = Managers.Grid.GetGridPosition(transform.position);

        if (newGridPosition != m_GridPosition)
        {
            // Unit changed Grid Position
            List<GridPosition> oldGridPositions = GetGridPositionListAtCurrentDir();
            m_GridPosition = newGridPosition;
            List<GridPosition> newGridPositions = GetGridPositionListAtCurrentDir();

            Managers.Grid.UpdateUnitPosition(this, oldGridPositions, newGridPositions);
        }
    }

    public GridPosition GetGridPosition() => m_GridPosition;

    public (int Min, int Max) GetGridPositionYOffset()
    {
        int min = 0;
        int max = 0;

        if (m_GridPositionOffsets.Length > 0)
        {
            min = m_GridPositionOffsets.Min(offset => offset.floor);
            max = m_GridPositionOffsets.Max(offset => offset.floor);
        }

        return (min, max);
    }
    private void CheckRotateSymmetry()
    {
        string prefabKey = guid;

        // 딕셔너리 2번 조회 대신 TryGetValue 한 번
        if (!s_RotateSymmetryCache.TryGetValue(prefabKey, out bool isSymmetry))
        {
            // enum 0~3: West, South, East, North
            // 첫 방향 기준 좌표 계산
            var basePos = Util.ToGridPosition(m_GridPositionOffsets, m_GridPosition, E_Dir.West);

            isSymmetry = true;

            // 나머지 3방향과 비교
            for (int i = 1; i < 4; i++)
            {
                E_Dir dir = (E_Dir)i; // 1: South, 2: East, 3: North
                var pos = Util.ToGridPosition(m_GridPositionOffsets, m_GridPosition, dir);

                if (pos != basePos)
                {
                    isSymmetry = false;
                    break;
                }
            }

            s_RotateSymmetryCache[prefabKey] = isSymmetry;
        }

        m_IsRotateSymmetry = isSymmetry;
    }

    #endregion

    public IEnumerable<(Material mat, GameObject obj)> GetModelsMaterial()
    {
        if (m_ModelMaterials == null || m_ModelMaterials.Count == 0)
        {
            m_ModelMaterials = GetComponentsInChildren<Renderer>(true)
                .SelectMany(r => r.materials
                    .Where(m => m != null)
                    .Select(m => (mat: m, obj: r.gameObject)))
                .ToHashSet();
        }

        return m_ModelMaterials;
    }


    #region Select

    public void OnDeselected()
    {
        //Debug.Log($"{name} DeSelect");
        OnDeselectedEvent?.Invoke();
    }

    public void OnSelected()
    {
        //Debug.Log($"{name} Select");
        OnSelectedEvent?.Invoke();
    }

    #endregion

    #region Dir

    public E_Dir GetNextDir()
    {
        switch (m_CurrentEDir)
        {
            default:
            case E_Dir.South: return E_Dir.West;
            case E_Dir.West: return E_Dir.North;
            case E_Dir.North: return E_Dir.East;
            case E_Dir.East: return E_Dir.South;
        }
    }

    // GameEntity의 GridOffset과 원점인 GridPosition을 반환한다.
    public List<GridPosition> GetGridPositionListAtCurrentDir()
    {
        return GetGridPositionListAtSelectPosition(m_GridPosition);
    }

    // 지정된 위치인 pivot을 기준으로 GameEntity의 GridOffset을 반영한다.
    public List<GridPosition> GetGridPositionListAtSelectPosition(GridPosition pivot)
    {
        // 피벗 셀 포함해서 점유 셀 리스트 만들기
        var result = new List<GridPosition> { pivot };

        // m_GridPositionOffsets의 오프셋들을 pivot 기준으로 회전 적용
        result.AddRange(Util.ToGridPosition(m_GridPositionOffsets, pivot, m_CurrentEDir));

        return result;
    }

    // GetRotationAngle 함수: 각 방향에 따른 회전 각도를 반환합니다.
    public int GetRotationAngle()
    {
        switch (m_CurrentEDir)
        {
            default:
            case E_Dir.South: return 0;   // 기존 Dir.Down
            case E_Dir.West: return 90;  // 기존 Dir.Left
            case E_Dir.North: return 180; // 기존 Dir.Up
            case E_Dir.East: return 270; // 기존 Dir.Right
        }
    }

    #endregion

    #region Setup & DeSpawn

    // 플레이어가 오브젝트를 배치중일 때
    public void SelectSpawnObject()
    {
        // 타격 콜라이더 끄기
        // Find Hit Collider
        if (m_HitCollider == null)
            m_HitCollider = GetChildColliders().FirstOrDefault();

        m_HitCollider.enabled = false;
        m_IsSpawning = true;

        OnSpawnObjectSelected?.Invoke();
    }

    // 배치 완료 되었을 때 실행됨
    public void SpawnStart()
    {
        // 지금부터 공격 당할 수 있음.
        m_HitCollider.enabled = true;

        // ✅ 등록은 여기서(배치 시작 시점부터 맞을 수 있어야 하니까)
        SpawnRegister();

        OnObjectSpawnStart?.Invoke();

        // 회전 대칭 cache 제거
        if (string.IsNullOrEmpty(_guid))
            _guid = GUID.Generate().ToString();

        s_RotateSymmetryCache.Remove(guid);
    }

    // 소환 완료 후
    public void SpawnComplete()
    {
        // ✅ 씬 배치(Start에서 SpawnComplete만 호출) 같은 경우 대비
        SpawnRegister();

        m_IsSpawning = false;

        // Base Action
        if (m_CurrentAction == null && baseActionDict.Count > 0)
        {
            var action = baseActionDict.First().Value;
            if (action != null)
                SwitchToNextStateAction(action);
        }

        m_HitCollider.enabled = true;
        OnSpawnCompleted?.Invoke();
    }

    private void SpawnRegister()
    {
        if (_isSpawnRegistered) return;
        _isSpawnRegistered = true;

        // 월드에 "존재" 등록
        Managers.Object.Add(gameObject);

        // 그리드 점유 등록
        m_GridPosition = Managers.Grid.GetGridPosition(transform.position);
        Managers.Grid.RequestCell(GetGridPositionListAtCurrentDir(), this, E_EntityCellType.GameEntity);
    }

    private void SpawnUnregister()
    {
        if (!_isSpawnRegistered) return;
        _isSpawnRegistered = false;

        Managers.Grid.ReleaseCell(GetGridPositionListAtCurrentDir(), this);
        Managers.Object.Remove(gameObject);
    }

    // 보통은 디스폰을 사망 애니메이션이 끝나면 바로 호출.
    public void DeSpawnStart()
    {
        if (m_DisableDespawnFlowForAnimTest)
            return;

        OnObjectDespawned?.Invoke();
    }

    // 디스폰 후 호출되는 함수.
    public virtual void DeSpawnComplete()
    {
        SpawnUnregister();

        Managers.Resource.Destroy(gameObject);
        Managers.Selection.Deselect(this);
    }


    #endregion

    #region Action

    /// <summary>
    /// 매 Tick마다 호출되는 유닛의 메인 Action 루프
    /// 1. CommandQueue에 쌓인 명령을 우선 처리
    /// 2. Command가 없으면 FSM(Action) Tick 수행
    /// </summary>
    protected void ExecuteAction() 
    {
        // 사망 상태면 모든 Action 중단
        //if (m_AttributeSystem.m_IsDead)
        //    return;

        // Command는 FSM보다 우선 처리 (Tick당 1개)
        // Command가 소비되면 FSM Tick은 실행하지 않음
        if (TryConsumeCommand())
            return;

        // 일반 FSM Action Tick
        TickCurrentAction();
    }

    /// <summary>
    /// 현재 Action을 즉시 교체한다.
    /// (Action 종료 여부와 무관하게 강제 전환)
    /// </summary>
    public void SwitchToNextStateAction(BaseAction nextAction)
    {
        m_CurrentAction = nextAction;
        var type = nextAction.GetType();

        // 디버그 / UI / 로그용 Action 변경 이벤트
        OnActionChanged?.Invoke(type);
    }

    /// <summary>
    /// 현재 Action과 예약된 ActionQueue를 전부 초기화한다.
    /// (유닛 리셋 / 강제 상태 변경 시 사용)
    /// </summary>
    private void ClearAction()
    {
        m_CurrentAction = null;

        ActionQueueClear();
    }

    /// <summary>
    /// Command / Action 예약 큐를 완전히 비운다.
    /// </summary>
    private void ActionQueueClear()
    {
        m_ActionQueue.Clear();
    }

    /// <summary>
    /// 유닛이 보유한 모든 Action 목록을 반환한다.
    /// (디버그 / 초기화 / 이벤트 바인딩 용도)
    /// </summary>
    public IEnumerable<BaseAction> GetActions()
    {
        return baseActionDict.Values;
    }

    /// <summary>
    /// 특정 타입의 Action을 가져온다.
    /// (없으면 null 반환)
    /// </summary>
    public T GetAction<T>() where T : BaseAction
    {
        if (baseActionDict.TryGetValue(typeof(T), out var action))
            return action as T;
        return null;
    }

    /// <summary>
    /// 이전 Action이 존재하면 되돌아가고,
    /// 없으면 IdleAction으로 복귀한다.
    /// </summary>
    public BaseAction GetBackStateAction()
    {
        if (m_BeforeAction == null)
        {
            return GetAction<IdleAction>();
        }
        else
        {
            return m_BeforeAction;
        }
    }

    /// <summary>
    /// 다음에 실행할 Action을 큐에 예약한다.
    /// (현재 Command 또는 Action이 끝난 후 실행됨)
    /// </summary>
    public void EnqueueNextAction(BaseAction action, GridPosition grid)
    {
        if (action == null)
            return;

        m_ActionQueue.Enqueue((action, grid));
    }

    /// <summary>
    /// 여러 개의 Action을 순차적으로 예약한다.
    /// (Shift+명령, 연속 행동 등에 사용)
    /// </summary>
    public void EnqueueNextAction(List<(BaseAction action, GridPosition grid)> list)
    {
        if (list.Count == 0)
            return;

        foreach (var (action, grid) in list)
            m_ActionQueue.Enqueue((action, grid));
    }

    /// <summary>
    /// ActionQueue에 쌓인 Command를 하나 소비한다.
    /// - Tick당 최대 1개만 처리
    /// - Command 수행 중에는 중복 소비 방지
    /// </summary>
    /// <returns>
    /// Command를 소비했으면 true,
    /// 소비하지 않았으면 false
    /// </returns>
    private bool TryConsumeCommand()
    {
        // 예약된 Action이 없으면 처리하지 않음
        if (m_ActionQueue.Count == 0)
            return false;

        // 현재 CommandAction이 아직 끝나지 않음
        if (m_IsCommandAction)
            return false;

        var (action, grid) = m_ActionQueue.Dequeue();
        m_IsCommandAction = true;

        // CommandAction으로 즉시 전환
        SwitchToNextStateAction(action);

        // CommandAction은 항상 목표 Grid를 들고 최초 1회 실행
        TrySwitchByResult(m_CurrentAction.TakeAction(grid));

        return true;
    }

    /// <summary>
    /// 현재 Action의 일반 FSM Tick 처리
    /// (Command가 없을 때만 실행됨)
    /// </summary>
    private void TickCurrentAction()
    {
        if (m_CurrentAction == null)
            return;

        TrySwitchByResult(m_CurrentAction.TakeAction());
    }

    /// <summary>
    /// Action의 실행 결과로 반환된 다음 Action이 유효할 경우
    /// 상태를 전환한다.
    /// </summary>
    private void TrySwitchByResult(BaseAction next)
    {
        // null 이거나 같은 Action이면 전환하지 않음
        if (next == null || next == m_CurrentAction)
            return;

        SwitchToNextStateAction(next);
    }

    #endregion

    #region Team Service

    public bool IsEnemy(GameEntity target)
    => target != null && GameConfig.TeamRelation.IsEnemy(m_TeamId, target.m_TeamId);

    public bool IsAlly(GameEntity target)
        => target != null && GameConfig.TeamRelation.IsAlly(m_TeamId, target.m_TeamId);

    public GameEntity m_Target { get; protected set; }

    public void SetTarget(GameEntity target)
    {
        m_Target = target;
    }

    #endregion

    #region Save & Load Data

    public virtual BaseData CaptureSaveData()
    {
        return default;
        //var state = GetAnimationsManager().FirstOrDefault();

        //GameEntityAnimationData adata = null;
        
        //if(state != null)
        //{
        //    new GameEntityAnimationData()
        //    {
        //        stateNameHash = state.m_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash,
        //        normalizedTime = state.m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime,
        //        speed = GetAnimationsManager().FirstOrDefault().m_Animator.speed,
        //    };
        //}

        //AttackPatternData attackData = null;

        //if(m_CurrentAction != null && m_CurrentAction is CombatAction combatAction)
        //{
        //    if(combatAction.m_ThisTimeAttack != null)
        //    {
        //        attackData = combatAction.m_ThisTimeAttack.CaptureSaveData() as AttackPatternData;
        //    }
        //}

        //return new GameEntityData
        //{
        //    prefabName = name,
        //    position = transform.position,
        //    rotation = transform.rotation,
        //    guid = _guid,
        //    attributeSystemData = m_AttributeSystem.CaptureSaveData(),

        //    // Action type
        //    CurrentActionType = GetEActionTypeByAction(m_CurrentAction),
        //    BeforeActionType = GetEActionTypeByAction(m_BeforeAction),
        //    NextActionType = GetEActionTypeByAction(m_NextAction),

        //    gameEntityAnimationData = adata,

        //    thisAttackPattern = attackData
        //};


    }

    public virtual void RestoreSaveData(BaseData data)
    {
        return;
        GameEntityData gdata = data as GameEntityData;
        //_guid = gdata.guid;
        transform.position = gdata.position;
        transform.rotation = gdata.rotation;
        //m_AttributeSystem.RestoreSaveData(gdata.attributeSystemData);
        m_CurrentAction = GetActionByEActionType(gdata.CurrentActionType);
        m_BeforeAction = GetActionByEActionType(gdata.BeforeActionType);
        m_NextAction = GetActionByEActionType(gdata.NextActionType);

        GameEntityAnimationData animData = gdata.gameEntityAnimationData;
        if (animData != null)
        {
            var animator = GetComponentInChildren<Animator>();

            if (animator != null)
            {

                // 2. 저장된 스테이트와 진행 정도(Normalized Time)부터 재생 시작
                // Animator.Play(int stateNameHash, int layer, float normalizedTime) 사용
                animator.Play(animData.stateNameHash, 0, animData.normalizedTime);

                animator.speed = animData.speed;

            }
        }

        if (m_CurrentAction != null && m_CurrentAction is CombatAction combatAction)
        {
            //combatAction.m_ThisTimeAttack = m_AttributeSystem.m_AttackPatterns.FirstOrDefault(attack => attack.ID == gdata.thisAttackPattern.id);
        }

        BaseAction GetActionByEActionType(E_ActionType action)
        {
            if (action == E_ActionType.None)
                return null;

            switch (action)
            {
                case E_ActionType.None:
                    return null;
                case E_ActionType.Idle:
                    return GetAction<IdleAction>();
                case E_ActionType.Chase:
                    return GetAction<ChaseAction>();
                case E_ActionType.Combat:
                    return GetAction<CombatAction>();
                case E_ActionType.Patrol:
                    return GetAction<PatrolAction>();
                case E_ActionType.CommandAttack:
                    return GetAction<CommandAttackAction>();
                case E_ActionType.CommandMove:
                    return GetAction<CommandMoveAction>();
                default:
                    return null;
            }
        }
    }

    #endregion

    #region Handle Action

    public void RaiseAttackReadyFailed(AttackData failAttack)
    {
        OnAttackReadyFailed?.Invoke(failAttack);
    }

    protected virtual void HandleDead(OnAttackInfoEventArgs e)
    {
        OnDead?.Invoke(e); // 기존 호환용
        m_CurrentAction?.ClearAction();
        ClearAction();
    }

    protected virtual void HandleDamaged(OnAttackInfoEventArgs e)
    {
        OnDamaged?.Invoke(e);
    }

    private void HandleRevived()
    {
        OnRevived?.Invoke();
    }

    private void HandleStartAttack(AttackData e)
    {
        OnStartAttack?.Invoke(e);
    }

    private void HandlePhaseChange()
    {
        OnPhaseChange?.Invoke();
    }

    private void HandleStartMoving()
    {
        OnStartMoving?.Invoke();
    }

    private void HandleStopMoving()
    {
        OnStopMoving?.Invoke();
    }

    private void HandleChangedFloorsStarted(OnChangeFloorsStartedEventArgs e)
    {
        OnChangedFloorsStarted?.Invoke(e);
    }

    private void HandleStep()
    {
        OnStep?.Invoke();
    }

    private void HandleAttackPoint()
    {
        OnAttackPoint?.Invoke(_combatAction.m_ThisTimeAttack);
    }

    #endregion

    public void PlayPlacedSpawnAnimation()
    {
        if (m_SetupAnimation == null)
        {
            // ? SpawnComplete?
            SpawnComplete();
            return;
        }

        StartCoroutine(m_SetupAnimation.PlacedSpawnAnimation());
    }

    #region GUID

    public void SetGUID(string inputGuid) => _guid = inputGuid;

    #endregion

    #region Rotate

    private bool IsFacingTarget(float angleThreshold = 5f)
    {
        if (m_Target == null)
            return true;

        Vector3 moveDirection =
            (m_Target.transform.position - transform.position).normalized;

        float angle = Vector3.Angle(transform.forward, moveDirection);

        return angle < angleThreshold;
    }

    private void RotateStepTowardTarget()
    {
        if (m_Target == null)
            return;

        m_fRotateTimer -= Time.deltaTime;
        if (m_fRotateTimer > 0)
            return;

        m_fRotateTimer = m_rotateTick;

        Vector3 moveDirection =
            (m_Target.transform.position - transform.position).normalized;

        transform.forward = Vector3.Slerp(
            transform.forward,
            moveDirection,
            Time.deltaTime * rotateSpeed
        );
    }

    public bool RotateTowardTarget()
    {
        if (IsFacingTarget())
            return true;

        RotateStepTowardTarget();
        return false;
    }


    float m_fRotateTimer = 0;
    [SerializeField] float m_rotateTick = 0.1f;
    [SerializeField] float rotateSpeed = 70;

    #endregion
}


