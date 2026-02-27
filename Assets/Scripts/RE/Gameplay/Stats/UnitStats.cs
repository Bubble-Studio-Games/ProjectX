using System;

[Serializable]
public sealed class UnitStats
{
    public int MaxHP { get; private set; }
    public float MoveSpeed { get; private set; }
    public int AttackDamage { get; private set; }
    public float AttackRange { get; private set; }
    public float AttackCooldown { get; private set; }

    public int MaxMP { get; private set; }
    public int Defense { get; private set; }

    public UnitStats(UnitStatsSO so)
    {
        if (so == null) throw new ArgumentNullException(nameof(so));

        MaxHP = Math.Max(1, so.maxHP);
        MoveSpeed = Math.Max(0f, so.moveSpeed);
        AttackDamage = Math.Max(0, so.attackDamage);
        AttackRange = Math.Max(0f, so.attackRange);
        AttackCooldown = Math.Max(0f, so.attackCooldown);

        MaxMP = Math.Max(0, so.maxMP);
        Defense = Math.Max(0, so.defense);
    }
}
