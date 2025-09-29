using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Stat/GameEntityStat")]
public class ControllableObjectStat : BaseStat
{
    [Header("Player Only")]
    public int m_iSpawnCost; // 소환 비용 (아군 유닛일 때만)
}




