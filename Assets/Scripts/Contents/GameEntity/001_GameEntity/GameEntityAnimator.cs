using GLTF.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static Define;
using static Table_Camera_Shake;

/*
1. 애니메이션을 오버라이드.
2. 애니니메이션을 스텝 애니메이션으로 변경
3. 나중에 실시간으로 fps를 조정할 수 있게.
 */

[RequireComponent(typeof(Animator))]
public class GameEntityAnimator : MonoBehaviour
{
    [SerializeField] protected float m_fCrossTime = 0f;

    [Header("Ref")]
    StatSystem m_StatSystem;
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

        m_StatSystem = GetComponentInParent<StatSystem>();
        m_StatSystem.OnDead += (s, e) => Dead();
        m_StatSystem.OnRevived += (s, e) => ChangeAnimationAtStart(E_GameEntityClipType.Revive.ToString(), m_ReviveAnimationClip);
        m_StatSystem.OnDamaged += Animation_Damaged;

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

    protected virtual void Animation_Damaged(object sender, StatSystem.OnAttackInfoEventArgs e) { }
    
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
        if (newClip == null)
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
            // Ragdoll
            //AnimationStop();
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
}
