using System.Collections.Generic;
using System.Linq;
using static Define;

public class IdleAction : BaseAction
{
    IdleAction()
    {
        m_actionName = "Idle";
    }

    protected override void Start()
    {
        base.Start();
        m_iGetActionValidRange = m_GameEntity.m_AttributeSystem.m_Stat.m_iDetectRange;
    }

    public override BaseAction TakeAction(GridPosition gridPosition)
    {
        if (!m_GameEntity.m_AttributeSystem.m_CanMoveableGameEntity)
            return this;


        // 감지 범위 내 모든 적들의 위치 탐색
        var serchTargetPosList = GetValidActionGridPositionList();

        // 적이 탐지 되지 않으면 대기
        if (serchTargetPosList.Count == 0)
            return this;

        // 탐지된 적들을 경로거리 기반으로 정렬
        var unitPos = m_GameEntity.GetGridPosition();

        // 가까운 위치 순으로 정렬
        serchTargetPosList = Managers.Path.SortByPathDistance(m_GameEntity.GetGridPosition(), serchTargetPosList);

        // 가장 가까운 적들부터 현재 공격 가능한 위치가 있는지 체크
        foreach (var serchTargetPos in serchTargetPosList)
        {
            var serachTareget = Managers.Grid.GetUnitAt(serchTargetPos);

            // 현재 특정 조건을 만족하는 공격 후보들을 가져오기
            // 쿨타임, 거리, 바로 사용 가능.
            var filterResult =  Managers.Game.EvaluateAttackPatternsByCondition
                (m_GameEntity, serachTareget,
                E_AttackCondition.Fail_CoolTime,
                E_AttackCondition.Fail_ManaCost,
                E_AttackCondition.Fail_Distance,
                E_AttackCondition.Success);

            if (!filterResult.Any())
                continue;

            // 조건별 그룹화
            var grouped = filterResult
            .GroupBy(r => r.condition)
            .ToDictionary(g => g.Key, g => g.ToList());

            // 1. Success가 하나라도 있는가?
            if (grouped.TryGetValue(E_AttackCondition.Success, out var successList))
            {
                // 바로 공격 가능!
                var best = successList.First();

                //Debug.Log("현재 타겟 : " + serachTareget);
                //Debug.Log($"성공적인 공격 패턴 : {best.pattern.name}");
#if UNITY_EDITOR
                //Util.DrawDebugPositions(best.canAttackPosition);
#endif

                m_GameEntity.SetTarget(serachTareget);
                ActionStart();
                return m_GameEntity.GetAction<CombatAction>();
            }

            // 2. Fail_Distance가 있는가?
            else if (grouped.TryGetValue(E_AttackCondition.Fail_Distance, out var distList))
            {

                var best = distList.First();
                //Debug.Log("현재 타겟 : " + serachTareget);
                //Debug.Log($"거리가 먼 공격 패턴 : {best.pattern.name}");

#if UNITY_EDITOR
                //Util.DrawDebugPositions(best.canAttackPosition);
#endif
                m_GameEntity.SetTarget(serachTareget);
                ActionStart();
                return m_GameEntity.GetAction<ChaseAction>();
                // 이동해서 공격 가능한 후보가 있음 -> 이동 행동 리턴
                // distList.First().canAttackPosition 중 하나로 이동
            }

            // 3. Fail_CoolTime / Fail_ManaCost
            else if (grouped.TryGetValue(E_AttackCondition.Fail_CoolTime, out var coolList)
                    || grouped.TryGetValue(E_AttackCondition.Fail_ManaCost, out var manaList))
            {
                // 쿨타임 또는 마나 때문에 지금 못 쓰지만 
                // 뒤로 빠지거나, 혹은 제자리 대기, 혹은 다른 행동
                // 대기하거나 방어 모드로 들어갈 수 있음
            }

            // 여기까지 왔으면 공격 가능한 게 없다니까 다음 적 검사
        }

        // 모든 적들에 대한, 모든 공격 패턴으로 공격 가능한 위치가 없거나, 해당 위치로의 이동이 불가능하다면 대기
        return this;
    }

    // 범위 내 모든 적들의 위치 탐색
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

                    // 오브젝트가 적인가?
                    var target = Managers.Grid.GetUnitAt(testGridPosition);
                    if (m_GameEntity.IsEnemy(target) == false)
                        continue;


                    // 너무 멀면 패스
                    //int pathfindingDistanceMultiplier = 10;
                    //if (Managers.Path.GetPathLength(unitGridPosition, testGridPosition) > 
                    //    m_iDetectRange * pathfindingDistanceMultiplier)
                    //{
                    //    // Path length is too long
                    //    continue;
                    //}

                    validGridPositionList.Add(testGridPosition);
                }
            }
        }

        // 몬스터의 경우 추가 설정
        if (m_GameEntity.m_TeamId == E_TeamId.Monster && m_GameEntity.m_IsTowardDungeonCore)
        {
            foreach (var core in Managers.Player.playerHealth.Cores)
                validGridPositionList.Add(core.GetGridPosition());
        }

        return validGridPositionList.ToList();
    }
}
