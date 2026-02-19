using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "GameConfig/Team Relation Settings")]
public class TeamRelationSettings : ScriptableObject
{
    [Serializable]
    public class TeamRow
    {
        public E_TeamId Team;
        public int AllyMask;
        public int EnemyMask;
    }

    public List<TeamRow> Rows = new();

    // 빠른 lookup용 캐시
    private Dictionary<E_TeamId, TeamRow> _map;

    private void OnEnable()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        _map = new Dictionary<E_TeamId, TeamRow>(Rows.Count);
        foreach (var row in Rows)
            _map[row.Team] = row;
    }

    public bool IsEnemy(E_TeamId self, E_TeamId other)
    {
        if (!_map.TryGetValue(self, out var row))
            return false;

        int bit = 1 << (int)other;
        return (row.EnemyMask & bit) != 0;
    }

    public bool IsAlly(E_TeamId self, E_TeamId other)
    {
        if (!_map.TryGetValue(self, out var row))
            return false;

        int bit = 1 << (int)other;
        return (row.AllyMask & bit) != 0;
    }
}
