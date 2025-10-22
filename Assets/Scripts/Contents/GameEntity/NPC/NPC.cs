using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class NPC : Unit
{
    [Header("NPC Behavior / 디버그용")]
    [SerializeField] private Vector3 _targetPos;
    [SerializeField] private bool _hasReachedGoal = false;
    [SerializeField] private bool _isInteractable = false;
    [SerializeField] private bool _isInit = false;
    [SerializeField] private GameObject _interactionIcon;
    [SerializeField] private NPCStat _npcStat;

    private Coroutine _shopTimerCoroutine;
    private NPCExclamationIcon _exclamationIcon;
    private Color _outlineColor = Color.gray;

    public E_ObjectType m_ObjectType => E_ObjectType.NPC;
    private E_NPCState _state = E_NPCState.Neutral;
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
            UpdateOutlineColor();
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private Transform _dungeonCoreTransform => DungeonCore.instance.transform;

    public bool IsInteractable => _isInteractable;
    public Vector3 TargetPos => _targetPos;
    public NPCStat NPCStat { get => _npcStat; set => _npcStat = value; }

    public event EventHandler OnStateChanged;
    public event EventHandler OnReachedGoal;
    public event EventHandler OnInteractionStarted;
    public event EventHandler OnDeath;

    public void SetTarget(Vector3 targetPos)
    {
        _targetPos = targetPos;
    }

    protected override void Awake()
    {
        base.Awake();

        if (this.TryGetMyStat(out NPCStat npcStat))
        {
            _npcStat = npcStat;
            _state = _npcStat.InitialState;
            _isInit = true;
        }

        SetupExclamationIcon();
    }


    protected override void Start()
    {
        if (_isInit == false)
            return;

        State = _state;
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

    private void UpdateOutlineColor()
    {
        Color previousColor = _outlineColor;

        switch (_state)
        {
            case E_NPCState.Hostile:
                _outlineColor = new Color(0.5f, 0f, 0f, 1f);
                break;
            case E_NPCState.Neutral:
                _outlineColor = Color.gray;
                break;
            case E_NPCState.Friendly:
                _outlineColor = Color.green;
                break;
        }

        if (previousColor != _outlineColor)
            StartCoroutine(AnimateOutlineColorChange(previousColor, _outlineColor));
        else
            ApplyOutlineColor();
    }

    private void ApplyOutlineColor()
    {
        foreach (var (mat, obj) in GetModelsMaterial())
        {
            if (mat.HasProperty("_OutlineColor"))
                mat.SetColor("_OutlineColor", _outlineColor);
        }
    }

    private IEnumerator AnimateOutlineColorChange(Color previousColor, Color targetColor)
    {
        // 이전 색상과 새 색상이 같으면 애니메이션 불필요
        if (previousColor == targetColor)
            yield break;

        float animationDuration = 1.0f; // 1초 동안 애니메이션
        float elapsedTime = 0f;


        // 색상 보간 
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / animationDuration;
            Color currentColor = Color.Lerp(previousColor, targetColor, t);

            foreach (var (mat, obj) in GetModelsMaterial())
            {
                if (mat.HasProperty("_OutlineColor"))
                    mat.SetColor("_OutlineColor", currentColor);
            }

            yield return null;
        }

        ApplyOutlineColor();
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
            EnableInteraction();

            if (_shopTimerCoroutine != null)
                StopCoroutine(_shopTimerCoroutine);
            _shopTimerCoroutine = StartCoroutine(ShopOperationTimer());
        }

        OnReachedGoal?.Invoke(this, EventArgs.Empty);
    }

    private IEnumerator ShopOperationTimer()
    {
        Debug.Log($"{name}의 상점이 열렸습니다. {_npcStat.ShopDuration}초 후에 닫을 예정입니다.");

        yield return new WaitForSeconds(_npcStat.ShopDuration);

        Debug.Log($"{name}의 상점 운영 시간이 종료되었습니다. 원래 위치로 복귀합니다.");

        DisableInteraction();

        // 복귀 로직 실행
        if (_npcStat.ReturnToSpawnAfterGoal)
        {
            _hasReachedGoal = false;
            SetTarget(transform.position);

            // NPCMoveAction 다시 시작
            NPCMoveAction moveAction = GetComponent<NPCMoveAction>();
            if (moveAction != null)
            {
                moveAction.TakeAction();
            }
        }

        _shopTimerCoroutine = null;
    }

    public void EnableInteraction()
    {
        _isInteractable = true;
        UpdateInteractionIcon();
    }

    public void DisableInteraction()
    {
        _isInteractable = false;
        _interactionIcon?.SetActive(false);
    }

    private void UpdateInteractionIcon()
    {
        _exclamationIcon?.SetVisible(_isInteractable);
    }

    private void SetupExclamationIcon()
    {
        // _exclamationIcon = _interactionIcon?.GetOrAddComponent<NPCExclamationIcon>();
        // _exclamationIcon?.SetOwnerNPC(this);
    }

    public void OnExclamationIconClicked()
    {
        if (_isInteractable)
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (_isInteractable == false)
        {
            Debug.LogWarning($"[NPC] {name}은(는) 아직 상호작용 불가능합니다.");
            return;
        }

        OnInteractionStarted?.Invoke(this, EventArgs.Empty);

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
        dialogueUI.StartDialogue(this, name, _npcStat.DialogueScriptPath);
    }


    public override void DeSpawnStart()
    {
        base.DeSpawnStart();
        OnDeath?.Invoke(this, EventArgs.Empty);
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

        NPCMoveAction moveAction = GetComponent<NPCMoveAction>();
        moveAction?.TakeAction();
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
