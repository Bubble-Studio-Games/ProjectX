using static Define;
using UnityEngine;

public sealed class AttackPattern_Dot : AttackPattern<AttackData_Dot>
{
	protected override bool IsValidEnemyTarget(GameEntity attacker, GameEntity target)
	{
		// DOT는 적에게만 적용
		return attacker.IsEnemy(target);
	}

	public override void Attack(GameEntity attacker, GameEntity target, AttackData_Dot data)
	{
		base.Attack(attacker, target, data);

		var statusEffectSystem = Managers.SceneServices.StatusEffectSystem;
		if (statusEffectSystem == null)
			return;

		foreach (var t in GetAttackTargetGridPositions(attacker, target, data))
		{
			statusEffectSystem.ApplyStatusEffect<StatusEffect_Dot>(t, attacker, data);
		}
	}
}
