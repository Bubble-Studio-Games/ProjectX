using UnityEngine;

public sealed class UnitAnimView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private ActionController actionController;

    [Header("Layer Names")]
    [SerializeField] private string baseLayerName = "Base Layer";
    [SerializeField] private string overlayLayerName = "Overlay"; // 없으면 비워두기

    [Header("Base State Names")]
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string moveState = "Run";
    [SerializeField] private string combatState = "Combat";

    [Header("Overlay (One-shot) State Names")]
    [SerializeField] private string attackState = "Attack";
    [SerializeField] private string hitState = "Damaged";
    [SerializeField] private string deathState = "Death";

    [Header("Blend")]
    [SerializeField] private float baseCrossFade = 0.1f;
    [SerializeField] private float oneShotCrossFade = 0.03f;

    [Header("Overlay Auto Return")]
    [SerializeField] private string overlayDefaultState = "Empty"; // 있으면 가장 안정적
    [SerializeField] private bool fallbackDisableOverlayByWeight = true;
    [SerializeField] private float overlayWeightFadeOut = 0.08f;

    private int baseLayer;
    private int overlayLayer;

    private int lastBaseStateHash;
    private bool isDeadLocked;

    private bool isOverlayOneShotPlaying;
    private float overlayTimer;
    private float overlayTargetWeight = 1f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (actionController == null)
            actionController = GetComponentInParent<ActionController>();

        baseLayer = animator.GetLayerIndex(baseLayerName);
        if (baseLayer < 0) baseLayer = 0;

        overlayLayer = -1;
        if (!string.IsNullOrWhiteSpace(overlayLayerName))
        {
            int idx = animator.GetLayerIndex(overlayLayerName);
            if (idx >= 0) overlayLayer = idx;
        }
    }

    private void OnEnable()
    {
        if (actionController != null)
        {
            actionController.OnActionChanged += HandleActionChanged;
            actionController.OnBeTriggered += HandleTriggered;
        }

        ApplyBase(actionController != null ? actionController.Current : null, true);
    }

    private void OnDisable()
    {
        if (actionController != null)
        {
            actionController.OnActionChanged -= HandleActionChanged;
            actionController.OnBeTriggered -= HandleTriggered;
        }
    }

    private void Update()
    {
        UpdateOverlay(Time.deltaTime);
    }

    // =========================
    // Action → Base animation
    // =========================
    private void HandleActionChanged(IAction prev, IAction next)
    {
        if (isDeadLocked) return;
        ApplyBase(next, false);
    }
    private void ApplyBase(IAction action, bool immediate)
    {
        if (animator == null) return;

        string stateName = ResolveBaseState(action);
        if (string.IsNullOrEmpty(stateName)) return;

        int targetHash = Animator.StringToHash(stateName);

        // ✅ animator가 실제로 재생 중인지 확인
        var playing = animator.GetCurrentAnimatorStateInfo(baseLayer);
        bool isAlreadyPlaying = playing.shortNameHash == targetHash || playing.fullPathHash == targetHash;

        // "같은 상태면 스킵"은 animator가 진짜 그 상태일 때만
        if (!immediate && isAlreadyPlaying)
            return;

        if (immediate)
            animator.Play(targetHash, baseLayer, 0f);
        else
            animator.CrossFade(targetHash, baseCrossFade, baseLayer, 0f);
    }
    private string ResolveBaseState(IAction action)
    {
        if (action == null) return idleState;

        return action.Name switch
        {
            "Move" => moveState,
            "Chase" => moveState,
            "Combat" => combatState,
            _ => idleState
        };
    }

    // =========================
    // Trigger → One-shot
    // =========================
    private void HandleTriggered(TriggerActionType trigger)
    {
        if (animator == null) return;

        if (trigger == TriggerActionType.Die)
        {
            isDeadLocked = true;
            PlayOneShot(deathState);
            return;
        }

        switch (trigger)
        {
            case TriggerActionType.Attack:
                PlayOneShot(attackState);
                break;

            case TriggerActionType.Hit:
                PlayOneShot(hitState);
                break;
        }
    }

    private void PlayOneShot(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return;

        int layer = overlayLayer >= 0 ? overlayLayer : baseLayer;

        if (overlayLayer >= 0)
            animator.SetLayerWeight(overlayLayer, 1f);

        animator.CrossFade(stateName, oneShotCrossFade, layer, 0f);

        float len = GetCurrentClipLength(layer);
        overlayTimer = len > 0 ? len : 0.4f;
        isOverlayOneShotPlaying = true;
    }

    private void UpdateOverlay(float dt)
    {
        if (!isOverlayOneShotPlaying) return;

        overlayTimer -= dt;
        if (overlayTimer > 0f) return;

        isOverlayOneShotPlaying = false;

        if (isDeadLocked) return;

        if (overlayLayer >= 0 && !string.IsNullOrEmpty(overlayDefaultState))
        {
            animator.CrossFade(overlayDefaultState, 0.05f, overlayLayer, 0f);
        }
        else if (overlayLayer >= 0 && fallbackDisableOverlayByWeight)
        {
            animator.SetLayerWeight(overlayLayer, 0f);
        }
    }

    private float GetCurrentClipLength(int layer)
    {
        var clips = animator.GetCurrentAnimatorClipInfo(layer);
        if (clips == null || clips.Length == 0) return 0f;
        return clips[0].clip != null ? clips[0].clip.length : 0f;
    }
}
