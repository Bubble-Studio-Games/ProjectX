using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 엔티티의 동작으로 표현하는 객체단위
/// </summary>
public interface IAction
{
    void Enter();
    void Tick(float dt);
    void Exit();
    bool IsFinished { get; }
    string Name { get; }
}
/// <summary>
/// 공격/피격/사망등의 단일 수행 액션에 : IHardAction 구현하여 Registry에서 다른 Action 차단
/// </summary>
public interface IHardAction { }

/// <summary>
/// TODO: 파일분리 
/// </summary>
public sealed class RE_IdleAction : IAction
{
    public bool IsFinished => false;
    public string Name => "Idle";

    public void Enter() { }
    public void Tick(float dt) { }
    public void Exit() { }
}
public class RE_MoveAction : IAction
{
    private readonly UnitContext ctx;
    public string Name => "Move";
    public GridPosition Target { get; private set; }

    private bool needRepath;
    public bool IsFinished { get; private set; }

    private float speed = 5f;
    private float arriveDist = 0.1f;

    public RE_MoveAction(UnitContext ctx, GridPosition target)
    {
        this.ctx = ctx;
        Target = target;
    }

    public void SetTarget(GridPosition newTarget)
    {
        if (Target.Equals(newTarget)) return;
        Target = newTarget;
        needRepath = true;
    }

    public void Enter()
    {
        IsFinished = false;
        needRepath = false;
    }

    public void Tick(float dt)
    {
        if (IsFinished) return;

        // TODO: 나중에 Grid->World 변환 서비스로 교체
        Vector3 targetWorld = new Vector3(Target.x, ctx.Entity.WorldPosition.y, Target.z);

        Vector3 cur = ctx.Entity.WorldPosition;
        Vector3 next = Vector3.MoveTowards(cur, targetWorld, speed * dt);
        ctx.Entity.WorldPosition = next;

        Vector3 dir = (targetWorld - next);
        if (dir.sqrMagnitude > 0.0001f)
            ctx.Entity.Forward = dir.normalized;

        if (Vector3.Distance(next, targetWorld) <= arriveDist)
            IsFinished = true;
    }

    public void Exit() { }
}
public sealed class RE_AttackAction : IAction
{
    public string Name => "Attack";
    public bool IsFinished { get; private set; }

    private readonly UnitContext self;
    private readonly UnitContext target;
    private readonly ActionController controller;

    public RE_AttackAction(UnitContext self, ActionController controller, UnitContext target)
    {
        this.self = self;
        this.controller = controller;
        this.target = target;
    }

    public void Enter()
    {
        IsFinished = false;
    }

    public void Tick(float dt)
    {
        if (IsFinished) return;
        if (self == null || target == null) { IsFinished = true; return; }

        // 1) CombatModule 존재 확인 (Self-guarding)
        if (!self.Modules.TryGet<CombatModule>(out var selfCombat)) { IsFinished = true; return; }
        if (!target.Modules.TryGet<CombatModule>(out var targetCombat)) { IsFinished = true; return; }

        // 2) Dead 체크 (Health가 없을 수도 있으니 null-safe)
        if (selfCombat.Health != null && selfCombat.Health.IsDead) { IsFinished = true; return; }
        if (targetCombat.Health != null && targetCombat.Health.IsDead) { IsFinished = true; return; }

        // 3) Transform 체크
        var selfTr = self.Transform;
        var targetTr = target.Transform;
        if (selfTr == null || targetTr == null) { IsFinished = true; return; }

        // 4) 사거리 체크
        float dist = Vector3.Distance(selfTr.position, targetTr.position);
        if (dist > selfCombat.Stats.AttackRange)
        {
            // 사거리 밖이면 공격 종료 -> Decider가 다음 틱에 Move를 요청하게 됨
            IsFinished = true;
            return;
        }

        // 5) 쿨다운 체크 (CombatState 없으면 쿨다운 없이 1회 타격)
        float now = Time.time;
        if (selfCombat.State != null)
        {
            if (!selfCombat.State.CanAttack(now))
                return; // 쿨타임이면 대기

            selfCombat.State.ConsumeAttackCooldown(now, selfCombat.Stats.AttackCooldown);
        }

        // 공격 애니메이션 실행 
        controller?.BeTriggered(TriggerActionType.Attack);
        // 6) 데미지 적용 (Health 없으면 아무 일도 안 함)
        int damage = selfCombat.Stats.AttackDamage;
        targetCombat.Health?.TakeDamage(new DamageInfo(damage, context: self, cause: DamageCause.Attack));

        // 7) 1회 공격 후 종료
        IsFinished = true;
    }

    public void Exit() { }
}
/*
public class RE_MoveAction : IAction
{
    private readonly UnitContext ctx;
    public string Name => "Move";
    public GridPosition Target { get; private set; }
    private bool needRepath;

    public bool IsFinished { get; private set; }

    public RE_MoveAction(UnitContext ctx, GridPosition target)
    {
        this.ctx = ctx;
        Target = target;
    }

    public void SetTarget(GridPosition newTarget)
    {
        if (Target.Equals(newTarget)) return;
        Target = newTarget;
        needRepath = true;
    }

    public void Enter()
    {
        // 초기 경로 계산
        // BuildPath(Target);
        needRepath = false;
    }

    public void Tick(float dt)
    {
        if (IsFinished) return;

        if (needRepath)
        {
            // 경로 재계산
            // BuildPath(Target);
            needRepath = false;
        }

        // 실제 이동 처리
        // StepAlongPath(dt);

        // 목표 도달하면:
        // IsFinished = true;
    }

    public void Exit()
    {
        // 정리
    }
}
*/