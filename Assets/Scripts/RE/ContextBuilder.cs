using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BuildingContextBuilder 등 엔티티 타입별로 확장
/// Context의 의존성 주입 담당 생성객체 
/// </summary>
[RequireComponent(typeof(GameEntityBase))]
public abstract  class ContextBuilder : MonoBehaviour
{
    protected GameEntityBase entity;
    protected EntityContext context;

    protected virtual void Awake()
    {
        entity = GetComponent<GameEntityBase>();
    }

    protected virtual void OnDestroy()
    {
        if (context != null)
            EntityManager.Unregister(context);
    }

    /// <summary>
    /// 실제 EntityContext를 생성하는 책임
    /// </summary>
    protected abstract EntityContext BuildContext();
}
