using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[CreateAssetMenu(fileName = "BuffConfig", menuName = "Buff")]
public class BuffConfig : ScriptableObject
{
    public new string name;
    public int id;

    [Header("버프 타입")]
    public E_BuffType type;           // 버프, 디버프, All
    [Header("버프 대상")]
    public E_BuffTargetable targetable;       // 아군 / 적 / 아군&적
    [Header("버프 시간")]
    public float duration;          // 지속 시간
    public bool isPeriodic;        // 지속 실행 여부
    public float tickInterval;      // 틱 간격

    [Header("버프 수치")]
    public E_ControlableStat controlableStat;   // 버프할 스탯
    public E_BuffOperator @operator;            // 수치 연산자
    public float value;                         // 수치
    // id 유효 검사 필요
}
public struct BuffData
{
    public string Name { get; }
    public int ID { get; }
    public E_BuffType Type { get; }
    public E_BuffTargetable Targetable { get; }
    public float Duration { get; private set; }
    public bool IsPeriodic { get; }
    public float TickInterval { get; }
    public E_ControlableStat ControlableStat{get;}
    public E_BuffOperator Operator {get;}
    public float Value { get; }

    public BuffData(BuffConfig so)
    {
        Name = so.name;
        ID = so.id;
        Type = so.type;
        Targetable = so.targetable;
        Duration = so.duration;
        IsPeriodic = so.isPeriodic;
        TickInterval = so.tickInterval;
        ControlableStat = so.controlableStat;
        Operator = so.@operator;
        Value = so.value;
    }
    public BuffData(BuffData data)
    {
        Name = data.Name;
        ID = data.ID;
        Type = data.Type;
        Targetable = data.Targetable;
        Duration = data.Duration;
        IsPeriodic = data.IsPeriodic;
        TickInterval = data.TickInterval;
        ControlableStat = data.ControlableStat;
        Operator = data.Operator;
        Value = data.Value;
    }
    public void ResetDuration(float value)
    {
        Duration = value;
    }
}
public enum E_BuffTargetable
{
    Object,
    Building,
    All,
    None
}
public enum E_BuffType
{
    Buff,
    Debuff,
    All,
    None
}
public enum E_BuffOperator
{
    Add,        // +
    Multiply,   // *
    Subtract,   // -
    Divide      // /
}
public enum E_ControlableStat
{
    INT_MaxHP,
    FLOAT_RegenHP,
    INT_MaxMP,
    FLOAT_RegenMP,
    FLOAT_ChaseSpeed,
    FLOAT_WalkSpeed,
    INT_DefaultMoveRange,
    INT_DetectRange,
    INT_ChaseRange,
    INT_PhysicalDefence,
    INT_MagicalDefence,
    FLOAT_CounterAttackChance,
    FLOAT_EvasionChance,
    FLOAT_KnockbackRegist,
    None,
}