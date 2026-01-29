using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 현재 Action 보관, Action 교채, Action Tick(Update) 실행
/// </summary>
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
    public IAction Current { get; private set; }

    public void Init(UnitContext context) 
    {
        this.context = context!=null? context : this.context;
    }
    public void SetAction(IAction next)
    {
        if (ReferenceEquals(Current, next)) return;

        var prev = Current;
        prev?.Exit();

        Current = next;
        Current?.Enter();

        OnActionChanged?.Invoke(prev, Current);
    }
    private void Update()
    {
        if (context == null || Current == null) return;

        Current?.Tick(Time.deltaTime);

        if (Current.IsFinished)
        {
            var prev = Current;
            prev.Exit();
            Current = null;

            OnActionChanged?.Invoke(prev, null);
        }
    }
    public void BeTriggered(TriggerActionType triggerActionType)
    {
        OnBeTriggered?.Invoke(triggerActionType);
    }
    #region TODO Factory 
    public void RequestMove(GridPosition target)
    {
        SetAction(new RE_MoveAction(context, target));
    }
    public void RequestIdle()
    {
        SetAction(new RE_IdleAction());
    }

    #endregion

}