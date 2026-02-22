using System;

public sealed class TickManager : IManager
{
    public event Action OnUpdateActionTick;

    public void Tick()
    {
        OnUpdateActionTick?.Invoke();
    }

    public void Clear()
    {
        OnUpdateActionTick = null;
    }

    public void Init()
    {
    }
}
