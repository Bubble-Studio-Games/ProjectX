using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unit에 부착, 데이터 객체를 담는 컴포넌트
/// </summary>
public sealed class UnitContext : EntityContext
{
    //public Stat Stats;
    public int TargetEntityId = -1;

    public ActionController ActionController;

    //public bool IsDead => Stats.Hp <= 0;
    public float MoveSpeed;
    public UnitContext(
        IGameEntity gameEntity,
        GridPosition startGrid,
        float moveSpeed
    ) : base(EntityType.Unit, gameEntity, startGrid)
    {
        MoveSpeed = moveSpeed;
        //Stats = new StatBlock(100, 50);
        //ActionController = new ActionController(this);
    }

}
