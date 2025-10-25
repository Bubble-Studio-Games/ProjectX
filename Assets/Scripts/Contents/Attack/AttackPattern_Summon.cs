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
    [SerializeField] private bool m_IsRandomSpawnCount = true;
    [SerializeField] private int _minSummonCount = 0;
    [SerializeField] private int _maxSummonCount = 3;
    private List<GameEntity> _summonInstances = new List<GameEntity>();
    [SerializeField] private bool m_IsInfiniteSpawn = false;

    // 소환 위치 가져오기
    List<GridPosition> selectedPositions = new();

    public AttackPattern_Summon()
    {
        m_EAttackType = Define.E_AttackType.Summon;
    }

    public override void Init()
    {
        base.Init();
        _summonInstances.Clear();
    }

    public override (E_AttackCondition condition, HashSet<GridPosition> CanAttackablePos) 
        CanExecute(GameEntity attacker, GameEntity target)
    {
        var ret = base.CanExecute(attacker, target);
        if (ret.condition != E_AttackCondition.Success)
            return ret;

        if(m_IsInfiniteSpawn == false)
        {
            _summonInstances.RemoveAll(unit => unit == null || unit.m_AttributeSystem.m_IsDead);

            if (_summonInstances.Count >= _maxSummonCount)
                return (E_AttackCondition.Fail_IndividualCondition, default);
        }

        return ret;
    }

    public override void StartAttack(GameEntity attacker, GameEntity target, AttackPattern prevAttackpatern)
    {
        base.StartAttack(attacker, target, prevAttackpatern);

        // 초기화
        HashSet<GridPosition> summonablePositions = new();

        // 소환만 하면 됨.
        var attackerGridPosition = attacker.GetGridPosition();

        // 소환 범위 자리 예약
        foreach (var offset in Managers.Game.GetPatternOffsets(this))
        {
            var testGridPosition = attackerGridPosition + offset;

            // 소환 가능한 빈 공간 체크
            if (LevelGrid.Instance.IsGridPositionCheckType(testGridPosition, E_GridCheckType.Walkable))
            {
                summonablePositions.Add(testGridPosition);
            }
        }

        GameEntity spawnEneity = null;
        if (_summonUnitPrefab.TryGetComponent<GameEntity>(out var summonedUnit))
            spawnEneity = summonedUnit;

        int unitsToSummon = 0;

        if (m_IsInfiniteSpawn == false)
        {
            // 🔸 랜덤 소환 카운트 반영
            int randomCount = m_IsRandomSpawnCount
                ? UnityEngine.Random.Range(_minSummonCount, _maxSummonCount + 1)
                : _maxSummonCount;

            unitsToSummon = Mathf.Min(randomCount - _summonInstances.Count, summonablePositions.Count);
        }
        else
        {
            // 무한 소환이면 단순히 랜덤 or 최대치
            unitsToSummon = m_IsRandomSpawnCount
                ? UnityEngine.Random.Range(_minSummonCount, _maxSummonCount + 1)
                : _maxSummonCount;

            unitsToSummon = Mathf.Min(unitsToSummon, summonablePositions.Count);
        }


        // 랜덤 셔플 후 Take
        List<GridPosition> shuffled = summonablePositions.OrderBy(_ => UnityEngine.Random.value).ToList();
        selectedPositions = shuffled.Take(unitsToSummon).ToList();

        LevelGrid.Instance.SetGridPositionCellInfo(selectedPositions, E_GridCheckType.Reserve, spawnEneity);
    }

    /// <summary>
    /// 소환하는 용도로 사용
    /// </summary>
    public override void Attack(GameEntity attacker, GameEntity target)
    {
        foreach (GridPosition spawnPos in selectedPositions)
        {
            Vector3 worldPos = LevelGrid.Instance.GetWorldPosition(spawnPos);
            GameObject unitObj = Managers.Resource.Instantiate(_summonUnitPrefab, worldPos, Quaternion.identity);
            
            if (unitObj.TryGetComponent<GameEntity>(out var summonedUnit))
            {
                List<GridPosition> unitGridPositions = summonedUnit.GetGridPositionListAtCurrentDir();
                summonedUnit.SpawnStart();

                // 등급 업 시도
                if (summonedUnit is ControllableObject cobj)
                {
                    cobj.TryEnhanceGrade();
                }

                if(m_IsInfiniteSpawn == false)
                    _summonInstances.Add(summonedUnit);
            }
            else
            {
                LevelGrid.Instance.SetGridPositionCellInfo(spawnPos, E_GridCheckType.Walkable);
            }
        }

        selectedPositions.Clear();
    }
}

