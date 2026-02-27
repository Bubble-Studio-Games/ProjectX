using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EntityManager
{
    private static int _nextId = 1;
    private static readonly Dictionary<int, EntityContext> _entities = new();
    private static readonly Dictionary<Transform, EntityContext> byTransform = new();

    public static int GenerateId()
    {
        return _nextId++;
    }

    public static void Register(EntityContext ctx)
    {
        _entities[ctx.Id] = ctx; 
        if (ctx != null && ctx.Transform != null)
            byTransform[ctx.Transform] = ctx;
    }

    public static void Unregister(EntityContext ctx)
    {
        if (ctx != null)
            _entities.Remove(ctx.Id);
        if (ctx != null && ctx.Transform != null)
            byTransform.Remove(ctx.Transform);
    }

    public static bool TryGet(int id, out EntityContext ctx)
        => _entities.TryGetValue(id, out ctx);
    public static bool TryGet<T>(int id, out T ctx) where T : EntityContext
    {
        ctx = null;
        if (!_entities.TryGetValue(id, out var baseCtx))
            return false;

        ctx = baseCtx as T;
        return ctx != null;
    }
    public static bool TryGetByTransform<T>(Transform tr, out T ctx) where T : EntityContext
    {
        ctx = null;
        if (tr == null) return false;

        if (!byTransform.TryGetValue(tr, out var baseCtx))
            return false;

        ctx = baseCtx as T;
        return ctx != null;
    }

    public static IEnumerable<UnitContext> Units()
        => _entities.Values.OfType<UnitContext>();
    public static IEnumerable<EntityContext> All()
        => _entities.Values;
}
