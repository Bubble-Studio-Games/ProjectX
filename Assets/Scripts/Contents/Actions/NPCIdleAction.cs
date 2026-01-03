using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

/// <summary>
/// NPC 대기 액션
/// Hostile: 플레이어 감지 시 공격/추적
/// Neutral: 던전 코어로 이동 중, 미도달 시 대기
/// Friendly: 상호작용 대기 (상점 NPC 등)
/// </summary>
public class NPCIdleAction : BaseAction
{
    private NPC _owner;
    private int _detectRange => m_StatSystem.m_Stat.m_iDetectRange;
    private const float PLAYER_PROXIMITY_RANGE = 20.0f; // 플레이어 감지 범위

    protected override void Awake()
    {
        base.Awake();
        if (m_BaseObject is NPC npc)
            _owner = npc;
        else
            Debug.LogError($"NPC 컴포넌트를 찾을 수 없습니다. {m_BaseObject.name}");
    }

    public override string GetActionName() => "NPC Idle";
    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition) => new EnemyAIAction { gridPosition = gridPosition, actionValue = 0 };

    public override BaseAction TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        if (_owner == null)
            return _owner.ToAction<NPCIdleAction>();

        switch (_owner.State)
        {
            case E_NPCState.Hostile:
                return HandleHostileIdle(onActionComplete);
            case E_NPCState.Neutral:
                return HandleNeutralIdle();
            case E_NPCState.Friendly:
                return HandleFriendlyIdle(onActionComplete);
        }

        return _owner.ToAction<NPCIdleAction>();
    }



    /// <summary>
    /// 적대적 NPC - 플레이어 감지 시 공격/추적
    /// </summary>
    private BaseAction HandleHostileIdle(Action onActionComplete)
    {
        // 감지 범위 내의 적 유닛 탐색
        var (obj, pos) = LevelGrid.Instance.GetClosestTargetGridInfo(m_BaseObject.GetGridPosition(), GetValidActionGridPositionList());

        // 타겟 검증
        if (obj == null || obj.m_AttributeSystem.m_IsDead == true || obj.gameObject.activeSelf == false)
        {
            _owner.SetTarget(null);
            return _owner.ToAction<NPCIdleAction>();
        }
        else
        {
            _owner.SetTarget(obj);
        }

        ActionStart(onActionComplete);

        // 공격 가능 여부 확인
        var attackPatterns = Managers.Game.EvaluateAttackPatternsByCondition
                            (m_BaseObject,
                             _owner.m_Target,
                             E_AttackCondition.Success,
                             E_AttackCondition.Fail_Distance);

        // 바로 공격 가능
        if (attackPatterns.Count > 0)
            return _owner.ToAction<CombatAction>();
        else
            return _owner.ToAction<ChaseAction>();
    }

    /// <summary>
    /// 중립적 NPC
    /// - NPCIdleAction 상태를 진입하면 이 상태를 유지
    /// - 플레이어 상호작용이나, 기타 반응형으로 외부에서 강제 상태 변경.
    /// - 플레이어가 일정 범위 내 접근 시 느낌표 표시
    /// </summary>
    private BaseAction HandleNeutralIdle()
    {
        CheckPlayerDistance();
        return _owner.ToAction<NPCIdleAction>();
    }

    /// <summary>
    /// 플레이어 근접 여부 확인 및 상호작용 활성화/비활성화
    /// </summary>
    private void CheckPlayerDistance()
    {
        if (_owner.TryGetPlayer(out Player player) == false)
            return;

        float distance = Vector3.Distance(m_BaseObject.transform.position, player.transform.position);

        // if (distance <= PLAYER_PROXIMITY_RANGE)
        //     _owner.EnableInteraction();
        // else
        //     _owner.DisableInteraction();
    }

    private BaseAction HandleFriendlyIdle(Action onActionComplete)
    {
        ActionStart(onActionComplete);
        return _owner.ToAction<NPCIdleAction>();
    }


    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        GridPosition unitGridPosition = m_BaseObject.GetGridPosition();

        for (int x = -_detectRange; x <= _detectRange; x++)
        {
            for (int z = -_detectRange; z <= _detectRange; z++)
            {
                for (int floor = -_detectRange; floor <= _detectRange; floor++)
                {
                    GridPosition offsetGridPosition = new GridPosition(x, z, floor);
                    GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                    if (LevelGrid.Instance.IsValidGridPosition(testGridPosition) == false)
                    {
                        continue;
                    }

                    if (unitGridPosition == testGridPosition)
                    {
                        // Same Grid Position where the unit is already at
                        continue;
                    }

                    // Detect Object
                    if (LevelGrid.Instance.HasEnemyAtGridPosition(m_BaseObject.GetGridPosition(), testGridPosition) == false)
                        continue;

                    if (LevelGrid.Instance.GetGridPositionCellInfo(testGridPosition).gridType != E_GridCheckType.Walkable)
                    {
                        continue;
                    }

                    if (Pathfinding.Instance.HasPath(m_BaseObject.GetGridPosition(), testGridPosition) == false)
                    {
                        continue;
                    }

                    // 너무 멀면 패스
                    int pathfindingDistanceMultiplier = 10;
                    if (Pathfinding.Instance.GetPathLength(unitGridPosition, testGridPosition) >
                        _detectRange * pathfindingDistanceMultiplier)
                    {
                        // Path length is too long
                        continue;
                    }

                    validGridPositionList.Add(testGridPosition);
                }
            }
        }

        return validGridPositionList;
    }
}
