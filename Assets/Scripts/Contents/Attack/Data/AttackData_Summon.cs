using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 유닛 소환 공격 패턴
/// </summary>
[CreateAssetMenu(menuName = "Attack Pattern/Summon")]
public class AttackData_Summon : AttackData
{
    [Header("Summon Settings")]
    public GameObject _summonUnitPrefab;
    public bool m_IsRandomSpawnCount = true;
    public int _minSummonCount = 0;
    public int _maxSummonCount = 3;
    public List<GameEntity> _summonInstances = new List<GameEntity>();
    public bool m_IsInfiniteSpawn = false;

    public int m_iThisAttackSummonCount = 0;

    // 소환 위치 가져오기
    // 랜덤 결과 고정을 위해서 List 사용
    public List<GridPosition> selectedPositions;

    private void OnEnable()
    {
        AttackType = E_AttackType.Summon;
    }
}

