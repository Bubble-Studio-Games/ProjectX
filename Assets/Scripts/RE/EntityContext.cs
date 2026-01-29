using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityContext
{
    public int Id => Entity.Id;
    public EntityType Type { get; }

    public GridPosition GridPosition;
    public IGameEntity Entity { get; }

    public EntityContext(EntityType type, IGameEntity entity, GridPosition startGrid)
    {
        Type = type;
        Entity = entity;
        GridPosition = startGrid;
    }
}
