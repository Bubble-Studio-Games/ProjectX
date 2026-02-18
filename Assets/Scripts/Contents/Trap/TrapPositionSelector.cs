using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public class TrapPositionSelector : MonoBehaviour
{
	private IGridQuery _grid;
	private int _width;
	private int _height;
	private int _floor;

	public void Setup(IGridQuery grid, int floor)
	{
		_grid = grid;
		_floor = floor;
		_width = _grid.GetWidth();
		_height = _grid.GetHeight();
	}

	public List<GridPosition> GetRandomTrapPositions(int trapCount)
	{
		var ret = new List<GridPosition>();

		// 현재 층에서 모든 유효한 위치 수집
		var validPositions = GetAllValidPositions();

		if (validPositions.Count < trapCount)
		{
			Debug.LogWarning($"유효한 위치가 부족합니다. 요청: {trapCount}, 사용 가능: {validPositions.Count}");
			trapCount = validPositions.Count;
		}

		// Fisher-Yates 셔플로 랜덤 선택
		for (int i = 0; i < trapCount; i++)
		{
			int randomIndex = Random.Range(i, validPositions.Count);

			// 스왑
			(validPositions[i], validPositions[randomIndex]) = (validPositions[randomIndex], validPositions[i]);

			ret.Add(validPositions[i]);
		}

		return ret;
	}

	private List<GridPosition> GetAllValidPositions()
	{
		var ret = new List<GridPosition>();

		for (int x = 0; x < _width; x++)
		{
			for (int z = 0; z < _height; z++)
			{
				var gridPos = new GridPosition(x, z, _floor);

				// 유효한 위치만 수집 (장애물 제외)
				if (_grid.IsValidGridPosition(gridPos) == false)
					continue;

				if (_grid.IsGridPositionCheckType(gridPos, E_GridCheckType.Obstacle))
					continue;

				// 바닥이 있는지 레이캐스트로 확인
				Vector3 worldPos = _grid.GetWorldPosition(gridPos);
				if (HasGround(worldPos) == false)
					continue;

				ret.Add(gridPos);
			}
		}

		return ret;
	}

	private bool HasGround(Vector3 worldPos)
	{
		const float raycastOffsetDistance = 1f;
		const float raycastDistance = 2f;

		bool ret = Physics.Raycast(
			worldPos + Vector3.up * raycastOffsetDistance,
			Vector3.down,
			raycastDistance);

		return ret;
	}
}
