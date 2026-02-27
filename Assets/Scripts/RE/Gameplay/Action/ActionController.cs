using System;
using UnityEngine;
using Unit.ActionDecider;
using SO.Unit;

public enum TriggerActionType
{
    Attack,
    AttackEnd,
    Hit,
    Die,
}

public class ActionController : MonoBehaviour
{
    public event Action<IAction, IAction> OnActionChanged;
    public event Action<TriggerActionType> OnBeTriggered;

    private UnitContext context;
    private IActionDecider decider;

    public IAction Current { get; private set; }

    public void Init(UnitContext context, IActionDecider decider)
    {
        this.context = context;
        this.decider = decider;
        this.decider?.Init(this.context, this);
    }

    public void RequestIdle()
    {
        if (context == null) return;
        SetAction(context.Actions.GetIdle());
    }
    public void RequestMove(GridPosition target)
    {
        if (context == null) return;
        if (!context.Has(UnitCapabilities.CanMove)) return;

        var move = context.Actions.GetMove();
        if (move == null) return;

        if (ReferenceEquals(Current, move))
        {
            move.SetTarget(target);
            return;
        }

        move.SetTarget(target);
        SetAction(move);
    }
    public void RequestAttack(UnitContext targetCtx)
    {
        if (context == null) return;
        if (!context.Has(UnitCapabilities.CanAttack)) return;

        var atk = context.Actions.GetAttack(targetCtx,this);
        if (atk == null) return;

        SetAction(atk);
    }

    private void SetAction(IAction next)
    {
        if (next == null) return;
        if (ReferenceEquals(Current, next)) return;

        var prev = Current;
        prev?.Exit();

        Current = next;
        Current.Enter();

        OnActionChanged?.Invoke(prev, Current);
    }

    private void Update()
    {
        if (context == null) return;

        decider?.TickDecision(Time.deltaTime);

        if (Current == null) return;

        Current.Tick(Time.deltaTime);

        if (Current.IsFinished)
        {
            var prev = Current;
            prev.Exit();
            Current = null;
            OnActionChanged?.Invoke(prev, null);
        }
    }

    public void BeTriggered(TriggerActionType triggerActionType)
        => OnBeTriggered?.Invoke(triggerActionType);
    // controller.BeTriggered(TriggerActionType.Attack);
}