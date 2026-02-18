using UnityEngine;
using static Define;

public class EnviromentTrap : BaseTrap
{
	[SerializeField] private Bounds _bounds;
	protected override void Awake()
	{
		base.Awake();
	}

	public override void OnTriggerEnterAction(Collider other)
	{
		var target = other.GetComponentInParent<ControllableObject>();
		if (IsValidTarget(target) == false)
			return;

		ExecuteTrapActionDirect();
	}

	public override bool ExecuteTrap()
	{
		Shoot();
		return true;
	}

	public override bool ShouldDespawn()
	{
		return false;
	}

	[ContextMenu("애니메이션 테스트 발사")]
	private void Shoot()
	{
		var map = Managers.SceneServices.MapContainer;
		if (map == null)
			return;

		map.Active(false, m_GridPosition);
	}

	private void OnDrawGizmos()
	{
		if (_bounds.size == Vector3.zero)
			return;

		Gizmos.matrix = transform.localToWorldMatrix;

		Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
		Gizmos.DrawCube(_bounds.center, _bounds.size);

		Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
		Gizmos.DrawWireCube(_bounds.center, _bounds.size);

		Gizmos.matrix = Matrix4x4.identity;
	}

}
