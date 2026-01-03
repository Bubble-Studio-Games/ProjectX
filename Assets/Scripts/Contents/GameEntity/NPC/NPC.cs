using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public static class NPCEx
{
    public static BaseAction ToAction<T>(this NPC npc) where T : BaseAction
    {
        return npc.GetAction<T>();
    }
}

public class NPC : Unit
{
    [Header("NPC Behavior / 디버그용")]
    [SerializeField] private Vector3 _targetPos;
    [SerializeField] public bool _hasReachedGoal = false;
    [SerializeField] private bool _isInit = false;
    [SerializeField] private NPCStat _npcStat;
    [SerializeField] private NPCOutlineView _outline;
    [SerializeField] private NPCInteractionUI _exclamationIcon;
    [SerializeField] private Player _player;
    public Player Player 
    {
        get 
        {
            if (_player == null)
                _player = FindAnyObjectByType<Player>();
            return _player;
        }
    }
    public bool TryGetPlayer(out Player player)
    {
        player = Player;
        if (player == null)
        {
            Debug.LogError($"{name}: Player를 찾을 수 없습니다.");
            return false;
        }
        return true;
    }

    public new E_ObjectType m_ObjectType => E_ObjectType.NPC;
    [SerializeField] private E_NPCState _state = E_NPCState.Neutral;
    public E_NPCState State
    {
        get => _state;
        set
        {
            if (_state == value)
                return;

            ExitState(_state);
            _state = value;
            EnterState(_state);
            UpdateTeamID();
            OnStateChanged?.Invoke(this, _state);
        }
    }

    private Transform _dungeonCoreTransform
    {
        get
        {
            if (DungeonCore.instance == null)
                return null;
            return DungeonCore.instance.transform;
        }
    }
    private Coroutine _shopTimerCoroutine;

    public Vector3 TargetPos => _targetPos;
    public NPCStat NPCStat { get => _npcStat; set => _npcStat = value; }

    [Header("Quest")]
    [SerializeField] private string _mainQuestId;
    [SerializeField] private List<string> _subQuestIds = new List<string>();

    public string MainQuestId => _mainQuestId;
    public List<string> SubQuestIds => _subQuestIds;

    public static event Action<NPC> OnAnyNPCDeath;
    public event Action<NPC, E_NPCState> OnStateChanged;
    public event Action<NPC> OnReachedGoal;
    public event Action<NPC> OnInteractionStarted;
    
    public void SetTarget(Vector3 targetPos) => _targetPos = targetPos;

    [ContextMenu("Neutral 성향")] public void SetNeutralState() => State = E_NPCState.Neutral;
    [ContextMenu("Hostile 성향")] public void SetHostileState() => State = E_NPCState.Hostile;
    [ContextMenu("Friendly 성향")] public void SetFriendlyState() => State = E_NPCState.Friendly;

    protected override void Awake()
    {
        base.Awake();

        if (this.TryGetMyStat(out NPCStat npcStat))
        {
            _npcStat = npcStat;
            _state = _npcStat.InitState;
            _isInit = true;

            // 퀘스트 ID 로드
            _mainQuestId = _npcStat.MainQuestId;
            if (_npcStat.SubQuestIds != null)
            {
                _subQuestIds = new List<string>(_npcStat.SubQuestIds);
            }
        }

        // _exclamationIcon = this.gameObject.GetComponentInChildren<NPCExclamationIcon>(true);
        // _exclamationIcon.Init(this);
        // _outline = this.gameObject.GetOrAddComponent<NPCOutlineView>();
        // _outline.Init(this);    
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _exclamationIcon = null;
        _outline = null;
        OnAnyNPCDeath = null;
        OnStateChanged = null;
        OnReachedGoal = null;
        OnInteractionStarted = null;
    }

    protected override void Start()
    {
        if (_isInit == false)
            return;

        State = _state;
    }

    protected void InitStateAction()
    {
        var initAction = GetAction<NPCIdleAction>();
        if (initAction != null)
            SwitchToNextStateAction(initAction);
        else
            Debug.LogError($"{name}: NPCIdleAction을 찾을 수 없습니다.");
    }

    protected override void Update()
    {
        if (_isInit == false)
            return;

        base.Update();
        UpdateState(_state);
    }

    private void EnterState(E_NPCState state)
    {
        switch (state)
        {
            case E_NPCState.Hostile:
                EnterHostileState();
                break;
            case E_NPCState.Neutral:
                EnterNeutralState();
                break;
            case E_NPCState.Friendly:
                EnterFriendlyState();
                break;
        }
    }

    private void UpdateState(E_NPCState state)
    {
        switch (state)
        {
            case E_NPCState.Hostile:
                UpdateHostileState();
                break;
            case E_NPCState.Neutral:
                UpdateNeutralState();
                break;
            case E_NPCState.Friendly:
                UpdateFriendlyState();
                break;
        }
    }

    private void ExitState(E_NPCState state)
    {
        switch (state)
        {
            case E_NPCState.Hostile:
                ExitHostileState();
                break;
            case E_NPCState.Neutral:
                ExitNeutralState();
                break;
            case E_NPCState.Friendly:
                ExitFriendlyState();
                break;
        }
    }

    private void UpdateTeamID()
    {
        switch (_state)
        {
            case E_NPCState.Hostile:
                m_TeamId = E_TeamId.Monster;
                break;
            case E_NPCState.Neutral:
            case E_NPCState.Friendly:
                m_TeamId = E_TeamId.NPC;
                break;
        }
    }


    /// <summary>
    /// 공격받았을 때 호출 - 중립 -> 적대 전환
    /// </summary>
    public void OnAttacked()
    {
        if (_state == E_NPCState.Neutral)
        {
            State = E_NPCState.Hostile;
        }
    }


    public virtual void OnGoalReached()
    {
        _hasReachedGoal = true;
        HandleGoalReached();
    }

    protected virtual void HandleGoalReached()
    {
        if (_npcStat.NPCType == E_NPC.Shop)
        {
            if (_shopTimerCoroutine != null)
                StopCoroutine(_shopTimerCoroutine);
            _shopTimerCoroutine = StartCoroutine(ShopOperationTimer());
        }

        OnReachedGoal?.Invoke(this);
    }

    private IEnumerator ShopOperationTimer()
    {
        Debug.Log($"{name}의 상점이 열렸습니다. {_npcStat.ShopDuration}초 후에 닫을 예정입니다.");

        yield return new WaitForSeconds(_npcStat.ShopDuration);

        Debug.Log($"{name}의 상점 운영 시간이 종료되었습니다. 원래 위치로 복귀합니다.");

        // 복귀 로직 실행
        if (_npcStat.ReturnToSpawnAfterGoal)
        {
            _hasReachedGoal = false;
            SetTarget(transform.position);

            // NPCMoveAction 다시 시작
            m_CommandAction = GetAction<NPCMoveAction>();
        }

        _shopTimerCoroutine = null;
    }

    public void OnExclamationIconClicked()
    {
        Interact();
    }

    public void Interact()
    {
        OnInteractionStarted?.Invoke(this);

        Debug.Log($"[NPC] {name}과(와) 상호작용 시작!");

        switch (_npcStat.NPCType)
        {
            case E_NPC.Shop:
                OpenShop();
                break;
            case E_NPC.Quest:
                StartDialogue();
                break;
            case E_NPC.Event:
                break;
            default:
                Debug.LogError($"알 수 없는 NPC 타입: {_npcStat.NPCType}");
                break;
        }
    }

    private void OpenShop()
    {
        Debug.Log($"NPC - 상점 열기 - {name}");
    }

    private void StartDialogue()
    {
        Debug.Log($"NPC - 대화 시작 - {name}");
        DialogueUI dialogueUI = Managers.UI.ShowPopupUI<DialogueUI>();
    }


    public new void DeSpawnStart()
    {
        base.DeSpawnStart();
        OnAnyNPCDeath?.Invoke(this); 
    }

    private void EnterHostileState()
    {
    }

    private void UpdateHostileState()
    {
    }

    private void ExitHostileState()
    {
    }

    private void EnterNeutralState()
    {
        if (_npcStat.MoveTowardsDungeonCore == false)
            return;

        if (_dungeonCoreTransform.position == Vector3.zero)
            return;

        SetTarget(_dungeonCoreTransform.position);

        m_CommandAction = GetAction<NPCMoveAction>();
    }

    private void UpdateNeutralState()
    {
    }

    private void ExitNeutralState()
    {
    }

    private void EnterFriendlyState()
    {
    }

    private void UpdateFriendlyState()
    {
    }

    private void ExitFriendlyState()
    {
    }

}
