using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class CommandMoveAction : MoveAction
{
    CommandMoveAction()
    {
        m_actionName = "Command Move";
    }

    protected override void Start()
    {
        base.Start();
        m_iGetActionValidRange = m_GameEntity.m_AttributeSystem.m_Stat.m_iCommandMoveRange;
    }

    public override BaseAction TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        if(gridPosition != default)
            DestGirdPosition = gridPosition;

        if (m_iPathCurrentCount >= m_iPathMaxCount)
        {
            DestGirdPosition = default;
            m_iPathCurrentCount = 0;
            ActionComplete();
            return m_GameEntity.GetAction<IdleAction>();
        }

        // Find Path
        List<GridPosition> pathGridPositionList = 
            Pathfinding.Instance.FindPath(m_GameEntity.GetGridPosition(), 
            DestGirdPosition, 
            out int pathLength);

        if (pathGridPositionList != null && pathGridPositionList.Count >= Remove_MOVE_GRID)
        {
            pathGridPositionList.RemoveAt(0);

            int count = pathGridPositionList.Count;

            // 마지막 위치 & 다음 위치에 유닛이 있는가?
            if (!LevelGrid.Instance.HasAnyUnitOnGridPosition(pathGridPositionList[0]))
            {
                if (forwardPosition != default)
                    LevelGrid.Instance.SetGridPositionCellInfo(LevelGrid.Instance.GetGridPosition(forwardPosition), E_GridCheckType.Walkable);

                forwardPosition = LevelGrid.Instance.GetWorldPosition(pathGridPositionList[0]);

                LevelGrid.Instance.SetGridPositionCellInfo(pathGridPositionList[0], E_GridCheckType.Reserve, m_GameEntity);

                // Event
                ActionStart(onActionComplete);
            }
            else
            {
                while (count-- > 0)
                {
                    if(pathGridPositionList.Count >= Remove_MOVE_GRID)
                    {
                        pathGridPositionList.RemoveAt(0);

                    }
                }

                m_iPathCurrentCount++;
            }
        }
        else
        {
            m_iPathCurrentCount++;
        }

        return this;
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        GridPosition unitGridPosition = m_GameEntity.GetGridPosition();

        for (int x = -m_iGetActionValidRange; x <= m_iGetActionValidRange; x++)
        {
            for (int z = -m_iGetActionValidRange; z <= m_iGetActionValidRange; z++)
            {
                for (int floor = -m_iGetActionValidRange; floor <= m_iGetActionValidRange; floor++)
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

                    // 빈 곳이어야만 함.
                    if (!LevelGrid.Instance.IsGridPositionCheckType(testGridPosition, E_GridCheckType.Walkable))
                        continue;

                    if (!Pathfinding.Instance.HasPath(unitGridPosition, testGridPosition))
                    {
                        continue;
                    }

                    int pathfindingDistanceMultiplier = 10;
                    if (Pathfinding.Instance.GetPathLength(unitGridPosition, testGridPosition) > m_iGetActionValidRange * pathfindingDistanceMultiplier)
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
