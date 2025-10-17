using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Define;

public class IdleAction : BaseAction
{
    int m_iDetectRange => m_StatSystem.m_Stat.m_iDetectRange;

    public override BaseAction TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        // 감지 범위 내의 적 유닛 탐색
        var (obj, pos) = LevelGrid.Instance.GetClosestTargetGridInfo(m_BaseObject.GetGridPosition(), GetValidActionGridPositionList());
        if (obj == null)
        {
            // 몬스터의 최종 목적지는 던전 핵!
            if (m_BaseObject.m_TeamId == E_TeamId.Monster)
            {

                if(DungeonCore.instance == null || DungeonCore.instance.m_AttributeSystem.m_IsDead)
                {
                    return this;
                }
                else
                {
                    if (m_BaseObject.m_isChaseCore)
                    {
                        m_BaseObject.SetTarget(DungeonCore.instance);
                    }
                    else
                    {
                        return this;
                    }
                }
            }
            else if (m_BaseObject.m_TeamId == E_TeamId.Player)
                return this;
        }
        else
        {
            m_BaseObject.SetTarget(obj);
        }

        ActionStart(onActionComplete);


        var attackPatterns = Managers.Game.EvaluateAttackPatternsByCondition
                            (m_BaseObject,
                             m_BaseObject.m_Target,
                             E_AttackCondition.Success,
                             E_AttackCondition.Fail_Distance);

        // 바로 공격 가능
        if (attackPatterns.Count > 0)
        {
            return m_BaseObject.GetAction<CombatAction>();
        }
        else
        {
            return m_BaseObject.GetAction<ChaseAction>();
        }

    }

    public override string GetActionName()
    {
        return "Idle";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        throw new NotImplementedException();
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        GridPosition unitGridPosition = m_BaseObject.GetGridPosition();

        for (int x = -m_iDetectRange; x <= m_iDetectRange; x++)
        {
            for (int z = -m_iDetectRange; z <= m_iDetectRange; z++)
            {
                for (int floor = -m_iDetectRange; floor <= m_iDetectRange; floor++)
                {
                    GridPosition offsetGridPosition = new GridPosition(x, z, floor);
                    GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                    if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                    {
                        continue;
                    }

                    if (unitGridPosition == testGridPosition)
                    {
                        // Same Grid Position where the unit is already at
                        continue;
                    }

                    // Detect Object
                    if (!LevelGrid.Instance.HasEnemyAtGridPosition(m_BaseObject.GetGridPosition(), testGridPosition))
                            continue;

                    if (!Pathfinding.Instance.IsWalkableGridPosition(testGridPosition))
                    {
                        continue;
                    }

                    if (!Pathfinding.Instance.HasPath(unitGridPosition, testGridPosition))
                    {
                        continue;
                    }

                    // 너무 멀면 패스
                    int pathfindingDistanceMultiplier = 10;
                    if (Pathfinding.Instance.GetPathLength(unitGridPosition, testGridPosition) > 
                        m_iDetectRange * pathfindingDistanceMultiplier)
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
