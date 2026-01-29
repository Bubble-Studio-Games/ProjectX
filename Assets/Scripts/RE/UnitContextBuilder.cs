using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ActionController))]
public sealed class UnitContextBuilder : ContextBuilder
{
    [Header("Unit Config")]
    [SerializeField] private float moveSpeed = 6f;

    protected override void Awake()
    {
        base.Awake();

        context = BuildContext();
        EntityManager.Register(context);

        var controller = GetComponent<ActionController>();
        controller.Init((UnitContext)context);
    }

    protected override EntityContext BuildContext()
    {
        var grid = Managers.SceneServices.Grid.GetGridPosition(entity.WorldPosition);

        return new UnitContext(
            entity,
            grid,
            moveSpeed
        );
    }
}
