public sealed class AttackPattern_Melee : AttackPattern<AttackData_Melee>
{
    public override void Attack(GameEntity attacker, GameEntity target, AttackData_Melee data)
    {
        base.Attack(attacker, target, data);

        foreach (var t in GetAttackTargetGridPositions(attacker, target, data))
        {
            t.m_AttributeSystem.Hit(data, attacker);
        }
    }
}
