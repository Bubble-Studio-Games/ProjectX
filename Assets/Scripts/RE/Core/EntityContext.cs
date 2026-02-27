using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EntityContext
{
    public int Id => Entity.Id;
    public IGameEntity Entity { get; }
    public Transform Transform;
    //public GridPosition GridPosition;

    public EntityContext(IGameEntity entity/*, GridPosition startGrid*/)
    {
        Entity = entity;
       // GridPosition = startGrid;
    }
}
