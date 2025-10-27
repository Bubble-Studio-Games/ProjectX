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
        if (m_BaseObject is PassiveObject pobj)
        {
            return this;
        }
        else if (m_BaseObject is ControllableObject cobj)
        {
            // 감지 범위 내의 적 유닛 탐색
            var (obj, pos) = LevelGrid.Instance.GetClosestTargetGridInfo(cobj.GetGridPosition(), GetValidActionGridPositionList());

            // 타겟 검증: 타겟이 없거나 사망했거나 유효하지 않으면 타겟 초기화
            if (obj == null || obj.m_AttributeSystem.m_IsDead || !obj.gameObject.activeSelf)
            {
                cobj.SetTarget(null);

                // 몬스터의 최종 목적지는 던전 핵!
                if (cobj.m_TeamId == E_TeamId.Monster)
                {
                    if (DungeonCore.instance == null || DungeonCore.instance.m_AttributeSystem.m_IsDead || !DungeonCore.instance.gameObject.activeSelf)
                    {
                        return this;
                    }
                    else
                    {
                        if (cobj.m_isChaseCore)
                        {
                            cobj.SetTarget(DungeonCore.instance);
                        }
                        else
                        {
                            return this;
                        }
                    }
                }
                else if (cobj.m_TeamId == E_TeamId.Player)
                    return this;
            }
            else
            {
                cobj.SetTarget(obj);
            }

            // 현재 적을 발견한 상태

            ActionStart(onActionComplete);

            // 현재 가능한 공격 패턴들 뽑아오기
            var attackPatterns = Managers.Game.EvaluateAttackPatternsByCondition
                                (cobj,
                                 cobj.m_Target,
                                 E_AttackCondition.Success,
                                 E_AttackCondition.Fail_Distance);

            // 바로 공격 가능하면 전투로 돌입
            if (attackPatterns.Count > 0)
            {
                return cobj.GetAction<CombatAction>();
            }
            // 거리 때문에 멀어졌다면 추적 상태 돌입
            else
            {
                return cobj.GetAction<ChaseAction>();
            }
        }
        else
        {
            return this;

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
