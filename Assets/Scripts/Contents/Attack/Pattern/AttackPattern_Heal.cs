using static Define;

public sealed class AttackPattern_Heal : AttackPattern<AttackData_Heal>
{
    protected override bool IsValidAllyTarget(GameEntity attacker, GameEntity target)
    {
        // 힐: 체력이 꽉 찬 아군은 제외 
        // 팀 비교로 구현
        if (attacker.IsAlly(target) && target.m_AttributeSystem.FullHealth == false)
            return true;
        else
            return false;
    }

    public override void Attack(GameEntity attacker, GameEntity target, AttackData_Heal data)
    {
        base.Attack(attacker, target, data);

        foreach (var t in GetAttackTargetGridPositions(attacker, target, data))
        {
            t.m_AttributeSystem.Heal(data.HealAmount, E_HealType.None, attacker);
            if (data.HealEffectPrefab != null)
                Managers.Resource.Instantiate(data.HealEffectPrefab.gameObject, t.transform.position, t.transform.rotation);
        }
    }
}
