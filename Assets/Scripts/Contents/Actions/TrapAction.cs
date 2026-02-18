using UnityEngine;
using static Define;

/// <summary>
/// 함정 액션 - 실행을 ITrap에 위임하는 래퍼
/// </summary>
public class TrapAction : BaseAction
{
	private bool _isInstant;

	public TrapAction()
	{
		m_actionName = "TrapAction";
	}

	public override BaseAction TakeAction(GridPosition gridPosition = default)
	{
		// 이미 실행 중이면 다시 시작하지 않음
		if (m_bIsActive)
			return this;

		ActionStart();

		var trap = m_GameEntity.GetComponent<ITrap>();
		if (trap == null)
		{
			ActionComplete();
			return this;
		}

		_isInstant = trap.ExecuteTrap();

		if (_isInstant)
			ActionComplete();

		return this;
	}

	public override bool IsValidActionGridPosition(GridPosition gridPosition)
	{
		return true;
	}

	protected override void Update()
	{
		base.Update();

		if (m_GameEntity.m_CurrentAction != this)
			return;

		if (m_bIsActive == false)
			return;

		// 즉시 완료 함정은 Update 불필요
		if (_isInstant)
			return;

		var trap = m_GameEntity as BaseTrap;
		if (trap == null)
			return;

		var animator = trap.Animator;
		if (animator == null)
			return;

		AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

		// 애니메이션 재생 완료 확인
		if (stateInfo.normalizedTime >= 1f)
			ActionComplete();
	}

	protected override void ActionComplete()
	{
		base.ActionComplete();

		var trap = m_GameEntity.GetComponent<ITrap>();
		if (trap != null && trap.ShouldDespawn())
			m_GameEntity.DeSpawnComplete();
		else if (m_GameEntity is BaseTrap baseTrap)
		{
			baseTrap.ReturnToIdleAfterTrap();
		}

		_isInstant = false;
	}
}
