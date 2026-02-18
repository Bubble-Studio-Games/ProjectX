using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public class GridObject : MonoBehaviour
{
    private readonly HashSet<GameEntity> _units = new(); // 중복 방지

    private GridSystem<GridObject> gridSystem;
    [SerializeField] private GridPosition gridPosition;

    public bool HasAnyUnit() => _units.Count > 0;
    public IReadOnlyCollection<GameEntity> GetUnits() => _units;
    public GridPosition GridPosition => gridPosition;

    public void Init(GridSystem<GridObject> gridSystem, GridPosition gridPosition)
    {
        this.gridSystem = gridSystem;
        this.gridPosition = gridPosition;
    }

    public void Active(bool value)
    {
        var obj = GetComponentInChildren<MeshFilter>().gameObject;
        obj.SetActive(value);
    }

    public override string ToString()
    {
        return $"{gridPosition}\nCount:{_units.Count}";
    }


    public void AddUnit(GameEntity unit)
    {
        if (unit != null) _units.Add(unit);
    }

    public void RemoveUnit(GameEntity unit)
    {
        if (unit != null) _units.Remove(unit);
    }

    // “대표 엔티티” 규칙: 우선순위(건물 > 유닛 > 기타) 같은 걸로 고를 수도 있음
    public GameEntity GetTopUnitOrNull()
    {
        if (_units.Count == 0) return null;
        return _units.First(); // 우선은 아무거나. (나중에 규칙 넣기)
    }
}