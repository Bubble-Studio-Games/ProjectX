using UnityEngine;

/// <summary>
/// 함정 스탯 - 함정 특화 속성 관리
/// </summary>
[CreateAssetMenu(menuName = "Stat/Trap Stat")]
public class TrapStat : BaseStat
{
	[Header("함정 공격 설정")]
	[SerializeField, Range(-1, 10)] private int _maxAttackCount = 5;
	[SerializeField, Range(-1, 30)] private float _coolTime = 0f;
	[SerializeField] private bool _isDotType = false;

	[Header("함정 통과 설정")]
	[SerializeField] private bool _isPassable = true;

	[Header("함정 파괴 설정")]
	[SerializeField] private bool _isDestructible = true;

	public int MaxAttackCount => _maxAttackCount;
	public float CoolTime => _coolTime;
	public bool IsDotType => _isDotType;
	public bool IsPassable => _isPassable;
	public bool IsDestructible => _isDestructible;
}
