using System;
using System.Collections.Generic;

public sealed class ModuleRegistry
{
    private readonly Dictionary<Type, object> modules = new();

    public void Add<T>(T module) where T : class
        => modules[typeof(T)] = module;

    public bool TryGet<T>(out T module) where T : class
    {
        if (modules.TryGetValue(typeof(T), out var obj) && obj is T t)
        {
            module = t;
            return true;
        }

        module = null;
        return false;
    }

    public T GetOrNull<T>() where T : class
        => TryGet<T>(out var m) ? m : null;

    public bool Has<T>() where T : class
        => modules.ContainsKey(typeof(T));

    public bool Has(Type type)
        => type != null && modules.ContainsKey(type);

    public IEnumerable<Type> DebugTypes()
        => modules.Keys;
}