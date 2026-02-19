using UnityEngine;

[CreateAssetMenu(menuName = "Attack Pattern/Dot")]
public class AttackData_Dot : AttackData
{
	[Header("DOT 설정")]
	[SerializeField, Range(0, 5)] private float _dotTickInterval = 0.1f;
	[SerializeField, Range(1, 100)] private int _dotTickCount = 5;
	[SerializeField, Range(0, 10)] private int _dotDamagePerTick = 10;

	public float DotTickInterval => _dotTickInterval;
	public int DotTickCount => _dotTickCount;
	public int DotDamagePerTick => _dotDamagePerTick;
	public float TotalDuration => _dotTickInterval * _dotTickCount;
}
