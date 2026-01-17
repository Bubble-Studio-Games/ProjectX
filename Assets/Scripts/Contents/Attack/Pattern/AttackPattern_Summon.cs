using System.Collections.Generic;
using System.Linq;
using static Define;
using UnityEngine;

public sealed class AttackPattern_Summon
    : AttackPattern<AttackData_Summon>
{
    public override (E_AttackCondition condition, List<GridPosition> CanAttackablePos)
    CanExecute(GameEntity attacker, GameEntity target, AttackData_Summon data, AttackData prevData)
    {
        var ret = base.CanExecute(attacker, target, data, prevData);
        if (ret.condition != E_AttackCondition.Success)
            return ret;

        if (data.m_IsInfiniteSpawn == false)
        {
            data._summonInstances.RemoveAll(unit => unit == null || unit.m_AttributeSystem.m_IsDead);

            if (data._summonInstances.Count >= data._maxSummonCount)
                return (E_AttackCondition.Fail_IndividualCondition, default);
        }

        return ret;
    }

    /// <summary>
    /// 소환의 경우 미리 소환할 만큼만 그리드 예약
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    /// <param name="prevAttackpatern"></param>
    public override void StartAttack(GameEntity attacker, GameEntity target, AttackData_Summon data, AttackData prevAttackpatern)
    {
        base.StartAttack(attacker, target, data, prevAttackpatern);

        // 소환 범위 그리드 리스트
        var spawnCandidate = GetAttackRangeGridPositions(attacker.GetGridPosition(), target, data);

        // 그리드 체크
        var spawnfilterd = spawnCandidate.Where(pos => GetGridListValidByCheckTypes(pos, attacker, data));

        Debug.Log($"소환 가능 : {string.Join(" \n", spawnfilterd)}");

        if (data.m_IsInfiniteSpawn == false)
        {
            // 🔸 랜덤 소환 카운트 반영
            int randomCount = data.m_IsRandomSpawnCount
                ? UnityEngine.Random.Range(data._minSummonCount, data._maxSummonCount + 1)
                : data._maxSummonCount;

            data.m_iThisAttackSummonCount = Mathf.Min(randomCount - data._summonInstances.Count, spawnfilterd.Count());
        }
        else
        {
            // 무한 소환이면 단순히 랜덤 or 최대치
            data.m_iThisAttackSummonCount = data.m_IsRandomSpawnCount
                ? UnityEngine.Random.Range(data._minSummonCount, data._maxSummonCount + 1)
                : data._maxSummonCount;

            data.m_iThisAttackSummonCount = Mathf.Min(data.m_iThisAttackSummonCount, spawnfilterd.Count());
        }

        // 소환 오브젝트
        GameEntity spawnEneity = null;
        if (data._summonUnitPrefab.TryGetComponent<GameEntity>(out var summonedUnit))
            spawnEneity = summonedUnit;

        // 섞음
        data.selectedPositions = spawnfilterd.OrderBy(_ => UnityEngine.Random.value).Take(data.m_iThisAttackSummonCount).ToList();

        Debug.Log($"예약 : {string.Join(" ", data.selectedPositions)}");

        Managers.SceneServices.GridMut.SetCellType(data.selectedPositions, E_GridCheckType.Reserve, spawnEneity);
    }

    protected override IEnumerable<GridPosition> GetAttackSelectGridPositions(IEnumerable<GridPosition> rangeGridList, GameEntity attacker, GameEntity target, AttackData data)
    {
        return ((AttackData_Summon)data).selectedPositions;
    }

    /// <summary>
    /// 소환하는 용도로 사용
    /// </summary>
    public override void Attack(GameEntity attacker, GameEntity target, AttackData_Summon data)
    {
        base.Attack(attacker, target, data);

        Debug.Log($"소환 : {string.Join(" ", data.selectedPositions)}");

        foreach (GridPosition spawnPos in data.selectedPositions)
        {
            Vector3 worldPos = Managers.SceneServices.Grid.GetWorldPosition(spawnPos);
            GameObject unitObj = Managers.Resource.Instantiate(data._summonUnitPrefab, worldPos, Quaternion.identity);

            if (unitObj.TryGetComponent<GameEntity>(out var summonedUnit))
            {
                List<GridPosition> unitGridPositions = summonedUnit.GetGridPositionListAtCurrentDir();
                summonedUnit.SpawnStart();

                // 등급 업 시도
                if (summonedUnit is ControllableObject cobj)
                {
                    cobj.TryEnhanceGrade();
                }

                if (data.m_IsInfiniteSpawn == false)
                    data._summonInstances.Add(summonedUnit);
            }
            else
            {
                Managers.SceneServices.GridMut.SetCellType(spawnPos, E_GridCheckType.Walkable);
            }
        }

        data.selectedPositions = null;
    }
}
