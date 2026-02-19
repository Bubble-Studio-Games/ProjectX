using UnityEngine;
using static Define;

[DefaultExecutionOrder(1)]
public class MapContainer : MonoBehaviour, IMapContainer
{
	private IGridQuery _gridQuery;
	private IGridMutation _gridMutation;
	private PoisonSwampField _poisonSwamp;

	public IGridQuery Grid => _gridQuery;
	public IGridMutation GridMut => _gridMutation;

	private PoisonSwampField PoisonSwamp
	{
		get
		{
			if (_poisonSwamp == null)
			{
				_poisonSwamp = GetComponentInChildren<PoisonSwampField>();
				_poisonSwamp.Setup(this);
			}

			return _poisonSwamp;
		}
	}

	private void Awake()
	{
		_gridQuery = Managers.SceneServices.Grid;
		_gridMutation = Managers.SceneServices.GridMut;

		if (_gridQuery == null || _gridMutation == null)
		{
			Debug.LogError("Grid 서비스가 등록되지 않았습니다!");
			return;
		}

		Managers.SceneServices.Register<IMapContainer>(this);
	}

	public void Active(bool isActive, GridPosition gridPos)
	{
		_gridMutation?.Active(isActive, gridPos);
		PoisonSwamp.ActiveBubble(gridPos);
	}
}
