using UnityEngine;
using static Define;

public class TrapSystem : MonoBehaviour
{
	[Header("함정 배치 설정")]
	[SerializeField, Range(0, 100)] private int _trapCountPerFloor = 8;
	[SerializeField] private E_TrapType _trapType = E_TrapType.All;

	[SerializeField] private TrapPositionSelector _positionSelector;
	[SerializeField] private TrapCreator _trapCreator;

	private IMapContainer _map;

	private void OnValidate()
	{
		if (_positionSelector == null)
			_positionSelector = this.gameObject.GetOrAddComponent<TrapPositionSelector>();

		if (_trapCreator == null)
			_trapCreator = this.gameObject.GetOrAddComponent<TrapCreator>();
	}

	private void Start()
	{
		Init();
	}

	private void Init()
	{
		_map = Managers.SceneServices.MapContainer;

		if (_map == null)
		{
			Debug.LogError("MapContainer 서비스가 등록되지 않았습니다!");
			return;
		}

		_trapCreator.Setup(_map);
		_trapCreator.SetTrapType(_trapType);

		int floorAmount = _map.Grid.GetFloorAmount();
		for (int floor = 0; floor < floorAmount; floor++)
			InitTrapPositionsForFloor(floor);
	}

	private void InitTrapPositionsForFloor(int floor)
	{
		_positionSelector.Setup(_map.Grid, floor);

		// 함정 배치 위치 선택
		var trapPositions = _positionSelector.GetRandomTrapPositions(_trapCountPerFloor);

		int physicalTrapCount = 0;

		foreach (var gridPos in trapPositions)
		{
			var trap = _trapCreator.CreateTrap(gridPos);
			if (trap == null)
				continue;

			physicalTrapCount++;
		}

		Debug.Log($"Floor {floor}: 물리 함정 {physicalTrapCount}개 배치 완료");
	}
}

