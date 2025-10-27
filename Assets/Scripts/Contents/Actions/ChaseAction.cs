using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

// 타겟을 쫓아가는 액션
// 목표 거리까지 쫓아가면 전투 액션으로 진입

public class ChaseAction : MoveAction
{
    protected override void Awake()
    {
        base.Awake();

        m_iMaxMoveDistance = m_StatSystem.m_Stat.m_iChaseRange;
        SetActionComlete(() => { m_BaseObject.SwitchToNextStateAction(m_BaseObject.GetAction<CombatAction>()); });
    }

    public override BaseAction TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        if (m_BaseObject is PassiveObject pobj)
        {
            return this;
        }
        else if (m_BaseObject is ControllableObject cobj)
        {
            GridPosition selfPos = m_BaseObject.GetGridPosition();


            if (cobj.m_isDetectionsurroundingsEnabled)
            {
                // 감지 범위 내의 적 유닛 탐색
                var (obj, pos) = LevelGrid.Instance.GetClosestTargetGridInfo(selfPos, GetValidActionGridPositionList());
                if (obj == null)
                {
                    // 몬스터의 최종 목적지는 던전 핵!
                    if (cobj.m_TeamId == E_TeamId.Monster)
                    {

                        if (DungeonCore.instance == null || DungeonCore.instance.m_AttributeSystem.m_IsDead)
                        {
                            return cobj.GetAction<IdleAction>();
                        }
                        else
                        {
                            if (cobj.m_isChaseCore)
                            {
                                cobj.SetTarget(DungeonCore.instance);
                            }
                            else
                            {
                                return cobj.GetAction<IdleAction>();
                            }
                        }
                    }
                    else if (cobj.m_TeamId == E_TeamId.Player)
                        return FailSerachTarget();

                }
                else
                    cobj.SetTarget(obj);
            }
            // Command Attack으로 지정 정만을 향해 달려가는 중.
            else
            {
                if (cobj.m_Target == null || cobj.m_Target.m_AttributeSystem.m_IsDead)
                {
                    cobj.m_isDetectionsurroundingsEnabled = true;

                    return this;
                }
            }


            // 정보 갱신
            GridPosition targetPos = cobj.m_Target.GetGridPosition();

            // 현재 공격하기에 가장 좋은 위치를 탐색함.
            List<GridPosition> attackPosPath = GetAttackableBestGridPositionPath(selfPos, targetPos);

            if (attackPosPath == null || attackPosPath.Count() == 0)
            {
                return FailSerachTarget();
            }
            else
                DestGirdPosition = attackPosPath[attackPosPath.Count - 1];

            // Change Reserve
            if (forwardPosition != default)
                LevelGrid.Instance.SetGridPositionCellInfo(LevelGrid.Instance.GetGridPosition(forwardPosition), E_GridCheckType.Walkable);

            LevelGrid.Instance.SetGridPositionCellInfo(attackPosPath[0], E_GridCheckType.Reserve, cobj);

            forwardPosition = LevelGrid.Instance.GetWorldPosition(attackPosPath[0]);

            // Event
            ActionStart(onActionComplete);

            return this;

            // 공격 가능한 최적의 위치를 가져오기
            List<GridPosition> GetAttackableBestGridPositionPath(GridPosition attackerGridPosition, GridPosition targetPosition)
            {
                // 1️ 현재 가능한 공격 패턴 가져오기 (Evaluate 기반)
                var evaluations = Managers.Game.EvaluateAttackPatternsByCondition
                                    (cobj,
                                     cobj.m_Target,
                                     E_AttackCondition.Success,
                                     E_AttackCondition.Fail_Distance);

                if (evaluations.Count == 0)
                    return default;

                // 2. 현재 가지고 있는 공격들에서 적을 공격할 수 있는 위치를 합치기
                HashSet<GridPosition> canAttackPos = new();
                foreach (var evaluation in evaluations)
                    canAttackPos.AddRange(evaluation.canAttackPosition);

                if (canAttackPos.Count == 0)
                    return default;

                // 현재 위치가 바로 공격 가능한 위치라면
                if (canAttackPos.FirstOrDefault() == attackerGridPosition)
                {
                    return new List<GridPosition>() { attackerGridPosition };
                }

                // 3. 공격 가능한 위치들 중에서 가장 빠르게 도달할 수 있는 위치를 찾아서, 경로 반환
                var bestAttackPosPath = Pathfinding.Instance.FindNearestCandidatePath(m_BaseObject , attackerGridPosition, canAttackPos, allowApproachWhenUnreachable: true);
                return LastFilter(bestAttackPosPath);

                // 필터
                List<GridPosition> LastFilter(List<GridPosition> bestPosition)
                {
                    if (bestPosition.Count >= Remove_MOVE_GRID)
                        bestPosition.RemoveAt(0); // 현재 유닛 위치 제거.

                    return bestPosition;
                }
            }
        }
        else
        {
            return this;
        }
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        GridPosition unitGridPosition = m_BaseObject.GetGridPosition();

        for (int x = -m_iMaxMoveDistance; x <= m_iMaxMoveDistance; x++)
        {
            for (int z = -m_iMaxMoveDistance; z <= m_iMaxMoveDistance; z++)
            {
                for (int floor = -m_iMaxMoveDistance; floor <= m_iMaxMoveDistance; floor++)
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

                    int pathfindingDistanceMultiplier = 10;
                    if (Pathfinding.Instance.GetPathLength(unitGridPosition, testGridPosition) > m_iMaxMoveDistance * pathfindingDistanceMultiplier)
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
