using GLTF.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.Text;
using static Define;
using static Table_Camera_Shake;

/*
1. 애니메이션을 오버라이드.
2. 애니니메이션을 스텝 애니메이션으로 변경
3. 나중에 실시간으로 fps를 조정할 수 있게.
 */

[RequireComponent(typeof(Animator))]
[Serializable]
public class GameEntityAnimator : MonoBehaviour
{
    [SerializeField] protected float m_fCrossTime = 0f;

    [Header("Ref")]
    AttributeSystem m_StatSystem;
    private GameEntity m_GameEntity;
    protected GameEntitySounder m_GameEntitySounder;

    public Animator m_Animator { get; protected set; }
    protected AnimatorOverrideController overrideController;

    [Header("Spawn And DeSpawn")]
    public AnimationClip[] m_SpawnAnimationClip;
    public AnimationClip[] m_DeSpawnAnimationClip;

    [Header("Live")]
    public AnimationClip[] m_ReviveAnimationClip;
    public AnimationClip[] m_DeathAnimationClip;

    [Header("Oder")]
    public AnimationClip[] m_OrderAnimationClip;
    public string[] m_orderAnimationStateName;

    [Header("Value")]
    public  float m_AnimatorOriginalVale = 1f;


    protected virtual void Awake()
    {
        // 애니메이션을 fps 설정에 따라 스텝 애니메이션으로 전부 변경
        SettingManager.Instance.ReplaceAllAnimationClipArraysInObject(this);

        m_GameEntity = GetComponentInParent<GameEntity>();
        m_GameEntitySounder = GetComponentInParent<GameEntitySounder>();

        m_Animator = GetComponent<Animator>();
        if(m_Animator.runtimeAnimatorController != null)
            overrideController = new AnimatorOverrideController(m_Animator.runtimeAnimatorController);

        m_GameEntity.OnObjectSpawned += Spawned;
        m_GameEntity.OnObjectDespawned += DeSpawned;

        m_StatSystem = GetComponentInParent<AttributeSystem>();
        m_StatSystem.OnDead += (s, e) => Dead();
        m_StatSystem.OnRevived += (s, e) => ChangeAnimationAtStart(E_GameEntityClipType.Revive.ToString(), m_ReviveAnimationClip);
        m_StatSystem.OnDamaged += Animation_Damaged;


        // Event 등록
        if (m_GameEntity.m_ActionsTransform.TryGetComponent<CombatAction>(out CombatAction combatAction))
        {
            combatAction.OnStartAttack += CombatAction_OnAttack;
        }

        m_Animator.SetBool("IsControllableObject", m_GameEntity is ControllableObject);
    }

    protected virtual void Start()
    {
        // 2. 변경된 애니메이션이 있는 컨트롤러 교체
         m_Animator.runtimeAnimatorController = overrideController;
    }

    protected void OnEnable()
    {
        AnimationPlay();
    }

    protected virtual void Animation_Damaged(object sender, AttributeSystem.OnAttackInfoEventArgs e) { }
    
    public  void StepSoundPlay()
    {
        m_GameEntitySounder.StepSoundPlay();
    }

    public void ChangeAnimationAtStart(string AnimationStateName, AnimationClip[] newClips, bool isImmediatelyStart = true)
    {
        if (newClips.Length == 0)
        {
            //Debug.Log($"{m_GameEntity.name}의 {AnimationStateName} animation 이 없습니다.");
            return;
        }

        int rand = UnityEngine.Random.Range(0, newClips.Length);
        AnimationClip newClip = newClips[rand];

        ChangeAnimationAtStart(AnimationStateName, newClip, isImmediatelyStart);
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

        if(isImmediatelyStart)
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

    private void Spawned(object s, EventArgs e)
    {
        if(m_SpawnAnimationClip.Length > 0)
        {
            ChangeAnimationAtStart(E_GameEntityClipType.Spawn.ToString(), m_SpawnAnimationClip);
        }
        else
        {
            m_GameEntity.SpawnComplete();
        }
    }

    private void DeSpawned(object s, EventArgs e)
    {
        if(m_DeSpawnAnimationClip.Length > 0)
        {
            ChangeAnimationAtStart(E_GameEntityClipType.DeSpawn.ToString(), m_DeSpawnAnimationClip);
        }
        else
        {
            m_GameEntity.DeSpawnComplete();
        }
    }

    protected virtual void Dead()
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

    public void AnimationStop()
    {
        m_Animator.speed = 0f; // 모든 레이어 애니메이션 정지
    }

    public void AnimationPlay()
    {
        m_Animator.speed = 1f; // 모든 레이어 애니메이션 정지
    }

    public void AnimatonSpeedRestoreOriginalSpeed()
    {
        m_Animator.speed = m_AnimatorOriginalVale;

    }

    protected virtual void CombatAction_OnAttack(object sender, CombatAction.OnAttackBaseEventArgs e)
    {
        if (e.attackPattern.Validate(true) == false)
        {
            Debug.LogError($"{m_GameEntity.name} 공격 애니메이션 검증 오류");
            return;
        }

        AttackPatternInfoClip m_TempInfo = null;

        if (e.attackPattern is AttackPattern_Range range)
        {
            if (range.context.ObstacleHeight >= 1)
                m_TempInfo = e.attackPattern.GetBaseClip().FirstOrDefault(clip => clip.AttackAnimationClip.name.Contains("Parabola"));
            
            // 위로 던지는 애니메이션이 없다면 그냥 일반 공격으로 대체
            if (m_TempInfo == null)
                m_TempInfo = e.attackPattern.GetBaseClip().Where(clip => !clip.AttackAnimationClip.name.Contains("Parabola")).RandomPick(); ;
        }
        else 
        {
            m_TempInfo = e.attackPattern.GetBaseClip().RandomPick();
        }

        if (m_TempInfo == null)
        {
            Debug.LogError($"{m_GameEntity.name} 공격 애니메이션 클립이 존재하지 않습니다.");
            return;
        }

        ChangeAnimationAtStart(E_GameEntityClipType.Attack.ToString(), m_TempInfo.AttackAnimationClip);

        // 선택한 공격 패턴의 공격 스피드를 애니메이터 스테이트의 스피드를 조정함.
        // 공격 스피드 조정
        // 런타임 중에 state의 speed 값 변경은 불가함.
        m_Animator.speed = e.attackPattern.m_fAttackSpeed;
<<<<<<< HEAD
=======

>>>>>>> develop
    }

    protected bool _attackValid = true;
    public virtual void AttackPoint()
    {
        // 어택
        var combatAction = m_GameEntity.GetAction<CombatAction>();

        // Fail
        if (combatAction.m_ThisTimeAttack == null)
        {
            Debug.Log("attack null " + m_GameEntity.name);
            //combatAction.OnEndAttackEventInvoke();
            _attackValid = false;
            return;
        }
        else
        {
            // 사운드
            m_GameEntity.GetSounderManager().AttackSoundPlay(combatAction.m_ThisTimeAttack);

            _attackValid = true;
        }

    }
}
