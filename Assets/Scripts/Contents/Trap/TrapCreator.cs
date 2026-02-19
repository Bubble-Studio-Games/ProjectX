using System;
using UnityEngine;
using static Define;

public interface ITrap
{
	public GameObject GetObj();
	public bool ExecuteTrap();
	public bool ShouldDespawn();
}

public class TrapCreator : MonoBehaviour
{
	private IMapContainer _map;
	private Transform _trapRoot;
	private E_TrapType _trapType = E_TrapType.All;

	private Transform TrapRoot
	{
		get
		{
			if (_trapRoot == null)
			{
				GameObject trapParentObj = GameObject.Find("@Trap");
				if (trapParentObj == null)
					trapParentObj = new GameObject("@Trap");
				_trapRoot = trapParentObj.transform;
			}
			return _trapRoot;
		}
	}

	private static readonly string[] PHYSICAL_TRAP_PATHS = {
		Define.WOODEN_SPEAR_FLOOR_TRAP,
		Define.WOODEN_SPIKE_TRAP
	};

	private static readonly string[] ENVIRONMENT_TRAP_PATHS = {
		Define.POISON_TRAP
	};

	public void Setup(IMapContainer map)
	{
		_map = map;
	}

	public void SetTrapType(E_TrapType trapType)
	{
		_trapType = trapType;
	}

	public ITrap CreateTrap(GridPosition gridPos)
	{
		bool canCreatePhysical = (_trapType & E_TrapType.Physical) != 0;
		bool canCreateEnvironment = (_trapType & E_TrapType.Environment) != 0;

		if (canCreatePhysical && canCreateEnvironment)
		{
			bool isPhysicalTrap = UnityEngine.Random.value > 0.5f;
			return isPhysicalTrap ? CreatePhysicalTrap(gridPos) : CreateEnvironmentTrap(gridPos);
		}
		else if (canCreatePhysical)
		{
			return CreatePhysicalTrap(gridPos);
		}
		else if (canCreateEnvironment)
		{
			return CreateEnvironmentTrap(gridPos);
		}

		return null;
	}

	private PhysicalTrap CreatePhysicalTrap(GridPosition gridPos)
	{
		var ret = CreateTrap<PhysicalTrap>(PHYSICAL_TRAP_PATHS, gridPos);
		return ret;
	}

	private EnviromentTrap CreateEnvironmentTrap(GridPosition gridPos)
	{
		var ret = CreateTrap<EnviromentTrap>(ENVIRONMENT_TRAP_PATHS, gridPos);
		return ret;
	}

	private T CreateTrap<T>(string[] paths, GridPosition gridPos) where T : Component, ITrap
	{
		string selectedPath = paths[UnityEngine.Random.Range(0, paths.Length)];

		GameObject trapPrefab = Managers.Resource.Load<GameObject>(selectedPath);
		if (trapPrefab == null)
		{
			Debug.LogError($"{selectedPath} 프리팹을 로드할 수 없습니다: {selectedPath}");
			return null;
		}

		var pos = _map.Grid.GetWorldPosition(gridPos);
		var trap = Managers.Resource.Instantiate<T>(trapPrefab, pos, TrapRoot);

		if (trap == null)
		{
			Debug.LogError($"{selectedPath} 컴포넌트를 찾을 수 없습니다!");
			return null;
		}

		return trap;
	}
}
