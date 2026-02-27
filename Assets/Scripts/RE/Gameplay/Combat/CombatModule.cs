public sealed class CombatModule
{
    public UnitStats Stats { get; }
    public RuntimeStats Runtime { get; }
    public Health Health { get; }
    public CombatState State { get; }

    public CombatModule(UnitStats stats, RuntimeStats runtime, Health health, CombatState state)
    {
        Stats = stats;
        Runtime = runtime;
        Health = health;
        State = state;
    }
}