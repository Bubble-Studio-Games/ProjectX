using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityType
{
    Unit,
    Building,
    Interactable,
    Trap,
    Projectile
}
public interface IGameEntity
{
    int Id { get; }
    EntityType Type { get; }
    Vector3 WorldPosition { get; set; }
    Vector3 Forward { get; set; }
}
/// <summary>
/// 게임에 등장하는 모든 상호작용 오브젝트들 의 베이스클래스
/// </summary>
public abstract class GameEntityBase : MonoBehaviour, IGameEntity
{
    [SerializeField] private int id;
    public abstract EntityType Type { get; }
    public int Id => id;
    public Vector3 WorldPosition
    {
        get => transform.position;
        set => transform.position = value;
    }
    public Vector3 Forward
    {
        get => transform.forward;
        set => transform.forward = value;
    }


    protected virtual void Awake()
    {
        if (id <= 0) id = EntityManager.GenerateId();
    }
}




