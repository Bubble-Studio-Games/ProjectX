using RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

/*
1. 애니메이션을 오버라이드.
2. 애니니메이션을 스텝 애니메이션으로 변경
3. 나중에 실시간으로 fps를 조정할 수 있게.
 */

[Serializable]
public class GameEntityAnimator : MonoBehaviour
{
    public event Action OnStep; // 발자국 이벤트
    public event Action OnAttackPoint;      // AttackPoint 애니 이벤트
    public event Action OnReadyFailPoint;   // ReadyFail 애니 이벤트
    public event Action OnSpawnAnimFinished;
    public event Action OnDespawnAnimFinished;

    [SerializeField] protected float m_fCrossTime = 0f;

    [Header("Ref")]
    private GameEntity m_GameEntity;
    private IInteractable m_Interactable;

    public Animator m_Animator { get; protected set; }
    protected AnimatorOverrideController overrideController;

    [Header("Spawn And DeSpawn")]
    public AnimationClip[] m_SpawnAnimationClip;
    public AnimationClip[] m_DeSpawnAnimationClip;

    [Header("Live")]
    public AnimationClip[] m_ReviveAnimationClip;
    public AnimationClip[] m_DeathAnimationClip;

    [Header("Damaged")]
    public AnimationClip[] m_CriticalDamagedAnimationClip;
    public AnimationClip[] m_DamagedAnimationClip;


    [Header("Move")]
    public AnimationClip[] m_IdleAnimationClip;
    public AnimationClip[] m_WalkAnimationClip;
    public AnimationClip[] m_RunAnimationClip;

    [Header("Interact")]
    public AnimationClip[] m_InteractAnimationClip;

    [Header("Value")]
    public  float m_AnimatorOriginalVale = 1f;

    protected virtual void Awake()
    {
        m_GameEntity = GetComponentInParent<GameEntity>();
        m_Animator = GetComponent<Animator>();

        if (m_Animator?.runtimeAnimatorController != null)
            overrideController = new AnimatorOverrideController(m_Animator.runtimeAnimatorController);

        m_Interactable = m_GameEntity.GetComponentInParent<IInteractable>();
    }

    protected virtual void Start()
    {
        // 애니메이션을 fps 설정에 따라 스텝 애니메이션으로 전부 변경
        Managers.Setting.ReplaceAllAnimationClipArraysInObject(m_GameEntity.AnimKeyName, this);

        // 변경된 애니메이션이 있는 컨트롤러 교체
        m_Animator.runtimeAnimatorController = overrideController;

        // Idle
        ChangeAnimationAtStart(E_GameEntityClipType.Idle.ToString(), m_IdleAnimationClip, false);

        // Walk
        ChangeAnimationAtStart(E_GameEntityClipType.Walk.ToString(), m_WalkAnimationClip, false);

        // Run
        ChangeAnimationAtStart(E_GameEntityClipType.Run.ToString(), m_RunAnimationClip, false);
    }

    protected void OnEnable()
    {
        AnimationPlay();

        // 이벤트 등록
        m_GameEntity.OnObjectSpawnStart += Spawned;
        m_GameEntity.OnObjectDespawned += DeSpawned;

        if (m_Interactable != null)
            m_Interactable.OnInteracted += Interact;

        // ✅ Attribute forward 구독
        m_GameEntity.OnDead += Dead;
        m_GameEntity.OnRevived += Revived;
        m_GameEntity.OnDamaged += Animation_Damaged;


        // ✅ Action forward 구독
        m_GameEntity.OnStartAttack += CombatAction_OnAttack;
        m_GameEntity.OnStartMoving += MoveAction_OnStartMoving;
        m_GameEntity.OnStopMoving += MoveAction_OnStopMoving;
        m_GameEntity.OnChangedFloorsStarted += MoveAction_OnChangedFloorsStarted;

        m_GameEntity.OnAttackReadyFailed += AttackReadyFailPoint;
    }

    protected void OnDisable()
    {
        m_GameEntity.OnObjectSpawnStart -= Spawned;
        m_GameEntity.OnObjectDespawned -= DeSpawned;

        if (m_Interactable != null)
            m_Interactable.OnInteracted -= Interact;

        m_GameEntity.OnDead -= Dead;
        m_GameEntity.OnRevived -= Revived;
        m_GameEntity.OnDamaged -= Animation_Damaged;

        m_GameEntity.OnStartAttack -= CombatAction_OnAttack;
        m_GameEntity.OnStartMoving -= MoveAction_OnStartMoving;
        m_GameEntity.OnStopMoving -= MoveAction_OnStopMoving;
        m_GameEntity.OnChangedFloorsStarted -= MoveAction_OnChangedFloorsStarted;

        m_GameEntity.OnAttackReadyFailed -= AttackReadyFailPoint;
    }


    protected virtual void Animation_Damaged(OnAttackInfoEventArgs e)
    {
        if (m_GameEntity.m_IsSetuping)
            return;

        // 공격 미스라면 넘기기
        if (e.EHitDeCisionType == E_HitDecisionType.AttackMiss)
            return;

        if (e.EHitDeCisionType == E_HitDecisionType.CriticalHit && m_CriticalDamagedAnimationClip.Length > 0)
        {
            ChangeAnimationAtStart(E_GameEntityClipType.Damaged.ToString(), m_CriticalDamagedAnimationClip);
        }
        else
            ChangeAnimationAtStart(E_GameEntityClipType.Damaged.ToString(), m_DamagedAnimationClip);
    }

    public void StepSoundPlay()=> OnStep?.Invoke();
    private void Revived() => ChangeAnimationAtStart(E_GameEntityClipType.Revive.ToString(), m_ReviveAnimationClip);

    private void Spawned()
    {
        if(m_SpawnAnimationClip.Length > 0)
        {
            ChangeAnimationAtStart(E_GameEntityClipType.Spawn.ToString(), m_SpawnAnimationClip);
        }
        else
        {
            // ❌ m_GameEntity.SpawnComplete();
            OnSpawnAnimFinished?.Invoke();
        }
    }

    private void DeSpawned()
    {
        if(m_DeSpawnAnimationClip.Length > 0)
        {
            ChangeAnimationAtStart(E_GameEntityClipType.DeSpawn.ToString(), m_DeSpawnAnimationClip);
        }
        else
        {
            // ❌ m_GameEntity.DeSpawnComplete();
            OnDespawnAnimFinished?.Invoke();
        }
    }

    private void Interact() => ChangeAnimationAtStart(E_GameEntityClipType.Interact.ToString(), m_InteractAnimationClip);

    protected virtual void Dead(OnAttackInfoEventArgs e)
    {
        if(m_DeathAnimationClip.Length > 0)
        {
            ChangeAnimationAtStart(E_GameEntityClipType.Death.ToString(), m_DeathAnimationClip);
        }
        else
        {
            if (m_GameEntity.m_IsDirectDesawnAtDeath)
            {
                m_GameEntity.DeSpawnStart();
            }
        }
    }

    public void AnimationStop() => m_Animator.speed = 0f; // 모든 레이어 애니메이션 정지
    public void AnimationPlay() => m_Animator.speed = 1f; // 모든 레이어 애니메이션 정지
    public void AnimatonSpeedRestoreOriginalSpeed() => m_Animator.speed = m_AnimatorOriginalVale;

    #region Attack
    protected virtual void CombatAction_OnAttack(AttackData e)
    {
        ChangeAnimationAtStart(E_GameEntityClipType.Attack.ToString(), e.selectInfoClip.AttackAnimationClip);

        // 선택한 공격 패턴의 공격 스피드를 애니메이터 스테이트의 스피드를 조정함.
        // 공격 스피드 조정
        // 런타임 중에 state의 speed 값 변경은 불가함.
        m_Animator.speed = e.m_fAttackSpeed;
    }

    public virtual void AttackPoint() => OnAttackPoint?.Invoke();
    public void AttackReadyFailPoint(AttackData ready) => OnReadyFailPoint?.Invoke();

    #endregion

    BodyTilt m_BodyTilt;
    FullBodyBipedIK m_FullBodyBipedIK;
    public virtual void SetHandIKForWeapon(RightHandIKTarget rightHandTarget, LeftHandIKTarget leftHandTarget, bool isTwoHandingWeapon)
    {
        // 두 손의 경우 왼 손 무기는 집어 넣고, 오른 손 무기를 두 손으로 잡기
        if (isTwoHandingWeapon)
        {
            if (rightHandTarget != null)
            {
                m_FullBodyBipedIK.solver.rightHandEffector.target = rightHandTarget.transform;
                m_FullBodyBipedIK.solver.rightHandEffector.positionWeight = 1;
                m_FullBodyBipedIK.solver.rightHandEffector.rotationWeight = 1;
            }

            if (leftHandTarget != null)
            {
                m_FullBodyBipedIK.solver.leftHandEffector.target = leftHandTarget.transform;
                m_FullBodyBipedIK.solver.leftHandEffector.positionWeight = 1;
                m_FullBodyBipedIK.solver.leftHandEffector.rotationWeight = 1;
            }

            if (rightHandTarget != null && leftHandTarget != null)
            {
                m_FullBodyBipedIK.solver.spineMapping.twistWeight = 1;
            }
        }
        else
        {
            m_FullBodyBipedIK.solver.rightHandEffector.target = null;
            m_FullBodyBipedIK.solver.leftHandEffector.target = null;
        }
    }

    #region Move


    private void MoveAction_OnStartMoving()
    {
        // 무브 스테이트에 따라 바꾸기
        if (m_GameEntity.m_AttributeSystem.m_EMoveType == E_MoveType.Walk)
            m_Animator.CrossFade("Walk", m_fCrossTime);
        else if (m_GameEntity.m_AttributeSystem.m_EMoveType == E_MoveType.Run)
            m_Animator.CrossFade("Run", m_fCrossTime);
    }


    private void MoveAction_OnStopMoving()
    {
        // 움직이지 않으니까 제자리
        m_Animator.CrossFade("Idle", m_fCrossTime);
    }


    private void MoveAction_OnChangedFloorsStarted(OnChangeFloorsStartedEventArgs e)
    {
        if (e.targetGridPosition.floor > e.unitGridPosition.floor)
        {
            // Jump
            m_Animator.CrossFade("JumpUp", m_fCrossTime);
        }
        else
        {
            // Drop
            m_Animator.CrossFade("JumpDown", m_fCrossTime);
        }
    }
    #endregion


    public void ChangeAnimationAtStart(string AnimationStateName, AnimationClip[] newClips, bool isImmediatelyStart = true)
    {
        if (newClips.Length == 0)
        {
            Debug.Log($"{m_GameEntity.name}의 {AnimationStateName} animation 이 없습니다.");
            return;
        }

        ChangeAnimationAtStart(AnimationStateName, newClips.RandomPick(), isImmediatelyStart);
    }

    // 현재 가지고 있는 애니메이션 클립을 애니메이션 컨트롤러의 원하는 스테이트의 클립과 교체하기
    public void ChangeAnimationAtStart(string AnimationStateName, AnimationClip newClip, bool isImmediatelyStart = true)
    {
        if (newClip == null || m_Animator == null || m_Animator.runtimeAnimatorController == null)
            return;

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            if (overrides[i].Key.name == AnimationStateName)
            {
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, newClip);
                break; // 원하는 클립만 바꿨으니 종료
            }
        }

        overrideController.ApplyOverrides(overrides);

        if (isImmediatelyStart)
        {
            m_Animator.CrossFade(AnimationStateName.ToString(), m_fCrossTime);
        }
    }

    public void PlayTargetAnimation(string targetAnim, bool isInteracting)
    {
        if (m_Animator == null || m_Animator.runtimeAnimatorController == null)
            return;

        m_Animator.applyRootMotion = isInteracting;
        m_Animator.CrossFade(targetAnim, m_fCrossTime);
    }

}
