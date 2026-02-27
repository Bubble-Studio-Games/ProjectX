
//필요한 액션을 보관하고 돌려주는 역할
using SO.Unit;
/// <summary>
/// Action 요청 및 생성 처리
/// </summary>
public sealed class ActionRegistry
{
    private readonly UnitContext ctx;

    private RE_IdleAction idle;
    private RE_MoveAction move;
    private RE_AttackAction attack;
    public ActionRegistry(UnitContext ctx)
    {
        this.ctx = ctx;
    }
    public RE_IdleAction GetIdle()
    {
        idle ??= new RE_IdleAction();
        return idle;
    }
    public RE_MoveAction GetMove()
    {
        if (!ctx.Has(UnitCapabilities.CanMove))
            return null;

        move ??= new RE_MoveAction(ctx, default);
        return move;
    }
    public RE_AttackAction GetAttack(UnitContext target,ActionController controller)
    {
        if (!CanAttack)
            return null;

        attack ??= new RE_AttackAction(ctx,controller, target);
        return attack;
    }
    public bool CanAttack => ctx.Has(UnitCapabilities.CanAttack);

}
