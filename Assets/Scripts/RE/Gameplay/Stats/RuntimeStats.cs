using System;

/// <summary>
/// 인게임 도중 변동되는 Stat수치에 대한 처리 및 보관  
/// </summary>
public sealed class RuntimeStats
{
    public int CurrentHP { get; private set; }
    public int CurrentMP { get; private set; }
    public int Shield { get; private set; }

    public int MaxHP { get; private set; }
    public int MaxMP { get; private set; }

    public event Action<int, int> OnHPChanged; // (current, max)
    public event Action<int, int> OnMPChanged;

    public RuntimeStats(int maxHP, int maxMP = 0)
    {
        MaxHP = Math.Max(1, maxHP);
        MaxMP = Math.Max(0, maxMP);

        CurrentHP = MaxHP;
        CurrentMP = MaxMP;
        Shield = 0;
    }

    public void SetHP(int value)
    {
        int clamped = Clamp(value, 0, MaxHP);
        if (clamped == CurrentHP) return;

        CurrentHP = clamped;
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    public void SetMP(int value)
    {
        int clamped = Clamp(value, 0, MaxMP);
        if (clamped == CurrentMP) return;

        CurrentMP = clamped;
        OnMPChanged?.Invoke(CurrentMP, MaxMP);
    }

    public void AddShield(int amount)
    {
        Shield = Math.Max(0, Shield + amount);
        // 필요하면 ShieldChanged 이벤트 추가
    }

    public void ConsumeShield(int amount)
    {
        Shield = Math.Max(0, Shield - amount);
    }

    private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
}
