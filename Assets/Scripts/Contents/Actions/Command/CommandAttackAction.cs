using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Define;

public class CommandAttackAction : BaseAction, ICommandAction
{
    CommandAttackAction()
    {
        m_actionName = "Command Attack";
    }

    int m_iMaxDistance = 10;

    public override BaseAction TakeAction(GridPosition gridPosition = default)
    {
        // 유저가 선택한 오브젝트의 위치의 적을 가져오기
        var target = LevelGrid.Instance.GetObjectAtGridPosition(gridPosition);

        if (target == null || target.m_AttributeSystem.m_IsDead)
            return m_GameEntity.GetBackStateAction();

        m_GameEntity.SetTarget(target);

        return m_GameEntity.GetAction<ChaseAction>();
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        throw new NotImplementedException();
    }

    public override bool IsValidActionGridPosition(GridPosition gridPosition)
    {
        // 적이 있는가?
        // 갈 수 있는가?
        // 이것만 체크하면 된다.
        GridPosition unitGridPosition = m_GameEntity.GetGridPosition();

        // 적이 있는가?
        if (!LevelGrid.Instance.HasEnemyAtGridPosition(unitGridPosition, gridPosition))
            return false;

        // 얼마나 먼가?
        int pathfindingDistanceMultiplier = 10;
        int len = Pathfinding.Instance.GetPathLength(unitGridPosition, gridPosition);
        if (len == 0 || len > m_iMaxDistance * pathfindingDistanceMultiplier) // 0의 의미는 길을 못 찾았다는 것.
            return false;

        return true;
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        throw new NotImplementedException();
    }
}
