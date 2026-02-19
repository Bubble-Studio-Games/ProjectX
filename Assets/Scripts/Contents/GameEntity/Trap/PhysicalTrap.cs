using UnityEngine;
using static Define;

public class PhysicalTrap : BaseTrap
{
	[SerializeField] private AttackData _attackData;

	private Animator _animator;
	public override Animator Animator
	{
		get
		{
			if (_animator == null)
				_animator = GetComponentInChildren<Animator>();
			return _animator;
		}
	}


	private GameEntity _triggerTarget;
	private int _currentAttackCount;

	public override void OnTriggerEnterAction(Collider other)
	{
		var target = other.GetComponentInParent<ControllableObject>();
		if (IsValidTarget(target) == false)
			return;

		_triggerTarget = target;
		ExecuteTrapActionDirect();
	}


	/// <summary>
	/// 함정 실행 - 애니메이션 트리거
	/// </summary>
	public override bool ExecuteTrap()
	{
		Animator.SetTrigger("TrapTrigger");
		return false;
	}

	/// <summary>
	/// 애니메이션 이벤트 - 공격 발동
	/// </summary>
	public void OnAttackPoint()
	{
		if (_triggerTarget == null)
			return;

		var attackPattern = Managers.Game.AttackPattern(_attackData);
		if (attackPattern != null)
			attackPattern.StartAttack(this, _triggerTarget, _attackData, null);

		_currentAttackCount++;
		_triggerTarget = null;
	}

	/// <summary>
	/// 최대 공격 횟수 도달 여부 확인
	/// </summary>
	public override bool ShouldDespawn()
	{
		TrapStat trapStat = m_AttributeSystem.m_Stat as TrapStat;
		if (trapStat == null)
			return false;

		var ret = trapStat.MaxAttackCount > 0 && _currentAttackCount >= trapStat.MaxAttackCount;
		return ret;
	}

	[ContextMenu("테스트 발사")]
	public void Shoot()
	{
		Animator.SetTrigger("TrapTrigger");
	}

}
