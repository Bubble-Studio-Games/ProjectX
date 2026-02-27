using System;

/// <summary>
/// 체력 상호작용 관련 로직
/// RuntimeStats 의 수치에 대한 계산 담당
/// 결과에 대한 이벤트 처리 
/// </summary>
public sealed class Health
{
    private readonly RuntimeStats runtime;
    public bool IsDead { get; private set; }

    public event Action<DamageInfo> OnDamaged;
    public event Action OnDead;
    public event Action<int> OnHealed;
    public event Action OnRevived;

    public Health(RuntimeStats runtimeStats)
    {
        runtime = runtimeStats ?? throw new ArgumentNullException(nameof(runtimeStats));
        IsDead = runtime.CurrentHP <= 0;
    }

    public void TakeDamage(in DamageInfo info)
    {
        if (IsDead) return;
        if (info.Amount <= 0) return;

        int remaining = info.Amount;

        //Shield 우선 계산 - 정책에 따라 변동
        if (runtime.Shield > 0)
        {
            int absorbed = Math.Min(runtime.Shield, remaining);
            runtime.ConsumeShield(absorbed);
            remaining -= absorbed;
        }

        if (remaining <= 0)
        {
            OnDamaged?.Invoke(info);
            return;
        }

        runtime.SetHP(runtime.CurrentHP - remaining);
        OnDamaged?.Invoke(info);

        if (runtime.CurrentHP <= 0)
        {
            IsDead = true;
            OnDead?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        int before = runtime.CurrentHP;
        runtime.SetHP(runtime.CurrentHP + amount);

        int healed = runtime.CurrentHP - before;
        if (healed > 0) OnHealed?.Invoke(healed);

        if (IsDead && runtime.CurrentHP > 0)
        {
            IsDead = false;
            OnRevived?.Invoke();
        }
    }

    public void ReviveFull()
    {
        if (!IsDead) return;
        runtime.SetHP(runtime.MaxHP);
        IsDead = false;
        OnRevived?.Invoke();
    }
}
