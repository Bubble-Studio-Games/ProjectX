using System.Collections.Generic;
using System.Linq;

public static class EntityManager
{
    private static int _nextId = 1;
    private static readonly Dictionary<int, EntityContext> _entities = new();

    public static int GenerateId()
    {
        return _nextId++;
    }

    public static void Register(EntityContext ctx)
    {
        _entities[ctx.Id] = ctx;
    }

    public static void Unregister(EntityContext ctx)
    {
        if (ctx != null)
            _entities.Remove(ctx.Id);
    }

    public static bool TryGet(int id, out EntityContext ctx)
        => _entities.TryGetValue(id, out ctx);

    public static IEnumerable<UnitContext> Units()
        => _entities.Values.OfType<UnitContext>();

    public static IEnumerable<EntityContext> All()
        => _entities.Values;
}
