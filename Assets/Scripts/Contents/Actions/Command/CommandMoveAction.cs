using System.Collections.Generic;
using static Define;

public class CommandMoveAction : MoveAction, ICommandAction
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

    public override BaseAction TakeAction(GridPosition gridPosition)
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
            Managers.Path.FindPath(m_GameEntity.GetGridPosition(), 
            DestGirdPosition, 
            out int pathLength);

        if (pathGridPositionList != null && pathGridPositionList.Count >= Remove_MOVE_GRID)
        {
            pathGridPositionList.RemoveAt(0);

            int count = pathGridPositionList.Count;

            // 마지막 위치 & 다음 위치에 유닛이 있는가?
            if (Managers.Grid.GetUnitAt(pathGridPositionList[0]) == null)
            {
                if (forwardPosition != default)
                    Managers.Grid.ReleaseCell(Managers.Grid.GetGridPosition(forwardPosition), m_GameEntity);

                forwardPosition = Managers.Grid.GetWorldPosition(pathGridPositionList[0]);

                Managers.Grid.RequestCell(pathGridPositionList[0], m_GameEntity, E_EntityCellType.Reserve);

                // Event
                ActionStart();
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

                    if (unitGridPosition == testGridPosition)
                    {
                        // Same Grid Position where the unit is already at
                        continue;
                    }

                    // 목적지가 이동 가능한 지역 그리드인가?
                    if (!Managers.Grid.CanMoveTo(testGridPosition, unitGridPosition))
                        continue;

                    // 해당 위치로 가는 길이 있는가?
                    if (!Managers.Path.HasPath(unitGridPosition, testGridPosition))
                    {
                        continue;
                    }

                    int pathfindingDistanceMultiplier = 10;
                    if (Managers.Path.GetPathLength(unitGridPosition, testGridPosition) > m_iGetActionValidRange * pathfindingDistanceMultiplier)
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

    public override bool IsValidActionGridPosition(GridPosition grid)
    {
        if (GetValidActionGridPositionList().Contains(grid) == false)
            return false;

        return true;
    }
}
