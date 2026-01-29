using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IAction
{
    void Enter();
    void Tick(float dt);
    void Exit();
    bool IsFinished { get; }
    string Name { get; }
}
public sealed class RE_IdleAction : IAction
{
    public bool IsFinished => false;
    public string Name => "Idle";

    public void Enter() { }
    public void Tick(float dt) { }
    public void Exit() { }
}
public sealed class RE_MoveAction : IAction
{
    private readonly UnitContext ctx;
    private readonly GridPosition target;

    private List<GridPosition> path;
    private int pathIndex;

    private Vector3 currentWorldTarget;

    private const float StopDistance = 0.05f;

    public string Name => "Move";
    public bool IsFinished { get; private set; }

    public RE_MoveAction(UnitContext ctx, GridPosition target)
    {
        this.ctx = ctx;
        this.target = target;
    }

    public void Enter()
    {
        IsFinished = false;

        var start = ctx.GridPosition;

        path = Managers.SceneServices.Pathfinder.FindPath(start, target, out _);

        if (path == null || path.Count == 0)
        {
            // 도달 불가능
            IsFinished = true;
            return;
        }

        pathIndex = 0;
        AdvanceToNextNode();
    }

    public void Tick(float dt)
    {
        if (IsFinished)
            return;

        var pos = ctx.Entity.WorldPosition;
        var to = currentWorldTarget - pos;

        // 노드 도착
        if (to.sqrMagnitude <= StopDistance * StopDistance)
        {
            AdvanceToNextNode();
            return;
        }

        // 이동
        ctx.Entity.WorldPosition = Vector3.MoveTowards(
            pos,
            currentWorldTarget,
            ctx.MoveSpeed * dt
        );

        // 회전
        var dir = to.normalized;
        if (dir.sqrMagnitude > 0.0001f)
            ctx.Entity.Forward = Vector3.Slerp(ctx.Entity.Forward, dir, dt * 15f);
    }

    public void Exit()
    {
        // 최종 GridPosition 보정
        ctx.GridPosition = target;
    }

    private void AdvanceToNextNode()
    {
        if (pathIndex >= path.Count)
        {
            IsFinished = true;
            return;
        }

        var nextGrid = path[pathIndex++];
        currentWorldTarget = Managers.SceneServices.Grid.GetWorldPosition(nextGrid);

        // 중간 경유 노드 갱신
        ctx.GridPosition = nextGrid;
    }
}