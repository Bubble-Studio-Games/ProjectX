using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using static Define;

public class MonsterSpawner : PassiveObject
{
    [Header("Core")]
    MonsterSpawnerCore Core;
    HashSet<GridPosition> m_SpawnGridPosition = new HashSet<GridPosition>();

    float m_currentSpawnTimer;

    MonsterSpawnerStat m_MSS;

    protected override void Awake()
    {
        base.Awake();

        // 애니메이션 교체

        foreach (var animator in GetAnimationsManager())
        {
            animator.ChangeAnimationAtStart
            ("Spawn Object", animator.m_OrderAnimationClip.Where(ani => ani.name.Contains("Core")).FirstOrDefault());
        }

    }

    protected override void Start()
    {
        base.Start();

        Core = GetComponentInChildren<MonsterSpawnerCore>();

        if (m_IsSetuping)
            return;
    }

    public override void SpawnComplete()
    {
        base.SpawnComplete();

        // Base Action
        SwitchToNextStateAction(GetAction<CombatAction>());

        // UnitActionSystem
        UnitActionSystem.Instance.OnUpdateActionTick += ExecuteAction;
    }

    public override void OnDestroy()
    {
        if (UnitActionSystem.Instance != null)
            UnitActionSystem.Instance.OnUpdateActionTick -= ExecuteAction;
    }

    protected override void ExecuteAction(object sender, GridPosition args)
    {
        base.ExecuteAction(sender, args);

        if (m_AttributeSystem.m_IsDead)
            return;

        m_NextAction = m_CurrentAction?.TakeAction();

        if (m_NextAction is not null && m_NextAction != m_CurrentAction)
        {
            m_CurrentAction.ClearAction();
            SwitchToNextStateAction(m_NextAction);
        }
    }

    // 몬스터 소환
    private void SpawnObject()
    {
        // Animation
        GetAnimationsManager().ForEach(anim =>  anim.PlayTargetAnimation("Spawn Object", false));
    }
}
