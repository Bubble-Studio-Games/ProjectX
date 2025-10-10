using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 버프 인터페이스
/// </summary>
public interface IBuff
{
    IBuffSender Sender { get; }
    BuffData Data { get; }
    bool IsExpired { get; } // 만료 여부
    void SetOwner(IBuffSender owner);
    void Update(float deltaTime);
    void Reset(BuffData data);
}

/// <summary>
/// 버프를 가질 수 있는 객체에게 붙이는 인터페이스
/// </summary>
public interface IBuffReceiver
{
    public List<IBuff> TakeBuffs { get; } // 받은 버프 넣는 곳
    void ApplyBuff(IBuff buff)
    {
        if (buff == null) return;
        IBuff buffToRemove = null;
        bool canAdd = true;

        foreach (var hasBuff in TakeBuffs)
        {
            // 동일한 건물/오브젝트인지 검사 
            bool isEqualSender = hasBuff.Sender.ObjectID == buff.Sender.ObjectID;
            // 같은 스텟인지 검사
            bool isEqualStat = hasBuff.Data.ControlableStat == buff.Data.ControlableStat;

            if (isEqualSender && isEqualStat)
            {
                var higher = SelectHigherStatValue(in hasBuff, in buff, out IBuff lower);
                buffToRemove = lower;
                if (higher == hasBuff)
                    canAdd = false; // 기존이 더 강하면 새 버프 무시
                else
                    canAdd = true;  // 새 버프가 더 강하면 교체
                break;
            }
        }

        if (buffToRemove != null)
            TakeBuffs?.Remove(buffToRemove);

        if (canAdd)
            TakeBuffs?.Add(buff);
    }
    void RemoveBuff(IBuff buff)
    {
        if (buff == null) return;
        if (TakeBuffs?.Count == 0) return;

        TakeBuffs.Remove(buff);
    }
    IBuff SelectHigherStatValue(in IBuff a, in IBuff b, out IBuff lower)
    {
        lower = null;

        var dataA = a.Data;
        var dataB = b.Data;

        // 연산자 기준이 같다고 가정 (+,*,-,/)
        switch (dataA.Operator)
        {
            case E_BuffOperator.Add:
            case E_BuffOperator.Multiply:
                // 둘 다 클수록 강함
                if (dataA.Value >= dataB.Value)
                {
                    lower = b;
                    return a;
                }
                else
                {
                    lower = a;
                    return b;
                }

            case E_BuffOperator.Subtract:
            case E_BuffOperator.Divide:
                // 둘 다 작을수록 강함
                if (dataA.Value <= dataB.Value)
                {
                    lower = b;
                    return a;
                }
                else
                {
                    lower = a;
                    return b;
                }

            default:
                // 예외 처리
                UnityEngine.Debug.LogWarning($"예외: {dataA.Operator}");
                lower = b;
                return a;
        }
    }
}
/// <summary>
/// 버프를 줄 수 있는 객체에게 붙이는 인터페이스
/// </summary>
public interface IBuffSender
{
    int ObjectID { get; } // 오브젝트 ID
    List<int> BuffID { get; }
    E_GridShape Shape { get; }
    int2 ShapeOffset { get; }
    void IssueRequest(RequestType requestType, in IBuffReceiver receiver, int id, in IBuff instance = null)
    {
        Request request = new Request(requestType, this, in receiver, id, null);
        Managers.Buff.SubmitRequest(in request);
    }
}
