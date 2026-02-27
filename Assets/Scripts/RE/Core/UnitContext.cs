using SO.Unit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 참조 객체들의 종속성 처리 및 주입 
/// </summary>
public sealed class UnitContext : EntityContext
{
    // 유닛 종속성 및 동작정책 관련
    public UnitCapabilities Capabilities { get; }
    public ActionRegistry Actions { get; }

    // 컨테이너 
    public ModuleRegistry Modules { get; } = new();

    public UnitContext(IGameEntity entity, UnitCapabilities capabilities)
        : base(entity)
    {
        Capabilities = capabilities;
        Actions = new ActionRegistry(this);
    }

    public bool Has(UnitCapabilities cap) => (Capabilities & cap) == cap;
}