using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CommandAttackAction : BaseAction
{
    int m_iMaxDistance = 10;

    public override BaseAction TakeAction(GridPosition gridPosition = default, Action onActionComplete = null)
    {
        // 적의 위치를 가져오는 상태
        if(gridPosition != default)
        {
            // 유저가 선택한 오브젝트의 위치의 적을 가져오기
            var target = LevelGrid.Instance.GetObjectAtGridPosition(gridPosition);

            if (target == null || target.m_AttributeSystem.m_IsDead)
                return m_BaseObject.GetBackStateAction();

            m_BaseObject.SetTarget(target);

            // 지정한 적만 따라가도록
            m_BaseObject.m_isDetectionsurroundingsEnabled = false;

            // 1. 공격 사거리까지 이동 후 공격
        }

        return m_BaseObject.GetAction<ChaseAction>();

        // 이동

        // 공격 여부 판단

        // 선택 사항
        // 1. 유닛에서 가장 가까운 위치의 적이 차지하는 위치를 반환해줄 것인가?
        // 2. 선택한 위치의 적을 공격할 것인가?
        // ex) 3x3 의 위치를 차지하는 적이 있으면 유닛에서 가까운 위치를 반환? 아니면 선택한 위치로 이동해 공격?

        // 가장 가까운 영역을 차지하는 적의 위치를 타겟으로 설정함.


    }

    public override string GetActionName()
    {
        throw new NotImplementedException();
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
        GridPosition unitGridPosition = m_BaseObject.GetGridPosition();

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
