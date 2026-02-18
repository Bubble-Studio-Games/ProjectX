using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public abstract class BaseTrap : PassiveObject, ITrap, ITriggerTarget
{
	public virtual Animator Animator { get; }

	public abstract bool ExecuteTrap();

	public abstract bool ShouldDespawn();

	public GameObject GetObj() => this.gameObject;

	public abstract void OnTriggerEnterAction(Collider other);

    public override List<Collider> GetChildColliders()
	{
		return GetComponentsInChildren<Collider>().ToList();	
	}

	/// <summary>
	/// 함정 액션을 직접 실행하고 m_CurrentAction을 업데이트 - 충돌 기반 실행용
	/// </summary>
	protected void ExecuteTrapActionDirect()
	{
		var trapAction = GetAction<TrapAction>();
		if (trapAction == null)
			return;

		m_CurrentAction = trapAction;
		trapAction.TakeAction();
	}

	/// <summary>
	/// TrapAction 완료 후 IdleAction으로 전환 - TrapAction에서 호출
	/// </summary>
	public void ReturnToIdleAfterTrap()
	{
		var idleAction = GetAction<IdleAction>();
		if (idleAction != null)
		{
			m_CurrentAction = idleAction;
		}
	}

	protected bool IsValidTarget(GameEntity target)
	{
		if (target == null || target == this)
			return false;

		if (target.m_EObjectType != E_ObjectType.Unit)
			return false;

		return IsEnemy(target);
	}
}
