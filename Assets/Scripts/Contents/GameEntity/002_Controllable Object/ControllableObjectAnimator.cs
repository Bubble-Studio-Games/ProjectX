using GLTF.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Define;

public class ControllableObjectAnimator : GameEntityAnimator
{
    private ControllableObject m_ControllableObject;
    [SerializeField] protected Transform m_ActionsTransform;


    [Header("Base Clips")]
    [SerializeField] AnimationClip[] m_IdleAnimationClip;
    [SerializeField] AnimationClip[] m_WalkAnimationClip;
    [SerializeField] AnimationClip[] m_RunAnimationClip;

    [Header("Damaged")]
    [SerializeField] AnimationClip[] m_CriticalDamagedAnimationClip;
    [SerializeField] AnimationClip[] m_DamagedAnimationClip;


    protected override void Awake()
    {
        base.Awake();

        #region Action Event

        m_ControllableObject = GetComponentInParent<ControllableObject>();

        foreach (var move in m_ActionsTransform.GetComponents<MoveAction>())
        {
            move.OnStartMoving += MoveAction_OnStartMoving;
            move.OnStopMoving += MoveAction_OnStopMoving;
            move.OnChangedFloorsStarted += MoveAction_OnChangedFloorsStarted;
        }

        if (m_ActionsTransform.TryGetComponent<CombatAction>(out CombatAction combatAction))
        {
            combatAction.OnStartAttack += CombatAction_OnAttack;
        }

        #endregion

        // Idle
        ChangeAnimationAtStart(E_GameEntityClipType.Idle.ToString(), m_IdleAnimationClip, false);

        // Walk
        ChangeAnimationAtStart(E_GameEntityClipType.Walk.ToString(), m_WalkAnimationClip, false);

        // Run
        ChangeAnimationAtStart(E_GameEntityClipType.Run.ToString(), m_RunAnimationClip, false);

    }

    protected override void Start()
    {
        base.Start();
    }

    private void MoveAction_OnStartMoving(object sender, EventArgs e)
    {

        // 무브 스테이트에 따라 바꾸기
        if (m_ControllableObject.m_EMoveType == E_MoveType.Walk)
            m_Animator.CrossFade("Walk", m_fCrossTime);
        else if (m_ControllableObject.m_EMoveType == E_MoveType.Run)
            m_Animator.CrossFade("Run", m_fCrossTime);
    }

    private void MoveAction_OnChangedFloorsStarted(object sender, MoveAction.OnChangeFloorsStartedEventArgs e)
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

    private void MoveAction_OnStopMoving(object sender, EventArgs e)
    {
        // 움직이지 않으니까 제자리
        m_Animator.CrossFade("Idle", m_fCrossTime);
    }

    private void CombatAction_OnAttack(object sender, CombatAction.OnAttackBaseEventArgs e)
    {
        if (e.attackPattern.Validate(true) == false)
        {
            Debug.LogError($"{m_ControllableObject.name} 공격 애니메이션 검증 오류");
            return;
        }

        AttackPatternInfoClip m_TempInfo = null;

        if (e.attackPattern is  AttackPattern_Range range)
        {
            if (range.m_iIsLaunchProjectileParabola)
            {
                m_TempInfo = e.attackPattern.GetBaseClip().FirstOrDefault(clip => clip.AttackAnimationClip.name.Contains("Parabola"));
            }

            // 위로 던지는 애니메이션이 없다면 그냥 일반 공격으로 대체
            if (range.m_iIsLaunchProjectileParabola == false || m_TempInfo == null)
            {
                var clips = e.attackPattern.GetBaseClip().Where(clip => !clip.AttackAnimationClip.name.Contains("Parabola"));
                m_TempInfo = clips.RandomPick();
            }
        }
        else if (e.attackPattern is AttackPattern_Melee melee )
        {
            m_TempInfo = e.attackPattern.GetBaseClip().RandomPick();
        }
        else if (e.attackPattern is AttackPattern_Ready radey)
        {
            m_TempInfo = e.attackPattern.GetBaseClip().RandomPick();
        }
        else if (e.attackPattern is AttackPattern_Summon summon)
        {
            m_TempInfo = e.attackPattern.GetBaseClip().RandomPick();
        }

        ChangeAnimationAtStart(E_GameEntityClipType.Attack.ToString(), m_TempInfo.AttackAnimationClip);

        // 선택한 공격 패턴의 공격 스피드를 애니메이터 스테이트의 스피드를 조정함.
        // 공격 스피드 조정
        // 런타임 중에 state의 speed 값 변경은 불가함.
        m_Animator.speed =  e.attackPattern.m_fAttackSpeed;
    }

    protected override void Animation_Damaged(object sender, AttributeSystem.OnAttackInfoEventArgs e)
    {
        base.Animation_Damaged(sender, e);

        // 소환 중이라면 재생x
        if (m_ControllableObject.m_IsSetuping)
            return;

        // 공격 준비중이라면 공격 실패 모션으로
        if (m_ControllableObject.GetAction<CombatAction>().m_ThisTimeAttack != null)
            return;

        if (m_ControllableObject.m_AttributeSystem.m_IsDead)
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

    protected void OnAttackReadyFail(object sender, AttributeSystem.OnAttackInfoEventArgs e)
    {
        AttackReadyFail();
    }

    public void AttackReadyFail()
    {
        var attack = m_ControllableObject.GetAction<CombatAction>().m_ThisTimeAttack;

        if (attack == null)
            return;

        // AttackPattern_Ready로 캐스팅하고 Ready 타입인지 확인
        if (attack is not AttackPattern_Ready readyPattern)
            return;

        if(readyPattern.m_Clips[0].ReadyFailAnimationClip != null)
        {
            ChangeAnimationAtStart(E_GameEntityClipType.AttackReadyFail.ToString(), readyPattern.m_Clips[0].ReadyFailAnimationClip);
        }
        else
        {
            m_ControllableObject.m_ControllableObjectCombatManager.AttackReadyFailEnd();
        }


    }

    // 애니메이션 이벤트에서 호출한다.
    public void AttackPoint()
    {
        // 어택
        var combatAction = m_ControllableObject.GetAction<CombatAction>();

        // Fail
        if(combatAction.m_ThisTimeAttack == null)
        {
            Debug.Log("attack null " + m_ControllableObject.name);
            //combatAction.OnEndAttackEventInvoke();
            return;
        }

        // Success
        else if (m_ControllableObject.m_Target != null && !m_ControllableObject.m_Target.m_AttributeSystem.m_IsDead)
        {
            combatAction.m_ThisTimeAttack.Attack(m_ControllableObject, m_ControllableObject.m_Target);
        }

        // 사운드
        m_ControllableObject.GetSounderManager().AttackSoundPlay(combatAction.m_ThisTimeAttack);

        // Reduce Mana
        m_ControllableObject.m_AttributeSystem.ReduceMP((int)combatAction.m_ThisTimeAttack.m_iManaCost.Value);
    }

}
