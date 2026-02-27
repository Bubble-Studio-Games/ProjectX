public enum DamageCause { Attack, DoT, Trap }

public readonly struct DamageInfo
{
    public readonly int Amount;
    public readonly UnitContext Context;    // 공격주체 참조
    public readonly DamageCause Cause;

    public DamageInfo(int amount, UnitContext context, DamageCause cause = DamageCause.Attack)
    {
        Amount = amount < 0 ? 0 : amount;
        Context = context;
        Cause = cause;
    }
}