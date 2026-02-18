using UnityEngine;
using static Define;

public class PoisonSwampField : MonoBehaviour
{
	[SerializeField] private SwampBubbleVFX _vfx;
	[SerializeField] private GameObject _swampPlane;

	private IMapContainer _mapContainer;
	private IGridQuery _grid;

    private const string SWAMP_BUBBLE_VFX = "SwampBubbleVFX";
    private const string SWAMP_PLANE = "SwampBubbleVFX";

	private void Reset()
	{
		if (_vfx == null)
			_vfx = this.transform.EnsureChild(SWAMP_BUBBLE_VFX).GetComponent<SwampBubbleVFX>();

		if (_swampPlane == null)
			_swampPlane = this.transform.EnsureChild(SWAMP_PLANE).gameObject;
	}

	public void Setup(IMapContainer mapContainer)
	{
		_mapContainer = mapContainer;
		_grid = mapContainer.Grid;
		_vfx.Setup(_grid);
	}

	public void ActiveBubble(GridPosition gridPos) => _vfx.ActiveSwampTile(gridPos.x, gridPos.z);

	public void Active(bool isActive, GridPosition gridPos)
	{
		_mapContainer?.Active(isActive, gridPos);
	}
}
