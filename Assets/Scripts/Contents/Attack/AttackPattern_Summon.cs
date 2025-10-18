using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

/// <summary>
/// 유닛 소환 공격 패턴
/// </summary>
[CreateAssetMenu(menuName = "Attack Pattern/Summon")]
public class AttackPattern_Summon : AttackPattern<AttackPatternInfoClip>
{
    [Header("Summon Settings")]
    [SerializeField] private GameObject _summonUnitPrefab;
    [SerializeField] private int _maxSummonCount = 3;

    private List<GameEntity> _summonInstances = new List<GameEntity>();

    public AttackPattern_Summon()
    {
        m_EAttackType = Define.E_AttackType.Summon;
    }

    public override void Init()
    {
        base.Init();
        _summonInstances.Clear();
    }

    public override E_AttackCondition CanExecute(ControllableObject attacker, GameEntity target)
    {
        var ret = base.CanExecute(attacker, target);
        if (ret != E_AttackCondition.Success)
            return ret;

        _summonInstances.RemoveAll(unit => unit == null || unit.m_AttributeSystem.m_IsDead);

        if (_summonInstances.Count >= _maxSummonCount)
            return E_AttackCondition.Fail_IndividualCondition;

        return E_AttackCondition.Success;
    }

    public override void StartAttack(ControllableObject attacker, GameEntity target, AttackPattern prevAttackpatern)
    {
        base.StartAttack(attacker, target, prevAttackpatern);
    }

    /// <summary>
    /// 소환하는 용도로 사용
    /// </summary>
    public override void Attack(ControllableObject attacker, GameEntity target)
    {
        GridPosition selfPos = attacker.GetGridPosition();
        GridPosition targetPos = target.GetGridPosition();
        E_Dir dir = LevelGrid.Instance.GetDirGridPosition(selfPos, targetPos);

        HashSet<GridPosition> summonablePositions =
            m_RangeOffset
            .Select(x => LevelGrid.Instance.ToGridPosition(x, selfPos, dir))
            .Where(pos => LevelGrid.Instance.IsValidGridPosition(pos) && 
                         !LevelGrid.Instance.HasAnyUnitOnGridPosition(pos))
            .ToHashSet();

        if (summonablePositions.Count <= 0)
            return;

        int unitsToSummon = Mathf.Min(_maxSummonCount - _summonInstances.Count, summonablePositions.Count);
        
        List<GridPosition> selectedPositions = summonablePositions.Take(unitsToSummon).ToList();

        foreach (GridPosition spawnPos in selectedPositions)
        {
            Vector3 worldPos = LevelGrid.Instance.GetWorldPosition(spawnPos);
            GameObject unitObj = Managers.Resource.Instantiate(_summonUnitPrefab, worldPos, Quaternion.identity);
            
            if (unitObj.TryGetComponent<GameEntity>(out var summonedUnit))
            {
                List<GridPosition> unitGridPositions = summonedUnit.GetGridPositionListAtCurrentPosition();
                LevelGrid.Instance.AddUnitAtGridPosition(unitGridPositions, summonedUnit);
                
                _summonInstances.Add(summonedUnit);
            }
        }
    }

    public override void EndAttack(ControllableObject attacker, GameEntity target)
    {
        base.EndAttack(attacker, target);
    }
}

